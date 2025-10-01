using UnityEngine;

public class StartSceneButtonUI : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        GameManager.Instance.StartNewGame();
    }
    public void OnQuitButtonClicked()
    {
        GameManager.Instance.QuitGame();
    }
}
