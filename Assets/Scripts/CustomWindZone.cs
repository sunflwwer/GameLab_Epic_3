using UnityEngine;

/// <summary>
/// 바람 영역을 정의합니다.
/// 플레이어가 이 영역에 들어오면 바람의 영향을 받습니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CustomWindZone : MonoBehaviour
{
    [Header("Wind Settings")]
    [SerializeField, Tooltip("바람 방향 (정규화됨)")]
    private Vector2 windDirection = Vector2.right;
    
    [SerializeField, Tooltip("바람 세기")]
    private float windStrength = 5f;
    
    [SerializeField, Tooltip("공중에 있을 때만 영향을 받음")]
    private bool onlyAffectWhenAirborne = true;
    
    [SerializeField, Tooltip("글라이드 중일 때 바람 영향 배율")]
    private float glideWindMultiplier = 1.5f;
    
    [SerializeField, Tooltip("일반 상태일 때 바람 영향 배율")]
    private float normalWindMultiplier = 1f;
    
    [Header("Visual Settings")]
    [SerializeField, Tooltip("바람 방향 화살표 그리기")]
    private bool drawWindDirection = true;
    
    [SerializeField, Tooltip("바람 색상")]
    private Color windColor = new Color(0.5f, 0.8f, 1f, 0.3f);
    
    public Vector2 WindDirection => windDirection.normalized;
    public float WindStrength => windStrength;
    public bool OnlyAffectWhenAirborne => onlyAffectWhenAirborne;
    public float GlideWindMultiplier => glideWindMultiplier;
    public float NormalWindMultiplier => normalWindMultiplier;
    
    private void OnValidate()
    {
        // 바람 방향을 자동으로 정규화
        if (windDirection.sqrMagnitude > 0.001f)
        {
            windDirection = windDirection.normalized;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            WindController windController = other.GetComponent<WindController>();
            if (windController != null)
            {
                windController.EnterWindZone(this);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            WindController windController = other.GetComponent<WindController>();
            if (windController != null)
            {
                windController.ExitWindZone(this);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!drawWindDirection)
        {
            return;
        }
        
        // 바람 영역 표시
        Gizmos.color = windColor;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds bounds = col.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        
        // 바람 방향 화살표 표시
        Vector3 center = transform.position;
        Vector3 direction = windDirection.normalized;
        float arrowLength = 2f;
        
        Gizmos.color = Color.cyan;
        Vector3 arrowEnd = center + (Vector3)direction * arrowLength;
        Gizmos.DrawLine(center, arrowEnd);
        
        // 화살표 머리
        Vector3 arrowHead1 = arrowEnd - (Vector3)(Quaternion.Euler(0, 0, 30) * direction) * 0.5f;
        Vector3 arrowHead2 = arrowEnd - (Vector3)(Quaternion.Euler(0, 0, -30) * direction) * 0.5f;
        Gizmos.DrawLine(arrowEnd, arrowHead1);
        Gizmos.DrawLine(arrowEnd, arrowHead2);
        
        // 바람 세기 텍스트 (에디터에서만)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(center + Vector3.up * 0.5f, $"Wind: {windStrength:F1}");
#endif
    }
}

