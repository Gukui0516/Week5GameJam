using UnityEngine;
using UnityEngine.UI;
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    void Awake()
    {
        restartButton.onClick.AddListener(OnRestartClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        Hide(); // 시작 시 숨김
    }

    public void Show()
    {
        panel.SetActive(true);
        // 추가: 애니메이션, 사운드 등
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void OnRestartClicked()
    {
        Hide();
        GameManager.Instance.Restart();
    }

    private void OnQuitClicked()
    {
        Hide();
        GameManager.Instance.QuitGame();
    }
}