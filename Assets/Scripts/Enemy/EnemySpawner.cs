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
        public float spawnInterval = 2f;
        public int maxCount = 5;

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
    private int lastAppliedStage = -1; // 마지막으로 적용된 스테이지 추적

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

        // 스테이지에 따른 적 설정 적용
        ApplyStageEnemySettings();

        StartAllSpawners();
    }

    void Update()
    {
        // 매 프레임 스테이지 변경 체크
        if (GameManager.Instance != null)
        {
            int currentStage = (int)GameManager.Instance.Current;
            if (currentStage != lastAppliedStage)
            {
                Debug.Log($"[EnemySpawner] Stage changed: {lastAppliedStage} -> {currentStage}");

                // 기존 스폰 중지
                StopAllSpawners();

                // 모든 적 제거
                ClearAllEnemies();

                // 새 스테이지 설정 적용
                ApplyStageEnemySettings();

                // 스폰 재시작
                StartAllSpawners();
            }
        }
    }

    void OnDestroy()
    {
        StopAllSpawners();
    }

    #endregion

    #region Stage Settings

    void ApplyStageEnemySettings()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[EnemySpawner] GameManager.Instance is null!");
            return;
        }

        int currentStage = (int)GameManager.Instance.CurrentStage;
        lastAppliedStage = currentStage; // 적용된 스테이지 기록

        Debug.Log($"[EnemySpawner] Applying enemy settings for Stage: {currentStage}");

        foreach (var enemyData in enemyTypes)
        {
            int previousMaxCount = enemyData.maxCount;

            if (enemyData.enemyName == "Normal")
            {
                switch (currentStage)
                {
                    case 1: enemyData.maxCount = 8; break;
                    case 2: enemyData.maxCount = 10; break;
                    case 3: enemyData.maxCount = 12; break;
                    default: enemyData.maxCount = 8; break;
                }
            }
            else if (enemyData.enemyName == "LightSeeker")
            {
                switch (currentStage)
                {
                    case 1: enemyData.maxCount = 0; break;
                    case 2: enemyData.maxCount = 3; break;
                    case 3: enemyData.maxCount = 5; break;
                    default: enemyData.maxCount = 0; break;
                }
            }

            Debug.Log($"[EnemySpawner] Stage {currentStage}: {enemyData.enemyName} maxCount changed {previousMaxCount} -> {enemyData.maxCount}");
        }
    }

    void ClearAllEnemies()
    {
        // 활성화된 모든 적을 풀로 반환
        foreach (var kvp in new Dictionary<GameObject, EnemySpawnData>(instanceToData))
        {
            GameObject enemy = kvp.Key;
            if (enemy != null && enemy.activeInHierarchy)
            {
                ReturnEnemy(enemy);
            }
        }

        Debug.Log($"[EnemySpawner] All enemies cleared. Total count: {totalEnemyCount}");
    }

    #endregion

    #region Pool Initialization

    void InitializePools()
    {
        foreach (var enemyData in enemyTypes)
        {
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

        instanceToData[enemy] = data;

        enemy.SetActive(false);
        return enemy;
    }

    void OnGetEnemy(GameObject enemy, EnemySpawnData data)
    {
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
        foreach (var enemyData in enemyTypes)
        {
            if (enemyData.maxCount > 0 && enemyData.spawnCoroutine == null)
            {
                enemyData.spawnCoroutine = StartCoroutine(SpawnCoroutine(enemyData));
                Debug.Log($"[EnemySpawner] Started spawner for {enemyData.enemyName} (maxCount: {enemyData.maxCount})");
            }
        }
    }

    void StopAllSpawners()
    {
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
            enemy.SetActive(true);
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
        if (instanceToData.TryGetValue(enemy, out EnemySpawnData data))
        {
            data.pool.Release(enemy);
        }
    }

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