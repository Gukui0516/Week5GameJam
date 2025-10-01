using UnityEngine;
using UnityEngine.UI;
using static KeyCollector;

/// <summary>
/// ExitDoor 위에 필요한 키 아이콘들을 시각적으로 표시하는 UI 컴포넌트
/// </summary>
public class ExitDoorKeyUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("연결할 ExitDoor 스크립트")]
    [SerializeField] private ExitDoor exitDoor;
    
    [Header("Key Icon Images")]
    [Tooltip("Circle 키 아이콘 Image")]
    [SerializeField] private Image circleKeyImage;
    [Tooltip("Clover 키 아이콘 Image")]
    [SerializeField] private Image cloverKeyImage;
    [Tooltip("Heart 키 아이콘 Image")]
    [SerializeField] private Image heartKeyImage;
    [Tooltip("Square 키 아이콘 Image")]
    [SerializeField] private Image squareKeyImage;
    
    [Header("Colors")]
    [Tooltip("모든 키 아이콘의 기본 색상 (기본: 검정색)")]
    [SerializeField] private Color iconColor = Color.black;
    
    [Header("Optional: Player Detection")]
    [Tooltip("플레이어가 근처에 있을 때만 표시")]
    [SerializeField] private bool showOnlyWhenPlayerNearby = false;
    [Tooltip("플레이어 감지를 위한 Collider (ExitDoor의 Trigger 사용)")]
    [SerializeField] private Collider2D detectionCollider;
    [SerializeField] private string playerTag = "Player";
    
    private bool playerInRange = false;
    private KeyCollector playerCollector;
    
    private void Awake()
    {
        // ExitDoor가 지정되지 않았으면 부모에서 찾기
        if (exitDoor == null)
            exitDoor = GetComponentInParent<ExitDoor>();
        
        // Collider가 지정되지 않았으면 ExitDoor에서 찾기
        if (detectionCollider == null && exitDoor != null)
            detectionCollider = exitDoor.GetComponent<Collider2D>();
        
        if (exitDoor == null)
        {
            Debug.LogError("ExitDoorKeyUI: ExitDoor를 찾을 수 없습니다!");
        }
        
        // 초기 상태 업데이트
        UpdateKeyIcons();
    }
    
    private void OnEnable()
    {
        UpdateKeyIcons();
    }
    
    private void Update()
    {
        // 플레이어가 근처에 있고 키를 수집하면 실시간으로 UI 업데이트
        if (playerInRange && playerCollector != null)
        {
            UpdateKeyIcons();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        playerInRange = true;
        playerCollector = other.GetComponent<KeyCollector>();
        
        if (showOnlyWhenPlayerNearby)
            gameObject.SetActive(true);
        
        UpdateKeyIcons();
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        playerInRange = false;
        playerCollector = null;
        
        if (showOnlyWhenPlayerNearby)
            gameObject.SetActive(false);
    }
    
    /// <summary>
    /// ExitDoor의 현재 Requirements를 읽어서 키 아이콘 색상 업데이트
    /// </summary>
    public void UpdateKeyIcons()
    {
        if (exitDoor == null) return;
        
        // ExitDoor의 활성 요구사항 가져오기 (Reflection 사용)
        var activeReqs = GetActiveRequirementsFromExitDoor();
        
        // 각 키 종류별로 요구량 확인
        int circleRequired = GetRequiredAmount(activeReqs, KeyKind.Circle);
        int cloverRequired = GetRequiredAmount(activeReqs, KeyKind.Clover);
        int heartRequired = GetRequiredAmount(activeReqs, KeyKind.Heart);
        int squareRequired = GetRequiredAmount(activeReqs, KeyKind.Square);
        
        // 이미지 색상 업데이트
        UpdateImageColor(circleKeyImage, circleRequired > 0);
        UpdateImageColor(cloverKeyImage, cloverRequired > 0);
        UpdateImageColor(heartKeyImage, heartRequired > 0);
        UpdateImageColor(squareKeyImage, squareRequired > 0);
    }
    
    private KeyRequirement[] GetActiveRequirementsFromExitDoor()
    {
        // GetActiveRequirements() 메서드를 호출 (이미 StageTable 처리가 되어있음)
        var method = exitDoor.GetType().GetMethod("GetActiveRequirements", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var result = method.Invoke(exitDoor, null);
            if (result != null)
                return (KeyRequirement[])result;
        }
        
        // Reflection 실패시 빈 배열 반환
        Debug.LogWarning("ExitDoorKeyUI: GetActiveRequirements 호출 실패");
        return new KeyRequirement[0];
    }
    
    private int GetRequiredAmount(KeyRequirement[] requirements, KeyKind kind)
    {
        foreach (var req in requirements)
        {
            if (req.kind == kind)
                return req.amount;
        }
        return 0;
    }
    
    private void UpdateImageColor(Image image, bool isRequired)
    {
        if (image == null) return;
        
        // 기본 색상 유지, 알파값만 변경
        Color newColor = iconColor;
        newColor.a = isRequired ? 1f : 0f; // 필요하면 보이고, 아니면 투명
        image.color = newColor;
    }
    
    // Inspector에서 수동으로 업데이트할 수 있도록
    [ContextMenu("Update Key Icons")]
    private void UpdateKeyIconsMenu()
    {
        UpdateKeyIcons();
    }
}