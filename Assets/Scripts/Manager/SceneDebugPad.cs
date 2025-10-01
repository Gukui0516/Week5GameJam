using UnityEngine;

public class SceneDebugPad : MonoBehaviour
{
    [Header("IMGUI 버튼도 띄울지")]
    [SerializeField] bool showOnGUI = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) GameManager.Instance?.GoTitle();            // 타이틀로
        if (Input.GetKeyDown(KeyCode.G)) GameManager.Instance?.StartNewGame();       // 게임 씬(스테이지 1부터)
        if (Input.GetKeyDown(KeyCode.R)) GameManager.Instance?.Restart();            // 재시작(새 게임)
        if (Input.GetKeyDown(KeyCode.L)) GameManager.Instance?.ReloadStage();        // 현재 스테이지로 재로드
        if (Input.GetKeyDown(KeyCode.N)) GameManager.Instance?.AdvanceStageAndReload(); // 스테이지+1 후 재로드

        if (Input.GetKeyDown(KeyCode.P))                                             // 일시정지 토글
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.IsPaused) GameManager.Instance.Resume();
            else GameManager.Instance.Pause();
        }
    }

    void OnGUI()
    {
        if (!showOnGUI) return;

        float w = 200, h = 28, y = 10;
        Rect Btn(float yy) => new Rect(10, yy, w, h);

        if (GUI.Button(Btn(y), "Title (T)")) GameManager.Instance?.GoTitle(); y += h + 6;
        if (GUI.Button(Btn(y), "Game (G)")) GameManager.Instance?.StartNewGame(); y += h + 6;
        if (GUI.Button(Btn(y), "Restart (R)")) GameManager.Instance?.Restart(); y += h + 6;
        if (GUI.Button(Btn(y), "Reload Stage (L)")) GameManager.Instance?.ReloadStage(); y += h + 6;
        if (GUI.Button(Btn(y), "Next Stage+Reload (N)")) GameManager.Instance?.AdvanceStageAndReload(); y += h + 6;

        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        if (GUI.Button(Btn(y), paused ? "Resume (P)" : "Pause (P)"))
        {
            if (paused) GameManager.Instance.Resume(); else GameManager.Instance.Pause();
        }
        y += h + 8;

        int stage = GameManager.Instance != null ? GameManager.Instance.CurrentStage : -1;
        string state = GameManager.Instance != null ? GameManager.Instance.Current.ToString() : "None";
        GUI.Label(new Rect(10, y, 360, 20), $"Stage: {stage} | State: {state}");
    }
}
