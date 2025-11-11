using UnityEngine;

public class PlayerStandPlatformRiser : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Tooltip("플레이어가 올라갔을 때 이동할 목표 Y 위치 (로컬 좌표)")]
    private float targetY = 5f;
    
    [SerializeField, Tooltip("원래 Y 위치 (초기화용, 비워두면 시작 위치 사용)")]
    private float initialY = 0f;
    
    [SerializeField, Tooltip("초기 Y 위치 자동 저장")]
    private bool useStartPosition = true;
    
    [SerializeField, Tooltip("이동 속도")]
    private float moveSpeed = 2f;
    
    [SerializeField, Tooltip("부드러운 보간 사용 (Lerp) - false면 일정한 속도로 이동")]
    private bool useSmoothMovement = false;
    
    [Header("Player Detection")]
    [SerializeField, Tooltip("플레이어가 올라타고 대기하는 시간(초)")]
    private float delayBeforeRising = 1f;
    
    [SerializeField, Tooltip("플레이어가 내려가면 원래 위치로 돌아갈지 여부")]
    private bool returnWhenPlayerLeaves = true;
    
    [SerializeField, Tooltip("플레이어 태그")]
    private string playerTag = "Player";
    
    [Header("Debug")]
    [SerializeField, Tooltip("디버그 로그 출력")]
    private bool debugLog = true;
    
    private float startY;
    private float currentTargetY;
    private bool playerOnPlatform = false;
    private float playerEnteredTime = 0f;
    
    private void Start()
    {
        // 초기 Y 위치 저장
        if (useStartPosition)
        {
            startY = transform.localPosition.y;
        }
        else
        {
            startY = initialY;
        }
        
        currentTargetY = startY;
    }
    
    private void Update()
    {
        // 플레이어가 올라가 있으면 목표 Y로, 아니면 원래 Y로
        if (playerOnPlatform)
        {
            // 지연 시간이 지났는지 확인
            if (Time.time - playerEnteredTime >= delayBeforeRising)
            {
                currentTargetY = targetY;
            }
            // 아직 지연 시간 중이면 원래 위치 유지
        }
        else if (returnWhenPlayerLeaves)
        {
            currentTargetY = startY;
        }
        
        // 현재 위치를 목표 위치로 이동
        Vector3 pos = transform.localPosition;
        
        if (useSmoothMovement)
        {
            // 부드러운 보간
            pos.y = Mathf.Lerp(pos.y, currentTargetY, 1f - Mathf.Exp(-moveSpeed * Time.deltaTime));
            
            // 목표에 충분히 가까우면 바로 설정 (느려지는 문제 해결)
            if (Mathf.Abs(pos.y - currentTargetY) < 0.05f)
            {
                pos.y = currentTargetY;
            }
        }
        else
        {
            // 일정한 속도로 이동 (추천! 끝까지 일정한 속도 유지)
            pos.y = Mathf.MoveTowards(pos.y, currentTargetY, moveSpeed * Time.deltaTime);
        }
        
        transform.localPosition = pos;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerOnPlatform = true;
            playerEnteredTime = Time.time;
            
            if (debugLog)
            {
                Debug.Log($"[PlayerStandPlatformRiser] 플레이어가 플랫폼에 올라감: {gameObject.name} - {delayBeforeRising}초 후 상승 시작");
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerOnPlatform = false;
            
            if (debugLog)
            {
                Debug.Log($"[PlayerStandPlatformRiser] 플레이어가 플랫폼에서 내려감: {gameObject.name}");
            }
        }
    }
    
    // 외부에서 강제로 위치를 리셋할 수 있는 메서드
    public void ResetToInitialPosition()
    {
        Vector3 pos = transform.localPosition;
        pos.y = startY;
        transform.localPosition = pos;
        currentTargetY = startY;
        playerOnPlatform = false;
        playerEnteredTime = 0f;
    }
    
    // 목표 Y 위치를 동적으로 변경
    public void SetTargetY(float newTargetY)
    {
        targetY = newTargetY;
    }
    
    // 현재 플레이어가 올라가 있는지 확인
    public bool IsPlayerOnPlatform()
    {
        return playerOnPlatform;
    }
}

