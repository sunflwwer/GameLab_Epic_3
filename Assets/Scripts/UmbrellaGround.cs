using UnityEngine;

[DisallowMultipleComponent]
public class UmbrellaGround : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkRadius = 0.08f;
    [SerializeField] private Vector2 checkOffset = new Vector2(0f, -0.02f);

    public bool IsTouchingGround { get; private set; }

    private void Update()
    {
        Vector2 center = (Vector2)transform.position + checkOffset;
        IsTouchingGround = Physics2D.OverlapCircle(center, checkRadius, groundLayer) != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsTouchingGround ? Color.green : Color.red;
        Vector3 c = transform.position + (Vector3)checkOffset;
        Gizmos.DrawWireSphere(c, checkRadius);
    }
#endif
}