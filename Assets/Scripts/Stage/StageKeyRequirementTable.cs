using System;
using UnityEngine;
using static KeyCollector;

[CreateAssetMenu(menuName = "Game/Stage Key Requirement Table", fileName = "StageKeyRequirementTable")]
public class StageKeyRequirementTable : ScriptableObject
{
    [Serializable]
    public struct StageRow
    {
        [Min(1)] public int stage;
        public KeyRequirement[] requirements;
    }

    public enum OverflowPolicy
    {
        ClampToLast,     // 최대 단계 요구치를 계속 사용
        RepeatLast,      // 의미상 같음. 필요시 확장 여지
        ScaleByFactor    // 최대 단계 요구치에 배수를 적용
    }

    [Header("Stage rows (보통 1~10)")]
    public StageRow[] rows = new StageRow[10]
    {
        new StageRow{stage=1}, new StageRow{stage=2}, new StageRow{stage=3}, new StageRow{stage=4}, new StageRow{stage=5},
        new StageRow{stage=6}, new StageRow{stage=7}, new StageRow{stage=8}, new StageRow{stage=9}, new StageRow{stage=10},
    };

    [Header("Stages beyond highest row")]
    public OverflowPolicy overflowPolicy = OverflowPolicy.ClampToLast;

    [Tooltip("ScaleByFactor일 때 배수. 예: 2면 10단계 대비 11단계는 x2, 12단계는 x4 ...")]
    [Min(1)] public float overflowScaleFactor = 2f;

    [Tooltip("Scale을 매 단계가 아닌, N단계마다 1회 적용하고 싶다면 설정. 1이면 매 단계 적용.")]
    [Min(1)] public int scaleStep = 1;

    public KeyRequirement[] GetForStage(int stage)
    {
        if (rows == null || rows.Length == 0) return Array.Empty<KeyRequirement>();
        if (stage < 1) stage = 1;

        // 1) 정확히 같은 stage 우선
        for (int i = 0; i < rows.Length; i++)
            if (rows[i].stage == stage && rows[i].requirements != null)
                return Clone(rows[i].requirements);

        // 2) 가장 가까운 '<= stage' 항목 찾기
        int bestIdx = -1;
        int bestStage = int.MinValue;
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].stage <= stage && rows[i].stage > bestStage && rows[i].requirements != null)
            {
                bestStage = rows[i].stage;
                bestIdx = i;
            }
        }

        // 없으면 가장 큰 단계 행 사용
        if (bestIdx < 0)
        {
            int lastIdx = GetLastValidIndex();
            if (lastIdx < 0) return Array.Empty<KeyRequirement>();
            bestIdx = lastIdx;
            bestStage = rows[lastIdx].stage;
        }

        var baseReqs = rows[bestIdx].requirements;
        if (stage <= bestStage || overflowPolicy != OverflowPolicy.ScaleByFactor)
            return Clone(baseReqs);

        // 3) 초과 단계 Scaling
        if (overflowPolicy == OverflowPolicy.ScaleByFactor)
        {
            int stepsBeyond = Mathf.Max(0, stage - bestStage);
            int scales = Mathf.FloorToInt(stepsBeyond / Mathf.Max(1, scaleStep));
            float factor = Mathf.Pow(Mathf.Max(1f, overflowScaleFactor), scales);

            var scaled = new KeyRequirement[baseReqs.Length];
            for (int i = 0; i < baseReqs.Length; i++)
            {
                scaled[i].kind = baseReqs[i].kind;
                scaled[i].amount = Mathf.Max(0, Mathf.RoundToInt(baseReqs[i].amount * factor));
            }
            return scaled;
        }

        // Clamp/Repeat
        return Clone(baseReqs);
    }

    private int GetLastValidIndex()
    {
        int idx = -1;
        int maxStage = int.MinValue;
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].requirements != null && rows[i].stage > maxStage)
            {
                maxStage = rows[i].stage;
                idx = i;
            }
        }
        return idx;
    }

    private static KeyRequirement[] Clone(KeyRequirement[] src)
    {
        if (src == null) return Array.Empty<KeyRequirement>();
        var dst = new KeyRequirement[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            dst[i].kind = src[i].kind;
            dst[i].amount = src[i].amount;
        }
        return dst;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 빈 stage 번호 자동 채우기
        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].stage <= 0) rows[i].stage = i + 1;
        }
    }
#endif
}
