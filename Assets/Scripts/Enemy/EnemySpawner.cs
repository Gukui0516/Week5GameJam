using UnityEngine;
using UnityEngine.Pool;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnDistance = 15f;

    [Header("Pool Settings")]
    [SerializeField] private int defaultPoolCapacity = 10;
    [SerializeField] private int maxPoolSize = 20;

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> enemyPool;
    private int currentEnemyCount = 0;

    public static EnemySpawner Instance { get; private set; }

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
        enemyPool = new ObjectPool<GameObject>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
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

    GameObject CreateEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab);
        enemy.SetActive(false);
        return enemy;
    }

    void OnGetEnemy(GameObject enemy)
    {
        enemy.SetActive(true);
        currentEnemyCount++;
    }

    void OnReleaseEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        currentEnemyCount--;
    }

    void OnDestroyEnemy(GameObject enemy)
    {
        Destroy(enemy);
    }

    #endregion

    #region Spawn Logic

    IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentEnemyCount < maxEnemies)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                if (IsOutsideCameraView(spawnPosition))
                {
                    SpawnEnemy(spawnPosition);
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

    void SpawnEnemy(Vector2 position)
    {
        GameObject enemy = enemyPool.Get();
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;
    }

    #endregion

    #region Public Methods

    //Enemy에서 Die() 함수로 쓰세요
    public void ReturnEnemy(GameObject enemy)
    {
        enemyPool.Release(enemy);
    }

    #endregion
}