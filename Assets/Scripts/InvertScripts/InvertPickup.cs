using System;
using UnityEngine;

[DisallowMultipleComponent]
public class InvertPickup : MonoBehaviour, Item
{
    [Header("Settings")]
    [SerializeField, Min(0f)] private float invertDuration = 5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // (옵션) 에디터에서 연결해두면 우선 사용

    private bool consumed = false;

    /// <summary>스포너/팩토리에서 주입</summary>
    public void Init(WorldStateManager manager)
    {
        world = manager;
    }
    private void OnEnable()
    {
        consumed = false; // 풀에서 다시 꺼낼 때 리셋
    }
    /// <summary>
    /// 스포너가 연결해줄 수 있는 콜백.
    /// 예) pickup.onConsumed = () => ReleaseItem(item);
    /// </summary>
    public Action onConsumed;

    private void Awake()
    {
        // 에디터에서 안 넣었고, 주입도 안 됐을 경우 마지막 폴백
        if (!world)
        {
#if UNITY_2023_1_OR_NEWER
            world = FindFirstObjectByType<WorldStateManager>();
#else
            world = FindObjectOfType<WorldStateManager>();
#endif
        }
    }

    public void ActiveItem()
    {
        if (consumed) return; // 이미 소비된 아이템
        consumed = true;

        if (!world)
        {
            Debug.LogWarning("[InvertPickup] WorldStateManager가 연결되지 않음");
            return;
        }

        Debug.Log("[InvertPickup] 아이템 먹음");
        world.ActivateInversion(invertDuration);

        // PooledItem이 있으면 그것만 사용 (onConsumed는 내부적으로 호출됨)
        var pooled = GetComponent<PooledItem>();
        if (pooled != null)
        {
            pooled.ReturnToPoolNow();
            return;
        }

        // PooledItem이 없을 때만 onConsumed 직접 호출
        onConsumed?.Invoke();
        
        // 마지막 폴백: 그냥 비활성화
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}
