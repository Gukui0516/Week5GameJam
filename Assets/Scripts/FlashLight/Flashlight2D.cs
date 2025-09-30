using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteAlways]
public class Flashlight2D : MonoBehaviour
{
    public enum AimMode { TransformRight, MouseToWorld, CustomVector }
    [Header("Aim")]
    public AimMode aimMode = AimMode.TransformRight;
    [Tooltip("aimMode가 CustomVector일 때 사용")]
    public Vector2 customDirection = Vector2.right;
    [Tooltip("마우스 월드좌표 변환에 사용할 카메라. 비우면 Camera.main")]
    public Camera worldCamera;

    [Header("Shape")]
    [Min(0.1f)] public float range = 5f;
    [Range(1f, 180f)] public float coneAngle = 70f;
    [Range(6, 128)] public int arcSegments = 36;

    [Header("Visual")]
    [Tooltip("부채꼴 색상(알파로 투명도 조절)")]
    public Color color = new Color(1f, 1f, 0.7f, 0.35f);
    [Tooltip("정렬 레이어 이름")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 10;

    [Header("Detection")]
    [Tooltip("감지할 레이어들")]
    public LayerMask detectionMask = ~0;
    [Tooltip("감지할 태그가 비어있지 않다면 해당 태그만 허용")]
    public string requiredTag = "";

    [Header("Toggle")]
    public bool isOn = true;

    [Header("Events")]
    public UnityEvent<Collider2D> OnTargetEnter;
    public UnityEvent<Collider2D> OnTargetExit;

    // Runtime objects
    GameObject wedgeObj;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    PolygonCollider2D polyCollider;
    Rigidbody2D rb2d;

    Mesh mesh;
    readonly HashSet<Collider2D> inside = new HashSet<Collider2D>();

    // 캐싱
    float _lastRange, _lastAngle;
    int _lastSegs;
    Color _lastColor;
    bool _isInitialized = false;

    void Reset()
    {
        worldCamera = Camera.main;
        DestroyOldChild();
        BuildRuntimeObjects();
        RebuildGeometry();
        ApplyVisual();
    }

    void Awake()
    {
        if (!Application.isPlaying && !Application.isEditor) return;
        BuildRuntimeObjects();
        RebuildGeometry();
        ApplyVisual();
        ApplyToggle();
        _isInitialized = true;
    }

    void OnEnable()
    {
        if (_isInitialized)
        {
            ApplyToggle();
        }
    }

    void OnValidate()
    {
        // OnValidate에서는 가벼운 작업만
        if (Application.isPlaying)
        {
            BuildRuntimeObjects();
            RebuildGeometry();
            ApplyVisual();
            ApplyToggle();
        }
        else
        {
            // 에디터 모드에서는 다음 프레임에 실행
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    BuildRuntimeObjects();
                    RebuildGeometry();
                    ApplyVisual();
                    ApplyToggle();
                }
            };
        }
    }

    void Update()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (wedgeObj == null) return;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            wedgeObj.transform.rotation = Quaternion.Euler(0, 0, angleZ);
        }

        if (!Mathf.Approximately(_lastRange, range) ||
            !Mathf.Approximately(_lastAngle, coneAngle) ||
            _lastSegs != arcSegments)
        {
            RebuildGeometry();
        }

        if (_lastColor != color)
        {
            ApplyVisual();
        }

        wedgeObj.transform.position = transform.position;
    }

    Vector2 GetAimDirection()
    {
        switch (aimMode)
        {
            case AimMode.TransformRight:
                return transform.right;
            case AimMode.MouseToWorld:
                if (worldCamera == null) return Vector2.right;
                Vector3 m = worldCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector2 v = (Vector2)(m - transform.position);
                return v.sqrMagnitude < 0.0001f ? Vector2.right : v.normalized;
            case AimMode.CustomVector:
                return customDirection.sqrMagnitude < 0.0001f ? Vector2.right : customDirection.normalized;
        }
        return Vector2.right;
    }

    void DestroyOldChild()
    {
        // 기존 자식 오브젝트 정리
        Transform old = transform.Find("_Flashlight2D");
        if (old != null)
        {
            if (Application.isPlaying)
                Destroy(old.gameObject);
            else
                DestroyImmediate(old.gameObject);
        }
    }

    void BuildRuntimeObjects()
    {
        if (wedgeObj == null)
        {
            DestroyOldChild();
            
            wedgeObj = new GameObject("_Flashlight2D");
            wedgeObj.transform.SetParent(transform, false);
            wedgeObj.hideFlags = HideFlags.HideInHierarchy;

            meshFilter = wedgeObj.AddComponent<MeshFilter>();
            meshRenderer = wedgeObj.AddComponent<MeshRenderer>();
            polyCollider = wedgeObj.AddComponent<PolygonCollider2D>();
            rb2d = wedgeObj.AddComponent<Rigidbody2D>();

            polyCollider.isTrigger = true;
            rb2d.bodyType = RigidbodyType2D.Kinematic;
            rb2d.simulated = true;
        }
        else
        {
            // 컴포넌트 재확인
            if (meshFilter == null) meshFilter = wedgeObj.GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = wedgeObj.GetComponent<MeshRenderer>();
            if (polyCollider == null) polyCollider = wedgeObj.GetComponent<PolygonCollider2D>();
            if (rb2d == null) rb2d = wedgeObj.GetComponent<Rigidbody2D>();
        }

        if (mesh == null)
        {
            mesh = new Mesh { name = "Flashlight2D_Mesh" };
            mesh.MarkDynamic();
        }

        // 🔧 OnValidate 오류 방지: 이미 같은 메시면 할당 생략
        if (meshFilter != null && meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }
    }

    void RebuildGeometry()
    {
        if (mesh == null) return;

        _lastRange = range;
        _lastAngle = coneAngle;
        _lastSegs = arcSegments;

        int steps = Mathf.Max(2, arcSegments);
        float halfRad = Mathf.Deg2Rad * (coneAngle * 0.5f);
        float start = -halfRad;
        float delta = (2f * halfRad) / steps;

        var verts = new Vector3[steps + 2];
        var uv = new Vector2[verts.Length];
        var tris = new int[steps * 3];

        verts[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i <= steps; i++)
        {
            float a = start + delta * i;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * range, Mathf.Sin(a) * range, 0f);
            uv[i + 1] = new Vector2(0.5f + verts[i + 1].x / (2f * range), 0.5f + verts[i + 1].y / (2f * range));
        }

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

    void ApplyVisual()
    {
        _lastColor = color;
        if (meshRenderer == null) return;

        // 🔧 셰이더 우선순위: Sprites/Default → URP/Unlit → Unlit/Transparent
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError("Flashlight2D: 사용 가능한 셰이더를 찾을 수 없습니다!");
            return;
        }

        Material mat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;

        // 머티리얼이 없거나 셰이더가 다르면 새로 생성
        bool needsNewMaterial = mat == null || mat.shader != shader;
        
        if (needsNewMaterial)
        {
            mat = new Material(shader) { name = "Flashlight2D_Mat" };
        }

        // 셰이더별 설정
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

        if (!meshRenderer.enabled && isOn)
        {
            meshRenderer.enabled = true;
        }
    }

    void ApplyToggle()
    {
        if (wedgeObj == null) return;
        bool active = isOn;
        if (meshRenderer != null) meshRenderer.enabled = active;
        if (polyCollider != null) polyCollider.enabled = active;
        if (!active)
        {
            if (inside.Count > 0)
            {
                var copy = new List<Collider2D>(inside);
                inside.Clear();
                foreach (var c in copy)
                    OnTargetExit?.Invoke(c);
            }
        }
    }

    public void SetOn(bool on)
    {
        isOn = on;
        ApplyToggle();
    }

    public void Toggle() => SetOn(!isOn);

    // 🔧 자식 오브젝트의 Collider에서 호출됨
    void OnTriggerEnter2D(Collider2D other)
    {
        // 이 메서드는 자식의 PolygonCollider2D가 아닌
        // 부모에 직접 Collider가 있을 때만 작동하므로
        // 실제로는 작동하지 않습니다.
        // 아래 수동 체크 방식을 사용해야 합니다.
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // 위와 동일
    }

    // 🔧 수동 감지 시스템 (FixedUpdate 사용)
    void FixedUpdate()
    {
        if (!Application.isPlaying || !isOn || polyCollider == null) return;

        // OverlapCollider로 현재 접촉 중인 모든 Collider2D 가져오기
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(detectionMask);

        List<Collider2D> results = new List<Collider2D>();
        int count = Physics2D.OverlapCollider(polyCollider, filter, results);

        // 새로 들어온 것 감지
        HashSet<Collider2D> currentSet = new HashSet<Collider2D>(results);
        
        foreach (var col in currentSet)
        {
            if (!inside.Contains(col))
            {
                // 태그 체크
                if (!string.IsNullOrEmpty(requiredTag) && !col.CompareTag(requiredTag))
                    continue;

                inside.Add(col);
                Debug.Log($"Flashlight2D 감지: {col.name}");
                OnTargetEnter?.Invoke(col);
            }
        }

        // 나간 것 감지
        List<Collider2D> toRemove = new List<Collider2D>();
        foreach (var col in inside)
        {
            if (!currentSet.Contains(col))
            {
                toRemove.Add(col);
            }
        }

        foreach (var col in toRemove)
        {
            inside.Remove(col);
            Debug.Log($"Flashlight2D 벗어남: {col.name}");
            OnTargetExit?.Invoke(col);
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

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
        Vector3 pos = transform.position;
        float half = coneAngle * 0.5f;

        Vector2 dir = GetAimDirection();
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector3 from = Quaternion.Euler(0, 0, baseAngle - half) * Vector3.right;
        UnityEditor.Handles.color = new Color(1f, 1f, 0.2f, 0.2f);
        UnityEditor.Handles.DrawSolidArc(pos, Vector3.forward, from, coneAngle, range);
        UnityEditor.Handles.color = new Color(1f, 1f, 0.2f, 0.8f);
        UnityEditor.Handles.DrawWireArc(pos, Vector3.forward, from, coneAngle, range);
    }
#endif
}