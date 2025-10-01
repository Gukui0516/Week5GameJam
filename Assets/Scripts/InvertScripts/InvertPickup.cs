using UnityEngine;

[DisallowMultipleComponent]
public class InvertPickup : MonoBehaviour, Item
{
    // 아이템이랑 플레이어 충돌한 사실만 worldstatemanager로 전달

    [Header("Settings")]
    [SerializeField, Min(0f)] private float invertDuration = 5f; // 아이템 지속 시간
    [SerializeField] private string playerTag = "Player";

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // 인스펙터에서 할당

    public void ActiveItem()
    {
        if (!world) 
        {
            Debug.Log("WorldStateManager가 연결되지 않음");
            return;
        }
        Debug.Log("아이템 먹음");
        world.ActivateInversion(invertDuration);
        Destroy(gameObject);
    }
}
