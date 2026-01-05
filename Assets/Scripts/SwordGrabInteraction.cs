using UnityEngine;

/// <summary>
/// Script simple pour attraper une épée en VR.
/// Basé sur le fonctionnement de BouleDeFeu.cs
/// </summary>
public class SwordGrabInteraction : MonoBehaviour
{
    [Header("Hand Reference")]
    public OVRHand rightHand;
    
    [Header("Zone Settings")]
    public float grabZoneRadius = 0.5f;
    
    [Header("Grab Settings")]
    public float chargeTime = 2f;
    
    [Range(0f, 0.5f)]
    public float openHandThreshold = 0.15f;
    
    [Header("Attachment Settings")]
    public Vector3 attachmentOffset = new Vector3(0, -0.05f, 0.1f);
    public Vector3 attachmentRotationOffset = new Vector3(-90, 0, 0);
    
    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État privé
    private bool isGrabbing = false;
    private bool isPinching = false;
    private float currentChargeTime = 0f;
    private bool isFullyCharged = false;
    private bool isAttached = false;
    
    private Rigidbody swordRigidbody;
    private GameObject chargingVisual;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        swordRigidbody = GetComponent<Rigidbody>();
        if (swordRigidbody == null)
        {
            swordRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Corriger les Mesh Colliders concaves SEULEMENT sur cet objet (l'épée)
        MeshCollider[] meshColliders = GetComponents<MeshCollider>();
        foreach (MeshCollider mc in meshColliders)
        {
            if (!mc.convex && swordRigidbody != null && !swordRigidbody.isKinematic)
            {
                mc.convex = true;
                if (showDebugInfo)
                {
                    Debug.Log($"⚠️ Mesh Collider de l'épée rendu convexe (requis par Unity)");
                }
            }
        }
        
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Créer la visualisation de la zone
        CreateZoneVisualization();
        
        if (showDebugInfo)
        {
            Debug.Log("✅ SwordGrabInteraction initialisé");
        }
    }
    
    void CreateZoneVisualization()
    {
        if (!showChargingEffect) return;
        
        chargingVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chargingVisual.name = "SwordGrabZone";
        chargingVisual.transform.SetParent(transform);
        chargingVisual.transform.localPosition = Vector3.zero;
        chargingVisual.transform.localScale = Vector3.one * grabZoneRadius * 2;
        
        // Rendre transparent
        Renderer renderer = chargingVisual.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Transparent/Diffuse"));
        mat.color = new Color(0.3f, 0.8f, 1f, 0.2f);
        renderer.material = mat;
        
        // Supprimer le collider
        Destroy(chargingVisual.GetComponent<Collider>());
    }
    
    void Update()
    {
        if (rightHand == null)
        {
            if (showDebugInfo && Time.frameCount % 300 == 0)
                Debug.LogWarning("⚠️ Main droite non assignée!");
            return;
        }
        
        // Si l'épée est attachée, la faire suivre la main
        if (isAttached)
        {
            FollowHand();
            CheckDetach();
            return;
        }
        
        // Vérifier si la main est dans la zone
        bool handInZone = IsHandInZone();
        
        // LOG: Afficher la distance en temps réel
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            float distance = Vector3.Distance(rightHand.transform.position, transform.position);
            Debug.Log($"📍 Distance main→épée: {distance:F2}m | Dans zone ({grabZoneRadius}m): {handInZone}");
        }
        
        if (!handInZone)
        {
            // Main hors zone - réinitialiser
            if (isGrabbing || isPinching)
            {
                if (showDebugInfo)
                {
                    Debug.Log("🚫 Main sortie de la zone - annulation");
                }
                CancelCharge();
            }
            return;
        }
        
        // La main est dans la zone
        bool grabActive = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool handOpen = IsHandOpen();
        
        // LOG: Détails des gestes
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"✋ Main DANS la zone | Grab/Pinch: {grabActive} | Main ouverte: {handOpen} | Charge: {currentChargeTime:F1}s/{chargeTime}s | Chargé: {isFullyCharged}");
        }
        
        // Commence à charger si pinch/grab activé
        if (grabActive && !isGrabbing && !isPinching)
        {
            StartCharge();
        }
        
        // Continue de charger
        if (grabActive && (isGrabbing || isPinching))
        {
            UpdateCharge();
        }
        
        // Attache si main ouverte et charge complète
        if (handOpen && isFullyCharged)
        {
            AttachSword();
        }
        
        // Annule si main ouverte sans charge complète
        if (handOpen && !isFullyCharged && (isGrabbing || isPinching))
        {
            CancelCharge();
        }
        
        isGrabbing = grabActive;
        isPinching = grabActive;
    }
    
    bool IsHandInZone()
    {
        if (rightHand == null) return false;
        float distance = Vector3.Distance(rightHand.transform.position, transform.position);
        bool inZone = distance <= grabZoneRadius;
        
        // LOG détaillé à chaque changement d'état
        if (showDebugInfo && inZone && Time.frameCount % 30 == 0)
        {
            Debug.Log($"🎯 MAIN DANS LA ZONE! Distance: {distance:F3}m / Rayon: {grabZoneRadius}m");
        }
        
        return inZone;
    }
    
    bool IsHandOpen()
    {
        if (rightHand == null) return false;
        
        return rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < openHandThreshold &&
               rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) < openHandThreshold &&
               rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) < openHandThreshold &&
               rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) < openHandThreshold;
    }
    
    void StartCharge()
    {
        currentChargeTime = 0f;
        isFullyCharged = false;
        
        if (chargingVisual != null)
        {
            Renderer renderer = chargingVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 1f, 0f, 0.4f); // Jaune
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("🔄 CHARGEMENT DÉMARRÉ - Maintenez le grab/pinch pendant 2 secondes!");
        }
    }
    
    void UpdateCharge()
    {
        currentChargeTime += Time.deltaTime;
        
        // Mise à jour visuelle
        if (chargingVisual != null)
        {
            float progress = Mathf.Clamp01(currentChargeTime / chargeTime);
            Color chargeColor = Color.Lerp(new Color(1f, 1f, 0f, 0.4f), new Color(0f, 1f, 0f, 0.5f), progress);
            
            Renderer renderer = chargingVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = chargeColor;
            }
            
            chargingVisual.transform.localScale = Vector3.one * grabZoneRadius * 2 * (1f + progress * 0.2f);
        }
        
        // Charge complète
        if (currentChargeTime >= chargeTime && !isFullyCharged)
        {
            isFullyCharged = true;
            
            if (showDebugInfo)
            {
                Debug.Log("✅ Charge complète! Ouvrez la main pour attraper l'épée.");
            }
        }
    }
    
    void CancelCharge()
    {
        currentChargeTime = 0f;
        isFullyCharged = false;
        isGrabbing = false;
        isPinching = false;
        
        if (chargingVisual != null)
        {
            Renderer renderer = chargingVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            }
            chargingVisual.transform.localScale = Vector3.one * grabZoneRadius * 2;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("❌ Chargement annulé");
        }
    }
    
    void AttachSword()
    {
        isAttached = true;
        currentChargeTime = 0f;
        isFullyCharged = false;
        isGrabbing = false;
        isPinching = false;
        
        // Désactiver physique
        if (swordRigidbody != null)
        {
            swordRigidbody.isKinematic = true;
            swordRigidbody.useGravity = false;
        }
        
        // Cacher la zone
        if (chargingVisual != null)
        {
            chargingVisual.SetActive(false);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("⚔️ Épée attachée!");
        }
    }
    
    void FollowHand()
    {
        if (rightHand == null) return;
        
        transform.position = rightHand.transform.position + rightHand.transform.TransformDirection(attachmentOffset);
        transform.rotation = rightHand.transform.rotation * Quaternion.Euler(attachmentRotationOffset);
    }
    
    void CheckDetach()
    {
        // Détacher si la main fait un pinch/grab
        if (rightHand != null && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            DetachSword();
        }
    }
    
    void DetachSword()
    {
        isAttached = false;
        
        // Réactiver physique
        if (swordRigidbody != null)
        {
            swordRigidbody.isKinematic = false;
            swordRigidbody.useGravity = true;
            swordRigidbody.linearVelocity = Vector3.zero;
        }
        
        // Montrer la zone
        if (chargingVisual != null)
        {
            chargingVisual.SetActive(true);
            Renderer renderer = chargingVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            }
            chargingVisual.transform.localScale = Vector3.one * grabZoneRadius * 2;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("🔓 Épée détachée");
        }
    }
    
    void OnDrawGizmos()
    {
        // Dessiner la zone dans l'éditeur
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, grabZoneRadius);
    }
}
