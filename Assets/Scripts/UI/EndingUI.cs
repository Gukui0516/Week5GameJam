using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EndingUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;   // 엔딩 UI 전체 루트 (배경+텍스트 포함)

    private void Awake()
    {
        // GameManager가 있으면 자기 자신을 등록해둠 (GameManager에 public EndingUI endingUI; 필요)
        if (GameManager.Instance)
            GameManager.Instance.endingUI = this;

        Hide(); // 시작 시 숨김
    }

    public void Show(string message = null)
    {

        if (panel && !panel.activeSelf)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel && panel.activeSelf)
            panel.SetActive(false);
    }
}
