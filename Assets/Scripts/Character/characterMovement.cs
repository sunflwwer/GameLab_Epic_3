using UnityEngine;
using GMTK.PlatformerToolkit;

//This script handles moving the character on the X axis, both on the ground and in the air.

public class characterMovement : MonoBehaviour
{

    [Header("Components")]
    [SerializeField] movementLimiter moveLimit;
    [SerializeField] characterInputManager inputManager;
    [SerializeField] characterUmbrella umbrellaController;
    [SerializeField] private PlayerHookController hookController;
    private Rigidbody2D body;
    characterGround ground;

    [Header("Movement Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("Maximum movement speed")] public float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed")] public float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop after letting go")] public float maxDecceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction")] public float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed when in mid-air")] public float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop in mid-air when no direction is used")] public float maxAirDeceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction when in mid-air")] public float maxAirTurnSpeed = 80f;
    [SerializeField][Tooltip("Friction to apply against movement on stick")] private float friction;
    [SerializeField][Tooltip("Maximum velocity limit (prevents being pushed too fast)")] private float maxVelocityLimit = 25f;

    [Header("Options")]
    [Tooltip("When false, the charcter will skip acceleration and deceleration and instantly move and stop")] public bool useAcceleration;
    public bool itsTheIntro = true;

    [Header("Calculations")]
    public float directionX;
    private Vector2 desiredVelocity;
    public Vector2 velocity;
    private float maxSpeedChange;
    private float acceleration;
    private float deceleration;
    private float turnSpeed;

    [Header("Current State")]
    public bool onGround;
    public bool pressingKey;
    
    private bool ignoreInput = false;
    private float ignoreInputTimer = 0f;

    private void Awake()
    {
        //Find the character's Rigidbody and ground detection script
        body = GetComponent<Rigidbody2D>();
        ground = GetComponent<characterGround>();
    }

    private void Update()
    {
        // Hook 중일 때 이동 차단
        bool isHooking = hookController != null && hookController.IsHooking;
        
        // 입력 무시 타이머 처리
        if (ignoreInput)
        {
            ignoreInputTimer -= Time.deltaTime;
            if (ignoreInputTimer <= 0f)
            {
                ignoreInput = false;
            }
        }
        
        if (moveLimit.characterCanMove && inputManager != null && !isHooking)
        {
            // 입력 무시 중이 아닐 때만 입력 적용
            if (!ignoreInput)
            {
                directionX = inputManager.moveInput.x;
            }
            // 입력 무시 중에는 directionX를 0으로 설정하지만 입력은 계속 받음
            else
            {
                directionX = 0;
            }
        }
        else if (isHooking)
        {
            // Hook 중에는 이동 입력 완전히 차단
            directionX = 0;
        }

        //Used to stop movement when the character is playing her death animation
        if (!moveLimit.characterCanMove && !itsTheIntro)
        {
            directionX = 0;
        }

        //Used to flip the character's sprite when she changes direction
        //Also tells us that we are currently pressing a direction button
        if (directionX != 0)
        {
            transform.localScale = new Vector3(directionX > 0 ? 1 : -1, 1, 1);
            pressingKey = true;
        }
        else
        {
            pressingKey = false;
        }

        //Calculate's the character's desired velocity - which is the direction you are facing, multiplied by the character's maximum speed
        //Friction is not used in this game
        desiredVelocity = new Vector2(directionX, 0f) * Mathf.Max(maxSpeed - friction, 0f);

    }

    private void FixedUpdate()
    {
        //Fixed update runs in sync with Unity's physics engine

        //Get Kit's current ground status from her ground script
        onGround = ground.GetOnGround();

        //Get the Rigidbody's current velocity
        velocity = body.linearVelocity;

        if (umbrellaController != null && umbrellaController.IsDashActive)
        {
            //Leave velocity untouched while dash impulse controls movement
            return;
        }

        //Calculate movement, depending on whether "Instant Movement" has been checked
        if (useAcceleration)
        {
            runWithAcceleration();
        }
        else
        {
            if (onGround)
            {
                runWithoutAcceleration();
            }
            else
            {
                runWithAcceleration();
            }
        }
    }

    private void runWithAcceleration()
    {
        //Set our acceleration, deceleration, and turn speed stats, based on whether we're on the ground on in the air

        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        if (pressingKey)
        {
            //If the sign (i.e. positive or negative) of our input direction doesn't match our movement, it means we're turning around and so should use the turn speed stat.
            if (Mathf.Sign(directionX) != Mathf.Sign(velocity.x))
            {
                maxSpeedChange = turnSpeed * Time.deltaTime;
            }
            else
            {
                //If they match, it means we're simply running along and so should use the acceleration stat
                maxSpeedChange = acceleration * Time.deltaTime;
            }
        }
        else
        {
            //And if we're not pressing a direction at all, use the deceleration stat
            maxSpeedChange = deceleration * Time.deltaTime;
        }

        //Move our velocity towards the desired velocity, at the rate of the number calculated above
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);

        //Limit overall velocity to prevent being pushed too fast by external forces
        velocity = Vector2.ClampMagnitude(velocity, maxVelocityLimit);

        //Update the Rigidbody with this new velocity
        body.linearVelocity = velocity;

    }

    private void runWithoutAcceleration()
    {
        //If we're not using acceleration and deceleration, just send our desired velocity (direction * max speed) to the Rigidbody
        velocity.x = desiredVelocity.x;

        //Limit overall velocity to prevent being pushed too fast by external forces
        velocity = Vector2.ClampMagnitude(velocity, maxVelocityLimit);

        body.linearVelocity = velocity;
    }

    // 외부에서 호출 가능한 입력 무시 메서드
    public void IgnoreInputForDuration(float duration)
    {
        ignoreInput = true;
        ignoreInputTimer = duration;
    }


}
