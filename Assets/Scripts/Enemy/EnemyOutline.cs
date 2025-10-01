using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    public Color outlineColor = Color.white;
    [Range(0.01f, 0.5f)]
    public float outlineThickness = 0.1f;
    public string sortingLayerName = "Default";
    public int sortingOrder = -1;

    [Header("Circle Settings")]
    [Tooltip("자동으로 스프라이트 크기에서 반지름 계산")]
    public bool autoRadius = true;
    [Tooltip("수동 반지름 (autoRadius가 false일 때)")]
    public float manualRadius = 0.5f;
    [Range(8, 64)]
    public int circleSegments = 32; // 원의 부드러움

    [Header("Toggle")]
    public bool showOutline = true;

    private EnemyOutlineVisual visualComponent;
    private SpriteRenderer spriteRenderer;

    // 캐싱
    private Sprite lastSprite;
    private Vector2 lastSpriteSize;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetupChildObject();
    }

    void Update()
    {
        UpdateVisual();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            SetupChildObject();
            UpdateVisual();
        }
        else
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    spriteRenderer = GetComponent<SpriteRenderer>();
                    SetupChildObject();
                    UpdateVisual();
                }
            };
        }
    }

    void SetupChildObject()
    {
        Transform child = transform.Find("_EnemyOutline");
        GameObject childObj;

        if (child == null)
        {
            childObj = new GameObject("_EnemyOutline");
            childObj.transform.SetParent(transform, false);
            childObj.transform.localPosition = Vector3.zero;
            childObj.transform.localRotation = Quaternion.identity;
            childObj.transform.localScale = Vector3.one;
        }
        else
        {
            childObj = child.gameObject;
        }

        visualComponent = childObj.GetComponent<EnemyOutlineVisual>();
        if (visualComponent == null)
        {
            visualComponent = childObj.AddComponent<EnemyOutlineVisual>();
        }

        visualComponent.Initialize(this);
    }

    void UpdateVisual()
    {
        if (visualComponent == null) return;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        float radius = GetCurrentRadius();

        visualComponent.UpdateGeometry(radius, outlineThickness, circleSegments);
        visualComponent.UpdateMaterial(outlineColor, sortingLayerName, sortingOrder);
        visualComponent.UpdateToggle(showOutline);
    }

    float GetCurrentRadius()
    {
        if (!autoRadius)
        {
            return manualRadius;
        }

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return 0.5f;
        }

        // 스프라이트가 변경되었는지 체크
        Sprite currentSprite = spriteRenderer.sprite;
        if (currentSprite != lastSprite)
        {
            lastSprite = currentSprite;
            lastSpriteSize = CalculateSpriteSize(currentSprite);
        }

        // 가로/세로 중 큰 값의 절반을 반지름으로
        return Mathf.Max(lastSpriteSize.x, lastSpriteSize.y) * 0.5f;
    }

    Vector2 CalculateSpriteSize(Sprite sprite)
    {
        if (sprite == null) return Vector2.one;

        Bounds bounds = sprite.bounds;
        return new Vector2(bounds.size.x, bounds.size.y);
    }

    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        if (visualComponent != null)
            visualComponent.UpdateMaterial(outlineColor, sortingLayerName, sortingOrder);
    }

    public void SetOutlineThickness(float thickness)
    {
        outlineThickness = Mathf.Max(0.01f, thickness);
        if (visualComponent != null)
        {
            float radius = GetCurrentRadius();
            visualComponent.UpdateGeometry(radius, outlineThickness, circleSegments);
        }
    }

    public void SetOutlineVisible(bool visible)
    {
        showOutline = visible;
        if (visualComponent != null)
            visualComponent.UpdateToggle(showOutline);
    }

    public bool IsOutlineVisible()
    {
        return showOutline;
    }
}