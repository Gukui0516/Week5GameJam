using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    #region Variables

    [SerializeField] private float speed = 3f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [SerializeField] private WorldStateManager worldStateManager;
    [SerializeField] private Flashlight2D flashlight;

    private Transform player;
    private bool isInLight = false; //손전등 빛에 있을 때 정지

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }

        if (flashlight == null)
        {
            flashlight = FindFirstObjectByType<Flashlight2D>();
        }

        if (flashlight != null)
        {
            flashlight.OnTargetEnter.AddListener(OnEnteredLight);
            flashlight.OnTargetExit.AddListener(OnExitedLight);
        }
    }

    void OnDestroy()
    {
        if (flashlight != null)
        {
            flashlight.OnTargetEnter.RemoveListener(OnEnteredLight);
            flashlight.OnTargetExit.RemoveListener(OnExitedLight);
        }
    }

    void Update()
    {
        // TODO: 플레이어 구현 후 null 체크 제거
        if (player == null) return;

        if (IsStoppedByInversion()) return;
        if (isInLight) return;

        MoveTowardsPlayer();
    }

    #endregion

    #region Movement
    private bool IsStoppedByInversion()
    {
        return worldStateManager != null && worldStateManager.IsInverted;
    }

    private void MoveTowardsPlayer()
    {
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }
    #endregion


    #region Flashlight Events

    private void OnEnteredLight(Collider2D col)
    {
        if (col.gameObject == gameObject)
        {
            isInLight = true;
            Debug.Log($"{gameObject.name} 손전등 진입!");
        }
    }

    private void OnExitedLight(Collider2D col)
    {
        if (col.gameObject == gameObject)
        {
            isInLight = false;
            Debug.Log($"{gameObject.name} 손전등 벗어남!");
        }
    }

    #endregion

    #region Public Methods

    public void Die()
    {
        EnemySpawner.Instance.ReturnEnemy(gameObject);
    }

    #endregion
}