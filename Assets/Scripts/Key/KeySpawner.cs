using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class KeySpawner : MonoBehaviour
{
    [Header("Spawn Settings (Keys only)")]
    [Tooltip("랜덤으로 뽑힐 키 프리팹들. 4개 권장.")]
    [SerializeField] private List<GameObject> keyPrefabs = new List<GameObject>(4);

    [Tooltip("키 전용 스폰 간격(초). ItemSpawner와 독립.")]
    [SerializeField, Min(0.05f)] private float spawnInterval = 3f;

    [Tooltip("동시에 존재 가능한 키 최대 개수")]
    [SerializeField, Min(0)] private int maxKeys = 6;

    [Tooltip("카메라 중심에서 반경으로 스폰")]
    [SerializeField, Min(0f)] private float spawnDistance = 12f;

    [Tooltip("스폰 지점이 카메라 밖인 경우만 생성")]
    [SerializeField] private bool outsideCameraOnly = true;

    [Header("Despawn Settings")]
    [Tooltip("플레이어와 이 거리보다 멀어지면 자동 반환")]
    [SerializeField, Min(0f)] private float despawnDistance = 25f;

    [Tooltip("수명 제한(초). 음수면 비활성)")]
    [SerializeField] private float optionalMaxLifetime = -1f;

    [Header("Pool Settings")]
    [SerializeField, Min(0)] private int defaultPoolCapacity = 8;
    [SerializeField, Min(1)] private int maxPoolSize = 24;

    [Header("Refs")]
    [SerializeField] private string playerTag = "Player";

    private Camera mainCamera;
    private Transform player;
    private ObjectPool<GameObject> pool;
    private int currentKeyCount;

    public static KeySpawner Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        pool = new ObjectPool<GameObject>(
            createFunc: CreateKey,
            actionOnGet: OnGetKey,
            actionOnRelease: OnReleaseKey,
            actionOnDestroy: OnDestroyKey,
            collectionCheck: true,
            defaultCapacity: defaultPoolCapacity,
            maxSize: maxPoolSize
        );
    }

    void Start()
    {
        mainCamera = Camera.main;

        var playerGO = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGO != null) player = playerGO.transform;
        else Debug.LogWarning("[KeySpawner] Player 태그 오브젝트를 찾지 못했습니다.");

        StartCoroutine(SpawnLoop());
    }

    // --- Pool callbacks ---
    private GameObject CreateKey()
    {
        if (keyPrefabs == null || keyPrefabs.Count == 0)
        {
            Debug.LogError("[KeySpawner] keyPrefabs 비어있음.");
            return new GameObject("Key_MissingPrefab");
        }

        // 4개 중 랜덤
        var prefab = keyPrefabs[Random.Range(0, keyPrefabs.Count)];
        if (prefab == null)
        {
            Debug.LogError("[KeySpawner] null prefab 발견. 리스트 확인 바람.");
            return new GameObject("Key_NullPrefab");
        }

        var go = Instantiate(prefab);
        go.SetActive(false);

        // 거리 기반 자동 반환용 컴포넌트 보장
        if (!go.TryGetComponent<PooledItem>(out _))
            go.AddComponent<PooledItem>();

        return go;
    }

    private void OnGetKey(GameObject key)
    {
        key.SetActive(true);
        currentKeyCount = Mathf.Max(0, currentKeyCount + 1);

        // 플레이어 거리 체크와 풀 반환 콜백 세팅
        var pooled = key.GetComponent<PooledItem>();
        pooled.Setup(
            player,
            despawnDistance,
            ReleaseKey,          // 멀어지면 스스로 풀로 돌아가도록
            optionalMaxLifetime  // 음수면 수명 제한 없음
        );
    }

    private void OnReleaseKey(GameObject key)
    {
        key.SetActive(false);
        currentKeyCount = Mathf.Max(0, currentKeyCount - 1);
    }

    private void OnDestroyKey(GameObject key)
    {
        Destroy(key);
    }

    private void ReleaseKey(GameObject go)
    {
        if (go != null) pool.Release(go);
    }

    // --- Spawning ---
    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (enabled)
        {
            yield return wait;

            if (currentKeyCount >= maxKeys) continue;

            Vector2 spawnPos = GetRandomSpawnPosition();
            if (!outsideCameraOnly || IsOutsideCameraView(spawnPos))
            {
                SpawnKey(spawnPos);
            }
        }
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 center = mainCamera != null ? (Vector2)mainCamera.transform.position : Vector2.zero;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(
            center.x + Mathf.Cos(angle) * spawnDistance,
            center.y + Mathf.Sin(angle) * spawnDistance
        );
    }

    private bool IsOutsideCameraView(Vector2 worldPos)
    {
        if (mainCamera == null) return true;
        Vector3 vp = mainCamera.WorldToViewportPoint(worldPos);
        return vp.x < 0 || vp.x > 1 || vp.y < 0 || vp.y > 1;
    }

    private void SpawnKey(Vector2 position)
    {
        var key = pool.Get();
        key.transform.SetPositionAndRotation(position, Quaternion.identity);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Vector3 c = mainCamera ? mainCamera.transform.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(c, spawnDistance);
    }
#endif
}
