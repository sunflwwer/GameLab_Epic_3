using UnityEngine;
using GMTK.PlatformerToolkit;
using Unity.Cinemachine;

/// <summary>
/// NPC와의 상호작용을 처리합니다.
/// 플레이어가 NPC 근처에서 우클릭하면 엔딩 시퀀스가 시작됩니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCInteraction : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField, Tooltip("상호작용 가능 거리")]
    private float interactionRange = 3f;
    
    [SerializeField, Tooltip("플레이어 Transform")]
    private Transform player;
    
    [Header("Umbrella Settings")]
    [SerializeField, Tooltip("우산 오브젝트 (umbrellaObject)")]
    private GameObject umbrellaObject;
    
    [SerializeField, Tooltip("우산의 목표 X 위치")]
    private float targetUmbrellaX = 0.69f;
    
    [SerializeField, Tooltip("우산의 목표 Rotation Z")]
    private float targetUmbrellaRotationZ = -23.61f;
    
    [SerializeField, Tooltip("우산 이동 속도 (0이면 즉시)")]
    private float umbrellaTransitionSpeed = 2f;
    
    [Header("Player Components")]
    [SerializeField] private characterMovement playerMovement;
    [SerializeField] private characterJump playerJump;
    [SerializeField] private characterUmbrella playerUmbrella;
    [SerializeField] private characterInputManager playerInputManager;
    [SerializeField] private movementLimiter playerMovementLimiter;
    [SerializeField] private Rigidbody2D playerRigidbody;
    
    [Header("Camera Settings")]
    [SerializeField, Tooltip("엔딩 시 전환할 시네마틱 카메라")]
    private CinemachineCamera cinematicCamera;
    
    [SerializeField, Tooltip("시네마틱 카메라 우선순위")]
    private int cinematicCameraPriority = 20;
    
    [SerializeField, Tooltip("기본 카메라 (자동으로 찾음)")]
    private CinemachineCamera defaultCamera;
    
    private int defaultCameraPriority = 10;
    
    [Header("UI Settings")]
    [SerializeField, Tooltip("상호작용 키 표시 UI (옵션)")]
    private GameObject interactionPrompt;
    
    [SerializeField, Tooltip("상호작용 키 텍스트")]
    private string interactionKeyText = "[우클릭] 상호작용";
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    
    private bool isPlayerInRange;
    private bool isEnding;
    private Vector3 targetUmbrellaLocalPosition;
    private Quaternion targetUmbrellaRotation;
    
    private void Awake()
    {
        // 플레이어 자동 찾기
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // 플레이어 컴포넌트 자동 찾기
        if (player != null)
        {
            if (playerMovement == null)
                playerMovement = player.GetComponent<characterMovement>();
            if (playerJump == null)
                playerJump = player.GetComponent<characterJump>();
            if (playerUmbrella == null)
                playerUmbrella = player.GetComponent<characterUmbrella>();
            if (playerInputManager == null)
                playerInputManager = player.GetComponent<characterInputManager>();
            if (playerMovementLimiter == null)
                playerMovementLimiter = player.GetComponent<movementLimiter>();
            if (playerRigidbody == null)
                playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
        
        // Collider를 Trigger로 설정
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // 상호작용 프롬프트 비활성화
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // 기본 카메라 자동 찾기 (가장 높은 Priority를 가진 카메라)
        if (defaultCamera == null)
        {
            CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            int highestPriority = int.MinValue;
            
            foreach (CinemachineCamera cam in cameras)
            {
                if (cam != cinematicCamera && cam.Priority > highestPriority)
                {
                    highestPriority = cam.Priority;
                    defaultCamera = cam;
                }
            }
            
            if (defaultCamera != null)
            {
                defaultCameraPriority = defaultCamera.Priority;
            }
        }
    }
    
    private void Update()
    {
        if (isEnding) return;
        
        // 플레이어가 범위 내에 있는지 체크
        CheckPlayerInRange();
        
        // 상호작용 프롬프트 표시
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(isPlayerInRange);
        }
        
        // 우클릭 입력 체크
        if (isPlayerInRange && Input.GetMouseButtonDown(1))
        {
            StartEndingSequence();
        }
    }
    
    private void CheckPlayerInRange()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        isPlayerInRange = distance <= interactionRange;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            
            if (debugLog)
            {
                Debug.Log("[NPCInteraction] 플레이어가 NPC 범위에 진입");
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            
            if (debugLog)
            {
                Debug.Log("[NPCInteraction] 플레이어가 NPC 범위에서 벗어남");
            }
        }
    }
    
    private void StartEndingSequence()
    {
        if (isEnding) return;
        
        isEnding = true;
        
        if (debugLog)
        {
            Debug.Log("[NPCInteraction] 엔딩 시퀀스 시작!");
        }
        
        // 1. 시네마틱 카메라로 전환
        SwitchToCinematicCamera();
        
        // 2. 모든 플레이어 입력 차단
        DisablePlayerControls();
        
        // 3. 플레이어 움직임 정지
        StopPlayerMovement();
        
        // 4. 우산 위치 및 회전 변경
        MoveUmbrellaToTarget();
    }
    
    private void SwitchToCinematicCamera()
    {
        if (cinematicCamera == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[NPCInteraction] 시네마틱 카메라가 설정되지 않았습니다!");
            }
            return;
        }
        
        // 시네마틱 카메라 우선순위를 높여서 활성화
        cinematicCamera.Priority = cinematicCameraPriority;
        
        // 기본 카메라 우선순위를 낮춤
        if (defaultCamera != null)
        {
            defaultCamera.Priority = 0;
        }
        
        if (debugLog)
        {
            Debug.Log($"[NPCInteraction] 시네마틱 카메라로 전환: {cinematicCamera.name}");
        }
    }
    
    private void DisablePlayerControls()
    {
        // movementLimiter로 모든 움직임 차단
        if (playerMovementLimiter != null)
        {
            playerMovementLimiter.characterCanMove = false;
        }
        
        // 점프 차단
        if (playerJump != null)
        {
            playerJump.desiredJump = false;
            playerJump.jumpBufferCounter = 0f;
            playerJump.canJumpAgain = false;
        }
        
        // characterMovement 비활성화
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        // characterJump 비활성화
        if (playerJump != null)
        {
            playerJump.enabled = false;
        }
        
        // 우산 강제 닫기
        if (playerUmbrella != null)
        {
            playerUmbrella.ForceClose();
            playerUmbrella.enabled = false;
        }
        
        // InputManager 비활성화
        if (playerInputManager != null)
        {
            playerInputManager.enabled = false;
        }
        
        if (debugLog)
        {
            Debug.Log("[NPCInteraction] 플레이어 컨트롤 비활성화 (점프 포함)");
        }
    }
    
    private void StopPlayerMovement()
    {
        // 플레이어 속도를 0으로
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
        
        if (debugLog)
        {
            Debug.Log("[NPCInteraction] 플레이어 움직임 정지");
        }
    }
    
    private void MoveUmbrellaToTarget()
    {
        if (umbrellaObject == null)
        {
            Debug.LogWarning("[NPCInteraction] umbrellaObject가 설정되지 않았습니다!");
            return;
        }
        
        // 목표 위치 및 회전 설정
        Vector3 currentLocalPos = umbrellaObject.transform.localPosition;
        targetUmbrellaLocalPosition = new Vector3(targetUmbrellaX, currentLocalPos.y, currentLocalPos.z);
        targetUmbrellaRotation = Quaternion.Euler(0, 0, targetUmbrellaRotationZ);
        
        if (umbrellaTransitionSpeed <= 0)
        {
            // 즉시 이동
            umbrellaObject.transform.localPosition = targetUmbrellaLocalPosition;
            umbrellaObject.transform.localRotation = targetUmbrellaRotation;
            
            if (debugLog)
            {
                Debug.Log($"[NPCInteraction] 우산 위치 변경: X={targetUmbrellaX}, Rotation Z={targetUmbrellaRotationZ}");
            }
        }
        else
        {
            // 부드럽게 이동 (코루틴 사용)
            StartCoroutine(SmoothMoveUmbrella());
        }
    }
    
    private System.Collections.IEnumerator SmoothMoveUmbrella()
    {
        if (umbrellaObject == null) yield break;
        
        Vector3 startPos = umbrellaObject.transform.localPosition;
        Quaternion startRot = umbrellaObject.transform.localRotation;
        
        // NPC 색상 변경을 위한 초기값
        SpriteRenderer npcRenderer = GetComponent<SpriteRenderer>();
        Color startColor = Color.white;
        Color targetColor = Color.white;
        
        if (npcRenderer != null)
        {
            startColor = npcRenderer.color;
        }
        
        float elapsed = 0f;
        float duration = 1f / umbrellaTransitionSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 우산 위치 및 회전 변경
            umbrellaObject.transform.localPosition = Vector3.Lerp(startPos, targetUmbrellaLocalPosition, t);
            umbrellaObject.transform.localRotation = Quaternion.Lerp(startRot, targetUmbrellaRotation, t);
            
            // NPC 색상을 흰색으로 점차 변경
            if (npcRenderer != null)
            {
                npcRenderer.color = Color.Lerp(startColor, targetColor, t);
            }
            
            yield return null;
        }
        
        // 최종 위치 및 색상 설정
        umbrellaObject.transform.localPosition = targetUmbrellaLocalPosition;
        umbrellaObject.transform.localRotation = targetUmbrellaRotation;
        
        if (npcRenderer != null)
        {
            npcRenderer.color = targetColor;
        }
        
        if (debugLog)
        {
            Debug.Log($"[NPCInteraction] 우산 이동 완료: X={targetUmbrellaX}, Rotation Z={targetUmbrellaRotationZ}");
            Debug.Log("[NPCInteraction] NPC 색상 변경 완료: 흰색");
        }
    }
    
    /// <summary>
    /// 엔딩 상태를 해제하고 플레이어 컨트롤을 복원합니다 (필요시)
    /// </summary>
    public void ResetEnding()
    {
        isEnding = false;
        
        // 카메라 복원
        if (cinematicCamera != null)
        {
            cinematicCamera.Priority = 0;
        }
        
        if (defaultCamera != null)
        {
            defaultCamera.Priority = defaultCameraPriority;
        }
        
        if (playerMovementLimiter != null)
        {
            playerMovementLimiter.characterCanMove = true;
        }
        
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        if (playerJump != null)
        {
            playerJump.enabled = true;
        }
        
        if (playerUmbrella != null)
        {
            playerUmbrella.enabled = true;
        }
        
        if (playerInputManager != null)
        {
            playerInputManager.enabled = true;
        }
        
        if (debugLog)
        {
            Debug.Log("[NPCInteraction] 엔딩 상태 해제 (모든 컴포넌트 및 카메라 재활성화)");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 상호작용 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // NPC 위치 표시
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }
}

