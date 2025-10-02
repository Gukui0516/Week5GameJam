using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour, Item
{
    [Header("Key")]
    public KeyKind kind = KeyKind.Circle;
    [Min(1)] public int amount = 1;

    [Header("Destroy")]
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Distance Check")]
    [SerializeField] private bool enableDistanceCheck = true;
    [SerializeField] private float maxDistanceFromPlayer = 30f;
    [SerializeField] private float checkInterval = 1f;

    private Transform playerTransform;
    private float distanceCheckTimer;

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (!enableDistanceCheck || playerTransform == null) return;

        distanceCheckTimer += Time.deltaTime;
        if (distanceCheckTimer >= checkInterval)
        {
            distanceCheckTimer = 0f;
            CheckDistanceFromPlayer();
        }
    }

    /// <summary>
    /// Player 태그를 가진 오브젝트를 찾아 참조 저장
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[KeyPickup] Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 플레이어와의 거리를 체크하고, 너무 멀면 삭제
    /// </summary>
    private void CheckDistanceFromPlayer()
    {
        if (playerTransform == null)
        {
            // 플레이어가 파괴되었을 수도 있으니 다시 찾기 시도
            FindPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > maxDistanceFromPlayer)
        {
            Debug.Log($"[KeyPickup] {kind} 키가 플레이어로부터 {distance:F1} 유닛 떨어져 삭제됨 (최대: {maxDistanceFromPlayer})");
            Destroy(gameObject);
        }
    }

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
