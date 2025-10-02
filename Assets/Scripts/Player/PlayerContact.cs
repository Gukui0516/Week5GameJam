using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] CircleCollider2D checkRadius;
    bool isContact = false;
    LayerMask enemyLayer;
    bool isInverted = false;
    private WorldStateManager worldStateManager;
    private void Awake()
    {
        isContact = false;
        worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager == null)
        {
            Debug.LogError("WorldStateManager not found");
        }
    }
    private void Start()
    {
        isContact = false;
        if(worldStateManager == null) worldStateManager = FindFirstObjectByType<WorldStateManager>();
        if (worldStateManager == null)
        {
            Debug.LogError("WorldStateManager not found");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CheckContact()//체크할 태그 이름
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius.radius);
        foreach (Collider2D hit in hits)
        {
            if(isContact) break;
            if (hit.gameObject.layer == enemyLayer)
            {
                if (hit.gameObject.CompareTag("Enemy"))
                {
                    if (worldStateManager.IsInverted == true)
                        return;
                    Debug.Log(hit.gameObject.name);
                    GetComponentInChildren<Flashlight2D>().isOn = false ;
                    GameManager.Instance.GameOver();
                    isContact = true;
                }
                else if (hit.gameObject.CompareTag("EnemyWhite"))
                {
                    //Debug.Log(hit.gameObject.name);
                    GetComponentInChildren<Flashlight2D>().isOn = false ;
                    GameManager.Instance.GameOver();
                    isContact = true;
                }

            }
            if (hit.gameObject.CompareTag("Item"))
            {
                Debug.Log("아이템 " + hit.name + " 획득");
                Item item = hit.GetComponent<Item>();
                if (item != null)
                {
                    item.ActiveItem();
                }
                else
                {
                    Debug.Log("아이템 연결 안됨");
                }
                //아이템 효과 발동 시키는 코드
            }

        }
    }
    private void Update()
    {
        CheckContact();
    }
}
