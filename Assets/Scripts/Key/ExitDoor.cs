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
        new KeyRequirement{kind=KeyKind.Red,   amount=1},
        new KeyRequirement{kind=KeyKind.Blue,  amount=1},
        new KeyRequirement{kind=KeyKind.Green, amount=1},
        new KeyRequirement{kind=KeyKind.Yellow,amount=1},
    };
    [Header("Stage-based Requirements")]
    public bool useStageTable = true;
    public StageKeyRequirementTable stageTable;
    [Tooltip("GameManager에서 현재 스테이지를 읽는다. 끄면 overrideStage 사용")]
    public bool readStageFromGameManager = true;
    [Min(1)] public int overrideStage = 1;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
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
}
