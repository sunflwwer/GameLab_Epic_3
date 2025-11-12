using UnityEngine;

/// <summary>
/// Hook이 이동할 수 있는 레일 경로를 정의합니다.
/// 여러 개의 포인트를 연결하여 경로를 만들 수 있습니다.
/// </summary>
public class HookRail : MonoBehaviour
{
    [Header("Rail Settings")]
    [SerializeField, Tooltip("레일 경로 포인트들 (순서대로 이동)")]
    private Transform[] pathPoints;
    
    [SerializeField, Tooltip("Hook 가능 여부")]
    private bool isHookable = true;
    
    [SerializeField, Tooltip("Hook 감지 거리")]
    private float detectionRadius = 3f;
    
    [Header("Visual Settings")]
    [SerializeField, Tooltip("에디터에서 레일 경로 그리기")]
    private bool drawGizmos = true;
    
    [SerializeField, Tooltip("레일 색상")]
    private Color railColor = Color.cyan;
    
    [SerializeField, Tooltip("시작점 색상")]
    private Color startPointColor = Color.green;
    
    [SerializeField, Tooltip("끝점 색상")]
    private Color endPointColor = Color.red;
    
    public bool IsHookable => isHookable;
    public float DetectionRadius => detectionRadius;
    public int PointCount => pathPoints != null ? pathPoints.Length : 0;
    
    private void OnValidate()
    {
        // 자동으로 자식 Transform들을 경로로 설정
        if (pathPoints == null || pathPoints.Length == 0)
        {
            pathPoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                pathPoints[i] = transform.GetChild(i);
            }
        }
    }
    
    /// <summary>
    /// 특정 인덱스의 경로 포인트 위치 반환
    /// </summary>
    public Vector3 GetPointPosition(int index)
    {
        if (pathPoints == null || index < 0 || index >= pathPoints.Length)
        {
            return transform.position;
        }
        
        return pathPoints[index] != null ? pathPoints[index].position : transform.position;
    }
    
    /// <summary>
    /// 시작 위치 반환
    /// </summary>
    public Vector3 GetStartPosition()
    {
        return GetPointPosition(0);
    }
    
    /// <summary>
    /// 끝 위치 반환
    /// </summary>
    public Vector3 GetEndPosition()
    {
        return GetPointPosition(PointCount - 1);
    }
    
    /// <summary>
    /// 레일의 전체 길이 계산
    /// </summary>
    public float GetTotalLength()
    {
        if (pathPoints == null || pathPoints.Length < 2)
        {
            return 0f;
        }
        
        float totalLength = 0f;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
            {
                totalLength += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }
        
        return totalLength;
    }
    
    /// <summary>
    /// 정규화된 진행도(0~1)에 따른 위치 반환
    /// </summary>
    public Vector3 GetPositionAtProgress(float normalizedProgress)
    {
        normalizedProgress = Mathf.Clamp01(normalizedProgress);
        
        if (pathPoints == null || pathPoints.Length == 0)
        {
            return transform.position;
        }
        
        if (pathPoints.Length == 1)
        {
            return GetPointPosition(0);
        }
        
        float totalLength = GetTotalLength();
        float targetDistance = totalLength * normalizedProgress;
        float currentDistance = 0f;
        
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            Vector3 start = GetPointPosition(i);
            Vector3 end = GetPointPosition(i + 1);
            float segmentLength = Vector3.Distance(start, end);
            
            if (currentDistance + segmentLength >= targetDistance)
            {
                float segmentProgress = (targetDistance - currentDistance) / segmentLength;
                return Vector3.Lerp(start, end, segmentProgress);
            }
            
            currentDistance += segmentLength;
        }
        
        return GetEndPosition();
    }
    
    /// <summary>
    /// 플레이어가 Hook 가능한 거리에 있는지 확인
    /// </summary>
    public bool IsPlayerInRange(Vector3 playerPosition)
    {
        if (!isHookable)
        {
            return false;
        }
        
        Vector3 startPos = GetStartPosition();
        float distance = Vector3.Distance(playerPosition, startPos);
        return distance <= detectionRadius;
    }
    
    private void OnDrawGizmos()
    {
        if (!drawGizmos || pathPoints == null || pathPoints.Length == 0)
        {
            return;
        }
        
        // 레일 경로 그리기
        Gizmos.color = railColor;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }
        
        // 포인트들 그리기
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] != null)
            {
                if (i == 0)
                {
                    Gizmos.color = startPointColor;
                }
                else if (i == pathPoints.Length - 1)
                {
                    Gizmos.color = endPointColor;
                }
                else
                {
                    Gizmos.color = railColor;
                }
                
                Gizmos.DrawSphere(pathPoints[i].position, 0.2f);
            }
        }
        
        // 감지 범위 그리기
        Gizmos.color = new Color(railColor.r, railColor.g, railColor.b, 0.3f);
        Gizmos.DrawWireSphere(GetStartPosition(), detectionRadius);
    }
}

