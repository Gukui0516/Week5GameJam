using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] CircleCollider2D checkRadius;
    bool isContact = false;
    private void Awake()
    {
        isContact = false;
    }
    private void Start()
    {
        isContact = false;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CheckContact()//체크할 태그 이름
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius.radius);
        foreach (Collider2D hit in hits)
        {
            
            if (hit.gameObject.CompareTag("Enemy"))
            {
                Debug.Log(hit.gameObject.name);
                if (isContact == false)
                {
                    GetComponentInChildren<Flashlight2D>().isOn = false ;
                    GameManager.Instance.GameOver();
                    isContact = true;
                }

            }
            else if (hit.gameObject.CompareTag("Item"))
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
