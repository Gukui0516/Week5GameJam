using System.Collections.Generic;
using UnityEngine;

public class SpriteTrail2D : MonoBehaviour
{
    public GameObject spritePrefab; // 배치할 스프라이트 프리팹
    public float spacing = 1f;      // 스프라이트 간격
    public float maxDistance = 10f; // 스프라이트 제거 기준 거리
    public float zPos = 0f;         // 고정 Z축

    public Vector2 startPos = Vector2.zero;   // 시작 위치
    public Vector2 direction = Vector2.right; // 스프라이트 증가 방향

    private List<GameObject> sprites = new List<GameObject>();

    void Start()
    {
        // 초기 스프라이트 배치
        int count = Mathf.CeilToInt(maxDistance / spacing);
        for (int i = 0; i <= count; i++)
        {
            Vector3 pos = new Vector3(startPos.x + direction.x * spacing * i,
                                      startPos.y + direction.y * spacing * i,
                                      zPos);
            GameObject obj = Instantiate(spritePrefab, pos, Quaternion.identity, transform);
            sprites.Add(obj);
        }
    }

    void Update()
    {
        // 제거 기준
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var s in sprites)
        {
            float dist = Vector2.Distance(new Vector2(s.transform.position.x, s.transform.position.y),
                                          startPos);
            if (dist > maxDistance)
                toRemove.Add(s);
        }

        foreach (var s in toRemove)
        {
            sprites.Remove(s);
            Destroy(s);
        }

        // 끝쪽에 새 스프라이트 추가
        if (sprites.Count > 0)
        {
            GameObject last = sprites[sprites.Count - 1];
            Vector2 lastPos2D = new Vector2(last.transform.position.x, last.transform.position.y);
            if (Vector2.Distance(lastPos2D, startPos) + spacing <= maxDistance)
            {
                Vector3 pos = new Vector3(lastPos2D.x + direction.x * spacing,
                                          lastPos2D.y + direction.y * spacing,
                                          zPos);
                GameObject newObj = Instantiate(spritePrefab, pos, Quaternion.identity, transform);
                sprites.Add(newObj);
            }
        }
    }
}
