using UnityEngine;

/// <summary>
/// Script simple pour attraper une épée en VR avec inertie physique réaliste.
/// Basé sur le fonctionnement de BouleDeFeu.cs avec système Spring-Damper.
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
    
    [Header("Physics Follow Settings (Inertie)")]
    [Tooltip("Force du ressort pour la position (plus élevé = plus rigide)")]
    public float positionSpring = 80f;
    [Tooltip("Amortissement de la position (réduit les oscillations)")]
    public float positionDamping = 8f;
    [Tooltip("Force maximale appliquée")]
    public float maxForce = 50f;
    [Tooltip("Force de rotation appliquée")]
    public float rotationStrength = 40f;
    [Tooltip("Amortissement de la rotation")]
    public float rotationDrag = 10f;
    [Tooltip("Masse de l'épée (kg)")]
    public float swordMass = 2f;
    
    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;
    [Tooltip("Activer l'effet de scintillement quand l'épée est au sol")]
    public bool showGlowEffect = true;
    [Tooltip("Vitesse du scintillement")]
    public float glowSpeed = 2f;
    [Tooltip("Intensité du scintillement")]
    public float glowIntensity = 2f;
    [Tooltip("Couleur du scintillement")]
    public Color glowColor = Color.yellow;
    
    [Header("UI Instructions")]
    [Tooltip("Afficher les instructions VR")]
    public bool showInstructions = true;
    [Tooltip("Caméra VR pour attacher le Canvas d'instructions")]
    public Camera vrCamera;
    [Tooltip("Distance du texte par rapport à l'épée")]
    public float instructionHeight = 0.5f;
    
    [Header("Audio")]
    [Tooltip("Son de chargement (loop pendant le pinch)")]
    public AudioClip chargingSound;
    [Tooltip("Son de validation quand l'épée s'attache à la main")]
    public AudioClip attachSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État privé
    private bool isGrabbing = false;
    private bool isPinching = false;
    private AudioSource audioSource;
    private bool isPlayingChargingSound = false;
    private float currentChargeTime = 0f;
    private bool isFullyCharged = false;
    private bool isAttached = false;
    
    private Rigidbody swordRigidbody;
    private GameObject chargingVisual;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    // Scintillement
    private Renderer[] swordRenderers;
    private Material[] originalMaterials;
    private bool isGlowing = false;
    
    // UI Instructions
    private Canvas instructionCanvas;
    private UnityEngine.UI.Text instructionText;
    
    // Pour le tracking de vélocité (inertie)
    private Vector3 previousHandPosition;
    private Vector3 handVelocity;
    
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
        
        // Ajouter un AudioSource si des sons sont configurés
        if (chargingSound != null || attachSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Son 3D
        }
        
        // Configurer le Rigidbody pour la physique avec inertie
        if (swordRigidbody != null)
        {
            swordRigidbody.mass = swordMass;
            swordRigidbody.linearDamping = 0.5f; // Résistance à l'air
            swordRigidbody.angularDamping = 1f; // Résistance à la rotation
            swordRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            swordRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            swordRigidbody.maxAngularVelocity = 30f; // Limiter la vitesse de rotation
        }
        
        // Créer la visualisation de la zone
        CreateZoneVisualization();
        
        // Initialiser les renderers pour l'effet de scintillement
        if (showGlowEffect)
        {
            InitializeGlowEffect();
        }
        
        // Créer le Canvas d'instructions
        if (showInstructions)
        {
            CreateInstructionCanvas();
        }
        
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
        
        // Mise à jour de l'effet de scintillement quand l'épée n'est pas attachée
        if (!isAttached && showGlowEffect)
        {
            UpdateGlowEffect();
        }
        
        // Si l'épée est attachée, la faire suivre la main
        if (isAttached)
        {
            CalculateHandVelocity();
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
        
        // Jouer le son de chargement en boucle
        if (chargingSound != null && audioSource != null && !isPlayingChargingSound)
        {
            audioSource.clip = chargingSound;
            audioSource.loop = true;
            audioSource.Play();
            isPlayingChargingSound = true;
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
        
        // Arrêter le son de chargement
        if (isPlayingChargingSound && audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isPlayingChargingSound = false;
        }
        
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
        
        // NE PAS rendre kinematic - garder la physique active pour l'inertie
        if (swordRigidbody != null)
        {
            swordRigidbody.isKinematic = false;
            swordRigidbody.useGravity = false;
        }
        
        // Initialiser le tracking de position pour le calcul de vélocité
        if (rightHand != null)
        {
            previousHandPosition = rightHand.transform.position + rightHand.transform.TransformDirection(attachmentOffset);
        }
        
        // Cacher la zone
        if (chargingVisual != null)
        {
            chargingVisual.SetActive(false);
        }
        
        // Cacher les instructions
        if (instructionCanvas != null)
        {
            instructionCanvas.gameObject.SetActive(false);
        }
        
        // Désactiver le scintillement
        if (isGlowing)
        {
            DisableGlowEffect();
        }
        
        // Arrêter le son de chargement et jouer le son de validation
        if (audioSource != null)
        {
            if (isPlayingChargingSound)
            {
                audioSource.Stop();
                audioSource.loop = false;
                isPlayingChargingSound = false;
            }
            
            if (attachSound != null)
            {
                audioSource.PlayOneShot(attachSound);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("⚔️ Épée attachée! (Mode physique avec inertie)");
        }
    }
    
    void CalculateHandVelocity()
    {
        if (rightHand == null || swordRigidbody == null) return;
        
        // Calculer la position cible
        Vector3 currentHandPos = rightHand.transform.position + rightHand.transform.TransformDirection(attachmentOffset);
        
        // Calculer la vélocité de la main
        handVelocity = (currentHandPos - previousHandPosition) / Time.deltaTime;
        previousHandPosition = currentHandPos;
    }
    
    void FixedUpdate()
    {
        // Appliquer les forces de suivi uniquement si attaché
        if (isAttached && rightHand != null && swordRigidbody != null)
        {
            FollowHandWithForces();
            FollowHandRotation();
        }
    }
    
    void FollowHandWithForces()
    {
        if (rightHand == null || swordRigidbody == null) return;
        
        // Position cible
        Vector3 targetPosition = rightHand.transform.position + rightHand.transform.TransformDirection(attachmentOffset);
        
        // Calculer l'erreur de position et de vélocité
        Vector3 positionError = targetPosition - swordRigidbody.position;
        Vector3 velocityError = handVelocity - swordRigidbody.linearVelocity;
        
        // PD Controller: Force = Kp * error + Kd * velocityError
        Vector3 force = positionSpring * positionError + positionDamping * velocityError;
        
        // Limiter la force
        force = Vector3.ClampMagnitude(force, maxForce);
        
        // Appliquer la force
        swordRigidbody.AddForce(force, ForceMode.Force);
    }
    
    void FollowHandRotation()
    {
        if (rightHand == null || swordRigidbody == null) return;
        
        // Rotation cible
        Quaternion targetRotation = rightHand.transform.rotation * Quaternion.Euler(attachmentRotationOffset);
        
        // Calculer la différence de rotation
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(swordRigidbody.rotation);
        
        // Convertir en angle-axis
        Vector3 rotationAxis;
        float rotationAngle;
        deltaRotation.ToAngleAxis(out rotationAngle, out rotationAxis);
        
        // Normaliser l'angle pour le plus court chemin
        if (rotationAngle > 180f)
            rotationAngle -= 360f;
        
        // Appliquer le torque si l'angle est valide
        if (!float.IsInfinity(rotationAxis.x) && rotationAxis != Vector3.zero && rotationAngle != 0)
        {
            // Convertir en radians et créer le vecteur de torque
            Vector3 targetAngularVelocity = rotationAxis.normalized * (rotationAngle * Mathf.Deg2Rad * rotationStrength);
            
            // Calculer le torque avec amortissement
            Vector3 torque = targetAngularVelocity - swordRigidbody.angularVelocity * rotationDrag;
            
            // Appliquer le torque
            swordRigidbody.AddTorque(torque, ForceMode.Acceleration);
        }
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
        
        // Montrer les instructions
        if (instructionCanvas != null)
        {
            instructionCanvas.gameObject.SetActive(true);
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
    
    void InitializeGlowEffect()
    {
        // Récupérer tous les renderers de l'épée
        swordRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[swordRenderers.Length];
        
        for (int i = 0; i < swordRenderers.Length; i++)
        {
            // Ignorer le chargingVisual
            if (swordRenderers[i].gameObject == chargingVisual)
                continue;
                
            originalMaterials[i] = swordRenderers[i].material;
            
            // Activer l'émission si le matériau le supporte
            if (swordRenderers[i].material.HasProperty("_EmissionColor"))
            {
                swordRenderers[i].material.EnableKeyword("_EMISSION");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"✨ Effet de scintillement initialisé sur {swordRenderers.Length} renderers");
        }
    }
    
    void UpdateGlowEffect()
    {
        if (swordRenderers == null || swordRenderers.Length == 0) return;
        
        // Calculer l'intensité du scintillement avec une courbe sinusoïdale
        float glow = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f; // Varie entre 0 et 1
        
        foreach (Renderer renderer in swordRenderers)
        {
            if (renderer == null || renderer.gameObject == chargingVisual)
                continue;
                
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", glowColor * glow * glowIntensity);
                isGlowing = true;
            }
        }
    }
    
    void DisableGlowEffect()
    {
        if (swordRenderers == null) return;
        
        foreach (Renderer renderer in swordRenderers)
        {
            if (renderer == null || renderer.gameObject == chargingVisual)
                continue;
                
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
        
        isGlowing = false;
    }
    
    void CreateInstructionCanvas()
    {
        // Trouver la caméra VR automatiquement si non assignée
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                Debug.LogWarning("⚠️ Aucune caméra VR trouvée pour les instructions");
                return;
            }
        }
        
        // Créer un Canvas World Space au-dessus de l'épée
        GameObject canvasObj = new GameObject("SwordInstructionCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, instructionHeight, 0);
        canvasObj.transform.localRotation = Quaternion.identity;
        
        instructionCanvas = canvasObj.AddComponent<Canvas>();
        instructionCanvas.renderMode = RenderMode.WorldSpace;
        instructionCanvas.worldCamera = vrCamera;
        
        // Configurer la taille du Canvas
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2f, 0.5f);
        canvasRect.localScale = Vector3.one * 0.001f; // Échelle pour adapter à la taille VR
        
        // Ajouter CanvasScaler
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Ajouter GraphicRaycaster
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Créer le texte
        GameObject textObj = new GameObject("InstructionText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        instructionText = textObj.AddComponent<UnityEngine.UI.Text>();
        instructionText.text = "Pince ta main gauche\npendant 2s pour\nrécupérer l'épée";
        instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instructionText.fontSize = 80;
        instructionText.fontStyle = FontStyle.Bold;
        instructionText.alignment = TextAnchor.MiddleCenter;
        instructionText.color = Color.yellow;
        
        // Ajouter un contour pour meilleure lisibilité
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Ajouter un script pour faire face à la caméra
        var lookAt = canvasObj.AddComponent<LookAtCamera>();
        lookAt.camera = vrCamera;
        
        if (showDebugInfo)
        {
            Debug.Log("📝 Instructions VR créées pour l'épée");
        }
    }
}

/// <summary>
/// Script simple pour faire face à la caméra en permanence
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    public Camera camera;
    
    void Update()
    {
        if (camera != null)
        {
            transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward,
                           camera.transform.rotation * Vector3.up);
        }
    }
}
