using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class swingAnchorPoint : MonoBehaviour
{
    [Header("Anchor Settings")]
    [SerializeField] private Transform anchorTransform;
    [SerializeField, Range(0.5f, 20f)] private float magnetRadius = 4f;
    [SerializeField, Range(-1f, 1f)] private float minAlignmentDot = -0.25f;
    [SerializeField, Range(0f, 5f)] private float priorityBoost;

    [Header("Optional Overrides")]
    [SerializeField] private Rigidbody2D connectedRigidbody;

    public float MagnetRadius => magnetRadius;
    public float MinAlignment => minAlignmentDot;
    public Vector2 AnchorWorldPosition => anchorTransform != null ? (Vector2)anchorTransform.position : (Vector2)transform.position;
    public Rigidbody2D TargetRigidbody => connectedRigidbody != null ? connectedRigidbody : GetComponentInParent<Rigidbody2D>();

    public float EvaluateScore(float alignmentDot, float distance)
    {
        float distanceScore = 1f - Mathf.Clamp01(distance / Mathf.Max(0.001f, magnetRadius));
        return priorityBoost + alignmentDot + distanceScore;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(AnchorWorldPosition, magnetRadius);
    }
}
