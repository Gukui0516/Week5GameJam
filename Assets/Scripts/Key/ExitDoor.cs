using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class ExitDoor : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private bool useGameManagerStage = true;
    [SerializeField, Min(1)] private int stageOverride = 1;

    [Header("Door/Trigger")]
    [SerializeField] private Collider2D trigger;
    [Tooltip("문 해제 시 활성화할 실제 출구(선택)")]
    [SerializeField] private GameObject realExitToActivate;

    [Header("Interact")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("플레이어가 범위에 있을 때 보여줄 상호작용 프롬프트")]
    [SerializeField] private GameObject InteractImageObj;

    [Header("Next Scene Options")]
    [Tooltip("GameManager의 AdvanceStageAndReload 사용 여부")]
    [SerializeField] private bool useGameManagerToAdvance = false;
    [Tooltip("이름이 설정되어 있으면 이 씬으로 이동")]
    [SerializeField] private string nextSceneName = "";
    [Tooltip("이 값이 0 이상이면 해당 빌드 인덱스로 이동")]
    [SerializeField] private int nextSceneBuildIndex = -1;

    [Header("Events")]
    public UnityEvent onUnlocked;

    private bool playerInRange;
    private KeyCollector playerCollector;
    private bool unlocked; // 1차 상호작용으로 문이 열린 상태인지

    private KeyCollector.KeyRequirement[] activeRequirements;

    private void Reset()
    {
        trigger = GetComponent<Collider2D>();
        if (realExitToActivate == null && transform.childCount > 0)
            realExitToActivate = transform.GetChild(0).gameObject;
    }

    private void Awake()
    {
        if (!trigger) trigger = GetComponent<Collider2D>();
        if (realExitToActivate) realExitToActivate.SetActive(false);

        // 시작 시엔 꺼두고, 플레이어가 범위에 들어오면 켠다
        if (InteractImageObj) InteractImageObj.SetActive(false);

        int stage = DetermineStage();
        activeRequirements = BuildRandomRequirementsForStage(stage);
        KeyStageContext.SetRequirements(stage, ExtractKinds(activeRequirements));
        unlocked = false;
    }

    private int DetermineStage()
    {
        if (useGameManagerStage && GameManager.Instance != null)
            return Mathf.Max(1, GameManager.Instance.CurrentStage);
        return Mathf.Max(1, stageOverride);
    }

    private int RequiredKeyCountForStage(int stage)
    {
        if (stage <= 1) return 1;
        if (stage == 2) return 2;
        return 4;
    }

    private KeyCollector.KeyRequirement[] BuildRandomRequirementsForStage(int stage)
    {
        int need = Mathf.Clamp(RequiredKeyCountForStage(stage), 1, 4);
        var pool = new List<KeyKind> { KeyKind.Circle, KeyKind.Clover, KeyKind.Heart, KeyKind.Square };
        var picked = new List<KeyKind>(need);
        for (int i = 0; i < need; i++)
        {
            int idx = Random.Range(0, pool.Count);
            picked.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        var reqs = new KeyCollector.KeyRequirement[picked.Count];
        for (int i = 0; i < reqs.Length; i++)
        {
            reqs[i].kind = picked[i];
            reqs[i].amount = 1;
        }
        return reqs;
    }

    private IReadOnlyList<KeyKind> ExtractKinds(KeyCollector.KeyRequirement[] reqs)
    {
        var list = new List<KeyKind>(reqs?.Length ?? 0);
        if (reqs != null)
            for (int i = 0; i < reqs.Length; i++) list.Add(reqs[i].kind);
        return list;
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!unlocked)
            {
                // 1차: 요구치 검사 및 문 열기
                if (playerCollector != null && playerCollector.TryUseRequirements(activeRequirements))
                {
                    var doorUI = GetComponentInChildren<ExitDoorKeyUI>(true);
                    if (doorUI) doorUI.HideAll();

                    if (realExitToActivate) realExitToActivate.SetActive(true);

                    // 상호작용 이미지는 범위에 있는 동안 계속 활성 상태 유지
                    if (InteractImageObj && !InteractImageObj.activeSelf)
                        InteractImageObj.SetActive(true);

                    unlocked = true;
                    onUnlocked?.Invoke();
                }
                // 미충족이면 그냥 대기. 텍스트 갱신은 요구사항상 없음.
            }
            else
            {
                // 2차: 다음 씬으로 이동
                ProceedToNextStageOrScene();
            }
        }
    }

    private void ProceedToNextStageOrScene()
    {
        if (!enabled) return;
        enabled = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceStageAndReload();
            
        }
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        playerCollector = other.GetComponent<KeyCollector>();

        // 요구량 검사 전에도 상호작용 이미지를 켠다
        if (InteractImageObj) InteractImageObj.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        playerCollector = null;

        // 범위를 벗어나면 끈다. 다시 들어오면 다시 켜짐.
        if (InteractImageObj) InteractImageObj.SetActive(false);
    }
}
