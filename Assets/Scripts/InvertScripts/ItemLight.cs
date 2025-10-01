using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemLight : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Sprite lineSprite;                // 1x1 흰색 스프라이트(없으면 자동 생성)
    [SerializeField] private Color color = Color.white;        // 선 색상
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 0;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float intervalSeconds = 0.6f; // 생성 주기
    [SerializeField, Min(0f)] private float growDuration = 0.15f;       // 길이 성장 시간
    [SerializeField, Min(0f)] private float holdDuration = 0.05f;       // 최대 길이 유지 시간
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;       // 페이드 아웃 시간

    [Header("Flight")]
    [SerializeField, Min(0f)] private float travelDuration = 5f;        // 비행 시간(초)
    [SerializeField] private float travelSpeed = 3.0f;                   // 비행 속도(월드 유닛/초)

    [Header("Shape")]
    [SerializeField] private Vector2 lengthRange = new Vector2(2f, 5f);  // 목표 길이(월드 유닛)
    [SerializeField] private float thickness = 0.15f;                    // 두께(월드 유닛)

    private Sprite runtimeWhite;
    private Coroutine loopCo;

    private void OnEnable()
    {
        loopCo = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }
        // 스트릭은 월드에 독립이므로 자식 정리는 필요 없음
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(intervalSeconds);
        while (true)
        {
            SpawnFlyingStreak();
            yield return wait;
        }
    }

    private void SpawnFlyingStreak()
    {
        // 월드에 독립 오브젝트 생성
        var go = new GameObject("Streak");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetLineSprite();
        sr.color = color;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;

        // 두께만 먼저 맞추고 길이는 0에서 시작
        Vector2 spriteSize = sr.sprite.bounds.size;
        float scaleY = thickness / Mathf.Max(0.0001f, spriteSize.y);
        go.transform.localScale = new Vector3(0f, scaleY, 1f);

        // 아이템 중심과 랜덤 발사 방향
        Vector3 spawnPos = transform.position;
        float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)).normalized;

        // 회전(스프라이트 +X가 진행 방향이 되도록)
        go.transform.rotation = Quaternion.FromToRotation(Vector3.right, new Vector3(dir.x, dir.y, 0f));

        // 초기 위치는 아이템 중심
        go.transform.position = spawnPos;

        float targetLen = Random.Range(lengthRange.x, lengthRange.y);

        StartCoroutine(GrowTravelFade(sr, go.transform, spawnPos, dir, spriteSize.x, targetLen));
    }

    private IEnumerator GrowTravelFade(SpriteRenderer sr, Transform tr, Vector3 spawnPos, Vector2 dir, float spriteWidth, float targetLen)
    {
        // 1) 성장: 길이 0 -> targetLen (시작점은 아이템 중심 유지)
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float u = growDuration > 0f ? Mathf.Clamp01(t / growDuration) : 1f;
            float currLen = Mathf.Lerp(0f, targetLen, u);

            float scaleX = currLen / Mathf.Max(0.0001f, spriteWidth);
            tr.localScale = new Vector3(scaleX, tr.localScale.y, 1f);

            // 시작점을 spawnPos에 고정: 길이/2만큼 진행 방향으로 밀어 배치
            tr.position = spawnPos + (Vector3)(dir * (currLen * 0.5f));
            yield return null;
        }

        // 2) 유지
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        // 3) 비행: 월드 공간에서 travelDuration 동안 travelSpeed로 이동
        float flight = 0f;
        while (flight < travelDuration)
        {
            float dt = Time.deltaTime;
            flight += dt;
            tr.position += (Vector3)(dir * travelSpeed * dt);
            yield return null;
        }

        // 4) 페이드 아웃
        float f = 0f;
        var initial = sr.color;
        while (f < fadeDuration)
        {
            f += Time.deltaTime;
            float a = fadeDuration > 0f ? Mathf.Lerp(1f, 0f, f / fadeDuration) : 0f;
            var c = initial; c.a = a;
            if (sr) sr.color = c;
            yield return null;
        }

        if (sr) Destroy(sr.gameObject);
    }

    private Sprite GetLineSprite()
    {
        if (lineSprite) return lineSprite;
        if (runtimeWhite) return runtimeWhite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        runtimeWhite = Sprite.Create(tex, new Rect(0, 0, 1, 1),
                                     new Vector2(0.5f, 0.5f), 1f);
        return runtimeWhite;
    }
}
