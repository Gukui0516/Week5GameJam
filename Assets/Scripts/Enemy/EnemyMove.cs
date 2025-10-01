using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    #region Variables

    [SerializeField] private float speed = 3f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [SerializeField] private WorldStateManager worldStateManager;

    private Transform player;
    private bool isInLight = false; //손전등 빛에 있을 때 정지
    private bool isInverted = false; //반전 상태 추적

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }

        // 이벤트 구독
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.AddListener(OnInversionChanged);
            // 초기 상태 동기화
            isInverted = worldStateManager.IsInverted;
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (worldStateManager != null)
        {
            worldStateManager.onIsInvertedChanged.RemoveListener(OnInversionChanged);
        }
    }

    void Update()
    {
        if (IsStoppedByInversion()) return;
        if (isInLight) return;

        MoveTowardsPlayer();
    }

    #endregion

    #region Movement
    private bool IsStoppedByInversion() //배경 바뀌면 멈춤
    {
        return worldStateManager != null && worldStateManager.IsInverted;
    }

    private void MoveTowardsPlayer() //플레이어 향해 이동
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }
    #endregion

    #region World State Events

    private void OnInversionChanged(bool inverted)
    {
        isInverted = inverted;
        Debug.Log($"{gameObject.name} 반전 상태: {inverted}");
    }

    #endregion


    #region Flashlight Events

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = true;
            Debug.Log($"{gameObject.name} 손전등 진입!");

            if (isInverted)
            {
                Die();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = false;
            Debug.Log($"{gameObject.name} 손전등 벗어남!");
        }
    }

    #endregion

    #region Public Methods

    public void Die() //적이 죽으면 오브젝트 풀에 반환
    {
        EnemySpawner.Instance.ReturnEnemy(gameObject);
    }

    #endregion
}