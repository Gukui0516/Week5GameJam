using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-100)]
public class EndingUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;   // 엔딩 UI 전체 루트 (배경+텍스트 포함)

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

    private CanvasGroup cg;
    private Coroutine fadeCo;
    private bool isInitialized;

    void Awake()
    {
        TryInit();

        // 시작 시 숨김(안전)
        if (isInitialized)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            panel.SetActive(false);
        }

        if (GameManager.Instance)
            GameManager.Instance.endingUI = this;
    }

    void TryInit()
    {
        if (!panel) { Debug.LogWarning("[EndingUI] panel이 비어있습니다."); return; }
        if (!panel.TryGetComponent(out cg))
            cg = panel.AddComponent<CanvasGroup>();
        isInitialized = (cg != null);
    }

    // GameManager 호환: 기존 코드에서 Show("text") 호출해도 동작하도록.
    public void Show(string _ignoredMessage) => Show();

    public void Show()
    {
        if (!isInitialized) { TryInit(); if (!isInitialized) return; }

        if (!panel.activeSelf) panel.SetActive(true);

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeRoutine(true));
    }

    public void Hide()
    {
        // 씬 로드 직후나 비활성 계층에 있을 때는 코루틴보다 즉시 처리 쪽이 안전
        if (!isInitialized || !gameObject.activeInHierarchy || !panel || !panel.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeRoutine(false));
    }

    public void ShowImmediate()
    {
        if (!isInitialized) { TryInit(); if (!isInitialized) return; }

        if (!panel.activeSelf) panel.SetActive(true);
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void HideImmediate()
    {
        if (!isInitialized) { TryInit(); if (!isInitialized) return; }

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        if (panel && panel.activeSelf) panel.SetActive(false);
    }

    IEnumerator FadeRoutine(bool show)
    {
        // 페이드 중 잠깐 입력 비활성
        cg.interactable = false;
        cg.blocksRaycasts = show;

        float start = cg.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;

        if (show && !panel.activeSelf) panel.SetActive(true);

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = (fadeDuration <= 0f) ? 1f : Mathf.Clamp01(t / fadeDuration);
            cg.alpha = Mathf.SmoothStep(start, end, u);
            yield return null;
        }

        cg.alpha = end;
        cg.interactable = show;
        cg.blocksRaycasts = show;

        if (!show && panel.activeSelf)
            panel.SetActive(false);

        fadeCo = null;
    }
}
