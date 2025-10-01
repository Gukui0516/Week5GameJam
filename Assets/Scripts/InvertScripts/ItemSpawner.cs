using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class ItemSpawner : MonoBehaviour
{
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
    [SerializeField] private float despawnDistance = 25f;   // 플레이어와 이 거리 이상 벌어지면 반환
    [SerializeField] private float optionalMaxLifetime = -1f; // 5f 등으로 설정하면 X초 후 자동 반환, 사용 안 하면 -1

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
        player = GameObject.FindGameObjectWithTag("Player").transform;
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

        // 스폰될 때마다 거리/수명 체크 세팅
        var pooled = item.GetComponent<PooledItem>();
        pooled.Setup(
            player,
            despawnDistance,
            ReleaseItem,            // 아이템이 직접 풀로 돌아가도록 콜백 전달
            optionalMaxLifetime     // -1이면 끔
        );
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
        // 아이템 쪽에서 호출되는 반환 콜백
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
        item.transform.position = position;
        item.transform.rotation = Quaternion.identity;
    }

    #endregion
}
