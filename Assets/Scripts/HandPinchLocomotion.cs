using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;

public class HandPinchLocomotion : MonoBehaviour
{
    public HandRef rightHand;        // Assign RightHand (from Hand Interactions)
    public Transform handDirection;  // Assign a child that tourne avec la main
    public float moveSpeed = 1.5f;
    public bool invert = false;

    public FirstPersonLocomotor playerLocomotor; // Le FirstPersonLocomotor du PlayerController

    void Start()
    {
        if (playerLocomotor == null)
        {
            Debug.LogError("FirstPersonLocomotor not assigned!");
        }
    }

    void Update()
    {
        if (rightHand == null || handDirection == null || playerLocomotor == null) return;

        if (rightHand.GetFingerIsPinching(HandFinger.Index))
        {
            // Direction horizontale seulement
            float yaw = handDirection.eulerAngles.y;
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            if (invert) forward = -forward;

            // Créer un événement de locomotion avec vélocité
            // Le FirstPersonLocomotor gère automatiquement les collisions via le CharacterController
            LocomotionEvent locomotionEvent = new LocomotionEvent(
                identifier: 0,
                pose: new Pose(forward * moveSpeed, Quaternion.identity),
                translationType: LocomotionEvent.TranslationType.Velocity,
                rotationType: LocomotionEvent.RotationType.None
            );

            // Envoyer l'événement au locomotor
            playerLocomotor.HandleLocomotionEvent(locomotionEvent);
        }
    }
}
