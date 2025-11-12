using System;
using System.Collections;
using UnityEngine;

//Handles umbrella activation, visuals, and glide parameters.
public class characterUmbrella : MonoBehaviour
{
    [SerializeField] private GameObject umbrellaObject;
    [SerializeField] private GameObject attackUmbrellaObject;
    [SerializeField] private GameObject dashUmbrellaObject;
    [SerializeField] private GameObject normalUmbrellaObject;
    [SerializeField] private Animator umbrellaAnimator;
    [SerializeField] private string umbrellaSpinBool = "IsSpin";
    [SerializeField] private string umbrellaAttackTrigger = "IsAttack";
    [SerializeField] private string umbrellaAttackBool = "IsSpin2";
    [SerializeField] private string umbrellaDashTrigger = "IsDash";

    [Header("Glide Settings")]
    [SerializeField, Tooltip("Hold jump for this long before the umbrella opens")] private float activationDelay = 0.5f;
    [SerializeField, Tooltip("Gravity multiplier to use while gliding downward")] private float glideGravityMultiplier = 0.05f;
    [SerializeField, Tooltip("Maximum downward speed while gliding (units per second)")] private float glideFallSpeedCap = 2.5f;

    // 공중 대시 중 낙하 제어
    [Header("Dash Fall (Air)")]
    [SerializeField, Tooltip("공중 대시 중 적용할 낙하 중력 배율(글라이드보다 조금 크게)")]
    private float aerialDashFallGravityMultiplier = 0.20f;   // 예: 글라이드 0.05보다 큼
    [SerializeField, Tooltip("공중 대시 중 최대 낙하 속도(0이면 비활성)")]
    private float aerialDashFallSpeedCap = 4.0f;             // 예: 글라이드 캡(2.5)보다 큼

    public float DashFallGravityMultiplier => aerialDashFallGravityMultiplier;
    public float DashFallSpeedCap => aerialDashFallSpeedCap;
    
    // 공중 공격 중 낙하 제어
    [Header("Attack Fall (Air)")]
    [SerializeField, Tooltip("공중 공격 중 적용할 낙하 중력 배율(대시와 동일하게 설정 권장)")]
    private float aerialAttackFallGravityMultiplier = 0.20f;
    [SerializeField, Tooltip("공중 공격 중 최대 낙하 속도")]
    private float aerialAttackFallSpeedCap = 4.0f;

    public float AttackFallGravityMultiplier => aerialAttackFallGravityMultiplier;
    public float AttackFallSpeedCap => aerialAttackFallSpeedCap;
    
