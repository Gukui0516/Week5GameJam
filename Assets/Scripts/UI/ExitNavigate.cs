using UnityEngine;

public class ExitNavigate : MonoBehaviour
{
    [SerializeField] GameObject exit;
    Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 dir = player.transform.position- exit.transform.position;

        // 거리
        float distance = dir.magnitude;

        // 각도 (라디안 -> 도 단위 변환)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.eulerAngles=new Vector3(0,0,angle+90f);
    }
}
