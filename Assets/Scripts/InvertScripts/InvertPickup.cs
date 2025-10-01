using UnityEngine;

[DisallowMultipleComponent]
public class InvertPickup : MonoBehaviour, Item
{
    [Header("Settings")]
    [SerializeField, Min(0f)] private float invertDuration = 5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // (옵션) 에디터에서 연결해두면 우선 사용

    /// <summary>스포너/팩토리에서 주입</summary>
    public void Init(WorldStateManager manager)
    {
        world = manager;
    }

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
        if (!world)
        {
            Debug.LogWarning("WorldStateManager가 연결되지 않음");
            return;
        }

        Debug.Log("아이템 먹음");
        world.ActivateInversion(invertDuration);
        Destroy(gameObject);
    }
}
