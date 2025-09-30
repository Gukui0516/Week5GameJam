using UnityEngine;

public class PlayerContact : MonoBehaviour
{
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float checkRadius = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void CheckContact()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, checkRadius, 0);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.CompareTag("Enemy"))
            {
                gameObject.SetActive(false);
            }
        }
    }
    private void Update()
    {
        
    }
}
