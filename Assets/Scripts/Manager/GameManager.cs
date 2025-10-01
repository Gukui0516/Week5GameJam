using UnityEngine;

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


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sceneDirector == null)
            sceneDirector = GetComponent<SceneDirector>();
    }

    void Start()
    {
        if (enterTitleOnBoot)
        {
            GoTitle();
        }
        else
        {
            StartNewGame();
        }
        Debug.Log("st");
    }

    // ======= 공개 API =======

    public void StartNewGame()
    {
        current = GameState.Boot;
        currentStage = 1;
        sceneDirector.LoadGame();
        current = GameState.Playing;
        Resume();
        Debug.Log("StartNewGame");
    }

    public void GoTitle()
    {
        current = GameState.Boot;
        sceneDirector.LoadTitle();
        Resume();
    }

    public void Restart()
    {
        // 현재 싱글씬이 게임씬이라고 가정
        StartNewGame();
    }

    // 현재 스테이지를 유지한 채 게임 씬 재로드
    public void ReloadStage()
    {
        if (current == GameState.GameOver) return;
        sceneDirector.LoadGame();
    }

    // 스테이지 +1 올리고 같은 게임 씬 다시 로드
    public void AdvanceStageAndReload()
    {
        currentStage = Mathf.Max(1, currentStage + 1);
        sceneDirector.LoadGame();
    }

    public void Pause()
    {
        if (IsPaused) return;
        Time.timeScale = 0f;
        current = GameState.Paused;
        // 입력 락 등 훅 추가 지점
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
        // 게임 종료
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    /*
    public class DifficultyScaler : MonoBehaviour
    {
        void Start()
        {
            int stage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : 1;
            ApplyDifficulty(stage);
        }

        void ApplyDifficulty(int stage)
        {
            // 스테이지별 난이도/가격/스폰 테이블 등 적용
        }
    }

    //다른 클래스에서 사용 시 이런식으로 인스턴스를 받아와 현재 스테이지 int정보를 가져와 초기화 처리하면 됨.

    */
}
