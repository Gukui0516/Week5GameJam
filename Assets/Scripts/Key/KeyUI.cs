using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class KeyUI : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private KeyKind keyKind = KeyKind.Clover;
    [SerializeField] private Image icon;                  // 아이콘 이미지
    [SerializeField] private TMP_Text countText;          // 수량 텍스트 (예: x0)
    [SerializeField] private TMP_Text titleText;          // 타이틀 텍스트 (예: CLOVER)

    [Header("Visual")]
    [Range(0,1)] [SerializeField] private float inactiveAlpha = 0f;     // 미보유 시 투명도
    [Range(0,1)] [SerializeField] private float activeAlpha   = 1f;     // 보유 시 투명도
    [SerializeField] private float fadeDuration = 0.12f;                 // 페이드 시간

    [Header("Behavior")]
    [Tooltip("씬 전환 시 UI 표시를 0개 상태로 초기화")]
    [SerializeField] private bool resetVisualOnSceneLoad = true;

    private KeyCollector collector;
    private Coroutine fadeCo;

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        TryAutoBindTexts(); // count/title 자동 매핑 시도
    }

    private void OnEnable()
    {
        TryHookCollector();
        if (resetVisualOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        // 첫 프레임 동기화
        RefreshFromCollector();
    }

    private void OnDisable()
    {
        UnhookCollector();
        if (resetVisualOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 표시만 0으로 리셋
        SetCountInstant(0);
        // 새 씬의 수집가를 다시 찾고 동기화
        UnhookCollector();
        TryHookCollector();
        RefreshFromCollector();
    }

    private void TryHookCollector()
    {
        if (collector != null) return;
#if UNITY_2023_1_OR_NEWER || UNITY_6_0_OR_NEWER
        collector = FindFirstObjectByType<KeyCollector>(FindObjectsInactive.Exclude);
#else
        collector = FindObjectOfType<KeyCollector>();
#endif
        if (collector == null) return;

        collector.onPicked.AddListener(OnKeyChanged);
        collector.onUsed.AddListener(OnKeyChanged);
    }

    private void UnhookCollector()
    {
        if (collector == null) return;
        collector.onPicked.RemoveListener(OnKeyChanged);
        collector.onUsed.RemoveListener(OnKeyChanged);
        collector = null;
    }

    private void OnKeyChanged(KeyKind kind, int newCount)
    {
        if (kind != keyKind) return;
        SetCount(newCount);
    }

    private void RefreshFromCollector()
    {
        if (collector == null)
        {
            // 수집가가 아직 없으면 비활성 비주얼
            SetCountInstant(0);
            return;
        }
        SetCountInstant(collector.Get(keyKind));
    }

    private void SetCount(int value)
    {
        if (countText != null) countText.text = $"x{Mathf.Max(0, value)}";
        float targetA = value > 0 ? activeAlpha : inactiveAlpha;
        FadeTo(targetA);
    }

    private void SetCountInstant(int value)
    {
        if (countText != null) countText.text = $"x{Mathf.Max(0, value)}";
        SetAlphaAll(value > 0 ? activeAlpha : inactiveAlpha);
    }

    private void FadeTo(float targetA)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeVisualAlpha(targetA, fadeDuration));
    }

    private IEnumerator FadeVisualAlpha(float targetA, float dur)
    {
        float t = 0f;

        float startIconA = icon ? icon.color.a : 0f;
        float startCountA = countText ? countText.color.a : 0f;
        float startTitleA = titleText ? titleText.color.a : 0f;

        Color ic = icon ? icon.color : default;
        Color cc = countText ? countText.color : default;
        Color tc = titleText ? titleText.color : default;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);

            if (icon)
                icon.color = new Color(ic.r, ic.g, ic.b, Mathf.Lerp(startIconA, targetA, k));
            if (countText)
                countText.color = new Color(cc.r, cc.g, cc.b, Mathf.Lerp(startCountA, targetA, k));
            if (titleText)
                titleText.color = new Color(tc.r, tc.g, tc.b, Mathf.Lerp(startTitleA, targetA, k));

            yield return null;
        }

        SetAlphaAll(targetA);
    }

    private void SetAlphaAll(float a)
    {
        SetIconAlpha(a);
        SetTextAlpha(countText, a);
        SetTextAlpha(titleText, a);
    }

    private void SetIconAlpha(float a)
    {
        if (!icon) return;
        Color c = icon.color;
        icon.color = new Color(c.r, c.g, c.b, a);
    }

    private void SetTextAlpha(TMP_Text t, float a)
    {
        if (!t) return;
        Color c = t.color;
        t.color = new Color(c.r, c.g, c.b, a);
    }

    private void TryAutoBindTexts()
    {
        // 인스펙터로 이미 꽂혀 있으면 존중
        if (countText != null && titleText != null) return;

        var tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps == null || tmps.Length == 0) return;

        // 이름 힌트 기반 매핑
        foreach (var t in tmps)
        {
            string n = t.name.ToLowerInvariant();
            if (countText == null && n.Contains("count")) { countText = t; continue; }
            if (titleText == null && n.Contains("title")) { titleText = t; continue; }
        }

        // 여전히 비어 있으면 순서로 보정
        if (countText == null) countText = tmps[0];
        if (titleText == null && tmps.Length > 1)
        {
            // 다른 것 하나를 타이틀로
            for (int i = 0; i < tmps.Length; i++)
            {
                if (tmps[i] != countText) { titleText = tmps[i]; break; }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (icon == null) icon = GetComponent<Image>();
        TryAutoBindTexts();
    }
#endif
}
