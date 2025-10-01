using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemLight : MonoBehaviour
{
    // 아이템에서 빛 뿜어져 나오는 거

    [Header("Visual")]
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int orderInLayer = 0;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float intervalSeconds = 0.6f;
    [SerializeField, Min(0f)] private float growDuration = 0.15f;
    [SerializeField, Min(0f)] private float holdDuration = 0.05f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    [Header("Flight")]
    [SerializeField, Min(0f)] private float travelDuration = 5f;
    [SerializeField] private float travelSpeed = 3.0f;

    [Header("Shape")]
    [SerializeField] private Vector2 lengthRange = new Vector2(2f, 5f);
    [SerializeField] private float thickness = 0.15f;

    private Sprite runtimeWhite;
    private Coroutine loopCo;
    private bool stopped; // 수집/비활성화 후 추가 생성 방지

    private void OnEnable()
    {
        stopped = false;
        loopCo = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        StopAndClearAll();
    }

    /// <summary>아이템이 플레이어에게 먹힌 순간 호출해주세요.</summary>
    public void OnCollected()
    {
        StopAndClearAll();
    }

    private void StopAndClearAll()
    {
        if (stopped) return;
        stopped = true;

        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        // 내가 부모인 모든 스트릭 즉시 제거
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child && child.name.StartsWith("Streak"))
                Destroy(child.gameObject);
        }
    }

    private IEnumerator Loop()
    {
        var wait = new WaitForSeconds(intervalSeconds);
        while (!stopped)
        {
            SpawnFlyingStreak();
            yield return wait;
        }
    }

    private void SpawnFlyingStreak()
    {
        var go = new GameObject("Streak");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetLineSprite();
        sr.color = color;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = orderInLayer;

        // 아이템의 자식으로 (로컬좌표 유지: false → 부모 기준 초기화)
        go.transform.SetParent(transform, false);

        // 두께 세팅(로컬 스케일)
        Vector2 spriteSize = sr.sprite.bounds.size;
        float scaleY = thickness / Mathf.Max(0.0001f, spriteSize.y);
        go.transform.localScale = new Vector3(0f, scaleY, 1f);

        // 로컬 방향/회전
        float angDeg = Random.Range(0f, 360f);
        Vector2 localDir = new Vector2(Mathf.Cos(angDeg * Mathf.Deg2Rad), Mathf.Sin(angDeg * Mathf.Deg2Rad)).normalized;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.right, new Vector3(localDir.x, localDir.y, 0f));

        // 로컬 위치(아이템 중심에서 시작)
        go.transform.localPosition = Vector3.zero;

        float targetLen = Random.Range(lengthRange.x, lengthRange.y);

        StartCoroutine(GrowTravelFadeLocal(sr, go.transform, localDir, spriteSize.x, targetLen));
    }

    private IEnumerator GrowTravelFadeLocal(SpriteRenderer sr, Transform tr, Vector2 localDir, float spriteWidth, float targetLen)
    {
        // 성장 (로컬)
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.deltaTime;
            float u = growDuration > 0f ? Mathf.Clamp01(t / growDuration) : 1f;
            float currLen = Mathf.Lerp(0f, targetLen, u);

            float scaleX = currLen / Mathf.Max(0.0001f, spriteWidth);
            tr.localScale = new Vector3(scaleX, tr.localScale.y, 1f);

            // 시작점을 부모(아이템) 중심에 고정: 길이/2만큼 밀기(로컬)
            tr.localPosition = (Vector3)(localDir * (currLen * 0.5f));
            yield return null;
        }

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        // 비행도 로컬 좌표로(부모가 움직여도 항상 같은 상대 이동)
        float flight = 0f;
        while (flight < travelDuration)
        {
            float dt = Time.deltaTime;
            flight += dt;
            tr.localPosition += (Vector3)(localDir * travelSpeed * dt);
            yield return null;
        }

        // 페이드
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
