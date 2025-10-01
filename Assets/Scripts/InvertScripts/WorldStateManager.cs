using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WorldStateManager : MonoBehaviour
{
    // 배경, 손전등 등 반전 상태가 있는 것들 상태
    // 반전시켜주는 기능

    // 디폴트 상태
    [SerializeField] private bool startBackgroundWhite = false; // 기본: 검정 배경
    [SerializeField] private bool startFlashlightBlack = false; // 기본: 하양 손전등

    // Events (Inspector에서 바인딩용)
    [Serializable] public class Bool2Event : UnityEvent<bool, bool> { } // (bgWhite, lightBlack)
    [SerializeField] private Bool2Event onPaletteFlagsChanged;
    [SerializeField] public UnityEvent<bool> onIsInvertedChanged;

    // 상태 플래그
    public bool BackgroundIsWhite { get; private set; }
    public bool FlashlightIsBlack { get; private set; }
    public bool IsInverted => BackgroundIsWhite && FlashlightIsBlack;

    Coroutine invertCo; // 아이템 지속시간

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
