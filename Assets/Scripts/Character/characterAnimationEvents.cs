using UnityEngine;

//This script handles animation events to trigger umbrella actions
public class characterAnimationEvents : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private characterUmbrella umbrellaController;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject dropWater;
    [SerializeField] private string umbrellaAttackTrigger = "IsAttack";

    private void Awake()
    {
        if (umbrellaController == null)
        {
            umbrellaController = GetComponentInParent<characterUmbrella>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    // 애니메이션 이벤트에서 호출: Umbrella Attack 트리거 실행
    public void TriggerUmbrellaAttack()
    {
        if (animator != null && !string.IsNullOrEmpty(umbrellaAttackTrigger))
        {
            dropWater.SetActive(true);
            animator.SetTrigger(umbrellaAttackTrigger);
        }
    }

    // 애니메이션 이벤트에서 호출: Attack Umbrella 해제
    public void ReleaseAttackUmbrella()
    {
        if (umbrellaController != null)
        {
            dropWater.SetActive(false);
        }
    }
}
