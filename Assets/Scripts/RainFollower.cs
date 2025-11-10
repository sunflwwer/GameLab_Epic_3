using UnityEngine;

/// <summary>
/// Keeps the rain particle system aligned with the player's X position and
/// tweaks the Force over Lifetime based on the player's horizontal input.
/// </summary>
public class RainFollower : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;

    [Header("Force Settings")]
    [SerializeField] private ParticleSystem rainParticles;
    [SerializeField] private float idleForceX = 0f;
    [SerializeField] private float moveRightForceX = -40f;
    [SerializeField] private float moveLeftForceX = 40f;
    [SerializeField] private float directionDeadZone = 0.1f;

    [Header("Input Source")]
    [SerializeField] private characterMovement playerMovement;

    private ParticleSystem.ForceOverLifetimeModule _forceModule;

    private void Awake()
    {
        if (rainParticles == null)
        {
            rainParticles = GetComponent<ParticleSystem>();
        }

        if (rainParticles != null)
        {
            _forceModule = rainParticles.forceOverLifetime;
            _forceModule.enabled = true;
        }
    }

    private void LateUpdate()
    {
        FollowTargetX();
        UpdateForce();
    }

    private void FollowTargetX()
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = target.position.x + offset.x;
        position.y = target.position.y + offset.y;
        position.z = target.position.z + offset.z;
        transform.position = position;
    }

    private void UpdateForce()
    {
        if (rainParticles == null || playerMovement == null)
        {
            return;
        }

        float input = playerMovement.directionX;
        float forceX = idleForceX;

        if (input > directionDeadZone)
        {
            forceX = moveRightForceX;
        }
        else if (input < -directionDeadZone)
        {
            forceX = moveLeftForceX;
        }

        _forceModule.x = new ParticleSystem.MinMaxCurve(forceX);
    }
}
