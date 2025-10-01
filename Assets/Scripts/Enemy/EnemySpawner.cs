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
        public string enemyName;
        public GameObject enemyPrefab;
        [Range(0f, 1f)] public float spawnWeight = 0.5f;
    }

    #endregion

    #region Variables

    [Header("Spawn Settings")]
    [SerializeField] private List<EnemySpawnData> enemyTypes = new List<EnemySpawnData>();
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnDistance = 15f;

    [Header("Pool Settings")]
    [SerializeField] private int defaultPoolCapacity = 10;
    [SerializeField] private int maxPoolSize = 20;

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> enemyPool; // 단일 풀!
    private int currentEnemyCount = 0;
    private float totalSpawnWeight;

    // 풀에서 어떤 프리팹으로 생성됐는지 추적
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

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

        // 단일 ObjectPool 초기화
        enemyPool = new ObjectPool<GameObject>(
            createFunc: CreateEnemy,
            actionOnGet: OnGetEnemy,
            actionOnRelease: OnReleaseEnemy,
            actionOnDestroy: OnDestroyEnemy,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSize
        );

        // 총 가중치 계산
        totalSpawnWeight = 0f;
        foreach (var enemyData in enemyTypes)
        {
            totalSpawnWeight += enemyData.spawnWeight;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(SpawnCoroutine());
    }

    #endregion

    #region Object Pool Callbacks

    GameObject CreateEnemy()
    {
        // 랜덤하게 프리팹 선택해서 생성
        GameObject selectedPrefab = SelectEnemyByWeight();
        GameObject enemy = Instantiate(selectedPrefab);

        // 어떤 프리팹으로 만들어졌는지 기록
        instanceToPrefab[enemy] = selectedPrefab;

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
        instanceToPrefab.Remove(enemy);
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

    void SpawnEnemy(Vector2 position)
    {
        GameObject enemy = enemyPool.Get();
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.identity;
    }

    GameObject SelectEnemyByWeight()
    {
        if (enemyTypes.Count == 0) return null;

        float randomValue = Random.Range(0f, totalSpawnWeight);
        float cumulativeWeight = 0f;

        foreach (var enemyData in enemyTypes)
        {
            cumulativeWeight += enemyData.spawnWeight;
            if (randomValue <= cumulativeWeight)
            {
                return enemyData.enemyPrefab;
            }
        }

        return enemyTypes[0].enemyPrefab;
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
        enemyPool.Release(enemy);
    }

    #endregion
}