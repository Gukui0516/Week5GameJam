using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType
    {
        Normal,      // 평소 움직임, 손전등에 멈춤
        LightSeeker  // 손전등 비춰질 때만 움직임
    }

    #region Variables

    [Header("Enemy Type")]
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;

    [SerializeField] private float speed = 3f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Visibility Settings")]
    [SerializeField] private bool useOutline = false; // 아웃라인 사용 여부
    [SerializeField] private float eyesVisibleDistance = 10f; // Eyes가 보이는 거리

    [SerializeField] private WorldStateManager worldStateManager;

    [Header("Outline Settings")]
    [SerializeField] private EnemyOutline enemyOutline; // EnemyOutline 컴포넌트 참조

    [Header("Eyes Settings")]
    [SerializeField] private GameObject eyesObject; // Eyes 게임오브젝트 참조

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

        // EnemyOutline 컴포넌트 자동 찾기
        if (enemyOutline == null)
        {
            enemyOutline = GetComponent<EnemyOutline>();
        }

        // Eyes 오브젝트 자동 찾기
        if (eyesObject == null)
        {
            Transform eyesTransform = transform.Find("Eyes");
            if (eyesTransform != null)
            {
                eyesObject = eyesTransform.gameObject;
            }
        }

        // 초기 설정
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(true); // 아웃라인 사용하면 항상 켜짐
            enemyOutline.SetOutlineColor(Color.black); //기본색상 설정
        }
        else if (enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false); // 아웃라인 사용 안하면 꺼짐
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(false); // 처음엔 꺼진 상태
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
        // 거리에 따라 Eyes 표시 여부 결정
        UpdateVisibility();

        if (IsStoppedByInversion()) return;

        // Enum 타입에 따라 움직임 결정
        if (ShouldMove())
        {
            MoveTowardsPlayer();
        }
    }

    #endregion

    #region Movement

    // Enum 타입에 따라 움직여야 하는지 판단
    private bool ShouldMove()
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
                return !isInLight; // 손전등 없을 때 움직임

            case EnemyType.LightSeeker:
                return isInLight; // 손전등 비춰질 때만 움직임

            default:
                return false;
        }
    }

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

    #region Visibility Management

    // 거리와 손전등 상태에 따라 Eyes 표시
    private void UpdateVisibility()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Eyes 업데이트
        UpdateEyesVisibility(distance);
    }

    // Eyes 업데이트 - 거리 안에 있거나 OR 손전등에 비춰질 때 켜짐
    private void UpdateEyesVisibility(float distance)
    {
        if (eyesObject == null) return;

        bool shouldBeActive = distance <= eyesVisibleDistance || isInLight;

        if (shouldBeActive != eyesObject.activeSelf)
        {
            eyesObject.SetActive(shouldBeActive);
        }
    }

    // 현재 상태에 맞는 아웃라인 색상 업데이트
    private void UpdateOutlineColor()
    {
        if (enemyOutline == null) return;
        else if (isInverted)
        {
            enemyOutline.SetOutlineColor(Color.white);
        }
        else
        {
            enemyOutline.SetOutlineColor(Color.black);
        }
    }

    #endregion

    #region World State Events

    private void OnInversionChanged(bool inverted)
    {
        isInverted = inverted;
        Debug.Log($"{gameObject.name} 반전 상태: {inverted}");

        // 반전 상태에 따라 아웃라인 색상 변경 (useOutline이 true일 때만)
        if (useOutline && enemyOutline != null)
        {
            UpdateOutlineColor();
        }
    }

    #endregion

    #region Flashlight Events

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Flashlight"))
        {
            isInLight = true;
            Debug.Log($"{gameObject.name} 손전등 진입!");

            // 손전등에 비춰지면 노란색 아웃라인 (useOutline이 true일 때만)
            if (useOutline && enemyOutline != null)
            {
                enemyOutline.SetOutlineColor(Color.black);
            }

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

            // 손전등에서 벗어나면 원래 색상으로 복구 (useOutline이 true일 때만)
            if (useOutline && enemyOutline != null)
            {
                UpdateOutlineColor();
            }
        }
    }

    #endregion

    #region Public Methods

    public void Die() //적이 죽으면 오브젝트 풀에 반환
    {
        // 죽을 때 아웃라인과 Eyes 끄기
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false);
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(false);
        }

        EnemySpawner.Instance.ReturnEnemy(gameObject);
    }

    #endregion
}
