using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class cameraSwitchLand : MonoBehaviour
{
    [SerializeField] private CinemachineCamera targetCamera;
    [SerializeField] private int activePriority = 10;
    [SerializeField] private bool resetOnExit = true;
    [SerializeField] private int defaultPriority = 0;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("cameraSwitchLand: Target camera is not assigned.");
            return;
        }

        targetCamera.Priority = activePriority;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!resetOnExit || !other.CompareTag("Player"))
        {
            return;
        }

        if (targetCamera != null)
        {
            targetCamera.Priority = defaultPriority;
        }
    }
}
