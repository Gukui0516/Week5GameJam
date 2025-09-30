using UnityEngine;

[DisallowMultipleComponent]
public class BGApplier : MonoBehaviour
{
    // worldstatemanager의 이벤트를 받아 카메라 배경색 반전시킴

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // 인스펙터에서 할당
    [SerializeField] private Camera targetCamera;     // 비우면 MainCamera

    [Header("Colors")]
    [SerializeField] private Color normalBg = Color.black;
    [SerializeField] private Color invertedBg = Color.white;

    void Start()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!world || !targetCamera) return;

        Apply(world.BackgroundIsWhite);

    }

    // UnityEvent<bool,bool>에 바인딩해서 호출
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
