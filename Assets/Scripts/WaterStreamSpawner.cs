using UnityEngine;

public class WaterStreamSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField, Tooltip("생성할 물줄기 프리팹")] private GameObject waterStreamPrefab;
    [SerializeField, Tooltip("물줄기 생성 간격 (초)")] private float spawnInterval = 0.2f;
    [SerializeField, Tooltip("물줄기가 흐르는 방향")] private Vector2 flowDirection = Vector2.right;
    [SerializeField, Tooltip("물줄기 이동 속도")] private float flowSpeed = 5f;
    
    [Header("Water Stream Settings")]
    [SerializeField, Tooltip("우산에 막혔을 때 scale 감소 속도")] private float scaleDecreaseSpeed = 2f;
    [SerializeField, Tooltip("우산이 없을 때 scale 회복 속도")] private float scaleRecoverSpeed = 1f;
    [SerializeField, Tooltip("최소 scale")] private float minScale = 0.1f;
    [SerializeField, Tooltip("물줄기가 사라지는 거리")] private float despawnDistance = 20f;

    [Header("Spawner State")]
    [SerializeField, Tooltip("스포너 활성화 여부")] private bool isActive = true;

    private float spawnTimer;

    private void Start()
    {
        // 프리팹이 없으면 경고
        if (waterStreamPrefab == null)
        {
            Debug.LogWarning("WaterStreamSpawner: waterStreamPrefab이 할당되지 않았습니다!");
        }

        spawnTimer = 0f;
    }

    private void Update()
    {
        // 스포너가 비활성화되어 있으면 생성하지 않음
        if (!isActive || waterStreamPrefab == null)
            return;

        spawnTimer += Time.deltaTime;

        // 생성 간격이 되면 물줄기 생성
        if (spawnTimer >= spawnInterval)
        {
            SpawnWaterStream();
            spawnTimer = 0f;
        }
    }

    private void SpawnWaterStream()
    {
        // 스포너 위치에 물줄기 생성
        GameObject waterStream = Instantiate(waterStreamPrefab, transform.position, Quaternion.identity);

        // WaterStream 컴포넌트 가져오기
        WaterStream waterStreamComponent = waterStream.GetComponent<WaterStream>();

        if (waterStreamComponent != null)
        {
            // WaterStream의 설정 값들을 스포너의 값으로 설정
            // Reflection을 사용하여 private 필드에 접근
            var type = typeof(WaterStream);
            
            // flowDirection 설정
            var flowDirectionField = type.GetField("flowDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (flowDirectionField != null)
                flowDirectionField.SetValue(waterStreamComponent, flowDirection);

            // flowSpeed 설정
            var flowSpeedField = type.GetField("flowSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (flowSpeedField != null)
                flowSpeedField.SetValue(waterStreamComponent, flowSpeed);

            // scaleDecreaseSpeed 설정
            var scaleDecreaseSpeedField = type.GetField("scaleDecreaseSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scaleDecreaseSpeedField != null)
                scaleDecreaseSpeedField.SetValue(waterStreamComponent, scaleDecreaseSpeed);

            // scaleRecoverSpeed 설정
            var scaleRecoverSpeedField = type.GetField("scaleRecoverSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (scaleRecoverSpeedField != null)
                scaleRecoverSpeedField.SetValue(waterStreamComponent, scaleRecoverSpeed);

            // minScale 설정
            var minScaleField = type.GetField("minScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (minScaleField != null)
                minScaleField.SetValue(waterStreamComponent, minScale);

            // despawnDistance 설정
            var despawnDistanceField = type.GetField("despawnDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (despawnDistanceField != null)
                despawnDistanceField.SetValue(waterStreamComponent, despawnDistance);
        }
    }

    // 외부에서 스포너를 활성화/비활성화할 수 있는 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }

    public void ToggleActive()
    {
        isActive = !isActive;
    }

    // Gizmo로 스포너 위치와 방향 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.cyan : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // 물줄기 방향 표시
        if (flowDirection != Vector2.zero)
        {
            Gizmos.color = isActive ? Color.blue : Color.gray;
            Vector3 direction = flowDirection.normalized;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}
