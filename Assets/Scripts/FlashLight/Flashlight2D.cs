using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

// 색 팔레트용 엔트리
[System.Serializable]
public struct NamedColor
{
    public string key;
    public Color color;
}



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

    // 자식 오브젝트 참조
    private Flashlight2DVisual visualComponent;
    [Header("Palette")]
    [SerializeField] private List<NamedColor> colorPalette = new List<NamedColor>();
    // 런타임 조회용
    private Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
    private WorldStateManager worldStateManager;

    // 내부 반전 상태 캐시
    [SerializeField] private bool isInverted;
    public bool IsInverted => isInverted;

    void Reset()
    {
        worldCamera = Camera.main;
        SetupChildObject();
        RebuildColorMap();
        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }
    }

    void Awake()
    {
        if (!Application.isPlaying && !Application.isEditor) return;
        SetupChildObject();
        RebuildColorMap();
        worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager == null)
        {
            Debug.LogWarning("Flashlight2D: WorldStateManager not found in scene.");
        }
    }
    // 활성화 시 구독, 비활성화 시 해제
    void OnEnable()
    {
        isOn = true;
        SubscribeWorldEvents();
        SyncInvertedImmediate();
    }

    void OnDisable()
    {
        UnsubscribeWorldEvents();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetupChildObject();
            RebuildColorMap();
            UpdateVisual();
        }
        else
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SetupChildObject();
                    RebuildColorMap();
                    UpdateVisual();
                }
            };
        }
    }


    void Update()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        UpdateVisual();
    }

    void FixedUpdate()
    {
        if (Application.isPlaying && visualComponent != null)
        {
            visualComponent.CheckDetection();
        }
    }

    void SetupChildObject()
    {
        // 기존 자식 찾기
        Transform child = transform.Find("_Flashlight2D");
        GameObject childObj;

        if (child == null)
        {
            // 새로 생성
            childObj = new GameObject("_Flashlight2D");
            childObj.transform.SetParent(transform, false);
            childObj.transform.localPosition = Vector3.zero;
            childObj.transform.localRotation = Quaternion.identity;
            childObj.transform.localScale = Vector3.one;
        }
        else
        {
            childObj = child.gameObject;
        }

        // Visual 컴포넌트 추가/가져오기
        visualComponent = childObj.GetComponent<Flashlight2DVisual>();
        if (visualComponent == null)
        {
            visualComponent = childObj.AddComponent<Flashlight2DVisual>();
        }

        // 초기 설정
        visualComponent.Initialize(this);
    }

    void UpdateVisual()
    {
        if (visualComponent == null) return;

        Vector2 dir = GetAimDirection();
        visualComponent.UpdateTransform(transform.position, dir);
        visualComponent.UpdateGeometry(range, coneAngle, arcSegments);
        visualComponent.UpdateMaterial(color, sortingLayerName, sortingOrder);
        visualComponent.UpdateToggle(isOn);
    }

    public Vector2 GetAimDirection()
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

    public void SetOn(bool on)
    {
        isOn = on;
        if (visualComponent != null)
        {
            visualComponent.UpdateToggle(isOn);
        }
    }

    public void Toggle() => SetOn(!isOn);

    // Visual 컴포넌트에서 호출될 이벤트 메서드
    public void NotifyTargetEnter(Collider2D col)
    {
        OnTargetEnter?.Invoke(col);
    }

    public void NotifyTargetExit(Collider2D col)
    {
        OnTargetExit?.Invoke(col);
    }

#if UNITY_EDITOR
    /*
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
    */
#endif
    // (변경) 팔레트 딕셔너리 재빌드 - 키 대소문자 무시
    private void RebuildColorMap()
    {
        if (colorMap == null) colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        else if (!(colorMap.Comparer is StringComparer)) colorMap = new Dictionary<string, Color>(colorMap, StringComparer.OrdinalIgnoreCase);
        colorMap.Clear();
        if (colorPalette == null) return;

        for (int i = 0; i < colorPalette.Count; i++)
        {
            var e = colorPalette[i];
            if (string.IsNullOrWhiteSpace(e.key)) continue;
            colorMap[e.key] = e.color;
        }
    }




    #region Public Methods (updated)

    // Range 세터
    public void SetRange(float newRange)
    {
        range = Mathf.Max(0.1f, newRange);
        if (visualComponent != null)
            visualComponent.UpdateGeometry(range, coneAngle, arcSegments);
    }

    // 부채꼴 각도 세터
    public void SetConeAngle(float newAngle)
    {
        coneAngle = Mathf.Clamp(newAngle, 1f, 180f);
        if (visualComponent != null)
            visualComponent.UpdateGeometry(range, coneAngle, arcSegments);
    }

    // 색 직접 세터
    public void SetLightColor(Color newColor)
    {
        color = newColor;
        if (visualComponent != null)
            visualComponent.UpdateMaterial(color, sortingLayerName, sortingOrder);
    }

    // 문자열 키로 색 변경 (인스펙터 팔레트 사용)
    public bool TrySetLightColorByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || colorMap == null) return false;
        if (!colorMap.TryGetValue(key, out var picked)) return false;
        SetLightColor(picked);
        return true;
    }

    public void ChangeFlashlightInverted()
    {
        if (TrySetLightColorByKey("Inverted"))
        {
            if (visualComponent != null)
                visualComponent.UpdateMaterial(color, sortingLayerName, sortingOrder);
        }
    }
    public void ChangeFlashlightNormal()
    {
        if (TrySetLightColorByKey("Normal"))
        {
            if (visualComponent != null)
                visualComponent.UpdateMaterial(color, sortingLayerName, sortingOrder);
        }
    }

    public void FlashlightOff()
    {
        SetOn(false);
    }
    public void FlashlightOn()
    {
        SetOn(true);
    }




#endregion


#region WorldStateManager Event Handling
    private void SubscribeWorldEvents()
    {
        if (worldStateManager == null)
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager != null)
            worldStateManager.onIsInvertedChanged.AddListener(HandleInvertedChanged);
    }

    private void UnsubscribeWorldEvents()
    {
        if (worldStateManager != null)
            worldStateManager.onIsInvertedChanged.RemoveListener(HandleInvertedChanged);
    }

    // [추가] 첫 프레임 동기화
    private void SyncInvertedImmediate()
    {
        if (worldStateManager != null)
            HandleInvertedChanged(worldStateManager.IsInverted);
    }

    // [추가] 콜백: 내부 bool 갱신 + 팔레트 색 즉시 반영
    private void HandleInvertedChanged(bool inverted)
    {
        isInverted = inverted;
        if (inverted) ChangeFlashlightInverted();
        else          ChangeFlashlightNormal();
    }

#endregion

}