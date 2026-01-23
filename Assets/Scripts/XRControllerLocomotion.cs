using UnityEngine;
using UnityEngine.InputSystem;
using Oculus.Interaction.Locomotion;

public class XRControllerLocomotion : MonoBehaviour
{
    [Header("Input Action")]
    public InputActionReference avancerAction; // L'action "Avancer" de votre Input Actions
    
    [Header("Movement Settings")]
    public Transform cameraTransform;  // La caméra/tête du joueur (pour la direction)
    public float moveSpeed = 1.5f;
    public bool invert = false;

    [Header("Locomotion")]
    public FirstPersonLocomotor playerLocomotor; // Le FirstPersonLocomotor du PlayerController

    private void OnEnable()
    {
        if (avancerAction != null && avancerAction.action != null)
        {
            avancerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (avancerAction != null && avancerAction.action != null)
        {
            avancerAction.action.Disable();
        }
    }

    void Start()
    {
        if (playerLocomotor == null)
        {
            Debug.LogError("FirstPersonLocomotor not assigned!");
        }
        
        if (avancerAction == null)
        {
            Debug.LogError("Avancer Action not assigned!");
        }
        
        if (cameraTransform == null)
        {
            Debug.LogError("Camera Transform not assigned!");
        }
    }

    void FixedUpdate()
    {
        if (avancerAction == null || cameraTransform == null || playerLocomotor == null) return;

        // Vérifie si le bouton est pressé
        if (avancerAction.action.IsPressed())
        {
            // Direction horizontale seulement (basée sur l'orientation de la tête/caméra)
            float yaw = cameraTransform.eulerAngles.y;
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            if (invert) forward = -forward;

            // Créer un événement de locomotion avec vélocité (exactement comme le pinch)
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