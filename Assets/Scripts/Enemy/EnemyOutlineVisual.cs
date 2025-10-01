using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EnemyOutlineVisual : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private EnemyOutline parent;

    // 캐싱
    private float lastRadius;
    private float lastThickness;
    private int lastSegments;
    private Color lastColor;
    private string lastSortingLayer;
    private int lastSortingOrder;

    public void Initialize(EnemyOutline parentOutline)
    {
        parent = parentOutline;
        SetupComponents();
    }

    void Awake()
    {
        SetupComponents();
    }

    void SetupComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (mesh == null)
        {
            mesh = new Mesh { name = "EnemyOutline_Mesh" };
            mesh.MarkDynamic();
        }

        if (meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }
    }

    public void UpdateGeometry(float radius, float thickness, int segments = 32)
    {
        // 변경사항 체크
        if (Mathf.Approximately(lastRadius, radius) &&
            Mathf.Approximately(lastThickness, thickness) &&
            lastSegments == segments)
            return;

        lastRadius = radius;
        lastThickness = thickness;
        lastSegments = segments;

        RebuildCircleOutline(radius, thickness, segments);
    }

    void RebuildCircleOutline(float radius, float thickness, int segments)
    {
        if (mesh == null) return;

        segments = Mathf.Max(8, segments); // 최소 8각형

        // 버텍스: 안쪽 원 + 바깥쪽 원
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // 안쪽 원 (반지름 radius)
            vertices[i] = new Vector3(cos * radius, sin * radius, 0);

            // 바깥쪽 원 (반지름 radius + thickness)
            vertices[i + segments] = new Vector3(
                cos * (radius + thickness),
                sin * (radius + thickness),
                0
            );
        }

        // 삼각형 생성 (링 형태)
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int nextI = (i + 1) % segments;

            // 사각형을 2개의 삼각형으로
            // 첫 번째 삼각형
            triangles[triIndex++] = i;
            triangles[triIndex++] = i + segments;
            triangles[triIndex++] = nextI;

            // 두 번째 삼각형
            triangles[triIndex++] = nextI;
            triangles[triIndex++] = i + segments;
            triangles[triIndex++] = nextI + segments;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    public void UpdateMaterial(Color color, string sortingLayerName, int sortingOrder)
    {
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
            Debug.LogError("EnemyOutline: 사용 가능한 쉐이더를 찾을 수 없습니다!");
            return;
        }

        // 런타임에서는 material, 그 외에는 sharedMaterial 사용
        bool useSharedMaterial = !Application.isPlaying;
        Material mat = useSharedMaterial ? meshRenderer.sharedMaterial : meshRenderer.material;

        bool needsNewMaterial = mat == null || mat.shader != shader;

        if (needsNewMaterial)
        {
            mat = new Material(shader) { name = "EnemyOutline_Mat" };
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

        if (useSharedMaterial)
        {
            meshRenderer.sharedMaterial = mat;
        }
        else
        {
            meshRenderer.material = mat;
        }

        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    public void UpdateToggle(bool isOn)
    {
        if (meshRenderer != null)
            meshRenderer.enabled = isOn;
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