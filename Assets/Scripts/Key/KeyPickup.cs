using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour, Item
{
    [Header("Key")]
    public KeyKind kind = KeyKind.Red;
    [Min(1)] public int amount = 1;

    [Header("Destroy")]
    [SerializeField] private bool destroyOnPickup = true;

    public void ActiveItem()
    {
        // 플레이어에 KeyCollector가 붙어 있다고 가정
        var collector = FindFirstObjectByType<KeyCollector>();
        if (collector == null)
        {
            Debug.LogWarning("KeyPickup: KeyCollector가 씬에 없음. 주워도 들어갈 인벤토리가 없다. 인생 같다.");
            return;
        }

        collector.Add(kind, amount);
        if (destroyOnPickup) Destroy(gameObject);
    }
}
