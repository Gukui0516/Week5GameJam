using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] CircleCollider2D checkRadius;
    private void Awake()
    {
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CheckContact()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius.radius);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.CompareTag("Enemy"))
            {
                Debug.Log("Contact Enemy");
                gameObject.SetActive(false);
            }
        }
    }
    private void Update()
    {
        CheckContact();
    }
}
