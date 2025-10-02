using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class ItemSpawner : MonoBehaviour
{
    // 아이템 스포너

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxItems = 10;
    [SerializeField] private float spawnDistance = 15f;

    [Header("Pool Settings")]
    [SerializeField] private int defaultPoolCapacity = 10;
    [SerializeField] private int maxPoolSize = 20;

    [Header("Despawn Settings")]
    [SerializeField] private float despawnDistance = 25f;
    [SerializeField] private float optionalMaxLifetime = -1f; // 거리 기반 반환 조건 안 걸릴 때 안전장치로

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // 씬의 매니저 연결(권장)

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> itemPool;

    public static ItemSpawner Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // world 참조가 비어 있으면 런타임에서 한 번 찾아서 캐시(폴백)
        if (!world)
        {
#if UNITY_2023_1_OR_NEWER
            world = FindFirstObjectByType<WorldStateManager>();
#else
            world = FindObjectOfType<WorldStateManager>();
#endif
            if (!world)
                Debug.LogWarning("[ItemSpawner] WorldStateManager를 찾지 못했습니다. 인스펙터에 연결하거나 씬에 1개 배치하세요.");
        }

        itemPool = new ObjectPool<GameObject>(
            createFunc: CreateItem,
            actionOnGet: OnGetItem,
            actionOnRelease: OnReleaseItem,
            actionOnDestroy: OnDestroyItem,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSize
        );
    }

    void Start()
    {
        mainCamera = Camera.main;

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO) player = playerGO.transform;
        else Debug.LogWarning("[ItemSpawner] Player 태그 오브젝트를 찾지 못했습니다.");

        // 게임 시작 시 첫 아이템 즉시 생성
        SpawnInitialItem();

        StartCoroutine(SpawnCoroutine());
    }

    /// <summary>
    /// 게임 시작 시 첫 아이템을 생성
    /// </summary>
    void SpawnInitialItem()
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        SpawnItem(spawnPosition);
        Debug.Log($"[ItemSpawner] Initial item spawned at {spawnPosition}");
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateItem()
    {
        GameObject item = Instantiate(itemPrefab);
        item.SetActive(false);

        // 아이템에 PooledItem 컴포넌트 보장
        if (!item.TryGetComponent<PooledItem>(out _))
            item.AddComponent<PooledItem>();

        return item;
    }

    void OnGetItem(GameObject item)
    {
        item.SetActive(true);

        // 1) 거리/수명 체크 세팅
        var pooled = item.GetComponent<PooledItem>();
        if (pooled != null)
        {
            pooled.Setup(
                player,
                despawnDistance,
                ReleaseItem,            // 아이템이 직접 풀로 돌아가도록 콜백 전달
                optionalMaxLifetime     // -1이면 끔
            );
        }
        else
        {
            Debug.LogWarning($"[ItemSpawner] PooledItem 누락: {item.name}");
        }

        // 2) 월드 매니저 주입 + 먹힘 시 풀 반환 연결
        if (item.TryGetComponent<InvertPickup>(out var pickup)
            || item.GetComponentInChildren<InvertPickup>(true) is InvertPickup childPickup && (pickup = childPickup) != null)
        {
            pickup.Init(world);
            // 먹혔을 때 Release로 되돌아오게
            pickup.onConsumed = () => ReleaseItem(item);
        }
        // 다른 타입의 아이템 가능성은 정보용 로그 생략
    }

    void OnReleaseItem(GameObject item)
    {
        // 풀로 반납 시 비활성화만 (파괴 금지)
        item.SetActive(false);
    }

    void OnDestroyItem(GameObject item)
    {
        Destroy(item);
    }

    private void ReleaseItem(GameObject go)
    {
        if (go != null)
            itemPool.Release(go);
    }

    #endregion

    #region Spawn Logic

    IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // CountActive 기준으로 생성 제한 판단
            if (itemPool.CountActive < maxItems)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                if (IsOutsideCameraView(spawnPosition))
                {
                    SpawnItem(spawnPosition);
                }
            }
        }
    }

    Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = mainCamera.transform.position;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = center.x + Mathf.Cos(randomAngle) * spawnDistance;
        float y = center.y + Mathf.Sin(randomAngle) * spawnDistance;
        return new Vector2(x, y);
    }

    bool IsOutsideCameraView(Vector2 position)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);
        return viewportPoint.x < 0 || viewportPoint.x > 1 ||
               viewportPoint.y < 0 || viewportPoint.y > 1;
    }

    void SpawnItem(Vector2 position)
    {
        GameObject item = itemPool.Get();
        item.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

    #endregion
}
