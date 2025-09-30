using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WorldStateManager : MonoBehaviour
{
    // 배경의 상태, 손전등의 상태, 반전되었는지 상태 체크
    // 색상 반전, 몇초 후에 원상 복귀 기능

    // 기본 상태
    [SerializeField] private bool startBackgroundWhite = false; // 기본: 검정 배경
    [SerializeField] private bool startFlashlightBlack = false; // 기본: 하양 손전등

    // Events (Inspector 바인딩용)
    [Serializable] public class Bool2Event : UnityEvent<bool, bool> { } // (bgWhite, lightBlack)
    [SerializeField] private Bool2Event onPaletteFlagsChanged;
    [SerializeField] private UnityEvent<bool> onIsInvertedChanged;

    // 읽기 전용 상태
    public bool BackgroundIsWhite { get; private set; }
    public bool FlashlightIsBlack { get; private set; }
    public bool IsInverted => BackgroundIsWhite && FlashlightIsBlack;

    Coroutine invertCo; // 아이템 지속 시간 끝나면 자동으로 원래 상태로 복귀

    void Awake()
    {
        SetPalette(startBackgroundWhite, startFlashlightBlack);
    }

    public void SetPalette(bool bgWhite, bool lightBlack)
    {
        bool prevInverted = IsInverted;

        BackgroundIsWhite = bgWhite;
        FlashlightIsBlack = lightBlack;

        onPaletteFlagsChanged?.Invoke(BackgroundIsWhite, FlashlightIsBlack);

        if (prevInverted != IsInverted)
            onIsInvertedChanged?.Invoke(IsInverted);
    }

    public void ActivateInversion(float duration)
    {
        SetPalette(true, true);

        if (invertCo != null) StopCoroutine(invertCo);
        invertCo = StartCoroutine(Co_RevertAfter(duration));
    }

    public void CancelInversion()
    {
        if (invertCo != null) StopCoroutine(invertCo);
        invertCo = null;
        SetPalette(false, false);
    }

    IEnumerator Co_RevertAfter(float t)
    {
        yield return new WaitForSeconds(t);
        CancelInversion();
    }
}
