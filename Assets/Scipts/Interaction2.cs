using UnityEngine;

public class Interaction2 : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public GameObject projectilePrefab; // Objet à lancer (boule de feu, projectile, etc.)

    [Header("Charge Settings")]
    public float chargeTime = 3f;
    public float launchForce = 15f;
    
    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;
    
    [Header("Open Hand Threshold")]
    [Range(0.01f, 0.2f)]
    public float openHandThreshold = 0.08f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isPinching = false;
    private bool wasCharging = false;
    private float currentChargeTime = 0f;
    private bool isFullyCharged = false;
    
    private GameObject chargingProjectile;
    private Vector3 chargeStartPosition;
    
    private bool wasHandOpen = false;

    void Update()
    {
        if (hand == null || projectilePrefab == null)
        {
            Debug.LogWarning("Hand or projectile prefab not assigned!");
            return;
        }

        bool pinchActive = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool handOpen = CheckOpenHand();

        // Début du pinch - commence à charger
        if (pinchActive && !isPinching)
        {
            StartCharging();
        }

        // Pendant le pinch - continue de charger
        if (pinchActive && isPinching && !handOpen)
        {
            UpdateCharging();
        }

        // Main ouverte pendant la charge - lance le projectile!
        if (handOpen && !wasHandOpen && isFullyCharged && chargingProjectile != null)
        {
            LaunchProjectile();
            ResetCharge();
        }
        // Fin du pinch sans main ouverte - annule
        else if (!pinchActive && isPinching && !isFullyCharged)
        {
            CancelCharging();
            ResetCharge();
        }

        isPinching = pinchActive;
        wasHandOpen = handOpen;
    }

    void StartCharging()
    {
        isPinching = true;
        wasCharging = true;
        currentChargeTime = 0f;
        isFullyCharged = false;
        chargeStartPosition = hand.transform.position + hand.transform.forward * 0.2f;

        if (showChargingEffect)
        {
            // Créer un petit projectile qui grandit pendant la charge
            chargingProjectile = Instantiate(projectilePrefab, chargeStartPosition, Quaternion.identity);
            chargingProjectile.transform.localScale = Vector3.zero;
            
            // Désactiver la physique pendant la charge
            Rigidbody rb = chargingProjectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("⚡ Charging started...");
        }
    }

    void UpdateCharging()
    {
        currentChargeTime += Time.deltaTime;
        
        // Calculer le pourcentage de charge
        float chargePercent = Mathf.Clamp01(currentChargeTime / chargeTime);

        if (currentChargeTime >= chargeTime && !isFullyCharged)
        {
            isFullyCharged = true;
            if (showDebugInfo)
            {
                Debug.Log("✅ Fully charged! Open hand to launch!");
            }
        }

        // Mise à jour visuelle du projectile pendant la charge
        if (showChargingEffect && chargingProjectile != null)
        {
            // Positionner le projectile devant la main
            Vector3 targetPos = hand.transform.position + hand.transform.forward * 0.2f;
            chargingProjectile.transform.position = Vector3.Lerp(
                chargingProjectile.transform.position, 
                targetPos, 
                Time.deltaTime * 10f
            );

            // Faire grandir le projectile pendant la charge
            float scale = chargePercent * maxChargeScale;
            chargingProjectile.transform.localScale = Vector3.Lerp(
                chargingProjectile.transform.localScale,
                Vector3.one * scale,
                Time.deltaTime * 5f
            );

            // Rotation pour plus d'effet
            chargingProjectile.transform.Rotate(Vector3.up, Time.deltaTime * 100f);
        }

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"⚡ Charge: {chargePercent * 100f:F0}%");
        }
    }

    void LaunchProjectile()
    {
        Vector3 launchPosition = hand.transform.position + hand.transform.forward * 0.2f;
        Vector3 launchDirection = hand.transform.forward;

        GameObject projectile;
        
        if (showChargingEffect && chargingProjectile != null)
        {
            // Utiliser le projectile déjà créé
            projectile = chargingProjectile;
            projectile.transform.localScale = Vector3.one * maxChargeScale;
        }
        else
        {
            // Créer un nouveau projectile
            projectile = Instantiate(projectilePrefab, launchPosition, Quaternion.identity);
            projectile.transform.localScale = Vector3.one * maxChargeScale;
        }

        // Activer la physique
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectile.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = launchDirection * launchForce;
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        // Détruire le projectile après quelques secondes
        Destroy(projectile, 5f);

        if (showDebugInfo)
        {
            Debug.Log($"🚀 Projectile launched! Direction: {launchDirection}, Force: {launchForce}");
        }

        chargingProjectile = null;
    }

    void CancelCharging()
    {
        if (showChargingEffect && chargingProjectile != null)
        {
            // Faire disparaître le projectile progressivement
            Destroy(chargingProjectile, 0.2f);
            chargingProjectile = null;
        }

        if (showDebugInfo)
        {
            Debug.Log($"❌ Charging cancelled (only {currentChargeTime:F1}s / {chargeTime}s)");
        }
    }

    void ResetCharge()
    {
        isPinching = false;
        wasCharging = false;
        currentChargeTime = 0f;
        isFullyCharged = false;
    }
    
    bool CheckOpenHand()
    {
        // Vérifie que tous les doigts sont ouverts
        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) < openHandThreshold;
    }
}
