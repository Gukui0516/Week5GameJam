using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveDefalutSpeed = 5f;
    [SerializeField] private float moveLightSpeed = 5f;
    [SerializeField] WorldStateManager worldStateManager;
    private Vector2 moveInput;
    private InputSystem_Actions controls;  // 자동 생성된 클래스

    private void Awake()
    {
        if (worldStateManager == null)
        {
            worldStateManager = FindFirstObjectByType<WorldStateManager>();
        }
        controls = new InputSystem_Actions();
    }
    private void OnEnable()//플레이어 활성화 시
    {
        controls.Enable();

        // Move 액션 이벤트 구독
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;  // 입력 해제 시 이동 중지
    }
    private void OnDisable()//플레이어 비활성화 시
    {
        controls.Player.Move.performed -= ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled -= ctx => moveInput = Vector2.zero;

        controls.Disable();
    }

    private void Update()
    {
        float moveSpeed;
        if (worldStateManager.IsInverted)
        {
            moveSpeed = moveLightSpeed;
        }
        else 
        {
            moveSpeed = moveDefalutSpeed;
        }
        Vector2 move = moveInput * moveSpeed * Time.deltaTime;
        transform.position += new Vector3(move.x, move.y, 0f);
        // Vector2 이동
    }
}
