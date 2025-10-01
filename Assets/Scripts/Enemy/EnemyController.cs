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

    [Header("LightSeeker Speed Settings")]
    [SerializeField] private float lightSeekerBaseSpeed = 2f;
    [SerializeField] private float speedIncreaseRate = 2f; // speedIncreaseInterval마다 2씩 증가
    [SerializeField] private float speedIncreaseInterval = 2f;
    [SerializeField] private float maxSpeed = 8f;

    [Header("Despawn Settings")]
    [SerializeField] private float despawnDistance = 25f; // 플레이어와 멀어지면 반환되는 거리
    [SerializeField] private float despawnCheckInterval = 1f; // 거리 체크 주기

    [Header("Visibility Settings")]
    [SerializeField] private bool useOutline = false; // 아웃라인 사용 여부
    [SerializeField] private float eyesVisibleDistance = 10f; // Eyes가 보이는 거리
    [SerializeField] private float lightSeekerVisibilityDistance = 10f; // LightSeeker가 흰색으로 보이는 거리

    [SerializeField] private WorldStateManager worldStateManager;

    [Header("Outline Settings")]
    [SerializeField] private EnemyOutline enemyOutline; // EnemyOutline 컴포넌트 참조

    [Header("Eyes Settings")]
    [SerializeField] private GameObject eyesObject; // Eyes 게임오브젝트 참조

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true; // 회전 활성화 여부
    [SerializeField] private float rotationSpeed = 10f; // 회전 속도 (부드러운 회전을 원하면 낮은 값, 즉시 회전은 높은 값)
    [SerializeField] private float rotationOffset = 180f; // 스프라이트가 아래를 보고 있으면 180

    private Transform player;
    private bool isInLight = false; //손전등 빛에 있을 때 정지
    private bool isInverted = false; //반전 상태 추적

    // LightSeeker 속도 관련 변수
    private float currentSpeed;
    private float timeInLight = 0f;

    // 거리 체크 관련 변수
    private float nextDespawnCheckTime;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        // 참조 찾기는 Awake에서 한 번만
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
    }

    void OnEnable()
    {
        // 풀에서 재활성화될 때마다 초기 상태 설정
        InitializeEnemy();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

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
        // 플레이어와의 거리 체크 (일정 간격마다)
        CheckDespawnDistance();

        // 거리에 따라 Eyes 표시 여부 결정
        UpdateVisibility();

        // Normal은 반전시 완전 정지 (회전도 멈춤)
        if (IsStoppedByInversion()) return;

        // 회전 조건: LightSeeker는 반전 상태에 따라, Normal은 손전등 밖에서만
        if (enableRotation && player != null && ShouldRotate())
        {
            RotateTowardsPlayer();
        }

        // LightSeeker 타입이고 손전등 안에 있을 때 속도 증가 (반전 아닐때만)
        if (enemyType == EnemyType.LightSeeker && isInLight && !isInverted)
        {
            UpdateLightSeekerSpeed();
        }

        // Enum 타입에 따라 움직임 결정
        if (ShouldMove())
        {
            MoveTowardsPlayer();
        }
    }

    #endregion

    #region Initialization

    // 적이 스폰되거나 재활성화될 때 초기 상태 설정
    private void InitializeEnemy()
    {
        // 아웃라인 초기 설정
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(true); // 아웃라인 사용하면 항상 켜짐
            UpdateOutlineColor(); // 초기 색상 설정
        }
        else if (enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false); // 아웃라인 사용 안하면 꺼짐
        }

        // Eyes 초기 설정
        if (eyesObject != null)
        {
            eyesObject.SetActive(false); // 처음엔 꺼진 상태
        }

        // 속도 초기화
        if (enemyType == EnemyType.LightSeeker)
        {
            currentSpeed = lightSeekerBaseSpeed;
        }
        else
        {
            currentSpeed = speed;
        }

        // 상태 초기화
        isInLight = false;
        timeInLight = 0f;

        // 거리 체크 타이머 초기화
        nextDespawnCheckTime = Time.time + despawnCheckInterval;
    }

    #endregion

    #region Despawn Check

    // 플레이어와 거리가 멀어지면 풀에 반환
    private void CheckDespawnDistance()
    {
        // 일정 간격마다만 체크 (성능 최적화)
        if (Time.time < nextDespawnCheckTime) return;
        nextDespawnCheckTime = Time.time + despawnCheckInterval;

        if (player == null) return;

        // sqrMagnitude 사용으로 성능 최적화 (제곱근 계산 생략)
        float distanceSqr = (transform.position - player.position).sqrMagnitude;

        if (distanceSqr > despawnDistance * despawnDistance)
        {
            Despawn();
        }
    }

    // 거리가 멀어져서 반환되는 경우
    private void Despawn()
    {
        // 아웃라인과 Eyes 끄기
        if (useOutline && enemyOutline != null)
        {
            enemyOutline.SetOutlineVisible(false);
        }

        if (eyesObject != null)
        {
            eyesObject.SetActive(false);
        }

        // LightSeeker의 경우 속도 초기화
        if (enemyType == EnemyType.LightSeeker)
        {
            currentSpeed = lightSeekerBaseSpeed;
            timeInLight = 0f;
        }

        // 손전등 상태 초기화
        isInLight = false;

        // 풀에 반환
        EnemySpawner.Instance?.ReturnEnemy(gameObject);
    }

    #endregion

    #region Movement & Rotation

    // Enum 타입에 따라 움직여야 하는지 판단
    private bool ShouldMove()
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
                return !isInLight; // 손전등 없을 때 움직임

            case EnemyType.LightSeeker:
                // 반전 상태일 때는 Normal처럼 행동 (손전등 밖에서 움직임)
                if (isInverted)
                {
                    return !isInLight;
                }
                // 평소에는 손전등 비춰질 때만 움직임
                return isInLight;

            default:
                return false;
        }
    }

    // 배경 바뀌면 멈춤 (LightSeeker는 멈추지 않고 패턴만 변경)
    private bool IsStoppedByInversion()
    {
        // LightSeeker는 반전 상태에서도 계속 움직임 (패턴만 변경)
        if (enemyType == EnemyType.LightSeeker)
        {
            return false;
        }

        return worldStateManager != null && worldStateManager.IsInverted;
    }

    // 회전 조건 판단
    private bool ShouldRotate()
    {
        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태일 때는 Normal처럼 손전등 밖에서만 회전
            if (isInverted)
            {
                return !isInLight;
            }
            // 평소에는 항상 회전
            return true;
        }

        return !isInLight; // Normal은 손전등 밖에서만 회전
    }

    private void MoveTowardsPlayer() //플레이어 향해 이동
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > stoppingDistance)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            transform.position += (Vector3)(direction * currentSpeed * Time.deltaTime);
        }
    }

    // 플레이어 방향으로 회전 (움직임과 별개로 조건에 맞을 때 실행)
    private void RotateTowardsPlayer()
    {
        // 플레이어 방향 벡터 계산
        Vector2 direction = (player.position - transform.position).normalized;

        // direction 벡터로부터 각도 계산
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 스프라이트의 기본 방향 오프셋 적용 (아래를 보고 있으므로 180도 추가)
        targetAngle -= rotationOffset;

        // 현재 회전값
        float currentAngle = transform.eulerAngles.z;

        // 부드러운 회전
        float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);

        // 회전 적용 (Z축만 회전)
        transform.rotation = Quaternion.Euler(0, 0, smoothAngle);
    }

    // LightSeeker의 속도를 손전등에 있는 시간에 따라 지수적으로 증가
    private void UpdateLightSeekerSpeed()
    {
        timeInLight += Time.deltaTime;

        // 지수적 증가: baseSpeed * (multiplier ^ 경과 인터벌 수)
        int intervals = Mathf.FloorToInt(timeInLight / speedIncreaseInterval);
        float speedMultiplier = Mathf.Pow(speedIncreaseRate, intervals);

        currentSpeed = Mathf.Min(lightSeekerBaseSpeed * speedMultiplier, maxSpeed);
    }

    // 선형 증가 버전 (주석 처리)
    // private void UpdateLightSeekerSpeed()
    // {
    //     timeInLight += Time.deltaTime;
    //     // speedIncreaseInterval마다 speedIncreaseRate씩 증가
    //     float speedBonus = Mathf.Floor(timeInLight / speedIncreaseInterval) * speedIncreaseRate;
    //     currentSpeed = Mathf.Min(lightSeekerBaseSpeed + speedBonus, maxSpeed);
    // }

    #endregion

    #region Visibility Management

    // 거리에 따라 Eyes 표시
    private void UpdateVisibility()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Eyes 업데이트
        UpdateEyesVisibility(distance);
        UpdateEyesColor(); // Eyes 색상 업데이트

        // 아웃라인 색상 업데이트 (거리에 따라)
        if (useOutline && enemyOutline != null)
        {
            UpdateOutlineColor();
        }
    }

    // Eyes 업데이트 - 거리에 따라 표시
    private void UpdateEyesVisibility(float distance)
    {
        if (eyesObject == null) return;

        bool shouldBeActive;

        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태일 때는 거리 상관없이 항상 보임
            if (isInverted)
            {
                shouldBeActive = true;
            }
            // 평소: 손전등 안에 있거나 거리 내에 있으면 눈 보임
            else
            {
                shouldBeActive = isInLight || distance <= lightSeekerVisibilityDistance;
            }
        }
        else
        {
            // Normal: 거리 안에 있을 때만 켜짐
            shouldBeActive = distance <= eyesVisibleDistance;
        }

        if (shouldBeActive != eyesObject.activeSelf)
        {
            eyesObject.SetActive(shouldBeActive);
        }
    }

    // Eyes 색상 업데이트 - 손전등 상태에 따라
    private void UpdateEyesColor()
    {
        if (eyesObject == null) return;

        if (enemyType == EnemyType.LightSeeker)
        {
            Color targetColor;

            // 반전 상태일 때는 무조건 검은색
            if (isInverted)
            {
                targetColor = Color.black;
            }
            else if (isInLight)
            {
                // 평소 + 손전등 안 → 검은색 눈
                targetColor = Color.black;
            }
            else
            {
                // 평소 + 손전등 밖 → 흰색 눈
                targetColor = Color.white;
            }

            // Eyes의 모든 자식 오브젝트의 색상 변경
            SpriteRenderer[] childRenderers = eyesObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in childRenderers)
            {
                renderer.color = targetColor;
            }
        }
        else
        {
            // Normal: 흰색 눈 유지
            SpriteRenderer[] childRenderers = eyesObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in childRenderers)
            {
                renderer.color = Color.white;
            }
        }
    }

    // 현재 상태에 맞는 아웃라인 색상 업데이트
    private void UpdateOutlineColor()
    {
        if (enemyOutline == null || player == null) return;

        if (enemyType == EnemyType.LightSeeker)
        {
            // 반전 상태일 때는 무조건 검은색
            if (isInverted)
            {
                enemyOutline.SetOutlineColor(Color.black);
            }
            else if (isInLight)
            {
                // 평소 + 손전등 안 → 검은색 아웃라인
                enemyOutline.SetOutlineColor(Color.black);
            }
            else
            {
                float distance = Vector2.Distance(transform.position, player.position);

                if (distance <= lightSeekerVisibilityDistance)
                {
                    // 평소 + 손전등 밖 + 가까움 → 흰색 아웃라인
                    enemyOutline.SetOutlineColor(Color.white);
                }
                else
                {
                    // 평소 + 손전등 밖 + 멀음 → 검은색 (안 보임)
                    enemyOutline.SetOutlineColor(Color.black);
                }
            }
        }
        else
        {
            // Normal: 반전 상태에 따라 색상 변경
            Color outlineColor = isInverted ? Color.white : Color.black;
            enemyOutline.SetOutlineColor(outlineColor);
        }
    }

    #endregion

    #region World State Events

    private void OnInversionChanged(bool inverted)
    {
        isInverted = inverted;
        Debug.Log($"{gameObject.name} 반전 상태: {inverted}");

        // 반전 상태가 바뀌면 즉시 Visibility 업데이트
        if (enemyType == EnemyType.LightSeeker)
        {
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                UpdateEyesVisibility(distance);
                UpdateEyesColor();
            }

            if (useOutline && enemyOutline != null)
            {
                UpdateOutlineColor();
            }
        }
        else
        {
            // 반전 상태에 따라 아웃라인 색상 변경 (useOutline이 true이고 Normal일 때만)
            if (useOutline && enemyOutline != null)
            {
                UpdateOutlineColor();
            }
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

            // 반전 상태에서 손전등 맞으면 모두 죽음
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

            // LightSeeker의 경우 속도와 시간 초기화
            if (enemyType == EnemyType.LightSeeker)
            {
                currentSpeed = lightSeekerBaseSpeed;
                timeInLight = 0f;
            }

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

        // LightSeeker의 경우 속도 초기화
        if (enemyType == EnemyType.LightSeeker)
        {
            currentSpeed = lightSeekerBaseSpeed;
            timeInLight = 0f;
        }

        EnemySpawner.Instance.ReturnEnemy(gameObject);
    }

    #endregion
}