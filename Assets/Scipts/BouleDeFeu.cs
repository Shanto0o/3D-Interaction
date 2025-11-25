using UnityEngine;

public class BouleDeFeu : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public GameObject fireBallPrefab; // FX_Fire_03

    [Header("Charge Settings")]
    public float chargeTime = 3f;
    public float launchForce = 15f;
    
    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isPinching = false;
    private bool wasCharging = false;
    private float currentChargeTime = 0f;
    private bool isFullyCharged = false;
    
    private GameObject chargingFireBall;
    private Vector3 chargeStartPosition;

    void Update()
    {
        if (hand == null || fireBallPrefab == null)
        {
            Debug.LogWarning("Hand or fireball prefab not assigned!");
            return;
        }

        bool pinchActive = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        // Début du pinch - commence à charger
        if (pinchActive && !isPinching)
        {
            StartCharging();
        }

        // Pendant le pinch - continue de charger
        if (pinchActive && isPinching)
        {
            UpdateCharging();
        }

        // Fin du pinch - lance si chargé
        if (!pinchActive && isPinching)
        {
            if (isFullyCharged)
            {
                LaunchFireBall();
            }
            else
            {
                CancelCharging();
            }
            ResetCharge();
        }

        isPinching = pinchActive;
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
            // Créer une petite boule de feu qui grandit pendant la charge
            chargingFireBall = Instantiate(fireBallPrefab, chargeStartPosition, Quaternion.identity);
            chargingFireBall.transform.localScale = Vector3.zero;
            
            // Désactiver la physique pendant la charge
            Rigidbody rb = chargingFireBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log("Charging started...");
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
                Debug.Log("Fireball fully charged!");
            }
        }

        // Mise à jour visuelle de la boule pendant la charge
        if (showChargingEffect && chargingFireBall != null)
        {
            // Positionner la boule devant la main
            Vector3 targetPos = hand.transform.position + hand.transform.forward * 0.2f;
            chargingFireBall.transform.position = Vector3.Lerp(
                chargingFireBall.transform.position, 
                targetPos, 
                Time.deltaTime * 10f
            );

            // Faire grandir la boule pendant la charge
            float scale = chargePercent * maxChargeScale;
            chargingFireBall.transform.localScale = Vector3.Lerp(
                chargingFireBall.transform.localScale,
                Vector3.one * scale,
                Time.deltaTime * 5f
            );

            // Rotation pour plus d'effet
            chargingFireBall.transform.Rotate(Vector3.up, Time.deltaTime * 100f);
        }

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Charge: {chargePercent * 100f:F0}%");
        }
    }

    void LaunchFireBall()
    {
        Vector3 launchPosition = hand.transform.position + hand.transform.forward * 0.2f;
        Vector3 launchDirection = hand.transform.forward;

        GameObject fireBall;
        
        if (showChargingEffect && chargingFireBall != null)
        {
            // Utiliser la boule déjà créée
            fireBall = chargingFireBall;
            fireBall.transform.localScale = Vector3.one * maxChargeScale;
        }
        else
        {
            // Créer une nouvelle boule
            fireBall = Instantiate(fireBallPrefab, launchPosition, Quaternion.identity);
            fireBall.transform.localScale = Vector3.one * maxChargeScale;
        }

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

        // Détruire la boule après quelques secondes
        Destroy(fireBall, 5f);

        if (showDebugInfo)
        {
            Debug.Log($"Fireball launched! Direction: {launchDirection}");
        }

        chargingFireBall = null;
    }

    void CancelCharging()
    {
        if (showChargingEffect && chargingFireBall != null)
        {
            // Faire disparaître la boule progressivement
            Destroy(chargingFireBall, 0.2f);
            chargingFireBall = null;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Charging cancelled (only {currentChargeTime:F1}s / {chargeTime}s)");
        }
    }

    void ResetCharge()
    {
        isPinching = false;
        wasCharging = false;
        currentChargeTime = 0f;
        isFullyCharged = false;
    }
}