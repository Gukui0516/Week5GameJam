// Assets/Scripts/UI/KeyUI.cs  (전체 교체)
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
    [SerializeField] private Image icon;                       

    [Header("Visual Alpha (0~1)")]
    [Tooltip("요구하지 않는 열쇠")]
    [Range(0,1)] [SerializeField] private float alphaNotRequired = 0f;          // 0
    [Tooltip("요구하지만 아직 미획득")]
    [Range(0,1)] [SerializeField] private float alphaRequiredNotOwned = 30f/255f; // 30
    [Tooltip("요구하고 획득함")]
    [Range(0,1)] [SerializeField] private float alphaRequiredOwned = 1f;        // 255
    [SerializeField] private float fadeDuration = 0.12f;                 

    [Header("Behavior")]
    [SerializeField] private bool resetVisualOnSceneLoad = true;

    private KeyCollector collector;
    private Coroutine fadeCo;
    private WorldStateManager worldStateManager;
    private bool isInverted = false;

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        //TryAutoBindTexts();
    }

    private void OnEnable()
    {
        TryHookCollector();
        TryHookWorldStateManager();
        
        if (resetVisualOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        KeyStageContext.RequirementsChanged += OnRequirementsChanged;

        RefreshFromCollectorAndStage();
    }

    private void OnDisable()
    {
        UnhookCollector();
        UnhookWorldStateManager();
        
        if (resetVisualOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        KeyStageContext.RequirementsChanged -= OnRequirementsChanged;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // 새 씬 기준으로 다시 동기화
        TryHookWorldStateManager();
        RefreshFromCollectorAndStage();
    }

    private void OnRequirementsChanged()
    {
        RefreshFromCollectorAndStage();
    }

    private void TryHookWorldStateManager()
    {
#if UNITY_2023_1_OR_NEWER || UNITY_6_0_OR_NEWER
        worldStateManager = FindFirstObjectByType<WorldStateManager>(FindObjectsInactive.Exclude);
#else
        worldStateManager = FindObjectOfType<WorldStateManager>();
#endif
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInvertedChanged);
            // 현재 상태 즉시 적용
            isInverted = worldStateManager.IsInverted;
            UpdateColor();
        }
    }

    private void UnhookWorldStateManager()
    {
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInvertedChanged);
            worldStateManager = null;
        }
    }

    private void OnInvertedChanged(bool inverted)
    {
        isInverted = inverted;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (icon == null) return;

        Color targetColor = isInverted ? Color.black : Color.white;
        Color currentColor = icon.color;
        icon.color = new Color(targetColor.r, targetColor.g, targetColor.b, currentColor.a);
    }

    private void TryHookCollector()
    {
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
        RefreshFromCollectorAndStage();
    }

    private void RefreshFromCollectorAndStage()
    {
        int count = collector ? collector.Get(keyKind) : 0;
        //if (countText) countText.text = $"x{Mathf.Max(0, count)}";

        bool required = KeyStageContext.IsRequired(keyKind);
        float targetA = required
            ? (count > 0 ? alphaRequiredOwned : alphaRequiredNotOwned)
            : alphaNotRequired;

        FadeTo(targetA);
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
        //float startCountA = countText ? countText.color.a : 0f;
        //float startTitleA = titleText ? titleText.color.a : 0f;

        Color ic = icon ? icon.color : default;
        //Color cc = countText ? countText.color : default;
        //Color tc = titleText ? titleText.color : default;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);

            if (icon)
            {
                Color targetColor = isInverted ? Color.black : Color.white;
                icon.color = new Color(targetColor.r, targetColor.g, targetColor.b, Mathf.Lerp(startIconA, targetA, k));
            }
            //if (countText) countText.color = new Color(cc.r, cc.g, cc.b, Mathf.Lerp(startCountA, targetA, k));
            //if (titleText) titleText.color = new Color(tc.r, tc.g, tc.b, Mathf.Lerp(startTitleA, targetA, k));

            yield return null;
        }
        SetAlphaAll(targetA);
    }

    private void SetAlphaAll(float a)
    {
        if (icon)
        {
            Color targetColor = isInverted ? Color.black : Color.white;
            icon.color = new Color(targetColor.r, targetColor.g, targetColor.b, a);
        }
        //if (countText) countText.color = new Color(countText.color.r, countText.color.g, countText.color.b, a);
        //if (titleText) titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, a);
    }
    /*
    private void TryAutoBindTexts()
    {
        if (countText != null && titleText != null) return;
        var tmps = GetComponentsInChildren<TMP_Text>(true);
        if (tmps == null || tmps.Length == 0) return;
        foreach (var t in tmps)
        {
            string n = t.name.ToLowerInvariant();
            if (countText == null && n.Contains("count")) { countText = t; continue; }
            if (titleText == null && n.Contains("title")) { titleText = t; continue; }
        }
        //if (countText == null) countText = tmps[0];
        if (titleText == null && tmps.Length > 1)
        {
            for (int i = 0; i < tmps.Length; i++)
                if (tmps[i] != countText) { titleText = tmps[i]; break; }
        }
    }*/

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (icon == null) icon = GetComponent<Image>();
        //TryAutoBindTexts();
    }
#endif
}