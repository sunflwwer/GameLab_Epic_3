using UnityEngine;

public class UmbrellaWaterDrop : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy 태그 처리 (적 제거)
        if (other.CompareTag("Enemy"))
        {
            Debug.Log($"Umbrella water drop hit enemy: {other.name}");

            if (other.attachedRigidbody != null)
            {
                Destroy(other.attachedRigidbody.gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }
            return;
        }

        // WaterReceiver 태그 처리 (Y step 증가)
        if (other.CompareTag("WaterReceiver"))
        {
            Debug.Log($"Umbrella water drop hit WaterReceiver: {other.name}");

            // AttackStepYDriver 컴포넌트 찾아서 스텝 증가
            AttackStepYDriver stepDriver = other.GetComponent<AttackStepYDriver>();
            if (stepDriver != null)
            {
                // HandleAttackFired는 private이므로 public 메서드 추가 필요
                // 또는 직접 스텝 증가 메서드 호출
                stepDriver.IncrementStep();
            }
            else
            {
                Debug.LogWarning($"WaterReceiver {other.name}에 AttackStepYDriver 컴포넌트가 없습니다!");
            }
        }
    }
}
