using UnityEngine;

public class ArrowTargetPractice : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public GameObject arrowPrefab;

    [Header("Pinch Settings")]
    [Tooltip("Doigt utilisé pour le pinch (par défaut : Middle = majeur)")]
    public OVRHand.HandFinger pinchFinger = OVRHand.HandFinger.Middle;

    [Header("Charge Settings")]
    public float chargeTime = 3f;
    [Tooltip("Force maximale à charge complète")]
    public float launchForce = 15f;
    [Tooltip("Force minimale sans charge")]
    public float minLaunchForce = 3f;

    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 1.0f;
    
    [Tooltip("Offset de rotation à appliquer à la flèche (en degrés Euler)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isPinching = false;
    private float currentChargeTime = 0f;
    private GameObject chargingArrow;

    void Update()
    {
        if (hand == null || arrowPrefab == null)
        {
            Debug.LogWarning("Hand or arrow prefab not assigned!");
            return;
        }

        bool pinchActive = hand.GetFingerIsPinching(pinchFinger);

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

        // Fin du pinch - lance la flèche
        if (!pinchActive && isPinching)
        {
            LaunchArrow();
            ResetCharge();
        }

        isPinching = pinchActive;
    }

    void StartCharging()
    {
        isPinching = true;
        currentChargeTime = 0f;

        if (showChargingEffect)
        {
            Vector3 chargeStartPosition = hand.transform.position + hand.transform.forward * 0.15f;
            
            // Instancier directement le prefab de la flèche
            chargingArrow = Instantiate(arrowPrefab);
            chargingArrow.name = "ChargingArrow";
            chargingArrow.transform.position = chargeStartPosition;
            chargingArrow.transform.rotation = Quaternion.Euler(rotationOffset) * hand.transform.rotation;
            chargingArrow.transform.localScale = Vector3.one * 0.5f; // Scale visible dès le départ

            // Ajouter le Rigidbody
            Rigidbody rb = chargingArrow.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = chargingArrow.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Ajouter un SphereCollider si nécessaire
            if (chargingArrow.GetComponent<Collider>() == null)
            {
                SphereCollider collider = chargingArrow.AddComponent<SphereCollider>();
                collider.radius = 0.15f;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"Charging started at position: {hand.transform.position}");
        }
    }

    void UpdateCharging()
    {
        currentChargeTime += Time.deltaTime;

        // Calculer le pourcentage de charge
        float chargePercent = Mathf.Clamp01(currentChargeTime / chargeTime);

        // Mise à jour visuelle de la flèche pendant la charge
        if (showChargingEffect && chargingArrow != null)
        {
            // Positionner la flèche devant la main et suivre son orientation
            Vector3 targetPos = hand.transform.position + hand.transform.forward * 0.15f;
            chargingArrow.transform.position = Vector3.Lerp(
                chargingArrow.transform.position,
                targetPos,
                Time.deltaTime * 10f
            );

            // Suivre la rotation de la main avec offset
            chargingArrow.transform.rotation = Quaternion.Lerp(
                chargingArrow.transform.rotation,
                Quaternion.Euler(rotationOffset) * hand.transform.rotation,
                Time.deltaTime * 10f
            );

            // Faire grandir la flèche pendant la charge (de 0.5 à maxChargeScale)
            float targetScale = Mathf.Lerp(0.5f, maxChargeScale, chargePercent);
            chargingArrow.transform.localScale = Vector3.Lerp(
                chargingArrow.transform.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * 5f
            );
        }

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Charge: {chargePercent * 100f:F0}% | Scale: {chargingArrow?.transform.localScale.x:F2}");
        }
    }

    void LaunchArrow()
    {
        // Calculer la force en fonction du pourcentage de charge
        float chargePercent = Mathf.Clamp01(currentChargeTime / chargeTime);
        float currentLaunchForce = Mathf.Lerp(minLaunchForce, launchForce, chargePercent);
        
        Vector3 launchPosition = hand.transform.position + hand.transform.forward * 0.15f;
        Vector3 launchDirection = hand.transform.forward;

        GameObject arrow;

        if (showChargingEffect && chargingArrow != null)
        {
            // Utiliser la flèche déjà créée
            arrow = chargingArrow;
            arrow.transform.localScale = Vector3.one * maxChargeScale;
        }
        else
        {
            // Créer une nouvelle flèche
            arrow = Instantiate(arrowPrefab);
            arrow.name = "Arrow";
            arrow.transform.position = launchPosition;
            arrow.transform.rotation = Quaternion.Euler(rotationOffset) * hand.transform.rotation;
            arrow.transform.localScale = Vector3.one * maxChargeScale;

            // Ajouter le Rigidbody si nécessaire
            Rigidbody rbCheck = arrow.GetComponent<Rigidbody>();
            if (rbCheck == null)
            {
                arrow.AddComponent<Rigidbody>();
            }

            // Ajouter un SphereCollider si nécessaire
            if (arrow.GetComponent<Collider>() == null)
            {
                SphereCollider collider = arrow.AddComponent<SphereCollider>();
                collider.radius = 0.15f;
            }
        }

        // Activer la physique
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = launchDirection * currentLaunchForce;
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        // Détruire la flèche après quelques secondes
        Destroy(arrow, 5f);

        if (showDebugInfo)
        {
            Debug.Log($"Arrow launched! Charge: {chargePercent * 100f:F0}% | Force: {currentLaunchForce:F1} | Direction: {launchDirection}");
        }

        chargingArrow = null;
    }

    void ResetCharge()
    {
        isPinching = false;
        currentChargeTime = 0f;
    }
}