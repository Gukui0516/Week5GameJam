using UnityEngine;

/// <summary>
/// KeyUI들의 표시 모드를 관리하는 매니저 스크립트
/// </summary>
public class KeyUIManager : MonoBehaviour
{
    public enum DisplayMode
    {
        /// <summary>
        /// 모드 1: 요구키/비요구키에 따라 알파값 조절
        /// - 비요구키: 알파 0
        /// - 요구키(미획득): 알파 낮음
        /// - 요구키(획득): 알파 255
        /// </summary>
        RequirementBased,

        /// <summary>
        /// 모드 2: 모든 키를 항상 표시하고 획득 여부만 구분
        /// - 미획득: 알파 낮음
        /// - 획득: 알파 255
        /// </summary>
        OwnedBased
    }

    [Header("Display Settings")]
    [SerializeField] private DisplayMode displayMode = DisplayMode.RequirementBased;

    [Header("Auto Find")]
    [Tooltip("체크하면 자식 오브젝트에서 자동으로 KeyUI들을 찾습니다")]
    [SerializeField] private bool autoFindKeyUIs = true;

    [Header("Manual References")]
    [SerializeField] private KeyUI[] keyUIs;

    private void Awake()
    {
        if (autoFindKeyUIs)
        {
            keyUIs = GetComponentsInChildren<KeyUI>(true);
            Debug.Log($"[KeyUIManager] Found {keyUIs?.Length ?? 0} KeyUI components");
        }
    }

    private void OnEnable()
    {
        ApplyDisplayModeToAll();
    }

    /// <summary>
    /// 모든 KeyUI에 현재 디스플레이 모드 적용
    /// </summary>
    private void ApplyDisplayModeToAll()
    {
        if (keyUIs == null || keyUIs.Length == 0) return;

        foreach (var keyUI in keyUIs)
        {
            if (keyUI != null)
            {
                keyUI.SetDisplayMode(displayMode);
            }
        }

        Debug.Log($"[KeyUIManager] Applied {displayMode} mode to {keyUIs.Length} KeyUIs");
    }

    /// <summary>
    /// 디스플레이 모드를 변경하고 모든 KeyUI에 적용
    /// </summary>
    public void SetDisplayMode(DisplayMode mode)
    {
        if (displayMode == mode) return;

        displayMode = mode;
        ApplyDisplayModeToAll();
    }

    /// <summary>
    /// 현재 디스플레이 모드 반환
    /// </summary>
    public DisplayMode GetDisplayMode()
    {
        return displayMode;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && keyUIs != null && keyUIs.Length > 0)
        {
            ApplyDisplayModeToAll();
        }
    }
#endif
}
