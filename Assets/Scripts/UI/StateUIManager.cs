using System.Collections;
using UnityEngine;
using TMPro;

public class StageUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI stageNumberText;
    [SerializeField] private TextMeshProUGUI stageInfoText;

    [Header("Settings")]
    [SerializeField] private int currentStage = 1;
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    // 스테이지별 정보 데이터
    private readonly string[] stageInfoMessages = new string[]
    {
        "빛을 보면 멈추는 괴물이 등장합니다.",                                    // 1스테이지
        "빛을 보면 따라오는 괴물이 추가되었습니다.",          // 2스테이지
        "흰색 괴물이 더 자주 등장합니다."       // 3스테이지
    };

    void Start()
    {
        // 초기 알파값을 1로 설정
        SetTextAlpha(stageNumberText, 1f);
        SetTextAlpha(stageInfoText, 1f);
        currentStage = GameManager.Instance.CurrentStage;
        // 스테이지 정보 표시 시작
        StartCoroutine(DisplayStageInfo());
    }

    private IEnumerator DisplayStageInfo()
    {
        // 스테이지 번호 텍스트 설정
        stageNumberText.text = $"Stage : {currentStage}";

        // 스테이지 정보 텍스트 설정
        if (currentStage <= stageInfoMessages.Length)
        {
            stageInfoText.text = stageInfoMessages[currentStage - 1];
        }
        else
        {
            stageInfoText.text = "";
        }

        // 1초간 표시
        yield return new WaitForSeconds(displayDuration);

        // 1초간 페이드아웃
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);

            SetTextAlpha(stageNumberText, alpha);
            SetTextAlpha(stageInfoText, alpha);

            yield return null;
        }

        // 완전히 투명하게
        SetTextAlpha(stageNumberText, 0f);
        SetTextAlpha(stageInfoText, 0f);

        // 선택사항: UI를 완전히 비활성화
        gameObject.SetActive(false);
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text != null)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    // 외부에서 스테이지 번호를 설정할 수 있는 메서드
    public void SetStage(int stage)
    {
        currentStage = stage;
    }

    // 스테이지 정보 배열에 새로운 메시지 추가 (선택사항)
    public void AddStageInfo(string message)
    {
        // 런타임에서 스테이지 정보를 추가하려면 List로 변경 필요
    }
}