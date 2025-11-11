using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField, Tooltip("우선순위 1) 지정된 Transform 위치로 리스폰")]
    private Transform respawnPoint;

    [SerializeField, Tooltip("우선순위 2) Transform이 비어있을 때 사용할 월드 좌표")]
    private Vector3 respawnPosition = new Vector3(-68f, -3f, 0f);

    [SerializeField, Tooltip("리스폰 시 Rigidbody2D 속도를 0으로 초기화할지")]
    private bool resetVelocity = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 목적지 결정
        Vector3 target = respawnPoint != null ? respawnPoint.position : respawnPosition;

        // 위치 이동
        other.transform.position = target;

        // 속도 리셋 옵션
        if (resetVelocity)
        {
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    // 에디터에서 gizmo로 위치 확인(선택 사항)
    private void OnDrawGizmosSelected()
    {
        Vector3 target = respawnPoint != null ? respawnPoint.position : respawnPosition;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, 0.3f);
    }
}