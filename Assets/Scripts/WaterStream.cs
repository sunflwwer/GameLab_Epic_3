using UnityEngine;
using GMTK.PlatformerToolkit;

public class WaterStream : MonoBehaviour
{
    [Header("Water Stream Settings")]
    [SerializeField, Tooltip("물줄기가 흐르는 방향")] private Vector2 flowDirection = Vector2.right;
    [SerializeField, Tooltip("물줄기 이동 속도")] private float flowSpeed = 5f;
    [SerializeField, Tooltip("우산에 막혔을 때 scale 감소 속도")] private float scaleDecreaseSpeed = 2f;
    [SerializeField, Tooltip("우산이 없을 때 scale 회복 속도")] private float scaleRecoverSpeed = 1f;
    [SerializeField, Tooltip("최소 scale (0보다 커야 함)")] private float minScale = 0.1f;
    [SerializeField, Tooltip("물줄기가 사라지는 거리")] private float despawnDistance = 20f;
    
    [Header("Player Interaction")]
    [SerializeField, Tooltip("플레이어에게 가할 힘")] private float pushForce = 10f;
    [SerializeField, Tooltip("ForceMode2D 타입")] private ForceMode2D forceMode = ForceMode2D.Impulse;
    [SerializeField, Tooltip("물에 맞았을 때 입력 비활성화 시간")] private float inputDisableDuration = 0.15f;

    private Vector3 initialScale;
    private Vector3 spawnPosition;
    private bool isBlockedByUmbrella;

    private void Start()
    {
        initialScale = transform.localScale;
        spawnPosition = transform.position;
        
    }

    private void Update()
    {
        // 물줄기 이동 (Rigidbody2D가 있으면 물리 엔진에 의해 중력 영향을 받으므로 Kinematic으로 설정 필요)
        transform.Translate(flowDirection.normalized * flowSpeed * Time.deltaTime, Space.World);

        // Scale 조정
        if (isBlockedByUmbrella)
        {
            // 우산에 막히면 scale 감소
            Vector3 currentScale = transform.localScale;
            currentScale.x = Mathf.Max(minScale, currentScale.x - scaleDecreaseSpeed * Time.deltaTime);
            currentScale.y = Mathf.Max(minScale, currentScale.y - scaleDecreaseSpeed * Time.deltaTime);
            transform.localScale = currentScale;

            // 너무 작아지면 비활성화
            if (currentScale.x <= minScale && currentScale.y <= minScale)
            {
                
                Destroy(gameObject);
            }
        }
        else
        {
            // 우산이 없으면 원래 크기로 회복
            Vector3 currentScale = transform.localScale;
            currentScale.x = Mathf.Min(initialScale.x, currentScale.x + scaleRecoverSpeed * Time.deltaTime);
            currentScale.y = Mathf.Min(initialScale.y, currentScale.y + scaleRecoverSpeed * Time.deltaTime);
            transform.localScale = currentScale;
        }

        // 일정 거리 이상 가면 제거
        float distance = Vector3.Distance(spawnPosition, transform.position);
        if (distance > despawnDistance)
        {
            
            Destroy(gameObject);
        }

        // 매 프레임마다 초기화 (다음 프레임에 다시 체크)
        isBlockedByUmbrella = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        
        // 우산 태그 확인
        if (collision.CompareTag("Umbrella"))
        {
            Debug.Log("우산과 충돌 감지됨! 물줄기 제거");
            Destroy(gameObject);
            return;
        }
        
        // 플레이어 태그 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log("플레이어와 충돌 감지됨!");
            
            // 대시 중인지 확인
            characterUmbrella umbrellaController = collision.GetComponent<characterUmbrella>();
            bool isDashing = umbrellaController != null && umbrellaController.IsDashActive;
            
            if (isDashing)
            {
                // 대시 중이면 물줄기가 대시를 막음 (물줄기는 사라지지 않음)
                
                // 대시 강제 종료는 umbrellaController에 메서드가 있다면 사용
                // 일단 velocity를 0으로 만들어서 멈춤
                Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero;
                }
                
                // 입력도 잠시 무시
                characterMovement playerMovement = collision.GetComponent<characterMovement>();
                if (playerMovement != null)
                {
                    playerMovement.IgnoreInputForDuration(inputDisableDuration * 2f); // 대시 중단 시 더 길게
                }
                
                return; // 힘을 가하지 않고 종료
            }
            
            // 일반 상태면 힘 적용
            Rigidbody2D playerRb2 = collision.GetComponent<Rigidbody2D>();
            if (playerRb2 != null)
            {
                // 먼저 velocity를 0으로 초기화 (기존 움직임 제거)
                playerRb2.linearVelocity = Vector2.zero;
                
                // 그 다음 물줄기 방향으로 힘 적용
                Vector2 force = flowDirection.normalized * pushForce;
                playerRb2.AddForce(force, forceMode);
                
                
                // 입력 일시 무시 (막는게 아니라 무시)
                characterMovement playerMovement = collision.GetComponent<characterMovement>();
                if (playerMovement != null)
                {
                    playerMovement.IgnoreInputForDuration(inputDisableDuration);
                    
                }
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 우산 태그 확인
        if (collision.CompareTag("Umbrella"))
        {
            isBlockedByUmbrella = true;
        }
        
        // 플레이어가 물줄기 안에 계속 있으면 지속적으로 힘 가하기
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            
            if (playerRb != null && forceMode == ForceMode2D.Force)
            {
                Vector2 force = flowDirection.normalized * pushForce;
                playerRb.AddForce(force, ForceMode2D.Force);
            }
        }
    }

    
    
}
