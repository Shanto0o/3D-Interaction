using UnityEngine;
using System;

public class BoundingBox : MonoBehaviour
{
    public string boxName;
    
    public event Action<BoundingBox> OnPlayerEnter;
    public event Action<BoundingBox> OnPlayerExit;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the trigger zone: " + boxName);
            OnPlayerEnter?.Invoke(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left the trigger zone: " + boxName);
            OnPlayerExit?.Invoke(this);
        }
    }
}
