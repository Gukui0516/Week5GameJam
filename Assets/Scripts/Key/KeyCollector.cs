using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum KeyKind
{
    Red, Blue, Green, Yellow
}

[DisallowMultipleComponent]
public class KeyCollector : MonoBehaviour
{
    [Serializable] public class KeyCountEvent : UnityEvent<KeyKind, int> { }
    [Serializable] public class AllRequirementEvent : UnityEvent { }
    [Serializable] public class ResetEvent : UnityEvent { }

    [Header("Counts (read-only at runtime)")]
    [SerializeField] private int[] counts = new int[4]; // index = (int)KeyKind

    [Header("Events")]
    [Tooltip("키를 획득할 때 (종류, 현재개수)")]
    public KeyCountEvent onPicked;
    [Tooltip("키를 사용(소모)할 때 (종류, 현재개수)")]
    public KeyCountEvent onUsed;
    [Tooltip("요구치를 모두 충족했을 때(출구가 듣거나, UI가 듣게 해도 됨)")]
    public AllRequirementEvent onAllRequirementsMet;
    [Tooltip("전체 리셋(0개)이 되었을 때")]
    public ResetEvent onReset;

    [Header("Reset Integration with SceneDirector")]
    [Tooltip("GameManager.Instance의 SceneDirector 정보를 사용해 씬 이름 기준으로 리셋한다.")]
    public bool integrateWithSceneDirector = true;
    [Tooltip("타이틀 씬 로드시 0으로 리셋")]
    public bool resetOnTitleLoad = true;
    [Tooltip("게임 씬 로드시 0으로 리셋")]
    public bool resetOnGameLoad = true;
    [Tooltip("그 외 씬 로드시도 0으로 리셋")]
    public bool resetOnOtherLoads = false;
    [Tooltip("리셋 후 각 키에 대해 onUsed(0)를 쏴서 UI를 즉시 동기화")]
    public bool broadcastZeroAfterReset = true;

    public int Get(KeyKind kind) => counts[(int)kind];

    public void Add(KeyKind kind, int amount = 1)
    {
        if (amount <= 0) return;
        int i = (int)kind;
        counts[i] = Mathf.Max(0, counts[i] + amount);
        onPicked?.Invoke(kind, counts[i]);
    }

    public bool CanUse(KeyKind kind, int amount = 1)
    {
        if (amount <= 0) return true;
        return counts[(int)kind] >= amount;
    }

    public bool TryUse(KeyKind kind, int amount = 1)
    {
        if (!CanUse(kind, amount)) return false;
        int i = (int)kind;
        counts[i] -= amount;
        onUsed?.Invoke(kind, counts[i]);
        return true;
    }

    [Serializable]
    public struct KeyRequirement
    {
        public KeyKind kind;
        [Min(0)] public int amount; // 0이면 요구 없음
    }

    public bool CheckRequirements(KeyRequirement[] reqs)
    {
        if (reqs == null || reqs.Length == 0) return true;
        for (int i = 0; i < reqs.Length; i++)
        {
            if (Get(reqs[i].kind) < reqs[i].amount) return false;
        }
        return true;
    }

    public bool TryUseRequirements(KeyRequirement[] reqs)
    {
        if (!CheckRequirements(reqs)) return false;
        // 모두 가능하니 일괄 소모
        for (int i = 0; i < reqs.Length; i++)
        {
            TryUse(reqs[i].kind, reqs[i].amount);
        }
        onAllRequirementsMet?.Invoke();
        return true;
    }

    // ===== SceneDirector 기반 리셋 =====
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (!integrateWithSceneDirector)
        {
            if (resetOnOtherLoads) ResetAllCounts(broadcastZeroAfterReset);
            return;
        }

        string title = null, game = null;
        var gm = GameManager.Instance;
        if (gm != null)
        {
            var dir = gm.GetComponent<SceneDirector>();
            if (dir != null)
            {
                title = dir.TitleSceneName;
                game  = dir.GameSceneName;
            }
        }

        bool matched = false;

        if (!string.IsNullOrEmpty(title) && s.name == title)
        {
            matched = true;
            if (resetOnTitleLoad) ResetAllCounts(broadcastZeroAfterReset);
        }

        if (!string.IsNullOrEmpty(game) && s.name == game)
        {
            matched = true;
            if (resetOnGameLoad) ResetAllCounts(broadcastZeroAfterReset);
        }

        if (!matched && resetOnOtherLoads)
        {
            ResetAllCounts(broadcastZeroAfterReset);
        }
    }

    /// <summary>
    /// 전 키를 0으로 초기화. 필요 시 UI 동기화 이벤트도 전송.
    /// </summary>
    public void ResetAllCounts(bool broadcastZero)
    {
        for (int i = 0; i < counts.Length; i++)
            counts[i] = 0;

        if (broadcastZero && onUsed != null)
            for (int i = 0; i < counts.Length; i++)
                onUsed.Invoke((KeyKind)i, 0);

        onReset?.Invoke();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (counts == null || counts.Length != 4) counts = new int[4];
        for (int i = 0; i < counts.Length; i++)
            counts[i] = Mathf.Max(0, counts[i]);
    }
#endif
}
