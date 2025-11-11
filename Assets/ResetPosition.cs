using System;
using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = new Vector3(-68, -3, 0);
        }
    }
}
