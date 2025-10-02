using UnityEngine;
using UnityEngine.UI;

public class ExitNavigate : MonoBehaviour
{
    [SerializeField] GameObject exit;
    [SerializeField] private WorldStateManager worldStateManager;
    Transform player;
    private Image uiImage;
    private Image uiImageChild;
    Vector2 length;
    [SerializeField] bool isOnScreen;//출구가 화면 안에 있는가
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        uiImage = GetComponent<Image>();
        uiImageChild = transform.GetChild(0).GetComponent<Image>();
        length = GetComponent<RectTransform>().pivot;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        ExitScreenIn();
        NavigateExit();
    }
    public void NavigateExit() 
    {
        if (worldStateManager.IsInverted)
        {
            uiImageChild.enabled = true;
            uiImage.enabled = true;
            Vector2 dir = player.transform.position - exit.transform.position;

            // 거리
            float distance = dir.magnitude;
            // 각도 (라디안 -> 도 단위 변환)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.eulerAngles = new Vector3(0, 0, angle + 90f);
        }
        else
        {
            uiImageChild.enabled = false;
            uiImage.enabled = false;
        }
        if (isOnScreen)
        {
            uiImageChild.enabled = false;
            uiImage.enabled = false; 
        }
    }
    public void ExitScreenIn()
    {

        Camera cam = Camera.main;
        Vector2 screenPos = cam.WorldToScreenPoint(exit.transform.position);

        // 화면 앞에 있는지, 스크린 범위 안에 있는지 체크
        isOnScreen = screenPos.x >= 0 && screenPos.x <= Screen.width &&
                          screenPos.y >= 0 && screenPos.y <= Screen.height;
    }

}
