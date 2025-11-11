using UnityEngine;

/// <summary>
/// 플레이어가 아래 우산(S키)을 활성화하지 않고 이 오브젝트에 닿으면 비활성화됩니다.
/// 아래 우산으로만 건널 수 있는 지형을 만들 때 사용합니다.
/// </summary>
public class RequireDownUmbrellaOrDisable : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("플레이어 태그")]
    private string playerTag = "Player";
    
    [SerializeField, Tooltip("비활성화 대신 파괴할지 여부")]
    private bool destroyInstead = false;
    
    [SerializeField, Tooltip("비활성화/파괴 전 지연 시간(초)")]
    private float disableDelay = 0f;
    
    [Header("Optional Effects")]
    [SerializeField, Tooltip("비활성화 시 재생할 파티클 효과 (옵션)")]
    private GameObject breakEffectPrefab;
    
    [SerializeField, Tooltip("비활성화 시 재생할 오디오 클립 (옵션)")]
    private AudioClip breakSound;
    
    [Header("Debug")]
    [SerializeField, Tooltip("디버그 로그 출력")]
    private bool debugLog = true;
    
    private AudioSource audioSource;
    private bool isDisabling = false;
    
    private void Awake()
    {
        // AudioSource 자동 생성 (소리가 있을 경우)
        if (breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 비활성화 중이면 무시
        if (isDisabling) return;
        
        // 플레이어인지 확인
        if (!other.CompareTag(playerTag)) return;
        
        // characterUmbrella 컴포넌트 찾기
        characterUmbrella umbrellaController = other.GetComponent<characterUmbrella>();
        if (umbrellaController == null)
        {
            if (debugLog)
            {
                Debug.LogWarning($"[RequireDownUmbrellaOrDisable] 플레이어에서 characterUmbrella 컴포넌트를 찾을 수 없습니다!");
            }
            return;
        }
        
        // 아래 우산이 활성화되어 있는지 확인
        bool downUmbrellaActive = umbrellaController.IsDownPoseActive;
        
        if (debugLog)
        {
            Debug.Log($"[RequireDownUmbrellaOrDisable] 플레이어 충돌 감지 - 아래 우산 활성화: {downUmbrellaActive}");
        }
        
        // 아래 우산이 활성화되지 않았으면 오브젝트 비활성화
        if (!downUmbrellaActive)
        {
            if (debugLog)
            {
                Debug.Log($"[RequireDownUmbrellaOrDisable] 아래 우산 미활성화 상태로 충돌! '{gameObject.name}' 비활성화/파괴");
            }
            
            isDisabling = true;
            
            if (disableDelay > 0f)
            {
                Invoke(nameof(DisableOrDestroy), disableDelay);
            }
            else
            {
                DisableOrDestroy();
            }
        }
        else
        {
            if (debugLog)
            {
                Debug.Log($"[RequireDownUmbrellaOrDisable] 아래 우산 활성화 상태로 안전하게 통과!");
            }
        }
    }
    
    private void DisableOrDestroy()
    {
        // 이펙트 생성
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 사운드 재생
        if (breakSound != null && audioSource != null)
        {
            // 오브젝트가 비활성화/파괴되어도 소리가 재생되도록 AudioSource.PlayClipAtPoint 사용
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }
        
        // 비활성화 또는 파괴
        if (destroyInstead)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    // 외부에서 리셋할 수 있도록 (다시 활성화될 때)
    private void OnEnable()
    {
        isDisabling = false;
    }
}

