using UnityEngine;

[DisallowMultipleComponent]
public class BGApplier : MonoBehaviour
{
    // worldstatemanager�� �̺�Ʈ�� �޾� ī�޶� ���� ������Ŵ

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // �ν����Ϳ��� �Ҵ�
    [SerializeField] private Camera targetCamera;     // ���� MainCamera

    [Header("Colors")]
    [SerializeField] private Color normalBg = Color.black;
    [SerializeField] private Color invertedBg = Color.white;

    void Start()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!world || !targetCamera) return;

        Apply(world.BackgroundIsWhite);

    }

    // UnityEvent<bool,bool>�� ���ε��ؼ� ȣ��
    public void OnFlags(bool bgWhite, bool lightBlack)
    {
        Apply(bgWhite);
    }

    void Apply(bool bgWhite)
    {
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = bgWhite ? invertedBg : normalBg;
    }
}
