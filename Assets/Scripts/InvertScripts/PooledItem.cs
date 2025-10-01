using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PooledItem : MonoBehaviour
{
    // 플레이어와 일정 거리 이상 벌어지거나, 설정한 수명 경과 시 풀로 반환

    private Transform player;
    private float maxDistance = Mathf.Infinity;
    private float sqrMaxDistance = float.PositiveInfinity;

    private float maxLifetime = -1f; // -1이면 사용 안 함
    private float life;

    private Action<GameObject> releaseToPool; // 스포너가 넘겨주는 반환 콜백
    private bool returned; // 중복 반환 방지

    public void Setup(Transform player, float maxDistance, Action<GameObject> releaseToPool, float maxLifetime = -1f)
    {
        this.player = player;
        this.maxDistance = maxDistance;
        this.sqrMaxDistance = (maxDistance < 0f || float.IsInfinity(maxDistance))
            ? float.PositiveInfinity
            : maxDistance * maxDistance;

        this.releaseToPool = releaseToPool;
        this.maxLifetime = maxLifetime;

        life = 0f;
        returned = false;
    }

    private void OnEnable()
    {
        life = 0f;
        returned = false;
    }

    private void Update()
    {
        if (returned) return;

        // 1) 거리 초과 시 반환
        if (player)
        {
            var sqr = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
            if (sqr > sqrMaxDistance)
            {
                ReturnToPoolNow();
                return;
            }
        }

        // 추가 옵션 2) 수명 경과 시 반환
        if (maxLifetime > 0f)
        {
            life += Time.deltaTime;
            if (life >= maxLifetime)
            {
                ReturnToPoolNow();
                return;
            }
        }
    }

    /// <summary>외부(예: 픽업 시)에서 즉시 풀 반환을 요청할 때 사용</summary>
    public void ReturnToPoolNow()
    {
        if (returned) return;
        returned = true;

        if (releaseToPool != null)
            releaseToPool(gameObject);
        else
            gameObject.SetActive(false); // 폴백: 최소한 비활성화
    }
}
