using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static KeyCollector;
// TextMeshPro를 쓰면 주석 해제
using TMPro;
using System.Reflection;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    [Header("Requirements (기본: 4종 1개씩)")]
    public KeyRequirement[] requirements = new KeyRequirement[]
    {
        new KeyRequirement{kind=KeyKind.Circle,   amount=1},
        new KeyRequirement{kind=KeyKind.Clover,  amount=1},
        new KeyRequirement{kind=KeyKind.Heart, amount=1},
        new KeyRequirement{kind=KeyKind.Square,amount=1},
    };
    [Header("Stage-based Requirements")]
    public bool useStageTable = true;
    public StageKeyRequirementTable stageTable;
    [Tooltip("GameManager에서 현재 스테이지를 읽는다. 끄면 overrideStage 사용")]
    public bool readStageFromGameManager = true;
    [Min(1)] public int overrideStage = 1;

    [Header("Position Settings")]
    [SerializeField] private bool autoPositionOnStart = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float stage1Distance = 80f;
    [SerializeField] private float stage2Distance = 120f;
    [SerializeField] private float stage3Distance = 160f;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool consumeOnOpen = true;

    [Header("Events")]
    public UnityEvent onDoorOpened;
    public UnityEvent onRequirementNotMet;

    [Header("Worldspace UI")]
    [Tooltip("자식에 있는 월드스페이스 Canvas 루트. 비워두면 자동 탐색")]
    [SerializeField] private GameObject worldspaceUIRoot;
    [Tooltip("프롬프트 텍스트 (선택 사항)")]
    [SerializeField] private TMP_Text promptText;
    [Tooltip("요구치 부족 안내 텍스트 (선택 사항)")]
    [SerializeField] private TMP_Text requirementText;
    [SerializeField] private bool showMissingWhenInsufficient = true;
    [SerializeField] private string promptFormat = "E";
    [SerializeField] private string missingFormat = "키가 없습니다: {0}";

    private bool playerInRange;
    private KeyCollector cachedCollector;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (worldspaceUIRoot == null)
            worldspaceUIRoot = FindWorldspaceCanvasInChildren();
        HideUI();
        cachedCollector = FindFirstObjectByType<KeyCollector>();
    }

    private void Start()
    {
        if (autoPositionOnStart)
            PositionExit();
    }

    private void OnDisable()
    {
        playerInRange = false;
        cachedCollector = null;
        HideUI();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;
        cachedCollector = other.GetComponent<KeyCollector>();
        if (cachedCollector == null)
        {
            Debug.LogWarning("ExitDoor: 플레이어에 KeyCollector가 없음.");
        }

        UpdateUIContents();
        ShowUI();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        cachedCollector = null;
        HideUI();
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (Input.GetKeyDown(interactKey))
            TryOpen();
    }

    private void TryOpen()
    {
        var activeReqs = GetActiveRequirements();

        if (cachedCollector == null)
        {
            onRequirementNotMet?.Invoke();
            UpdateUIContents(activeReqs);
            return;
        }

        bool ok = consumeOnOpen
            ? cachedCollector.TryUseRequirements(activeReqs)
            : cachedCollector.CheckRequirements(activeReqs);

        if (!ok)
        {
            onRequirementNotMet?.Invoke();
            UpdateUIContents(activeReqs);
            return;
        }

        HideUI();
        onDoorOpened?.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.AdvanceStageAndReload();
        else
            Debug.LogWarning("ExitDoor: GameManager 인스턴스 없음.");
    }

    // ===== Position =====

    private void PositionExit()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            Debug.LogWarning("ExitDoor: 플레이어를 찾을 수 없습니다.");
            return;
        }

        int currentStage = readStageFromGameManager ? GetStageFromGameManagerSafe() : overrideStage;
        float distance = GetDistanceForStage(currentStage);

        // 플레이어 기준 랜덤 각도
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float x = playerObj.transform.position.x + Mathf.Cos(randomAngle) * distance;
        float y = playerObj.transform.position.y + Mathf.Sin(randomAngle) * distance;

        transform.position = new Vector3(x, y, transform.position.z);

        Debug.Log($"[ExitDoor] Positioned at {transform.position} (Stage: {currentStage}, Distance: {distance})");
    }

    private float GetDistanceForStage(int stage)
    {
        switch (stage)
        {
            case 1: return stage1Distance;
            case 2: return stage2Distance;
            case 3: return stage3Distance;
            default: return stage1Distance;
        }
    }

    // ===== requirements / stage =====

    private KeyRequirement[] GetActiveRequirements()
    {
        if (useStageTable && stageTable != null)
        {
            int stage = readStageFromGameManager ? GetStageFromGameManagerSafe() : overrideStage;
            if (stage < 1) stage = 1;
            return stageTable.GetForStage(stage);
        }
        return requirements ?? System.Array.Empty<KeyRequirement>();
    }

    private int GetStageFromGameManagerSafe()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 1;

        // 1) 널리 쓰일 법한 프로퍼티부터
        var props = new[] { "CurrentStage", "Stage", "StageIndex", "Level" };
        foreach (var p in props)
        {
            var pi = gm.GetType().GetProperty(p, BindingFlags.Instance | BindingFlags.Public);
            if (pi != null && pi.PropertyType == typeof(int))
                return (int)pi.GetValue(gm, null);
        }
        // 2) 필드
        var fields = new[] { "currentStage", "stage", "stageIndex", "level" };
        foreach (var f in fields)
        {
            var fi = gm.GetType().GetField(f, BindingFlags.Instance | BindingFlags.Public);
            if (fi != null && fi.FieldType == typeof(int))
                return (int)fi.GetValue(gm);
        }
        // 3) 못 찾으면 1
        return 1;
    }

    // ===== UI =====

    private void ShowUI()
    {
        if (worldspaceUIRoot != null && !worldspaceUIRoot.activeSelf)
            worldspaceUIRoot.SetActive(true);
    }

    private void HideUI()
    {
        if (worldspaceUIRoot != null && worldspaceUIRoot.activeSelf)
            worldspaceUIRoot.SetActive(false);
    }

    private void UpdateUIContents() => UpdateUIContents(GetActiveRequirements());

    private void UpdateUIContents(KeyRequirement[] activeReqs)
    {
        if (promptText != null) promptText.text = promptFormat;

        if (requirementText != null)
        {
            string missing = GetMissingString(activeReqs);
            if (string.IsNullOrEmpty(missing) || !showMissingWhenInsufficient)
                requirementText.text = "";
            else
                requirementText.text = string.Format(missingFormat, missing);
        }
    }

    private string GetMissingString(KeyRequirement[] activeReqs)
    {
        if (cachedCollector == null) return "인벤토리 없음";

        List<string> parts = new List<string>();
        foreach (var r in activeReqs)
        {
            int have = cachedCollector.Get(r.kind);
            int need = r.amount;
            int shortfall = Mathf.Max(0, need - have);
            if (shortfall > 0) parts.Add($"{r.kind} {shortfall}");
        }
        return string.Join(", ", parts);
    }

    private GameObject FindWorldspaceCanvasInChildren()
    {
        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
            if (c.renderMode == RenderMode.WorldSpace)
                return c.gameObject;
        return null;
    }

    // ===== Public API =====

    /// <summary>
    /// 외부에서 출구 위치를 다시 설정할 때 호출
    /// </summary>
    public void RepositionExit()
    {
        PositionExit();
    }
}