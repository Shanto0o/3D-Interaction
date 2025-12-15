using UnityEngine;

public class HandPistolet : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public GameObject fireBallPrefab;

    [Header("Charge Settings")]
    public float chargeTime = 3f;
    public float launchForce = 15f;
    
    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;
    
    [Header("Gun Gesture Settings")]
    [Range(0.01f, 0.5f)]
    public float extendedThreshold = 0.3f; // Seuil pour détecter doigt tendu
    [Range(0.5f, 1f)]
    public float closedThreshold = 0.6f; // Seuil pour détecter doigt replié
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool wasGunGesture = false;
    private bool isInGunPose = false; // Position pistolet active

    void Update()
    {
        if (hand == null || fireBallPrefab == null)
        {
            Debug.LogWarning("Hand or fireball prefab not assigned!");
            return;
        }

        // Détecter le GESTE PISTOLET : index tendu + autres doigts repliés
        float indexStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middleStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        float ringStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinkyStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);
        
        // Geste pistolet = index tendu + autres doigts repliés
        bool indexExtended = indexStrength < extendedThreshold;
        bool middleClosed = middleStrength > closedThreshold;
        bool ringClosed = ringStrength > closedThreshold;
        bool pinkyClosed = pinkyStrength > closedThreshold;
        
        bool gunPose = indexExtended && middleClosed && ringClosed && pinkyClosed;
        
        // TIRER : quand on est en position pistolet ET qu'on pince l'index
        bool indexPinch = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool shootTrigger = gunPose && indexPinch;
        
        // Debug
        if (showDebugInfo)
        {
            Debug.Log($"🔫 Gun Pose: {gunPose} | Index Extended: {indexExtended} | Closed: M={middleClosed}, R={ringClosed}, P={pinkyClosed}");
            Debug.Log($"🎯 Shoot Trigger: {shootTrigger} (Pinch: {indexPinch})");
            Debug.Log($"Values - Index: {indexStrength:F2}, Middle: {middleStrength:F2}, Ring: {ringStrength:F2}, Pinky: {pinkyStrength:F2}");
        }

        // Tirer quand on pince l'index en position pistolet
        if (shootTrigger && !wasGunGesture)
        {
            LaunchFireBall();
        }

        wasGunGesture = shootTrigger;
        isInGunPose = gunPose;
    }

    void LaunchFireBall()
    {
        Vector3 launchPosition = hand.transform.position + hand.transform.forward * 0.2f;
        Vector3 launchDirection = hand.transform.forward;

        // Créer une nouvelle boule
        GameObject fireBall = Instantiate(fireBallPrefab, launchPosition, Quaternion.identity);
        fireBall.transform.localScale = Vector3.one * maxChargeScale;

        // Activer la physique
        Rigidbody rb = fireBall.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = fireBall.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = launchDirection * launchForce;
        rb.angularVelocity = Random.insideUnitSphere * 2f;
        
        // Collision continue pour éviter de traverser les murs
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Détruire la boule après quelques secondes
        Destroy(fireBall, 5f);

        if (showDebugInfo)
        {
            Debug.Log($"🔫 Fireball launched! Direction: {launchDirection}");
        }
    }
}
