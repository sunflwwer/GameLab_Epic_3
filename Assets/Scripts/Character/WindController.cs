using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 바람의 영향을 받도록 제어합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class WindController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private characterGround groundDetector;
    [SerializeField] private characterUmbrella umbrellaController;
    
    [Header("Wind Settings")]
    [SerializeField, Tooltip("바람 적용 방식 (Force/Velocity)")]
    private bool useForce = true;
    
    [SerializeField, Tooltip("바람 적용 부드러움 (Lerp)")]
    private bool smoothWind = true;
    
    [SerializeField, Tooltip("바람 부드러움 속도")]
    private float windSmoothSpeed = 5f;
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    
    // 현재 영향 받고 있는 바람 존들
    private List<CustomWindZone> activeWindZones = new List<CustomWindZone>();
    
    // 현재 바람 힘
    private Vector2 currentWindForce = Vector2.zero;
    
    private void Awake()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
        
        if (groundDetector == null)
        {
            groundDetector = GetComponent<characterGround>();
        }
        
        if (umbrellaController == null)
        {
            umbrellaController = GetComponent<characterUmbrella>();
        }
    }
    
    private void FixedUpdate()
    {
        if (activeWindZones.Count == 0)
        {
            // 바람이 없으면 바람 힘을 0으로
            if (smoothWind)
            {
                currentWindForce = Vector2.Lerp(currentWindForce, Vector2.zero, windSmoothSpeed * Time.fixedDeltaTime);
            }
            else
            {
                currentWindForce = Vector2.zero;
            }
            return;
        }
        
        // 모든 활성 바람 존의 영향 계산
        Vector2 totalWindForce = Vector2.zero;
        
        foreach (CustomWindZone windZone in activeWindZones)
        {
            if (windZone == null) continue;
            
            // 공중에 있을 때만 영향을 받는 옵션 체크
            bool isGrounded = groundDetector != null && groundDetector.GetOnGround();
            if (windZone.OnlyAffectWhenAirborne && isGrounded)
            {
                if (debugLog)
                {
                    Debug.Log("[WindController] 지상에 있어서 바람 영향 무시");
                }
                continue;
            }
            
            // 글라이드 중인지 확인
            bool isGliding = umbrellaController != null && umbrellaController.IsActive;
            float multiplier = isGliding ? windZone.GlideWindMultiplier : windZone.NormalWindMultiplier;
            
            // 바람 힘 계산 (올바른 순서로 수정)
            float strength = windZone.WindStrength * multiplier;
            Vector2 windForce = windZone.WindDirection * strength;
            totalWindForce += windForce;
            
            if (debugLog)
            {
                Debug.Log($"[WindController] 바람 적용: Direction={windZone.WindDirection}, Strength={strength}, Force={windForce}");
            }
        }
        
        // 부드럽게 바람 적용
        if (smoothWind)
        {
            currentWindForce = Vector2.Lerp(currentWindForce, totalWindForce, windSmoothSpeed * Time.fixedDeltaTime);
        }
        else
        {
            currentWindForce = totalWindForce;
        }
        
        // 바람 적용
        if (currentWindForce.sqrMagnitude > 0.001f)
        {
            if (useForce)
            {
                // Force 방식
                body.AddForce(currentWindForce, ForceMode2D.Force);
                
                if (debugLog)
                {
                    Debug.Log($"[WindController] 바람 힘 적용: {currentWindForce}, 현재 속도: {body.linearVelocity}");
                }
            }
            else
            {
                // Velocity 방식
                Vector2 newVelocity = body.linearVelocity + currentWindForce * Time.fixedDeltaTime;
                body.linearVelocity = newVelocity;
            }
        }
    }
    
    /// <summary>
    /// 바람 존에 진입했을 때 호출
    /// </summary>
    public void EnterWindZone(CustomWindZone windZone)
    {
        if (windZone == null || activeWindZones.Contains(windZone))
        {
            return;
        }
        
        activeWindZones.Add(windZone);
        
        if (debugLog)
        {
            Debug.Log($"[WindController] 바람 존 진입: {windZone.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 바람 존에서 나갔을 때 호출
    /// </summary>
    public void ExitWindZone(CustomWindZone windZone)
    {
        if (windZone == null)
        {
            return;
        }
        
        activeWindZones.Remove(windZone);
        
        if (debugLog)
        {
            Debug.Log($"[WindController] 바람 존 퇴장: {windZone.gameObject.name}");
        }
    }
    
    /// <summary>
    /// 현재 바람의 영향을 받고 있는지 확인
    /// </summary>
    public bool IsAffectedByWind()
    {
        return activeWindZones.Count > 0;
    }
    
    /// <summary>
    /// 현재 바람 힘 반환
    /// </summary>
    public Vector2 GetCurrentWindForce()
    {
        return currentWindForce;
    }
}

