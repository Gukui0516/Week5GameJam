using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D XY 커브 가속/감속 컨트롤러(중력 없음).
/// - 새 Input System Move(Vector2) 사용
/// - AnimationCurve(0..1)로 "속도 기반" 가속/감속
/// - 회전 각도별 속도 손실 규칙:
///   * ang <= noLossTurnAngleDeg      : 속도 크기 유지(방향만 전환)
///   * ang >= decelStartTurnAngleDeg  : 커브를 타는 "턴 감속" 발동
///   * ang >= hardFlipAngleDeg        : 강한 반전, 가속 진행도 리셋 옵션
/// - X/Y 최대속도가 다른 타원 한계를 내부 "정규화 속도공간"으로 처리
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region 2dMovement animationCurve
    [Header("Input System")]
    public InputActionReference moveAction;
    public InputActionReference interactAction;

    [Header("Max Speeds (Per Axis)")]
    public float maxSpeedX = 12f;
    public float maxSpeedY = 10f;

    [Header("Curves & Times")]
    public AnimationCurve curve;
    [Tooltip("최대속도(정규화)까지 도달 시간")]
    public float accelTime = 0.6f;
    [Tooltip("입력 없음 감속 시간(정지까지)")]
    public float decelTime = 0.25f;

    [Header("Turning Angles (Degrees)")]
    [Range(0, 180)]
    [Tooltip("이 각도까지는 속도 손실 없음")]
    public float noLossTurnAngleDeg = 91f;
    [Range(0, 180)]
    [Tooltip("이 각도부터 커브 감속 시작")]
    public float decelStartTurnAngleDeg = 134f;
    [Range(0, 180)]
    [Tooltip("이 각도 이상은 강한 반전 처리(가속 진행도 리셋 가능)")]
    public float hardFlipAngleDeg = 170f;

    [Header("Turning Options")]
    [Tooltip("hardFlip 이상에서 가속 진행도(uAccel) 0으로 리셋")]
    public bool resetOnHardFlip = true;
    [Tooltip("큰 각도일수록 감속을 더 빠르게 하기 위한 스케일(1=같음, 0.5=두배 빠름)")]
    [Range(0.25f, 1f)]
    public float minTurnDecelTimeScale = 0.6f;

    [Header("Input Tuning")]
    [Range(0f, 0.25f)] public float deadzone = 0.05f;

    [Header("Interact")]
    public float interactionRadius = 2f; // 상호작용 감지 반경
    public LayerMask interactableLayer;   // 상호작용 가능한 오브젝트의 레이어

    Rigidbody2D _rb;
    Vector2 _input;

    // 가속/감속 상태
    float _uAccel; // 0..1
    bool _decelerating;
    float _uDecel;       // 0..1
    float _decelStartS;  // 입력 없음 감속 시작 크기

    // 턴 감속 상태
    bool _turning;
    float _uTurn;        // 0..1
    float _turnStartS;
    float _turnLossLerp; // 각도 기반 손실(0..1)

    Vector2 _lastDirNorm = Vector2.right;
    #endregion



    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 각도 파라미터 안전장치
        noLossTurnAngleDeg = Mathf.Clamp(noLossTurnAngleDeg, 0, 180);
        decelStartTurnAngleDeg = Mathf.Clamp(decelStartTurnAngleDeg, noLossTurnAngleDeg, 180);
        hardFlipAngleDeg = Mathf.Clamp(hardFlipAngleDeg, decelStartTurnAngleDeg, 180);
    }

    void OnEnable()
    {
        if (moveAction && moveAction.action != null)
        {
            moveAction.action.performed += OnMove;
            moveAction.action.canceled += OnMove;
            moveAction.action.Enable();

        }
        

        SetVelocityAndSync(_rb.linearVelocity);
    }

    void OnDisable()
    {
        if (moveAction && moveAction.action != null)
        {
            moveAction.action.performed -= OnMove;
            moveAction.action.canceled -= OnMove;
            moveAction.action.Disable();

        }
        
    }

    void OnMove(InputAction.CallbackContext ctx) => _input = ctx.ReadValue<Vector2>();

    void Update()
    {
        

        
    }
    void FixedUpdate()
    {
        ApplyMovementFixed();
    }


    #region movementMethods
    /// <summary>FixedUpdate 본문 래핑</summary>
    public void ApplyMovementFixed()
    {
        float dt = Time.fixedDeltaTime;
        bool hasInput = _input.magnitude > deadzone;

        // 월드→정규화 사상
        Vector2 vWorld = _rb.linearVelocity;
        Vector2 vNorm = ToNorm(vWorld);
        float sCurr = vNorm.magnitude;
        Vector2 dirCurr = sCurr > 1e-6f ? vNorm / Mathf.Max(sCurr, 1e-6f) : _lastDirNorm;

        if (hasInput)
        {
            // 입력 방향을 정규화 공간으로
            Vector2 dNorm = new Vector2(
                _input.x / Mathf.Max(maxSpeedX, 1e-6f),
                _input.y / Mathf.Max(maxSpeedY, 1e-6f)
            );
            if (dNorm.sqrMagnitude > 1e-8f) dNorm.Normalize(); else dNorm = dirCurr;

            // 가속 커브 진행
            _uAccel = Mathf.Clamp01(_uAccel + (accelTime <= 0f ? 1f : dt / accelTime));
            float m = Mathf.Clamp01(_input.magnitude);                   // 아날로그 입력 크기
            float sGoalAccel = Mathf.Clamp01(curve.Evaluate(_uAccel)) * m;

            // 회전 각도
            float ang = Vector2.Angle(dirCurr, dNorm);

            float sNew;

            if (ang <= noLossTurnAngleDeg)
            {
                // 손실 없음: 방향만 전환, 크기는 유지 또는 현재 가속 목표까지
                sNew = Mathf.Max(sCurr, sGoalAccel);
                _turning = false;
                _uTurn = 0f;
            }
            else if (ang >= decelStartTurnAngleDeg)
            {
                // 턴 감속 시작: 각도에 따라 "보존율"을 1→0으로
                float lossLerp = Mathf.InverseLerp(decelStartTurnAngleDeg, hardFlipAngleDeg, ang); // 0..1
                float retain = 1f - lossLerp; // 남길 비율

                // 상태 초기화/갱신
                if (!_turning || Mathf.Abs(lossLerp - _turnLossLerp) > 0.05f)
                {
                    _turning = true;
                    _uTurn = 0f;
                    _turnStartS = sCurr;
                    _turnLossLerp = lossLerp;
                }

                // 목표 크기: 각도 기반 보존치와 가속목표 중 더 작은 쪽으로 떨어진다
                float sTarget = Mathf.Min(sGoalAccel, _turnStartS * retain);

                // 큰 각도일수록 빠르게 감속
                float timeScale = Mathf.Lerp(1f, minTurnDecelTimeScale, lossLerp);
                float turnDecelTime = Mathf.Max(1e-4f, decelTime * timeScale);

                _uTurn = Mathf.Clamp01(_uTurn + dt / turnDecelTime);
                float t = curve.Evaluate(_uTurn); // 커브 전방향으로 보간
                sNew = Mathf.Lerp(_turnStartS, sTarget, t);

                // 하드 플립: 가속 진행도 초기화 옵션
                if (ang >= hardFlipAngleDeg && resetOnHardFlip)
                    _uAccel = 0f;
            }
            else
            {
                // noLoss < ang < decelStart: 요구대로면 '감속 없음' 구간
                sNew = Mathf.Max(sCurr, sGoalAccel);
                _turning = false;
                _uTurn = 0f;
            }

            // 새 속도(정규화→월드)
            Vector2 vNormNew = dNorm * sNew;
            _rb.linearVelocity = ToWorld(vNormNew);

            // 입력 중에는 일반 감속 상태 종료
            _decelerating = false;
            _uDecel = 0f;
            _lastDirNorm = dNorm;
        }
        else
        {
            // 입력 없음: 커브 기반 감속
            if (!_decelerating)
            {
                _decelerating = true;
                _uDecel = 0f;
                _decelStartS = sCurr;
                _lastDirNorm = dirCurr;
                _turning = false;
                _uTurn = 0f;
            }

            if (_decelStartS <= 1e-5f || decelTime <= 0f)
            {
                _rb.linearVelocity = Vector2.zero;
                _decelerating = false;
                _uAccel = 0f;
                return;
            }

            _uDecel = Mathf.Clamp01(_uDecel + dt / decelTime);
            float k = 1f - Mathf.Clamp01(curve.Evaluate(_uDecel)); // 1→0
            float s = _decelStartS * k;

            if (s <= 1e-4f)
            {
                _rb.linearVelocity = Vector2.zero;
                _decelerating = false;
                _uAccel = 0f;
            }
            else
            {
                _rb.linearVelocity = ToWorld(_lastDirNorm * s);
            }
        }
    }

    // 월드속도 <-> 정규화 속도공간 변환
    Vector2 ToNorm(Vector2 vWorld)
    {
        return new Vector2(
            vWorld.x / Mathf.Max(maxSpeedX, 1e-6f),
            vWorld.y / Mathf.Max(maxSpeedY, 1e-6f)
        );
    }

    Vector2 ToWorld(Vector2 vNorm)
    {
        return new Vector2(
            vNorm.x * Mathf.Max(maxSpeedX, 1e-6f),
            vNorm.y * Mathf.Max(maxSpeedY, 1e-6f)
        );
    }

    /// <summary>외부에서 속도 주입 후 내부 진행도 동기화</summary>
    public void SetVelocityAndSync(Vector2 velocityWorld)
    {
        _rb.linearVelocity = velocityWorld;
        float s = ToNorm(velocityWorld).magnitude;
        _uAccel = ApproxInverse(curve, Mathf.Clamp01(s));
        _decelerating = false;
        _uDecel = 0f;
        _turning = false;
        _uTurn = 0f;

        Vector2 dn = ToNorm(velocityWorld);
        if (dn.sqrMagnitude > 1e-8f) _lastDirNorm = dn.normalized;
    }
    /// <summary>애니메이션 곡선에 대한 근사 역함수</summary>
    static float ApproxInverse(AnimationCurve c, float y, int samples = 64)
    {
        y = Mathf.Clamp01(y);
        float bestT = 0f, bestErr = float.MaxValue;
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float v = Mathf.Clamp01(c.Evaluate(t));
            float err = Mathf.Abs(v - y);
            if (err < bestErr) { bestErr = err; bestT = t; }
        }
        return bestT;
    }

    #endregion


    

}
