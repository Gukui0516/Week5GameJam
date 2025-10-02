// Assets/Scripts/Stage/KeyStageContext.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public static class KeyStageContext
{
    public static int CurrentStage { get; private set; } = 1;

    // 4종 키 중 요구되는 것만 true
    private static readonly bool[] requiredMask = new bool[4];
    public static event Action RequirementsChanged;

    public static void SetRequirements(int stage, IReadOnlyList<KeyKind> requiredKeys)
    {
        CurrentStage = Mathf.Max(1, stage);
        for (int i = 0; i < requiredMask.Length; i++) requiredMask[i] = false;
        if (requiredKeys != null)
        {
            for (int i = 0; i < requiredKeys.Count; i++)
                requiredMask[(int)requiredKeys[i]] = true;
        }
        RequirementsChanged?.Invoke();
    }

    public static bool IsRequired(KeyKind kind) => requiredMask[(int)kind];

    public static KeyKind[] GetRequiredKinds()
    {
        var list = new List<KeyKind>(4);
        for (int i = 0; i < requiredMask.Length; i++)
            if (requiredMask[i]) list.Add((KeyKind)i);
        return list.ToArray();
    }
}
