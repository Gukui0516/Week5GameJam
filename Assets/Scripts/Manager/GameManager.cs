using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Boot, Playing, Paused, GameOver }

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("상태")]
    [SerializeField] private GameState current = GameState.Boot;
    public GameState Current => current;

    [Header("설정")]
    [SerializeField, Tooltip("게임 시작 시 타이틀로 진입할지")]
    private bool enterTitleOnBoot = true;

    [Header("스테이지")]
    [SerializeField, Tooltip("현재 스테이지. 새 게임은 1부터 시작")]
    private int currentStage = 1;
    public int CurrentStage => currentStage;

    public bool IsPaused => current == GameState.Paused;

    private SceneDirector sceneDirector;

    [Header("UI 참조")]
    public GameOverUI gameOverUI;
    public EndingUI endingUI; // EndingUI가 Awake에서 자동 등록

    [Header("Ending (단순 표시)")]
    [SerializeField] private int endingStage = 4;        // 해당 스테이지 도달 시 엔딩
    [SerializeField] private string endingMessage = "You Escaped.";

    private bool endingShown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!sceneDirector)
            sceneDirector = GetComponent<SceneDirector>();

        // 씬 로드시 EndingUI 자동 재바인딩
        SceneManager.sceneLoaded += OnSceneLoaded_Rebind;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_Rebind;
    }

    private void Start()
    {
        if (enterTitleOnBoot) GoTitle();
        else StartNewGame();
    }

    private void OnSceneLoaded_Rebind(Scene s, LoadSceneMode mode)
    {
        // EndingUI가 씬 오브젝트라면, 씬 전환 시 새 오브젝트를 다시 잡아준다.
        if (!endingUI)
        {
            var hook = FindFirstObjectByType<EndingUI>();
            if (hook) endingUI = hook;
        }

        // 씬 진입 시엔 엔딩 UI는 숨김 상태가 자연스럽다.
        if (endingUI) endingUI.HideImmediate();  // ← Hide() 대신 HideImmediate() 권장
    }

    // ======= 공개 API =======

    public void StartNewGame()
    {
        current = GameState.Boot;
        currentStage = 1;
        endingShown = false;

        if (endingUI) endingUI.Hide();

        sceneDirector.LoadGame();
        current = GameState.Playing;
        Resume();
    }

    public void GoTitle()
    {
        current = GameState.Boot;
        endingShown = false;

        if (endingUI) endingUI.Hide();

        sceneDirector.LoadTitle();
        Resume();
    }

    public void Restart()
    {
        StartNewGame();
    }

    // 현재 스테이지 유지한 채 게임 씬 재로드
    public void ReloadStage()
    {
        current = GameState.Boot;
        endingShown = false;

        if (endingUI) endingUI.Hide();

        sceneDirector.LoadGame();
        current = GameState.Playing;
        Resume();
    }

    // 스테이지 +1 올리고 같은 게임 씬 재로드
    // 예: 3 클리어 → 호출되면 currentStage=4 → 즉시 엔딩 표시
    public void AdvanceStageAndReload()
    {
        currentStage = Mathf.Max(1, currentStage + 1);

        if (!endingShown && currentStage >= endingStage)
        {
            PlayEnding();
            return;
        }

        sceneDirector.LoadGame();
    }

    public void Pause()
    {
        if (IsPaused) return;
        Time.timeScale = 0f;
        current = GameState.Paused;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        if (current == GameState.Paused) current = GameState.Playing;
    }

    public void GameOver()
    {
        current = GameState.GameOver;
        Time.timeScale = 0f;

        if (gameOverUI != null)
            gameOverUI.Show();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ======= 엔딩: 단순 즉시 표시 =======

    private void PlayEnding()
    {
        if (endingShown) return;
        endingShown = true;

        // 상태 전환 & 정지
        current = GameState.GameOver;
        Time.timeScale = 0f;

        // EndingUI가 비어있다면 한 번 더 찾아본 후 표시
        if (!endingUI)
        {
            endingUI = FindFirstObjectByType<EndingUI>();
        }

        if (endingUI)
        {
            endingUI.Show(endingMessage);
        }
        else
        {
            Debug.LogError("[GameManager] EndingUI not found/bound. Place an EndingUI in the scene or bind a persistent prefab.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AdvanceStageAndReload();
        }
    }

    
}
