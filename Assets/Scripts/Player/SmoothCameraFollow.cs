using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target; // 플레이어
    [SerializeField] private float smoothTime = 0.2f; // 따라가는 딜레이 정도
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f); // 카메라 깊이 보정

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!target) return;

        // 목표 위치(플레이어 + 오프셋)
        Vector3 targetPos = target.position + offset;

        // 부드럽게 보간
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
