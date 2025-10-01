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
    [SerializeField] private float optionalMaxLifetime = -1f;

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // 씬의 매니저 연결(권장)

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> itemPool;
    private int currentItemCount = 0;

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

            world = FindFirstObjectByType<WorldStateManager>();

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

        StartCoroutine(SpawnCoroutine());
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
        currentItemCount++;

        // 1) 거리/수명 체크 세팅
        var pooled = item.GetComponent<PooledItem>();
        pooled.Setup(
            player,
            despawnDistance,
            ReleaseItem,            // 아이템이 직접 풀로 돌아가도록 콜백 전달
            optionalMaxLifetime     // -1이면 끔
        );

        // 2) 월드 매니저 주입(핵심)
        // 프리팹 루트에 InvertPickup이 붙어있을 수도, 자식에 있을 수도 있으니 둘 다 시도
        if (item.TryGetComponent<InvertPickup>(out var pickup)
            || item.GetComponentInChildren<InvertPickup>(true) is InvertPickup childPickup && (pickup = childPickup) != null)
        {
            // InvertPickup에 이미 만든 Init(WorldStateManager) 사용
            pickup.Init(world);
        }
        else
        {
            // 다른 타입의 아이템이 올 수도 있으니 로그는 정보용으로만
            // Debug.Log($"[ItemSpawner] InvertPickup 없음: {item.name}");
        }
    }

    void OnReleaseItem(GameObject item)
    {
        item.SetActive(false);
        currentItemCount = Mathf.Max(0, currentItemCount - 1);
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

            if (currentItemCount < maxItems)
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
