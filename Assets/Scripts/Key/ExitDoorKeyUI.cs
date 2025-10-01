// Assets/Scripts/UI/ExitDoorKeyUI.cs  (전체 교체)
using UnityEngine;
using UnityEngine.UI;

public class ExitDoorKeyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ExitDoor exitDoor;

    [Header("Key Icon Images")]
    [SerializeField] private Image circleKeyImage;
    [SerializeField] private Image cloverKeyImage;
    [SerializeField] private Image heartKeyImage;
    [SerializeField] private Image squareKeyImage;

    [Header("Colors")]
    [SerializeField] private Color baseColor = Color.white; // 알파만 바꾼다

    [Header("Optional: Player Detection")]
    [SerializeField] private bool showOnlyWhenPlayerNearby = false;
    [SerializeField] private Collider2D detectionCollider; // ExitDoor 트리거 재사용 권장
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange = false;

    private void Awake()
    {
        if (!exitDoor) exitDoor = GetComponentInParent<ExitDoor>();
        if (!detectionCollider && exitDoor) detectionCollider = exitDoor.GetComponent<Collider2D>();
        UpdateIcons();
    }

    private void OnEnable()
    {
        KeyStageContext.RequirementsChanged += UpdateIcons;
        UpdateIcons();
    }

    private void OnDisable()
    {
        KeyStageContext.RequirementsChanged -= UpdateIcons;
    }

    private void Update()
    {
        if (playerInRange) UpdateIcons();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!showOnlyWhenPlayerNearby) return;
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        gameObject.SetActive(true);
        UpdateIcons();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!showOnlyWhenPlayerNearby) return;
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        gameObject.SetActive(false);
    }

    public void UpdateIcons()
    {
        SetIcon(circleKeyImage,  KeyStageContext.IsRequired(KeyKind.Circle));
        SetIcon(cloverKeyImage,  KeyStageContext.IsRequired(KeyKind.Clover));
        SetIcon(heartKeyImage,   KeyStageContext.IsRequired(KeyKind.Heart));
        SetIcon(squareKeyImage,  KeyStageContext.IsRequired(KeyKind.Square));
    }

    public void HideAll()
    {
        SetIcon(circleKeyImage, false);
        SetIcon(cloverKeyImage, false);
        SetIcon(heartKeyImage, false);
        SetIcon(squareKeyImage, false);
    }

    private void SetIcon(Image img, bool required)
    {
        if (!img) return;
        var c = baseColor;
        c.a = required ? 1f : 0f; // 문 위 UI: 요구면 255, 아니면 0
        img.color = c;
    }
}