    [Header("Dash Settings")]
    [SerializeField, Tooltip("Animation delay before dash starts")] private float dashAnimationDelay = 0.12f;
    [SerializeField, Tooltip("Total time the umbrella dash lasts")] private float dashDuration = 0.35f;
    [SerializeField, Tooltip("Horizontal impulse applied right when the dash starts")] private float dashInitialImpulse = 30f;
    [SerializeField, Tooltip("Force curve applied during dash (0-1 time)")] private AnimationCurve dashForceCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.2f);
    [SerializeField, Tooltip("Base horizontal force magnitude applied over dash duration")] private float dashForceMagnitude = 60f;
    [SerializeField, Tooltip("Extra upward force when forcing an upward dash in the air")] private float aerialUpwardDashForce = 12f;
    [SerializeField, Tooltip("Extra upward force when forcing an upward dash from the ground")] private float groundedUpwardDashForce = 18f;
    [SerializeField, Tooltip("Clamp dash velocity magnitude after applying forces (0 to disable)")] private float dashVelocityClamp = 25f;

    [Header("Grounded Down Dash Jump")]
    [SerializeField, Tooltip("Jump height multiplier for grounded down dash (based on normal jump height)")] private float groundedDownDashJumpMultiplier = 2.5f;
    [SerializeField, Tooltip("Maximum Y velocity for grounded down dash jump (0 to disable)")] private float groundedDownDashMaxVelocity = 25f;
    public float DownPoseJumpMultiplier => groundedDownDashJumpMultiplier;
    
    // ↓우산 상태에서 첫 점프에만 배율 적용하기 위한 플래그
    private bool superJumpAvailable = false;
    public bool IsSuperJumpAvailable => downPoseActive && superJumpAvailable;
    public void ConsumeSuperJump() { superJumpAvailable = false; }
    
    [Header("Dash Visuals")]
    [SerializeField] private Vector3 dashUmbrellaDefaultPosition = new Vector3(-1f, 0f, 0f);
    [SerializeField] private Vector3 dashUmbrellaDefaultRotation = new Vector3(0f, 0f, 90f);
    [SerializeField] private Vector3 dashUmbrellaDownPosition = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 dashUmbrellaDownRotation = new Vector3(0f, 0f, 180f);
    
    [Header("DownPose Ground")]
    [SerializeField] private UmbrellaGround umbrellaGroundSensor; // MOD: ↓우산 끝 접지 센서 참조
    private bool downPoseJumpUsed = false;                        // MOD: 접지 중 1회 점프 소모 플래그
    private bool prevTouching = false;                            // MOD: 접지 전프레임 상태

    public bool IsUmbrellaTouchingGround => umbrellaGroundSensor != null && umbrellaGroundSensor.IsTouchingGround; // MOD
    
    private bool umbrellaActive;
    private bool attackUmbrellaActive;
    private bool dashActive;
    
    private bool downPoseActive;                    // ↓-포즈(우산 아래 고정) 상태
    public bool IsDownPoseActive => downPoseActive; // 외부 조회용

    [SerializeField, Tooltip("-1은 무제한, 0 이상은 허용 대시 수")]
    private int maxAirDashes = -1;
    private int airDashesUsed = 0;
    
    private float umbrellaTimer;
    private float dashTimer;
    private bool dashAnimationPlaying;
    private Vector2 dashDirection = Vector2.right;
    private Rigidbody2D dashBody;
    private bool dashForceUpwards;
    private bool dashStartedOnGround;
    private Coroutine dashVisualRoutine;
    [SerializeField] private characterJump jumpScript;

    public bool IsActive => umbrellaActive;
    public float GlideGravityMultiplier => glideGravityMultiplier;
    public bool AttackUmbrellaActive => attackUmbrellaActive;
    public bool IsDashActive => dashActive;

    private void Awake()
    {
        if (jumpScript == null)
        {
            jumpScript = GetComponent<characterJump>();
        }
        if (umbrellaGroundSensor == null) // MOD: 자동 검색(없으면 에디터에서 할당)
        {
            umbrellaGroundSensor = GetComponentInChildren<UmbrellaGround>();
        }
        UpdateUmbrellaVisuals();
    }

    private void OnDisable()
    {
        ForceClose();
    }
    
    private void UpdateUmbrellaVisuals()
    {
        bool dashVisualsShowing = dashUmbrellaObject != null && dashUmbrellaObject.activeSelf;
        bool showNormal = !umbrellaActive && !attackUmbrellaActive && !dashActive && !dashVisualsShowing;

        if (normalUmbrellaObject != null)
        {
            normalUmbrellaObject.SetActive(showNormal);
        }

        if (umbrellaObject != null)
        {
            umbrellaObject.SetActive(umbrellaActive);
        }

        if (attackUmbrellaObject != null)
        {
            attackUmbrellaObject.SetActive(attackUmbrellaActive);
        }
    }
    
    public bool CanDownPoseJump() // MOD: ↓우산 점프 가능?
    {
        return downPoseActive && IsUmbrellaTouchingGround && !downPoseJumpUsed;
    }

    public void MarkDownPoseJumpUsed() // MOD: 점프 1회 소모
    {
        downPoseJumpUsed = true;
    }

    public void UpdateUmbrellaState(bool pressingJump, bool onGround, float deltaTime)
    {
        // ↓우산일 때는 글라이드 금지
        if (downPoseActive)
        {
            umbrellaTimer = 0f;
            if (umbrellaActive) SetUmbrellaActive(false);

            // MOD: 접지 전환 감지 → 닿는 순간마다 1회 점프 재충전
            bool touching = IsUmbrellaTouchingGround;
            if (touching && !prevTouching)
            {
                downPoseJumpUsed = false; // 새로 닿았으니 1회 충전
            }
            prevTouching = touching;

            return;
        }

        bool shouldAccumulate = !onGround && pressingJump;

        if (attackUmbrellaActive)
        {
            umbrellaTimer = 0f;
            SetUmbrellaActive(false);
            ClampGlideVelocityIfNeeded();
            return;
        }

        if (dashActive)
        {
            if (shouldAccumulate) umbrellaTimer += deltaTime;
            else umbrellaTimer = 0f;

            SetUmbrellaActive(false);
            ClampGlideVelocityIfNeeded();
            return;
        }

        if (shouldAccumulate)
        {
            umbrellaTimer += deltaTime;
            if (!umbrellaActive && umbrellaTimer >= activationDelay)
                SetUmbrellaActive(true);
        }
        else
        {
            umbrellaTimer = 0f;
            if (umbrellaActive) SetUmbrellaActive(false);
        }

        ClampGlideVelocityIfNeeded();
    }

    public void BeginDownPose()
    {
        SetUmbrellaActive(false);
        if (dashActive) StopDash();
        if (!downPoseActive && jumpScript != null && jumpScript.onGround)
        {
            PerformGroundedUmbrellaBoost();
        }
        downPoseActive = true;
        ShowDashUmbrellaVisual(useDownVariant: true, autoHideDelay: 0f);

        // ↓우산 진입 시 '첫 점프' 슈퍼점프 1회 충전
        superJumpAvailable = true;

        prevTouching = IsUmbrellaTouchingGround;
        if (prevTouching) downPoseJumpUsed = false;
    }

    public void EndDownPose()
    {
        downPoseActive = false;
        if (dashUmbrellaObject != null) dashUmbrellaObject.SetActive(false);
        SetUmbrellaActive(false);
        UpdateUmbrellaVisuals();

        // ↓우산 종료 시 슈퍼점프 초기화
        superJumpAvailable = false;
        downPoseJumpUsed = false;
        prevTouching = false;
    }
    
    public void NotifyGroundState(bool grounded)
    {
        if (grounded)
        {
            airDashesUsed = 0;
            // ↓우산 점프 소모는 우산 끝 접지 기준이므로 여기서는 건드리지 않음
        }
    }

    public void ForceClose()
    {
        umbrellaTimer = 0f;
        downPoseActive = false;   
        SetUmbrellaActive(false);
        SetAttackUmbrellaActive(false);
        StopDash();                 // 대시 상태/타이머 초기화
        airDashesUsed = 0;          // 공중 대시 사용 횟수 리셋 (dashUsedThisAir 대체)
    
        if (dashVisualRoutine != null)
        {
            StopCoroutine(dashVisualRoutine);
            dashVisualRoutine = null;
        }
        if (dashUmbrellaObject != null)
        {
            dashUmbrellaObject.SetActive(false);
        }
        UpdateUmbrellaVisuals();
    }


    private void SetUmbrellaActive(bool active)
    {
        if (umbrellaActive == active)
        {
            return;
        }

        umbrellaActive = active;
        UpdateUmbrellaVisuals();

        if (umbrellaAnimator != null && !string.IsNullOrEmpty(umbrellaSpinBool))
        {
            umbrellaAnimator.SetBool(umbrellaSpinBool, active);
        }

        if (active)
        {
            ClampGlideVelocityIfNeeded();
        }
    }

    // 외부에서 구독 가능한 공격 이벤트(1회 입력당 1번만 쏨)
    public event Action OnAttackFired;

    public bool TryTriggerAttackUmbrella()
    {
        if (IsDownPoseActive)
            EndDownPose();

        // 대시 중에만 공격 불가 (글라이드 중에는 공격 가능하도록 변경)
        if (dashActive)
            return false;

        // 글라이드 중이면 글라이드 끄기
        if (umbrellaActive)
        {
            SetUmbrellaActive(false);
            umbrellaTimer = 0f;
        }

        SetAttackUmbrellaActive(true);

        // ← 여기서 공격 이벤트 발사 (애니메이션 트리거와 동시에 한 번)
        OnAttackFired?.Invoke();

        return true;
    }

    public void ReleaseAttackUmbrella()
    {
        SetAttackUmbrellaActive(false);
    }

    private void SetAttackUmbrellaActive(bool active)
    {
        if (attackUmbrellaActive == active)
        {
            return;
        }

        attackUmbrellaActive = active;
        UpdateUmbrellaVisuals();

        if (umbrellaAnimator != null)
        {
            // IsAttack 트리거를 먼저 실행
            if (active && !string.IsNullOrEmpty(umbrellaAttackTrigger))
            {
                umbrellaAnimator.SetTrigger(umbrellaAttackTrigger);
            }

            // 그 다음 IsSpin2 bool 설정
            if (!string.IsNullOrEmpty(umbrellaAttackBool))
            {
                umbrellaAnimator.SetBool(umbrellaAttackBool, active);
            }
        }
    }

    public bool TryStartDash(float horizontalDirection, bool isGrounded, Rigidbody2D body, bool forceUpwards, bool hasHorizontalInput)
    {
        if (dashActive)
        {
            return false;
        }

        // 글라이드/공격 우산이 켜져 있으면 끄고 대시로 전환
        if (umbrellaActive)
        {
            SetUmbrellaActive(false);
            umbrellaTimer = 0f;
        }
        if (attackUmbrellaActive)
        {
            SetAttackUmbrellaActive(false);
        }

        // 공중 대시 횟수 체크
        if (!isGrounded)
        {
            if (maxAirDashes >= 0 && airDashesUsed >= maxAirDashes)
            {
                return false; // 허용 횟수 소진
            }
        }

        // 지상에서 강제 위부스트는 대시로 세지지 않음(그대로 유지)
        if (isGrounded && forceUpwards && jumpScript != null)
        {
            PerformGroundedUmbrellaBoost();
            ShowDashUmbrellaVisual(true, 0.15f);
            return true;
        }

        bool hasDirectionalInput = hasHorizontalInput && !Mathf.Approximately(horizontalDirection, 0f);
        Vector2 lateralDirection = hasDirectionalInput ? new Vector2(Mathf.Sign(horizontalDirection), 0f) : Vector2.zero;
        if (lateralDirection == Vector2.zero && !forceUpwards)
        {
            lateralDirection = new Vector2(Mathf.Approximately(horizontalDirection, 0f) ? 1f : Mathf.Sign(horizontalDirection), 0f);
        }
        dashDirection = lateralDirection.sqrMagnitude > 0f ? lateralDirection.normalized : Vector2.zero;
        dashForceUpwards = forceUpwards;
        dashStartedOnGround = isGrounded;
        dashTimer = 0f;
        dashActive = true;
        dashAnimationPlaying = true;
        dashBody = body;

        // 공중에서 시작했다면 사용 횟수 증가
        if (!isGrounded)
        {
            airDashesUsed++;
        }

        ShowDashUmbrellaVisual(forceUpwards, 0f);

        if (umbrellaAnimator != null && !string.IsNullOrEmpty(umbrellaDashTrigger))
        {
            umbrellaAnimator.SetTrigger(umbrellaDashTrigger);
        }

        SetUmbrellaActive(false);
        SetAttackUmbrellaActive(false);

        return true;
    }

    public void UpdateDash(float deltaTime)
    {
        if (!dashActive)
        {
            return;
        }

        dashTimer += deltaTime;

        // 애니메이션 재생 중에는 대시 동작 시작 안 함
        if (dashAnimationPlaying)
        {
            if (dashTimer >= dashAnimationDelay)
            {
                dashAnimationPlaying = false;
                
                // 애니메이션이 끝나면 impulse 적용
                if (dashBody != null && dashInitialImpulse > 0f)
                {
                    Vector2 impulseDirection;
                    if (dashForceUpwards)
                    {
                        impulseDirection = dashDirection.sqrMagnitude > 0f ? (dashDirection + Vector2.up).normalized : Vector2.up;
                    }
                    else
                    {
                        impulseDirection = dashDirection.sqrMagnitude > 0f ? dashDirection : new Vector2(1f, 0f);
                    }

                    dashBody.AddForce(impulseDirection * dashInitialImpulse, ForceMode2D.Impulse);
                }
            }
            return;
        }

        // 아래 대시 점프는 빠르게 종료
        if (dashBody == null && dashTimer >= dashAnimationDelay + 0.1f)
        {
            StopDash();
            return;
        }

        if (dashBody != null)
        {
            float actualDashTime = dashTimer - dashAnimationDelay;
            float normalizedTime = dashDuration <= 0f ? 1f : Mathf.Clamp01(actualDashTime / dashDuration);
            float curveValue = dashForceCurve != null ? dashForceCurve.Evaluate(normalizedTime) : 1f;
            if (dashDirection.sqrMagnitude > 0f)
            {
                Vector2 lateralForce = dashDirection * dashForceMagnitude * curveValue;
                dashBody.AddForce(lateralForce * deltaTime, ForceMode2D.Force);
            }

            if (dashForceUpwards)
            {
                float upwardForce = (dashStartedOnGround ? groundedUpwardDashForce : aerialUpwardDashForce) * curveValue;
                if (upwardForce > 0f)
                {
                    dashBody.AddForce(Vector2.up * upwardForce * deltaTime, ForceMode2D.Force);
                }
            }

            if (dashVelocityClamp > 0f)
            {
                Vector2 currentVelocity = dashBody.linearVelocity;
                float currentMagnitude = currentVelocity.magnitude;
                if (currentMagnitude > dashVelocityClamp)
                {
                    currentVelocity = currentVelocity.normalized * dashVelocityClamp;
                    dashBody.linearVelocity = currentVelocity;
                }
            }
        }

        if (dashBody != null && aerialDashFallSpeedCap > 0f)
        {
            Vector2 v = dashBody.linearVelocity;
            float cap = -Mathf.Abs(aerialDashFallSpeedCap);
            if (v.y < cap)
            {
                v.y = cap;
                dashBody.linearVelocity = v;
            }
        }

        if (dashTimer >= dashAnimationDelay + dashDuration)
        {
            StopDash();
        }
    }

    private void StopDash()
    {
        if (!dashActive)
        {
            return;
        }

        dashActive = false;
        dashAnimationPlaying = false;
        dashTimer = 0f;
        dashBody = null;
        dashForceUpwards = false;
        if (dashVisualRoutine != null)
        {
            StopCoroutine(dashVisualRoutine);
            dashVisualRoutine = null;
        }

        if (dashUmbrellaObject != null)
        {
            dashUmbrellaObject.SetActive(false);
        }
        UpdateUmbrellaVisuals();
    }

    private void PerformGroundedUmbrellaBoost()
    {
        if (jumpScript == null || jumpScript.body == null)
        {
            return;
        }

        // 초기 입력 정리만 하고, 점프는 차단하지 않는다
        jumpScript.desiredJump = false;
        jumpScript.jumpBufferCounter = 0f;
        jumpScript.umbrellaBoostJumpBlocked = false;
        jumpScript.umbrellaBoostJumpBlockTimer = 0f;
        
        // 현재 Y 속도를 0으로 초기화 (바닥에서 시작하므로)
        Vector2 currentVelocity = jumpScript.body.linearVelocity;
        currentVelocity.y = 0f;
        jumpScript.body.linearVelocity = currentVelocity;

        // 일반 점프 메커니즘을 사용하여 부드러운 점프 구현
        float originalJumpHeight = jumpScript.jumpHeight;
        float boostedJumpHeight = originalJumpHeight * groundedDownDashJumpMultiplier;

        // 점프 속도 계산 (일반 점프와 동일한 공식 사용)
        float baseGravity = (-2f * boostedJumpHeight) / (jumpScript.timeToJumpApex * jumpScript.timeToJumpApex);
        float jumpSpeed = Mathf.Sqrt(-2f * baseGravity * boostedJumpHeight);

        // velocity를 직접 설정 (Impulse 대신)
        currentVelocity.y = jumpSpeed;

        // Y velocity 최대값 제한 (설정된 경우)
        if (groundedDownDashMaxVelocity > 0f)
        {
            currentVelocity.y = Mathf.Min(currentVelocity.y, groundedDownDashMaxVelocity);
        }

        jumpScript.body.linearVelocity = currentVelocity;

        // 점프 상태로 설정 (글라이드 즉시 사용 가능하도록)
        jumpScript.velocity = currentVelocity;
        jumpScript.currentlyJumping = true;

        // 공중 점프 방지 (우산 부스트 점프는 이미 점프로 간주)
        jumpScript.canJumpAgain = false;
    }

    private void ShowDashUmbrellaVisual(bool useDownVariant, float autoHideDelay)
    {
        if (dashUmbrellaObject == null)
        {
            return;
        }

        dashUmbrellaObject.SetActive(true);
        Vector3 position = useDownVariant ? dashUmbrellaDownPosition : dashUmbrellaDefaultPosition;
        Vector3 rotation = useDownVariant ? dashUmbrellaDownRotation : dashUmbrellaDefaultRotation;
        dashUmbrellaObject.transform.localPosition = position;
        dashUmbrellaObject.transform.localEulerAngles = rotation;
        UpdateUmbrellaVisuals();

        if (dashVisualRoutine != null)
        {
            StopCoroutine(dashVisualRoutine);
            dashVisualRoutine = null;
        }

        if (autoHideDelay > 0f)
        {
            dashVisualRoutine = StartCoroutine(HideDashUmbrellaAfterDelay(autoHideDelay));
        }
    }

    private IEnumerator HideDashUmbrellaAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!dashActive && dashUmbrellaObject != null)
        {
            dashUmbrellaObject.SetActive(false);
        }
        dashVisualRoutine = null;
        UpdateUmbrellaVisuals();
    }

    private void ClampGlideVelocityIfNeeded()
    {
        if (!umbrellaActive)
        {
            return;
        }

        if (glideFallSpeedCap <= 0f || jumpScript == null || jumpScript.body == null)
        {
            return;
        }

        Vector2 currentVelocity = jumpScript.body.linearVelocity;
        float maxDownwardSpeed = -Mathf.Abs(glideFallSpeedCap);
        if (currentVelocity.y < maxDownwardSpeed)
        {
            currentVelocity.y = maxDownwardSpeed;
            jumpScript.body.linearVelocity = currentVelocity;
        }
    }
}