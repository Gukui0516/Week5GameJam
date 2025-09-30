using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Flashlight2DVisual : MonoBehaviour
{
    // 컴포넌트 참조
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private PolygonCollider2D polyCollider;
    private Rigidbody2D rb2d;
    private Mesh mesh;

    // 부모 참조
    private Flashlight2D parent;

    // 감지 시스템
    private readonly HashSet<Collider2D> insideColliders = new HashSet<Collider2D>();

    // 캐싱
    private float lastRange, lastAngle;
    private int lastSegs;
    private Color lastColor;
    private string lastSortingLayer;
    private int lastSortingOrder;

    public void Initialize(Flashlight2D parentFlashlight)
    {
        parent = parentFlashlight;
        SetupComponents();
    }

    void Awake()
    {
        SetupComponents();
    }

    void SetupComponents()
    {
        // 컴포넌트 가져오기 또는 추가
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();

        // Rigidbody2D 설정
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;

        // PolygonCollider2D 설정
        polyCollider.isTrigger = true;

        // Mesh 생성
        if (mesh == null)
        {
            mesh = new Mesh { name = "Flashlight2D_Mesh" };
            mesh.MarkDynamic();
        }

        if (meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }
    }

    public void UpdateTransform(Vector3 position, Vector2 direction)
    {
        transform.position = position;

        if (direction.sqrMagnitude > 0.0001f)
        {
            float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angleZ);
        }
    }

    public void UpdateGeometry(float range, float coneAngle, int arcSegments)
    {
        // 변경사항이 없으면 스킵
        if (Mathf.Approximately(lastRange, range) &&
            Mathf.Approximately(lastAngle, coneAngle) &&
            lastSegs == arcSegments)
        {
            return;
        }

        lastRange = range;
        lastAngle = coneAngle;
        lastSegs = arcSegments;

        RebuildMesh(range, coneAngle, arcSegments);
    }

    void RebuildMesh(float range, float coneAngle, int arcSegments)
    {
        if (mesh == null) return;

        int steps = Mathf.Max(2, arcSegments);
        float halfRad = Mathf.Deg2Rad * (coneAngle * 0.5f);
        float start = -halfRad;
        float delta = (2f * halfRad) / steps;

        var verts = new Vector3[steps + 2];
        var uv = new Vector2[verts.Length];
        var tris = new int[steps * 3];

        // 중심점
        verts[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        // 원호 점들
        for (int i = 0; i <= steps; i++)
        {
            float a = start + delta * i;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * range, Mathf.Sin(a) * range, 0f);
            uv[i + 1] = new Vector2(0.5f + verts[i + 1].x / (2f * range), 0.5f + verts[i + 1].y / (2f * range));
        }

        // 삼각형 생성
        for (int i = 0, t = 0; i < steps; i++)
        {
            tris[t++] = 0;
            tris[t++] = i + 1;
            tris[t++] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        // PolygonCollider2D 경로 설정
        if (polyCollider != null)
        {
            var path = new Vector2[steps + 2];
            path[0] = Vector2.zero;
            for (int i = 0; i <= steps; i++)
            {
                path[i + 1] = verts[i + 1];
            }
            polyCollider.SetPath(0, path);
        }
    }

    public void UpdateMaterial(Color color, string sortingLayerName, int sortingOrder)
    {
        // 변경사항이 없으면 스킵
        if (lastColor == color && 
            lastSortingLayer == sortingLayerName && 
            lastSortingOrder == sortingOrder)
        {
            return;
        }

        lastColor = color;
        lastSortingLayer = sortingLayerName;
        lastSortingOrder = sortingOrder;

        ApplyMaterial(color, sortingLayerName, sortingOrder);
    }

    void ApplyMaterial(Color color, string sortingLayerName, int sortingOrder)
    {
        if (meshRenderer == null) return;

        // 쉐이더 찾기
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("Flashlight2D: 사용 가능한 쉐이더를 찾을 수 없습니다!");
            return;
        }

        Material mat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;

        // 머티리얼이 없거나 쉐이더가 다르면 새로 생성
        bool needsNewMaterial = mat == null || mat.shader != shader;
        
        if (needsNewMaterial)
        {
            mat = new Material(shader) { name = "Flashlight2D_Mat" };
        }

        // 쉐이더별 설정
        if (shader.name.Contains("Sprites"))
        {
            mat.color = color;
        }
        else if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.renderQueue = 3000;
        }
        else if (shader.name.Contains("Unlit"))
        {
            mat.color = color;
            mat.renderQueue = 3000;
        }

        // 머티리얼 할당
        if (Application.isPlaying)
        {
            meshRenderer.material = mat;
        }
        else
        {
            meshRenderer.sharedMaterial = mat;
        }

        // 렌더러 설정
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void UpdateToggle(bool isOn)
    {
        if (meshRenderer != null) meshRenderer.enabled = isOn;
        if (polyCollider != null) polyCollider.enabled = isOn;

        if (!isOn)
        {
            ClearAllDetections();
        }
    }

    public void CheckDetection()
    {
        if (!Application.isPlaying || parent == null || polyCollider == null) return;
        if (!polyCollider.enabled) return;

        // OverlapCollider로 현재 접촉 중인 모든 Collider2D 가져오기
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(parent.detectionMask);

        List<Collider2D> results = new List<Collider2D>();
        Physics2D.OverlapCollider(polyCollider, filter, results);

        // 새로 들어온 것 감지
        HashSet<Collider2D> currentSet = new HashSet<Collider2D>(results);
        
        foreach (var col in currentSet)
        {
            if (!insideColliders.Contains(col))
            {
                // 태그 체크
                if (!string.IsNullOrEmpty(parent.requiredTag) && !col.CompareTag(parent.requiredTag))
                    continue;

                insideColliders.Add(col);
                Debug.Log($"Flashlight2D 감지: {col.name}");
                parent.NotifyTargetEnter(col);
            }
        }

        // 나간 것 감지
        List<Collider2D> toRemove = new List<Collider2D>();
        foreach (var col in insideColliders)
        {
            if (col == null || !currentSet.Contains(col))
            {
                toRemove.Add(col);
            }
        }

        foreach (var col in toRemove)
        {
            insideColliders.Remove(col);
            if (col != null)
            {
                Debug.Log($"Flashlight2D 벗어남: {col.name}");
                parent.NotifyTargetExit(col);
            }
        }
    }

    void ClearAllDetections()
    {
        if (insideColliders.Count > 0 && parent != null)
        {
            var copy = new List<Collider2D>(insideColliders);
            insideColliders.Clear();
            foreach (var c in copy)
            {
                if (c != null)
                {
                    parent.NotifyTargetExit(c);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (mesh != null)
        {
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}