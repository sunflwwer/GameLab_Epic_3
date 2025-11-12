using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 Hook 시스템을 제어합니다.
/// 마우스 좌클릭으로 Hook 레일에 연결하고 이동합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHookController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GameObject hookObject;
    [SerializeField] private characterInputManager inputManager;
    [SerializeField] private characterUmbrella umbrellaController;
    [SerializeField] private Rigidbody2D body;
    
    [Header("Hook Settings")]
    [SerializeField, Tooltip("Hook 감지 범위")]
    private float hookDetectionRange = 5f;
    
    [SerializeField, Tooltip("Hook 이동 속도")]
    private float hookMoveSpeed = 8f;
    
    [SerializeField, Tooltip("Hook 레일 레이어")]
    private LayerMask hookRailLayer;
    
    [SerializeField, Tooltip("Hook 시작 시 중력 끄기")]
    private bool disableGravityDuringHook = true;
    
    [SerializeField, Tooltip("플레이어가 레일보다 아래에 위치할 Y 오프셋 (음수면 아래로)")]
    private float playerYOffset = -0.5f;
    
    [Header("Visual Settings")]
    [SerializeField, Tooltip("Hook 라인 렌더러 (옵션)")]
    private LineRenderer hookLine;
    
    [SerializeField, Tooltip("Hook 라인 월드 스페이스 사용")]
    private bool hookLineUseWorldSpace = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    
    // State
    private bool isHooking;
    private HookRail currentRail;
    private float hookProgress;
    private float originalGravityScale;
    private Coroutine hookCoroutine;
    
    public bool IsHooking => isHooking;
    
    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
        
        if (inputManager == null)
        {
            inputManager = GetComponent<characterInputManager>();
        }
        
        if (umbrellaController == null)
        {
            umbrellaController = GetComponent<characterUmbrella>();
        }
        
        if (hookObject != null)
        {
            hookObject.SetActive(false);
        }
        
        if (hookLine != null)
        {
            hookLine.enabled = false;
            hookLine.useWorldSpace = hookLineUseWorldSpace;
        }
        
        originalGravityScale = body.gravityScale;
    }
    
    private void Update()
    {
        // Hook 중이 아닐 때만 입력 체크
        if (!isHooking && inputManager != null)
        {
            // 좌클릭 입력 확인 (Peek로 먼저 확인, Hook 성공 시에만 Consume)
            if (inputManager.isUmbrellaActionHeld || Input.GetMouseButtonDown(0))
            {
                // Hook 가능한지 먼저 확인
                HookRail nearestRail = FindNearestHookableRail();
                
                if (nearestRail != null)
                {
                    // Hook 가능하면 입력 소비하고 시작
                    inputManager.ConsumeUmbrellaAction();
                    StartHookMovement(nearestRail);
                }
            }
        }
        
        // Hook 라인 업데이트
        UpdateHookLine();
    }
    
    private HookRail FindNearestHookableRail()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, hookDetectionRange, hookRailLayer);
        
        HookRail nearestRail = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D col in colliders)
        {
            HookRail rail = col.GetComponent<HookRail>();
            if (rail != null && rail.IsHookable)
            {
                // 레일의 시작점까지의 거리 확인
                if (rail.IsPlayerInRange(transform.position))
                {
                    float distance = Vector3.Distance(transform.position, rail.GetStartPosition());
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestRail = rail;
                    }
                }
            }
        }
        
        return nearestRail;
    }
    
    private void StartHookMovement(HookRail rail)
    {
        // 이미 Hook 중이면 무시
        if (isHooking)
        {
            return;
        }
        
        if (debugLog)
        {
            Debug.Log($"[PlayerHookController] Hook 시작: {rail.gameObject.name}");
        }
        
        // 모든 우산 비활성화
        if (umbrellaController != null)
        {
            umbrellaController.ForceClose();
        }
        
        currentRail = rail;
        isHooking = true;
        hookProgress = 0f;
        
        // Hook 오브젝트 활성화
        if (hookObject != null)
        {
            hookObject.SetActive(true);
        }
        
        // 중력 비활성화
        if (disableGravityDuringHook)
        {
            body.gravityScale = 0f;
        }
        
        // 속도 초기화
        body.linearVelocity = Vector2.zero;
        
        // Hook 이동 코루틴 시작
        if (hookCoroutine != null)
        {
            StopCoroutine(hookCoroutine);
        }
        hookCoroutine = StartCoroutine(HookMovementCoroutine());
    }
    
    private IEnumerator HookMovementCoroutine()
    {
        float totalLength = currentRail.GetTotalLength();
        
        while (hookProgress < 1f && isHooking)
        {
            // 진행도 증가
            float progressDelta = (hookMoveSpeed / totalLength) * Time.deltaTime;
            hookProgress += progressDelta;
            hookProgress = Mathf.Clamp01(hookProgress);
            
            // 레일을 따라 이동 (Y 오프셋 적용)
            Vector3 targetPosition = currentRail.GetPositionAtProgress(hookProgress);
            targetPosition.y += playerYOffset;
            transform.position = targetPosition;
            
            yield return null;
        }
        
        // Hook 종료
        EndHookMovement();
    }
    
    private void EndHookMovement()
    {
        if (!isHooking)
        {
            return;
        }
        
        if (debugLog)
        {
            Debug.Log("[PlayerHookController] Hook 종료");
        }
        
        isHooking = false;
        currentRail = null;
        hookProgress = 0f;
        
        // Hook 오브젝트 비활성화
        if (hookObject != null)
        {
            hookObject.SetActive(false);
        }
        
        // 중력 복원
        if (disableGravityDuringHook)
        {
            body.gravityScale = originalGravityScale;
        }
        
        // Hook 라인 비활성화
        if (hookLine != null)
        {
            hookLine.enabled = false;
        }
    }
    
    /// <summary>
    /// 외부에서 Hook을 강제로 중단할 때 사용
    /// </summary>
    public void ForceStopHook()
    {
        if (hookCoroutine != null)
        {
            StopCoroutine(hookCoroutine);
            hookCoroutine = null;
        }
        
        EndHookMovement();
    }
    
    private void UpdateHookLine()
    {
        if (hookLine == null)
        {
            return;
        }
        
        if (isHooking && currentRail != null)
        {
            if (!hookLine.enabled)
            {
                hookLine.enabled = true;
            }
            
            Vector3 playerPos = transform.position;
            Vector3 targetPos = currentRail.GetPositionAtProgress(hookProgress);
            
            hookLine.positionCount = 2;
            
            if (hookLineUseWorldSpace)
            {
                hookLine.SetPosition(0, playerPos);
                hookLine.SetPosition(1, targetPos);
            }
            else
            {
                hookLine.SetPosition(0, hookLine.transform.InverseTransformPoint(playerPos));
                hookLine.SetPosition(1, hookLine.transform.InverseTransformPoint(targetPos));
            }
        }
        else if (hookLine.enabled)
        {
            hookLine.enabled = false;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Hook 감지 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hookDetectionRange);
    }
}

