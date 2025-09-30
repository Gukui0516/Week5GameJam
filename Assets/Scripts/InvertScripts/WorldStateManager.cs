using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WorldStateManager : MonoBehaviour
{
    // ����� ����, �������� ����, �����Ǿ����� ���� üũ
    // ���� ����, ���� �Ŀ� ���� ���� ���

    // �⺻ ����
    [SerializeField] private bool startBackgroundWhite = false; // �⺻: ���� ���
    [SerializeField] private bool startFlashlightBlack = false; // �⺻: �Ͼ� ������

    // Events (Inspector ���ε���)
    [Serializable] public class Bool2Event : UnityEvent<bool, bool> { } // (bgWhite, lightBlack)
    [SerializeField] private Bool2Event onPaletteFlagsChanged;
    [SerializeField] public UnityEvent<bool> onIsInvertedChanged;

    // �б� ���� ����
    public bool BackgroundIsWhite { get; private set; }
    public bool FlashlightIsBlack { get; private set; }
    public bool IsInverted => BackgroundIsWhite && FlashlightIsBlack;

    Coroutine invertCo; // ������ ���� �ð� ������ �ڵ����� ���� ���·� ����

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
