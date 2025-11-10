using UnityEngine;
using GMTK.PlatformerToolkit;

[RequireComponent(typeof(Rigidbody2D))]
public class characterSwing : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private DistanceJoint2D swingJoint;
    [SerializeField] private characterInputManager inputManager;
    [SerializeField] private characterJump jumpController;
    [SerializeField] private movementLimiter moveLimit;
    [SerializeField] private characterGround groundDetector;
    [SerializeField] private Transform aimOrigin;

    [Header("Swing Detection")]
    [SerializeField] private LayerMask swingableLayers;
    [SerializeField, Range(1f, 20f)] private float maxProbeDistance = 12f;
    [SerializeField, Range(0.05f, 1f)] private float probeRadius = 0.25f;
    [SerializeField, Range(0.5f, 10f)] private float minRopeLength = 1.25f;
    [SerializeField, Range(1f, 20f)] private float maxRopeLength = 8f;
    [SerializeField, Range(0f, 1f)] private float verticalAimBias = 0.35f;

    [Header("Swing Control")]
    [SerializeField, Range(5f, 200f)] private float pumpForce = 60f;
    [SerializeField, Range(0.5f, 8f)] private float ropeReelSpeed = 3f;
    [SerializeField, Range(1f, 25f)] private float ropeLengthLerpSpeed = 10f;
    [SerializeField, Range(0f, 20f)] private float groundReleaseVelocity = 5f;
    [SerializeField, Range(0f, 0.6f)] private float reattachDelay = 0.2f;
    [SerializeField] private bool autoDetachOnGround = true;

    [Header("Anchor Assist")]
    [SerializeField] private bool enableAnchorAssist = true;
    [SerializeField, Range(1, 24)] private int maxAnchorCandidates = 12;

    [Header("Rope Visuals")]
    [SerializeField] private LineRenderer ropeLine;
    [SerializeField] private bool ropeLineUseWorldSpace = true;
    [SerializeField] private Transform ropeGraphic;
    [SerializeField] private bool stretchRopeGraphic = true;
    [SerializeField, Range(0.01f, 5f)] private float ropeGraphicLengthScale = 1f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug;

    private Rigidbody2D body;
    private bool isSwinging;
    private float cooldown;
    private Rigidbody2D attachedBody;
    private Vector2 cachedAnchorWorldPosition;
    private Vector2 lastProbeDirection = Vector2.up;
    private readonly Collider2D[] anchorBuffer = new Collider2D[24];
    private Vector3 ropeGraphicDefaultScale = Vector3.one;

    public bool IsSwinging => isSwinging;

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        swingJoint = GetComponent<DistanceJoint2D>();
        inputManager = GetComponent<characterInputManager>();
        jumpController = GetComponent<characterJump>();
        moveLimit = GetComponent<movementLimiter>();
        groundDetector = GetComponent<characterGround>();
        aimOrigin = transform;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (swingJoint == null)
        {
            swingJoint = GetComponent<DistanceJoint2D>();
            if (swingJoint == null)
            {
                swingJoint = gameObject.AddComponent<DistanceJoint2D>();
            }
        }

        swingJoint.autoConfigureDistance = false;
        swingJoint.enableCollision = true;
        swingJoint.enabled = false;

        if (aimOrigin == null)
        {
            aimOrigin = transform;
        }

        if (jumpController == null)
        {
            jumpController = GetComponent<characterJump>();
        }

        if (ropeLine != null)
        {
            ropeLine.enabled = false;
            ropeLine.positionCount = 0;
            ropeLine.useWorldSpace = ropeLineUseWorldSpace;
        }

        if (ropeGraphic != null)
        {
            ropeGraphicDefaultScale = ropeGraphic.localScale;
            ropeGraphic.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        cooldown -= Time.deltaTime;
        bool canReadInput = inputManager != null && (moveLimit == null || moveLimit.characterCanMove);

        if (!canReadInput)
        {
            if (isSwinging)
            {
                ReleaseSwing(false);
            }
            return;
        }

        bool pressed = inputManager.ConsumeInteract();
        bool jumpPressedWhileSwing = false;
        if (isSwinging && inputManager != null)
        {
            jumpPressedWhileSwing = inputManager.ConsumeJumpInput();
        }

        if (!isSwinging)
        {
            if (pressed && cooldown <= 0f)
            {
                TryAttachToAnchor();
            }
        }
        else
        {
            bool grounded = groundDetector != null && groundDetector.GetOnGround();
            bool shouldDrop = autoDetachOnGround && grounded && body.linearVelocity.magnitude <= groundReleaseVelocity;

            if (jumpPressedWhileSwing)
            {
                TriggerSwingJump();
            }
            else if (pressed)
            {
                ReleaseSwing(true);
            }
            else if (shouldDrop)
            {
                ReleaseSwing(true);
            }
            else
            {
                UpdateSwingForces(Time.deltaTime);
            }
        }

        UpdateRopeVisuals();
    }

    private void TryAttachToAnchor()
    {
        if (maxProbeDistance <= 0f)
        {
            return;
        }

        Vector2 origin = aimOrigin != null ? (Vector2)aimOrigin.position : body.worldCenterOfMass;
        Vector2 direction = DetermineAimDirection();
        lastProbeDirection = direction;

        bool attached = false;
        if (enableAnchorAssist)
        {
            attached = TryAttachUsingAnchorAssist(origin, direction);
        }

        if (!attached)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origin, Mathf.Max(0.01f, probeRadius), direction, maxProbeDistance, swingableLayers);
            if (hit.collider != null)
            {
                AttachToAnchorPoint(hit.point, hit.rigidbody, hit.distance, hit.collider.name);
                attached = true;
            }
        }

    }

    private bool TryAttachUsingAnchorAssist(Vector2 origin, Vector2 direction)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, maxProbeDistance, anchorBuffer, swingableLayers);
        int limit = Mathf.Min(hitCount, Mathf.Clamp(maxAnchorCandidates, 1, anchorBuffer.Length));

        swingAnchorPoint bestAnchor = null;
        Vector2 bestPoint = Vector2.zero;
        Rigidbody2D bestBody = null;
        float bestDistance = 0f;
        float bestScore = float.MinValue;

        for (int i = 0; i < limit; i++)
        {
            Collider2D candidate = anchorBuffer[i];
            if (candidate == null)
            {
                continue;
            }

            swingAnchorPoint anchor = candidate.GetComponent<swingAnchorPoint>();
            if (anchor == null)
            {
                continue;
            }

            Vector2 anchorPos = anchor.AnchorWorldPosition;
            Vector2 toAnchor = anchorPos - origin;
            float distance = toAnchor.magnitude;
            if (distance > anchor.MagnetRadius)
            {
                continue;
            }

            if (toAnchor.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            float alignment = Vector2.Dot(direction, toAnchor.normalized);
            if (alignment < anchor.MinAlignment)
            {
                continue;
            }

            float score = anchor.EvaluateScore(alignment, distance);
            if (score > bestScore)
            {
                bestScore = score;
                bestAnchor = anchor;
                bestPoint = anchorPos;
                bestBody = anchor.TargetRigidbody;
                bestDistance = distance;
            }
        }

        if (bestAnchor != null)
        {
            AttachToAnchorPoint(bestPoint, bestBody, bestDistance, bestAnchor.name);
            
            return true;
        }

        return false;
    }

    private void AttachToAnchorPoint(Vector2 anchorPoint, Rigidbody2D connectedBody, float initialDistance, string debugName)
    {
        attachedBody = connectedBody;
        if (attachedBody != null)
        {
            swingJoint.connectedBody = attachedBody;
            swingJoint.connectedAnchor = attachedBody.transform.InverseTransformPoint(anchorPoint);
        }
        else
        {
            swingJoint.connectedBody = null;
            swingJoint.connectedAnchor = anchorPoint;
        }

        float ropeLength = Mathf.Clamp(initialDistance, minRopeLength, maxRopeLength);
        swingJoint.distance = ropeLength;
        swingJoint.enabled = true;
        cachedAnchorWorldPosition = anchorPoint;
        isSwinging = true;
        
    }

    private void UpdateSwingForces(float deltaTime)
    {
        Vector2 anchor = GetAnchorWorldPosition();
        cachedAnchorWorldPosition = anchor;

        Vector2 ropeVector = (Vector2)transform.position - anchor;
        if (ropeVector.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 tangent = new Vector2(-ropeVector.y, ropeVector.x).normalized;
        float horizontalInput = inputManager.moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float direction = Mathf.Sign(horizontalInput);
            body.AddForce(tangent * direction * pumpForce * deltaTime, ForceMode2D.Force);
        }

        float verticalInput = inputManager.moveInput.y;
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            float desired = Mathf.Clamp(swingJoint.distance + (-verticalInput * ropeReelSpeed * deltaTime), minRopeLength, maxRopeLength);
            swingJoint.distance = Mathf.Lerp(swingJoint.distance, desired, ropeLengthLerpSpeed * deltaTime);
        }
    }

    private void ReleaseSwing(bool startCooldown)
    {
        if (!isSwinging)
        {
            return;
        }

        swingJoint.enabled = false;
        swingJoint.connectedBody = null;
        isSwinging = false;
        attachedBody = null;

        if (startCooldown)
        {
            cooldown = reattachDelay;
        }

        

        UpdateRopeVisuals();
    }

    private void TriggerSwingJump()
    {
        ReleaseSwing(true);

        if (jumpController != null)
        {
            jumpController.canJumpAgain = true;
            jumpController.desiredJump = true;
            jumpController.jumpBufferCounter = 0f;
        }

        
    }

    private Vector2 DetermineAimDirection()
    {
        Vector2 inputDir = inputManager != null ? inputManager.moveInput : Vector2.zero;

        if (inputDir.sqrMagnitude < 0.01f)
        {
            float facing = Mathf.Sign(transform.localScale.x);
            if (Mathf.Approximately(facing, 0f))
            {
                facing = 1f;
            }
            inputDir = new Vector2(facing, verticalAimBias);
        }
        else if (inputDir.y > -0.1f)
        {
            inputDir.y += verticalAimBias;
        }

        return inputDir.normalized;
    }

    private Vector2 GetAnchorWorldPosition()
    {
        if (attachedBody != null)
        {
            return attachedBody.transform.TransformPoint(swingJoint.connectedAnchor);
        }

        return swingJoint.connectedAnchor;
    }

    private void UpdateRopeVisuals()
    {
        Vector3 start = aimOrigin != null ? aimOrigin.position : transform.position;
        Vector3 end = isSwinging ? (Vector3)cachedAnchorWorldPosition : start;

        if (ropeLine != null)
        {
            if (isSwinging)
            {
                if (!ropeLine.enabled)
                {
                    ropeLine.enabled = true;
                }

                ropeLine.positionCount = 2;
                Vector3 p0 = ropeLineUseWorldSpace ? start : ropeLine.transform.InverseTransformPoint(start);
                Vector3 p1 = ropeLineUseWorldSpace ? end : ropeLine.transform.InverseTransformPoint(end);
                ropeLine.SetPosition(0, p0);
                ropeLine.SetPosition(1, p1);
            }
            else if (ropeLine.enabled)
            {
                ropeLine.positionCount = 0;
                ropeLine.enabled = false;
            }
        }

        if (ropeGraphic != null)
        {
            if (isSwinging)
            {
                if (!ropeGraphic.gameObject.activeSelf)
                {
                    ropeGraphic.gameObject.SetActive(true);
                }

                Vector3 midpoint = (start + end) * 0.5f;
                Vector3 direction = end - start;
                float length = direction.magnitude;
                ropeGraphic.position = midpoint;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    ropeGraphic.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                if (stretchRopeGraphic)
                {
                    Vector3 scale = ropeGraphicDefaultScale;
                    scale.x = ropeGraphicDefaultScale.x * Mathf.Max(0.01f, length) * ropeGraphicLengthScale;
                    ropeGraphic.localScale = scale;
                }
            }
            else if (ropeGraphic.gameObject.activeSelf)
            {
                ropeGraphic.gameObject.SetActive(false);
                ropeGraphic.localScale = ropeGraphicDefaultScale;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        Vector3 origin = aimOrigin != null ? aimOrigin.position : transform.position;
        Gizmos.color = isSwinging ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(origin, probeRadius);
        Gizmos.DrawLine(origin, origin + (Vector3)(lastProbeDirection * maxProbeDistance));

        if (isSwinging)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, cachedAnchorWorldPosition);
            Gizmos.DrawSphere(cachedAnchorWorldPosition, 0.1f);
        }
    }
}
