using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Platform;

//Script de base pour se déplacer avec l'hand tracking
//Regarder le script HandGuidedPlacement pour mieux comprendre comment utiliser l'hand tracking

public class HandPinchLocomotionBB : MonoBehaviour
{
    public HandRef rightHand;        // Assign RightHand (from Hand Interactions)
    public Transform handDirection;  // Assign a child that rotates with your hand
    public float moveSpeed = 1.5f;
    public bool invert = false;      // Toggle if forward seems backwards

    void Update()
    {
        if (rightHand == null || handDirection == null) return;

        if (rightHand.GetFingerIsPinching(HandFinger.Index))
        {
            float yaw = handDirection.eulerAngles.y;
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;

            if (invert) forward = -forward;

            transform.position += forward * moveSpeed * Time.deltaTime;

            Debug.DrawRay(transform.position, forward, Color.cyan, 0.1f);
        }
    }
}
