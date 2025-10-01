using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    #region Enemy Type Definition

    [System.Serializable]
    public class EnemySpawnData
    {
        [Header("Basic Info")]
        public string enemyName;
        public GameObject enemyPrefab;

        [Header("Spawn Settings")]
        public float spawnInterval = 2f;      // 각자의 스폰 간격
        public int maxCount = 5;              // 이 타입의 최대 동시 생성 수

        [Header("Pool Settings")]
        public int poolCapacity = 5;
        public int poolMaxSize = 10;

        [HideInInspector] public ObjectPool<GameObject> pool;
        [HideInInspector] public int currentCount = 0;
        [HideInInspector] public Coroutine spawnCoroutine;
    }

    #endregion

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private List<EnemySpawnData> enemyTypes = new List<EnemySpawnData>();
    [SerializeField] private float spawnDistance = 15f;

    private Camera mainCamera;
    private Transform player;
    private int totalEnemyCount = 0;

    // 인스턴스 → 어떤 EnemySpawnData에 속하는지 추적
    private Dictionary<GameObject, EnemySpawnData> instanceToData = new Dictionary<GameObject, EnemySpawnData>();

    public static EnemySpawner Instance { get; private set; }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    void Start()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartAllSpawners();
    }

    void OnDestroy()
    {
        StopAllSpawners();
    }

    #endregion

    #region Pool Initialization

    void InitializePools()
    {
        foreach (var enemyData in enemyTypes)
        {
            // 각 적 타입별로 독립적인 풀 생성
            enemyData.pool = new ObjectPool<GameObject>(
                createFunc: () => CreateEnemy(enemyData),
                actionOnGet: (enemy) => OnGetEnemy(enemy, enemyData),
                actionOnRelease: (enemy) => OnReleaseEnemy(enemy, enemyData),
                actionOnDestroy: (enemy) => OnDestroyEnemy(enemy, enemyData),
                collectionCheck: true,
                defaultCapacity: enemyData.poolCapacity,
                maxSize: enemyData.poolMaxSize
            );
        }
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateEnemy(EnemySpawnData data)
    {
        GameObject enemy = Instantiate(data.enemyPrefab);
        enemy.name = $"{data.enemyName}_Pooled";

        // 어떤 데이터에 속하는지 기록
        instanceToData[enemy] = data;

        enemy.SetActive(false);
        return enemy;
    }

    void OnGetEnemy(GameObject enemy, EnemySpawnData data)
    {
        // SetActive는 SpawnEnemy에서 위치 설정 후 호출
        data.currentCount++;
        totalEnemyCount++;
    }

    void OnReleaseEnemy(GameObject enemy, EnemySpawnData data)
    {
        enemy.SetActive(false);
        data.currentCount--;
        totalEnemyCount--;
    }

    void OnDestroyEnemy(GameObject enemy, EnemySpawnData data)
    {
        instanceToData.Remove(enemy);
        Destroy(enemy);
    }

    #endregion

    #region Spawn Logic

    void StartAllSpawners()
    {
        // 각 적 타입마다 독립적인 코루틴 시작
        foreach (var enemyData in enemyTypes)
        {
            enemyData.spawnCoroutine = StartCoroutine(SpawnCoroutine(enemyData));
        }
    }

    void StopAllSpawners()
    {
        // 모든 스폰 코루틴 중지
        foreach (var enemyData in enemyTypes)
        {
            if (enemyData.spawnCoroutine != null)
            {
                StopCoroutine(enemyData.spawnCoroutine);
                enemyData.spawnCoroutine = null;
            }
        }
    }

    IEnumerator SpawnCoroutine(EnemySpawnData data)
    {
        while (true)
        {
            yield return new WaitForSeconds(data.spawnInterval);

            // 이 타입의 현재 수가 최대치 미만일 때만 스폰
            if (data.currentCount < data.maxCount)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();

                if (IsOutsideCameraView(spawnPosition))
                {
                    SpawnEnemy(data, spawnPosition);
                }
            }
        }
    }

    void SpawnEnemy(EnemySpawnData data, Vector2 position)
    {
        if (data.pool != null)
        {
            GameObject enemy = data.pool.Get();
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.identity;
            enemy.SetActive(true);  // 위치 설정 후 활성화
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

    #endregion

    #region Public Methods

    public void ReturnEnemy(GameObject enemy)
    {
        // 어떤 풀에 속하는지 찾아서 반납
        if (instanceToData.TryGetValue(enemy, out EnemySpawnData data))
        {
            data.pool.Release(enemy);
        }
    }

    // 특정 타입의 스폰 일시정지/재개
    public void PauseSpawner(string enemyName)
    {
        var data = enemyTypes.Find(e => e.enemyName == enemyName);
        if (data != null && data.spawnCoroutine != null)
        {
            StopCoroutine(data.spawnCoroutine);
            data.spawnCoroutine = null;
        }
    }

    public void ResumeSpawner(string enemyName)
    {
        var data = enemyTypes.Find(e => e.enemyName == enemyName);
        if (data != null && data.spawnCoroutine == null)
        {
            data.spawnCoroutine = StartCoroutine(SpawnCoroutine(data));
        }
    }

    // 디버그용: 각 타입별 현재 스폰된 수 확인
    public void PrintPoolStatus()
    {
        Debug.Log("=== Pool Status ===");
        foreach (var data in enemyTypes)
        {
            Debug.Log($"{data.enemyName}: {data.currentCount}/{data.maxCount} active " +
                      $"(Interval: {data.spawnInterval}s, Pool: {data.poolCapacity}/{data.poolMaxSize})");
        }
        Debug.Log($"Total Active: {totalEnemyCount}");
    }

    #endregion
}