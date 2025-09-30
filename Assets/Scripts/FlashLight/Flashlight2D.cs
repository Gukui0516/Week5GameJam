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

    // 자식 오브젝트 참조
    private Flashlight2DVisual visualComponent;
    
    void Reset()
    {
        worldCamera = Camera.main;
        SetupChildObject();
    }

    void Awake()
    {
        if (!Application.isPlaying && !Application.isEditor) return;
        SetupChildObject();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetupChildObject();
            UpdateVisual();
        }
        else
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SetupChildObject();
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