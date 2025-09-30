using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    #region Variables

    [SerializeField] private float speed = 3f;
    [SerializeField] private float stoppingDistance = 1.5f;

    private Transform player;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        // TODO: 플레이어 구현 후 null 체크 제거
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }

    #endregion

    #region Movement

    // TODO: 손전등 빛에 있을 때 정지
    private void StopInLight()
    {
        // 손전등 코드 구현 후 작성
    }

    #endregion

    #region Public Methods

    public void Die()
    {
        EnemySpawner.Instance.ReturnEnemy(gameObject);
    }

    #endregion
}