using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class KeyUI : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private KeyKind keyKind = KeyKind.Red;
    [SerializeField] private Image icon;                 // 이 오브젝트의 Image
    [SerializeField] private TMP_Text countText;         // 자식 TMP_Text

    [Header("Visual")]
    [Range(0,1)] [SerializeField] private float inactiveAlpha = 50f/255f;
    [Range(0,1)] [SerializeField] private float activeAlpha   = 1f;
    [SerializeField] private float fadeDuration = 0.12f; // 짧게 페이드

    [Header("Behavior")]
    [Tooltip("씬 전환 시 UI 표시를 0개 상태로 초기화")]
    [SerializeField] private bool resetVisualOnSceneLoad = true;

    private KeyCollector collector;
    private Coroutine fadeCo;

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        if (countText == null) countText = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        TryHookCollector();
        if (resetVisualOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;
        // 첫 프레임에 동기화
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
        SetIconAlpha(value > 0 ? activeAlpha : inactiveAlpha);
    }

    private void FadeTo(float targetA)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeIconAlpha(targetA, fadeDuration));
    }

    private IEnumerator FadeIconAlpha(float targetA, float dur)
    {
        if (icon == null) yield break;
        Color c = icon.color;
        float startA = c.a;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetA, t / dur);
            icon.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        icon.color = new Color(c.r, c.g, c.b, targetA);
    }

    private void SetIconAlpha(float a)
    {
        if (icon == null) return;
        Color c = icon.color;
        icon.color = new Color(c.r, c.g, c.b, a);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (icon == null) icon = GetComponent<Image>();
        if (countText == null) countText = GetComponentInChildren<TMP_Text>(true);
    }
#endif
}
