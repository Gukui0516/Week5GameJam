using UnityEngine;

[DisallowMultipleComponent]
public class InvertPickup : MonoBehaviour
{
    // �������̶� �÷��̾� �浹�� ��Ǹ� worldstatemanager�� ����

    [Header("Settings")]
    [SerializeField, Min(0f)] private float invertDuration = 5f; // ������ ���� �ð�
    [SerializeField] private string playerTag = "Player";

    [Header("Refs")]
    [SerializeField] private WorldStateManager world; // �ν����Ϳ��� �Ҵ�

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!world) return;

        Debug.Log("������ ����");
        world.ActivateInversion(invertDuration);
    }
}
