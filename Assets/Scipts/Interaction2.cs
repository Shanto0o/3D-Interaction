using UnityEngine;

public class Interaction2 : MonoBehaviour
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
    
    [Header("Open Hand Settings")]
    [Range(0.01f, 0.5f)]
    public float openHandThreshold = 0.08f;
    
    [Header("Hand Orientation Settings")]
    public Transform handRot; // Transform de la main pour vérifier l'orientation
    public float palmUpThreshold = 0.5f; // Seuil pour détecter paume vers le haut (hand.up.y)
    public float palmDownThreshold = -0.5f; // Seuil pour détecter paume vers le bas (hand.up.y)
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool isHandOpen = false;
    private bool wasHandOpen = false;
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

        bool handOpen = CheckOpenHand();
        
        // Détecter l'orientation de la main (paume vers le haut ou vers le bas)
        float palmOrientation = 0f;
        bool palmUp = false;
        bool palmDown = false;
        
        if (hand != null && hand.transform != null)
        {
            // hand.transform.up pointe vers le dos de la main
            // Donc -hand.transform.up.y > 0 = paume vers le haut
            palmOrientation = -hand.transform.up.y;
            palmUp = palmOrientation > palmUpThreshold;
            palmDown = palmOrientation < palmDownThreshold;
        }
        
        // Debug des valeurs en CONTINU pour bien voir
        if (showDebugInfo)
        {
            Debug.Log($"Hand Open: {handOpen} | Palm Up: {palmUp} | Palm Down: {palmDown} | Orientation: {palmOrientation:F2}");
            Debug.Log($"Charged: {isFullyCharged} | Charging: {isHandOpen} | Can Launch: {(palmDown && isHandOpen && isFullyCharged)}");
        }

        // Début main ouverte + paume vers le haut - commence à charger
        if (handOpen && palmUp && !wasHandOpen && !isHandOpen)
        {
            StartCharging();
        }

        // Pendant main ouverte + paume vers le haut - continue de charger
        if (handOpen && palmUp && isHandOpen)
        {
            UpdateCharging();
        }

        // Retourne la main (paume vers le bas) - lance si chargé
        if (palmDown && isHandOpen)
        {
            if (isFullyCharged)
            {
                LaunchFireBall();
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Cannot launch: not fully charged ({currentChargeTime:F1}s / {chargeTime}s)");
                }
                CancelCharging();
            }
            ResetCharge();
        }
        // Ferme la main sans retourner - annule
        else if (!handOpen && isHandOpen && !palmDown)
        {
            CancelCharging();
            ResetCharge();
        }

        wasHandOpen = handOpen;
    }

    void StartCharging()
    {
        isHandOpen = true;
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
            Debug.Log("⚡ Charging started with palm up...");
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
                Debug.Log("✅ Fireball fully charged! Flip hand (palm down) to launch!");
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
        isHandOpen = false;
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