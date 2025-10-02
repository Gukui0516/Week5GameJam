// Assets/Scripts/Stage/KeySpawner.cs
using System;
using UnityEngine;

public class KeySpawner : MonoBehaviour
{
    [Serializable]
    public struct KeyPrefab
    {
        public KeyKind kind;
        public GameObject prefab;
    }

    [Serializable]
    public struct StageWeights
    {
        public int stage;
        [Min(0)] public float circle;
        [Min(0)] public float clover;
        [Min(0)] public float heart;
        [Min(0)] public float square;
    }

    [Header("Prefabs")]
    [SerializeField] private KeyPrefab[] keyPrefabs;

    [Header("Weights by Stage")]
    [Tooltip("스테이지별 기본 가중치. 요구 키엔 requiredMultiplier가 곱해진다.")]
    [SerializeField] private StageWeights[] stageWeights;

    [Header("Bias")]
    [Tooltip("요구 키에 곱해줄 배수. 예: 2면 요구 키 가중치가 두 배")]
    [SerializeField] private float requiredMultiplier = 2f;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private int maxAlive = 12;
    [SerializeField] private float radiusFromCamera = 7f;
    [SerializeField] private Transform spawnRoot; // 생성물 부모

    private float timer;

    private void Awake()
    {
        if (!spawnRoot) spawnRoot = this.transform;
    }

    private void OnEnable()
    {
        KeyStageContext.RequirementsChanged += OnRequirementsChanged;
    }
    private void OnDisable()
    {
        KeyStageContext.RequirementsChanged -= OnRequirementsChanged;
    }

    private void Start()
    {
        // 처음 한 번 확률 로그
        LogCurrentProbabilities();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        if (spawnRoot != null && spawnRoot.childCount >= maxAlive) return;

        var kind = SampleKind();
        var prefab = GetPrefab(kind);
        if (!prefab) return;

        var pos = PickSpawnPosNearCamera();
        var go = Instantiate(prefab, pos, Quaternion.identity, spawnRoot);

        // 스폰마다 확률 로그
        LogCurrentProbabilities();
    }

    private void OnRequirementsChanged()
    {
        // 필요 시 즉시 리액트할 로직이 있으면 여기에
        LogCurrentProbabilities();
    }

    private GameObject GetPrefab(KeyKind kind)
    {
        for (int i = 0; i < keyPrefabs.Length; i++)
            if (keyPrefabs[i].kind == kind) return keyPrefabs[i].prefab;
        return null;
    }

    private Vector3 PickSpawnPosNearCamera()
    {
        var cam = Camera.main;
        Vector3 center = cam ? cam.transform.position : Vector3.zero;
        float ang = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        var offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radiusFromCamera;
        var p = center + offset;
        p.z = 0f;
        return p;
    }

    private StageWeights GetWeightsFor(int stage)
    {
        for (int i = 0; i < stageWeights.Length; i++)
            if (stageWeights[i].stage == stage) return stageWeights[i];

        // 기본값: 균등
        return new StageWeights
        {
            stage = stage,
            circle = 1, clover = 1, heart = 1, square = 1
        };
    }

    private KeyKind SampleKind()
    {
        int stage = KeyStageContext.CurrentStage;
        var w = GetWeightsFor(stage);

        float wc = w.circle * (KeyStageContext.IsRequired(KeyKind.Circle) ? requiredMultiplier : 1f);
        float wv = w.clover * (KeyStageContext.IsRequired(KeyKind.Clover) ? requiredMultiplier : 1f);
        float wh = w.heart  * (KeyStageContext.IsRequired(KeyKind.Heart)  ? requiredMultiplier : 1f);
        float ws = w.square * (KeyStageContext.IsRequired(KeyKind.Square) ? requiredMultiplier : 1f);

        float sum = wc + wv + wh + ws;
        if (sum <= 0f) { wc = wv = wh = ws = 1f; sum = 4f; }

        float r = UnityEngine.Random.value * sum;
        if ((r -= wc) < 0f) return KeyKind.Circle;
        if ((r -= wv) < 0f) return KeyKind.Clover;
        if ((r -= wh) < 0f) return KeyKind.Heart;
        return KeyKind.Square;
    }

    private void LogCurrentProbabilities()
    {
        int stage = KeyStageContext.CurrentStage;
        var w = GetWeightsFor(stage);

        float wc = w.circle * (KeyStageContext.IsRequired(KeyKind.Circle) ? requiredMultiplier : 1f);
        float wv = w.clover * (KeyStageContext.IsRequired(KeyKind.Clover) ? requiredMultiplier : 1f);
        float wh = w.heart  * (KeyStageContext.IsRequired(KeyKind.Heart)  ? requiredMultiplier : 1f);
        float ws = w.square * (KeyStageContext.IsRequired(KeyKind.Square) ? requiredMultiplier : 1f);

        float sum = wc + wv + wh + ws;
        if (sum <= 0f) sum = 1f;

        Debug.Log($"[KeySpawner] Stage {stage} probs | Circle {(wc/sum):0.00}, Clover {(wv/sum):0.00}, Heart {(wh/sum):0.00}, Square {(ws/sum):0.00}");
    }
}
