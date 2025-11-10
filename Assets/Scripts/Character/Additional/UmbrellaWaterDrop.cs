using UnityEngine;

public class UmbrellaWaterDrop : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        Debug.Log($"Umbrella water drop hit enemy: {other.name}");

        if (other.attachedRigidbody != null)
        {
            Destroy(other.attachedRigidbody.gameObject);
        }
        else
        {
            Destroy(other.gameObject);
        }
    }
}
