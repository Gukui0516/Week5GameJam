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
    Transform wedgeRoot;
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

    void Reset()
    {
        worldCamera = Camera.main;
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
    }

    void OnEnable()
    {
        ApplyToggle();
    }

    void OnValidate()
    {
        BuildRuntimeObjects();
        RebuildGeometry();
        ApplyVisual();
        ApplyToggle();
    }

    void Update()
    {
        if (worldCamera == null) worldCamera = Camera.main;

        Vector2 dir = GetAimDirection();
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            wedgeRoot.rotation = Quaternion.Euler(0, 0, angleZ);
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

        wedgeRoot.position = transform.position;
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

    void BuildRuntimeObjects()
    {
        if (wedgeRoot == null)
        {
            var go = transform.Find("_Flashlight2D")?.gameObject;
            if (go == null)
            {
                go = new GameObject("_Flashlight2D");
                go.transform.SetParent(transform, false);
            }
            wedgeRoot = go.transform;
            wedgeRoot.hideFlags = HideFlags.HideInHierarchy;
        }

        meshFilter = wedgeRoot.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = wedgeRoot.gameObject.AddComponent<MeshFilter>();

        meshRenderer = wedgeRoot.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = wedgeRoot.gameObject.AddComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "Flashlight2D_Mesh" };
            mesh.MarkDynamic();
        }
        
        if (meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }

        polyCollider = wedgeRoot.GetComponent<PolygonCollider2D>();
        if (polyCollider == null) polyCollider = wedgeRoot.gameObject.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;

        rb2d = wedgeRoot.GetComponent<Rigidbody2D>();
        if (rb2d == null) rb2d = wedgeRoot.gameObject.AddComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true; // 중요: 시뮬레이션 활성화

        // 🔧 수정: 자식 오브젝트에 트리거 핸들러 추가
        var handler = wedgeRoot.GetComponent<FlashlightTriggerHandler>();
        if (handler == null) handler = wedgeRoot.gameObject.AddComponent<FlashlightTriggerHandler>();
        handler.parentFlashlight = this;
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

        var path = new Vector2[steps + 2];
        path[0] = Vector2.zero;
        for (int i = 0; i <= steps; i++)
        {
            path[i + 1] = verts[i + 1];
        }
        polyCollider.SetPath(0, path);
    }

    void ApplyVisual()
    {
        _lastColor = color;
        if (meshRenderer == null) return;
        
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            // URP가 아닌 경우 기본 셰이더 사용
            unlitShader = Shader.Find("Unlit/Transparent");
            if (unlitShader == null)
            {
                Debug.LogWarning("적절한 셰이더를 찾을 수 없습니다. Sprites/Default를 시도합니다.");
                unlitShader = Shader.Find("Sprites/Default");
            }
        }

        if (unlitShader == null)
        {
            Debug.LogError("사용 가능한 셰이더를 찾을 수 없습니다.");
            return;
        }

        Material mat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;

        if (mat == null || mat.shader != unlitShader)
        {
            mat = new Material(unlitShader) { name = "Flashlight2D_Mat" };
        }

        // URP 셰이더인 경우
        if (unlitShader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
        }
        else
        {
            // 기본 셰이더인 경우
            mat.color = color;
        }

        if (Application.isPlaying)
        {
            meshRenderer.material = mat;
        }
        else
        {
            meshRenderer.sharedMaterial = mat;
        }

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    void ApplyToggle()
    {
        if (wedgeRoot == null) return;
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

    // 🔧 수정: 자식에서 호출되는 메서드들
    internal void HandleTriggerEnter(Collider2D other)
    {
        if (!isOn) return;
        if (((1 << other.gameObject.layer) & detectionMask) == 0) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        if (inside.Add(other))
        {
            Debug.Log($"Flashlight2D 감지: {other.name}");
            OnTargetEnter?.Invoke(other);
        }
    }

    internal void HandleTriggerExit(Collider2D other)
    {
        if (inside.Remove(other))
        {
            Debug.Log($"Flashlight2D 벗어남: {other.name}");
            OnTargetExit?.Invoke(other);
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

// 🔧 새로 추가: 트리거 이벤트를 부모로 전달하는 헬퍼 컴포넌트
[ExecuteAlways]
public class FlashlightTriggerHandler : MonoBehaviour
{
    [HideInInspector] public Flashlight2D parentFlashlight;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentFlashlight != null)
            parentFlashlight.HandleTriggerEnter(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (parentFlashlight != null)
            parentFlashlight.HandleTriggerExit(other);
    }
}