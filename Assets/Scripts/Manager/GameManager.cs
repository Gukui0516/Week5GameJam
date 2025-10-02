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

    [SerializeField] int endingStage = 4;


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

        
    }


    private void Start()
    {
        if (enterTitleOnBoot) GoTitle();
        else StartNewGame();
    }

    

    // ======= 공개 API =======

    public void StartNewGame()
    {
        current = GameState.Boot;
        currentStage = 1;
        


        sceneDirector.LoadGame();
        current = GameState.Playing;
        Resume();
    }

    public void GoTitle()
    {
        current = GameState.Boot;

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

        sceneDirector.LoadGame();
        current = GameState.Playing;
        Resume();
    }

    // 스테이지 +1 올리고 같은 게임 씬 재로드
    // 예: 3 클리어 → 호출되면 currentStage=4 → 즉시 엔딩 표시
    public void AdvanceStageAndReload()
    {
        currentStage = Mathf.Max(1, currentStage + 1);

        if (currentStage >= endingStage)
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

    public void PlayEnding()
    {
        sceneDirector.LoadEnding();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AdvanceStageAndReload();
        }
    }

    
}
