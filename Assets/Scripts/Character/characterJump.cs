using UnityEngine;
using GMTK.PlatformerToolkit;
//This script handles moving the character on the Y axis, for jumping and gravity

public class characterJump : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public Rigidbody2D body;
    private characterGround ground;
    [HideInInspector] public Vector2 velocity;
    private characterJuice juice;
    [SerializeField] movementLimiter moveLimit;
    [SerializeField] characterInputManager inputManager;
    [SerializeField] private characterSwing swingController;

    [Header("Jumping Stats")]
    [SerializeField, Range(2f, 5.5f)][Tooltip("Maximum jump height")] public float jumpHeight = 7.3f;


//If you're using your stats from Platformer Toolkit with this character controller, please note that the number on the Jump Duration handle does not match this stat
//It is re-scaled, from 0.2f - 1.25f, to 1 - 10.
//You can transform the number on screen to the stat here, using the function at the bottom of this script



    [SerializeField, Range(0.2f, 1.25f)][Tooltip("How long it takes to reach that height before coming back down")] public float timeToJumpApex;
    [SerializeField, Range(0f, 5f)][Tooltip("Gravity multiplier to apply when going up")] public float upwardMovementMultiplier = 1f;
    [SerializeField, Range(1f, 10f)][Tooltip("Gravity multiplier to apply when coming down")] public float downwardMovementMultiplier = 6.17f;
    [SerializeField, Range(0, 1)][Tooltip("How many times can you jump in the air?")] public int maxAirJumps = 0;

    [Header("Options")]
    [Tooltip("Should the character drop when you let go of jump?")] public bool variablejumpHeight;
    [SerializeField, Range(1f, 10f)][Tooltip("Gravity multiplier when you let go of jump")] public float jumpCutOff;
    [SerializeField][Tooltip("The fastest speed the character can fall")] public float speedLimit;
    [SerializeField, Range(0f, 0.3f)][Tooltip("How long should coyote time last?")] public float coyoteTime = 0.15f;
    [SerializeField, Range(0f, 0.3f)][Tooltip("How far from ground should we cache your jump?")] public float jumpBuffer = 0.15f;
    [Header("Umbrella Glide")]
    [SerializeField] private characterUmbrella umbrellaController;

    [Header("Calculations")]
    public float jumpSpeed;
    private float defaultGravityScale;
    public float gravMultiplier;

    [Header("Current State")]
    public bool canJumpAgain = false;
    public bool desiredJump;
    public float jumpBufferCounter;
    private float coyoteTimeCounter = 0;
    private bool pressingJump;
    public bool onGround;
    public bool currentlyJumping;
    public bool umbrellaBoostJumpBlocked = false;
    public float umbrellaBoostJumpBlockTimer = 0f;
    private bool downBoostUsedInThisPress = false;

    void Awake()
    {
        //Find the character's Rigidbody and ground detection and juice scripts

        body = GetComponent<Rigidbody2D>();
        ground = GetComponent<characterGround>();
        juice = GetComponentInChildren<characterJuice>();
        defaultGravityScale = 1f;
        if (swingController == null)
        {
            swingController = GetComponent<characterSwing>();
        }
    }

    void Update()
    {
        //Check if we're on ground, using Kit's Ground script
        onGround = ground.GetOnGround();
        umbrellaController?.NotifyGroundState(onGround);

        bool umbrellaActionConsumed = false;
        bool umbrellaDashConsumed = false;

        bool swingLocked = swingController != null && swingController.IsSwinging;

        if (inputManager != null)
        {
            pressingJump = inputManager.isPressingJump;
            bool umbrellaActionHeld = inputManager.isUmbrellaActionHeld;

            if (umbrellaController != null && swingLocked)
            {
                umbrellaController.ReleaseAttackUmbrella();
            }
            
            // New logic for down-boost
            if (onGround && umbrellaController != null && inputManager.moveInput.y < -0.5f && !downBoostUsedInThisPress)
            {
                // Use facing direction for the boost
                float facingDir = Mathf.Sign(transform.localScale.x);
                umbrellaController.TryStartDash(facingDir, onGround, body, true, false);
                umbrellaDashConsumed = true; // Prevents regular jump
                downBoostUsedInThisPress = true;
            }

            if (inputManager.moveInput.y >= -0.5f)
            {
                downBoostUsedInThisPress = false;
            }

            if (!swingLocked && umbrellaController != null && inputManager.ConsumeUmbrellaAction())
            {
                umbrellaController.TryTriggerAttackUmbrella();
                umbrellaActionConsumed = true;
            }

            // Modified old dash logic
            if (!swingLocked && umbrellaController != null && inputManager.ConsumeUmbrellaDash())
            {
                float facingDir = Mathf.Sign(transform.localScale.x);
                float inputDir = Mathf.Abs(inputManager.moveInput.x) > 0.1f ? Mathf.Sign(inputManager.moveInput.x) : 0f;
                bool hasHorizontalInput = !Mathf.Approximately(inputDir, 0f);
                float dashDirection = hasHorizontalInput ? inputDir : (Mathf.Approximately(facingDir, 0f) ? 1f : facingDir);

                // forceUpwards is now false because it's handled by the new logic above
                bool forceUpwards = false;
                umbrellaController.TryStartDash(dashDirection, onGround, body, forceUpwards, hasHorizontalInput);
                umbrellaDashConsumed = true;
            }

            if (umbrellaController != null && !umbrellaActionHeld)
            {
                umbrellaController.ReleaseAttackUmbrella();
            }
        }
        else
        {
            pressingJump = false;
        }

        umbrellaController?.UpdateUmbrellaState(pressingJump, onGround, Time.deltaTime);

        // 점프 입력은 우산 대시/액션 처리 후에 처리 (우산 대시와 점프 중복 방지)
        if (inputManager != null && inputManager.ConsumeJumpInput())
        {
            // 우산 대시나 액션이 이번 프레임에 실행되었으면 점프 입력 무시
            if (umbrellaDashConsumed || umbrellaActionConsumed)
            {
                // Ignored
            }
            else if (!umbrellaBoostJumpBlocked)
            {
                desiredJump = true;
            }
        }

        // 우산 부스트 점프 블록 타이머 업데이트
        if (umbrellaBoostJumpBlocked)
        {
            umbrellaBoostJumpBlockTimer -= Time.deltaTime;
            if (umbrellaBoostJumpBlockTimer <= 0f)
            {
                umbrellaBoostJumpBlocked = false;
                // 블록 해제 시 desiredJump도 강제로 false (누적된 입력 방지)
                desiredJump = false;
                jumpBufferCounter = 0f;
            }
        }

        setPhysics();

        //Jump buffer allows us to queue up a jump, which will play when we next hit the ground
        if (jumpBuffer > 0)
        {
            //Instead of immediately turning off "desireJump", start counting up...
            //All the while, the DoAJump function will repeatedly be fired off
            if (desiredJump)
            {
                jumpBufferCounter += Time.deltaTime;

                if (jumpBufferCounter > jumpBuffer)
                {
                    //If time exceeds the jump buffer, turn off "desireJump"
                    desiredJump = false;
                    jumpBufferCounter = 0;
                }
            }
        }

        //If we're not on the ground and we're not currently jumping, that means we've stepped off the edge of a platform.
        //So, start the coyote time counter...
        if (!currentlyJumping && !onGround)
        {
            coyoteTimeCounter += Time.deltaTime;
        }
        else
        {
            //Reset it when we touch the ground, or jump
            coyoteTimeCounter = 0;
        }
    }

    private void setPhysics()
    {
        //Determine the character's gravity scale, using the stats provided. Multiply it by a gravMultiplier, used later
        Vector2 newGravity = new Vector2(0, (-2 * jumpHeight) / (timeToJumpApex * timeToJumpApex));
        body.gravityScale = (newGravity.y / Physics2D.gravity.y) * gravMultiplier;
    }

    private void FixedUpdate()
    {
        umbrellaController?.UpdateDash(Time.fixedDeltaTime);
        bool dashActive = umbrellaController != null && umbrellaController.IsDashActive;

        //Get velocity from Kit's Rigidbody
        velocity = body.linearVelocity;

        //Keep trying to do a jump, for as long as desiredJump is true
        if (!dashActive && desiredJump && !umbrellaBoostJumpBlocked)
        {
            DoAJump();
            body.linearVelocity = velocity;

            //Skip gravity calculations this frame, so currentlyJumping doesn't turn off
            //This makes sure you can't do the coyote time double jump bug
            return;
        }

        calculateGravity();
    }

    public void TriggerUmbrellaBoost(float multiplier, float extraImpulse, float velocityCap)
    {
        float baseJumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * body.gravityScale * jumpHeight);
        float boostedSpeed = baseJumpSpeed * multiplier;
        float currentYVelocity = body.linearVelocity.y;

        if (extraImpulse > 0f)
        {
            body.AddForce(Vector2.up * extraImpulse, ForceMode2D.Impulse);
            currentYVelocity = body.linearVelocity.y;
        }

        if (boostedSpeed > currentYVelocity)
        {
            currentYVelocity = boostedSpeed;
        }

        if (velocityCap > 0f && currentYVelocity > velocityCap)
        {
            currentYVelocity = velocityCap;
        }

        velocity = body.linearVelocity;
        velocity.y = currentYVelocity;
        body.linearVelocity = velocity;
        currentlyJumping = true;

        desiredJump = false;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        if (juice != null)
        {
            juice.jumpEffects();
        }
    }

    private void calculateGravity()
    {
        //We change the character's gravity based on her Y direction

        //If we're grounded and not already heading up, force a tiny downward velocity so the ground check clears immediately.
        if (onGround && body.linearVelocity.y <= 0.01f)
        {
            currentlyJumping = false;
            gravMultiplier = defaultGravityScale;
            float groundedVelocityY = -2f;
            velocity.y = groundedVelocityY;
            body.linearVelocity = new Vector2(body.linearVelocity.x, groundedVelocityY);
            return;
        }

        //If Kit is going up...
        if (body.linearVelocity.y > 0.01f)
        {
            if (onGround)
            {
                //Don't change it if Kit is stood on something (such as a moving platform)
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                //If we're using variable jump height...)
                if (variablejumpHeight)
                {
                    //Apply upward multiplier if player is rising and holding jump
                    if (pressingJump && currentlyJumping)
                    {
                        gravMultiplier = upwardMovementMultiplier;
                    }
                    //But apply a special downward multiplier if the player lets go of jump
                    else
                    {
                        gravMultiplier = jumpCutOff;
                    }
                }
                else
                {
                    gravMultiplier = upwardMovementMultiplier;
                }
            }
        }

        //Else if going down...
        else if (body.linearVelocity.y < -0.01f)
        {

            if (onGround)
            //Don't change it if Kit is stood on something (such as a moving platform)
            {
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                bool umbrellaActive = umbrellaController != null && umbrellaController.IsActive;
                //Otherwise, apply the downward gravity multiplier as Kit comes back to Earth
                gravMultiplier = umbrellaActive ? umbrellaController.GlideGravityMultiplier : downwardMovementMultiplier;
            }

        }
        //Else not moving vertically at all
        else
        {
            if (onGround)
            {
                currentlyJumping = false;
            }

            gravMultiplier = defaultGravityScale;
        }

        //Set the character's Rigidbody's velocity
        //But clamp the Y variable within the bounds of the speed limit, for the terminal velocity assist option
        body.linearVelocity = new Vector3(velocity.x, Mathf.Clamp(velocity.y, -speedLimit, 100));
    }

    private void DoAJump()
    {

        //Create the jump, provided we are on the ground, in coyote time, or have a double jump available
        if (onGround || (coyoteTimeCounter > 0.03f && coyoteTimeCounter < coyoteTime) || canJumpAgain)
        {
            desiredJump = false;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;

            if (onGround)
            {
                //Clear out any residual downward velocity so buffered inputs don't stack jump force.
                velocity.y = 0f;
            }

            //If we have double jump on, allow us to jump again (but only once)
            canJumpAgain = (maxAirJumps == 1 && canJumpAgain == false);

            //Determine the power of the jump, based purely on jump stats (ignore current gravity scale so buffered landings can't inflate the jump)
            float baseGravity = (-2f * jumpHeight) / (timeToJumpApex * timeToJumpApex);
            jumpSpeed = Mathf.Sqrt(-2f * baseGravity * jumpHeight);

            //If Kit is moving up or down when she jumps (such as when doing a double jump), change the jumpSpeed;
            //This will ensure the jump is the exact same strength, no matter your velocity.
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }

            //Apply the new jumpSpeed to the velocity. It will be sent to the Rigidbody in FixedUpdate;
            velocity.y = jumpSpeed;

            currentlyJumping = true;

            if (juice != null)
            {
                //Apply the jumping effects on the juice script
                juice.jumpEffects();
            }
        }

        if (jumpBuffer == 0)
        {
            //If we don't have a jump buffer, then turn off desiredJump immediately after hitting jumping
            desiredJump = false;
        }
    }

    public void bounceUp(float bounceAmount)
    {
        //Used by the springy pad
        body.AddForce(Vector2.up * bounceAmount, ForceMode2D.Impulse);
    }


/*

timeToApexStat = scale(1, 10, 0.2f, 2.5f, numberFromPlatformerToolkit)


  public float scale(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
    {

        float OldRange = (OldMax - OldMin);
        float NewRange = (NewMax - NewMin);
        float NewValue = (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;

        return (NewValue);
    }

*/




}
