using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PooledItem : MonoBehaviour
{
    // 아이템이 플레이어랑 일정 거리 이상 멀어지면 제거됨. 

    private Transform player;
    private float maxDistance = Mathf.Infinity;
    private float life;

    private Action<GameObject> releaseToPool; // 스폰너가 넘겨주는 반환 콜백

    public void Setup(Transform player, float maxDistance, Action<GameObject> releaseToPool, float maxLifetime = -1f)
    {
        this.player = player;
        this.maxDistance = maxDistance;
        this.releaseToPool = releaseToPool;
        life = 0f;
    }

    private void OnEnable()
    {
        life = 0f;
    }

    private void Update()
    {
        // 거리 초과 시 반환
        if (player)
        {
            var sqr = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
            if (sqr > maxDistance * maxDistance)
            {
                releaseToPool?.Invoke(gameObject);
                return;
            }
        }
    }
}
