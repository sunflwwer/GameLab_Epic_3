using UnityEngine;

public class ActiveFalseWaterDrop : MonoBehaviour
{
    [SerializeField] characterAnimationEvents animEvents;

    public void activeFalseWaterDrop()
    {
        if (animEvents != null)
            animEvents.ReleaseAttackUmbrella();
    }
}
