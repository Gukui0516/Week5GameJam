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

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> itemPool;
    private int currentItemCount = 0;

    public static ItemSpawner Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ObjectPool 초기화
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
        // TODO: 플레이어 구현 후 제대로 참조
        player = GameObject.FindGameObjectWithTag("Player").transform;

        StartCoroutine(SpawnCoroutine());
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateItem()
    {
        GameObject item = Instantiate(itemPrefab);
        item.SetActive(false);
        return item;
    }

    void OnGetItem(GameObject item)
    {
        item.SetActive(true);
        currentItemCount++;
    }

    void OnReleaseItem(GameObject item)
    {
        item.SetActive(false);
        currentItemCount--;
    }

    void OnDestroyItem(GameObject item)
    {
        Destroy(item);
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
        // 카메라 중심 위치
        Vector2 center = mainCamera.transform.position;

        // 원 위의 랜덤 각도 (0 ~ 360도)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // 원형으로 spawnDistance 떨어진 위치 계산
        float x = center.x + Mathf.Cos(randomAngle) * spawnDistance;
        float y = center.y + Mathf.Sin(randomAngle) * spawnDistance;

        return new Vector2(x, y);
    }

    bool IsOutsideCameraView(Vector2 position)
    {
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(position);

        // 뷰포트 좌표: (0,0) = 왼쪽 하단, (1,1) = 오른쪽 상단
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

    #region Public Methods

    #endregion
}
