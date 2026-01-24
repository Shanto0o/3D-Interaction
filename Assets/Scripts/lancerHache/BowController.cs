using UnityEngine;
using System.Collections;

/// <summary>
/// Contrôle l'arc avec pinch pouce-majeur :
/// - Main gauche : tenir l'arc
/// - Main droite : tirer la flèche
/// </summary>
public class BowController : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Main gauche OVR pour tenir l'arc")]
    public OVRHand leftHand;
    
    [Tooltip("Main droite OVR pour tirer")]
    public OVRHand rightHand;
    
    [Tooltip("Transform de la main gauche")]
    public Transform leftHandTransform;
    
    [Tooltip("Transform de la main droite")]
    public Transform rightHandTransform;

    [Header("Bow References")]
    [Tooltip("La corde de l'arc (pour l'animation de tir)")]
    public Transform bowString;
    
    [Tooltip("Point de départ de la flèche")]
    public Transform arrowSpawnPoint;
    
    [Tooltip("Prefab de la flèche")]
    public GameObject arrowPrefab;

    [Header("Pinch Settings")]
    [Tooltip("Seuil de détection du pinch (0-1)")]
    [Range(0f, 1f)]
    public float pinchThreshold = 0.7f;
    
    [Tooltip("Utiliser le pinch pouce-majeur (middle finger) au lieu de l'index")]
    public bool useMiddleFingerPinch = true;

    [Header("Bow Settings")]
    [Tooltip("Distance maximale de traction de la corde")]
    public float maxDrawDistance = 0.5f;
    
    [Tooltip("Force de tir de la flèche")]
    public float arrowForce = 20f;
    
    [Tooltip("Multiplicateur de force basé sur la traction")]
    public float forceMultiplier = 2f;
    
    [Tooltip("Offset de rotation de l'arc par rapport à la main (X, Y, Z en degrés)")]
    public Vector3 bowRotationOffset = new Vector3(0f, 30f, 90f);
    
    [Tooltip("Offset de position de l'arc par rapport à la main (X, Y, Z en mètres)")]
    public Vector3 bowPositionOffset = new Vector3(0.1f, 0f, 0.1f);
    
    [Tooltip("Correction de rotation de la flèche (X, Y, Z en degrés) - Ajustez si la flèche pointe dans la mauvaise direction")]
    public Vector3 arrowRotationCorrection = new Vector3(0f, 0f, 0f);
    
    [Tooltip("Offset de position de la flèche par rapport au spawn point (X, Y, Z en mètres)")]
    public Vector3 arrowPositionOffset = new Vector3(0f, 0f, 0f);
    
    [Tooltip("La flèche suit la main droite pendant le bandage")]
    public bool arrowFollowsHand = true;
    
    [Tooltip("Distance minimale de traction pour pouvoir tirer (en mètres)")]
    public float minDrawDistanceToShoot = 0.1f;
    
    [Tooltip("Longueur de la flèche pour calculer la position de l'arrière (en mètres)")]
    public float arrowLength = 0.5f;
    
    [Tooltip("La flèche s'oriente dans la direction de son mouvement")]
    public bool arrowFollowsVelocity = true;
    
    [Tooltip("Utiliser moins de gravité pour une trajectoire plus droite")]
    [Range(0f, 1f)]
    public float gravityScale = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Couleur de l'arc quand tenu")]
    public Color bowHeldColor = Color.green;
    
    [Tooltip("Couleur de l'arc quand bandé")]
    public Color bowDrawnColor = Color.yellow;
    
    private Renderer bowRenderer;

    [Header("Audio")]
    public AudioClip grabBowSound;
    public AudioClip drawBowSound;
    public AudioClip releaseArrowSound;
    private AudioSource audioSource;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool isBowHeld = false;
    private bool isDrawing = false;
    private GameObject currentArrow;
    private Vector3 initialStringPosition;
    private float currentDrawAmount = 0f;
    private Color originalBowColor;
    private bool wasLeftPinching = false;

    void Start()
    {
        // Vérifier les mains
        if (leftHand == null || rightHand == null)
        {
            Debug.LogError("❌ [BowController] Les mains OVR doivent être assignées dans l'Inspector!");
            enabled = false;
            return;
        }

        // Récupérer les transforms si non assignés
        if (leftHandTransform == null)
        {
            leftHandTransform = leftHand.transform;
        }

        if (rightHandTransform == null)
        {
            rightHandTransform = rightHand.transform;
        }

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        // Renderer
        bowRenderer = GetComponentInChildren<Renderer>();
        if (bowRenderer != null)
        {
            originalBowColor = bowRenderer.material.color;
        }

        // Sauvegarder la position initiale de la corde
        if (bowString != null)
        {
            initialStringPosition = bowString.localPosition;
        }

        if (showDebugInfo)
        {
            Debug.Log("✅ [BowController] Initialisé - Pinch gauche pour tenir, pinch droit pour tirer");
        }
    }

    void Update()
    {
        // Détecter le pinch de la main gauche (toggle pour tenir/relâcher l'arc)
        bool leftPinching = GetPinchStrength(leftHand) > pinchThreshold;
        
        // Détecter le moment où le pinch commence (transition de false à true)
        if (leftPinching && !wasLeftPinching)
        {
            // Toggle : si l'arc est tenu, le relâcher, sinon l'attraper
            if (isBowHeld)
            {
                ReleaseBow();
            }
            else
            {
                GrabBow();
            }
        }
        
        // Mettre à jour l'état précédent
        wasLeftPinching = leftPinching;

        // Si l'arc est tenu, gérer le tir
        if (isBowHeld)
        {
            // Appliquer la rotation avec offset pour que l'arc soit bien orienté
            Quaternion handRotation = leftHandTransform.rotation;
            Quaternion offsetRotation = Quaternion.Euler(bowRotationOffset);
            transform.rotation = handRotation * offsetRotation;
            
            // Suivre la main gauche avec offset de position
            Vector3 offsetPosition = leftHandTransform.TransformPoint(bowPositionOffset);
            transform.position = offsetPosition;

            // Vérifier le pinch de la main droite pour tirer
            bool rightPinching = GetPinchStrength(rightHand) > pinchThreshold;

            if (rightPinching && !isDrawing)
            {
                StartDrawing();
            }
            else if (rightPinching && isDrawing)
            {
                UpdateDrawing();
            }
            else if (!rightPinching && isDrawing)
            {
                ReleaseArrow();
            }
        }
    }

    /// <summary>
    /// Obtient la force du pinch (pouce-majeur ou pouce-index)
    /// </summary>
    float GetPinchStrength(OVRHand hand)
    {
        if (hand == null || !hand.IsTracked) return 0f;

        if (useMiddleFingerPinch)
        {
            // Pinch pouce-majeur
            return hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        }
        else
        {
            // Pinch pouce-index (par défaut)
            return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        }
    }

    /// <summary>
    /// Attraper l'arc avec la main gauche
    /// </summary>
    void GrabBow()
    {
        isBowHeld = true;

        if (bowRenderer != null)
        {
            bowRenderer.material.color = bowHeldColor;
        }

        if (audioSource != null && grabBowSound != null)
        {
            audioSource.PlayOneShot(grabBowSound);
        }

        if (showDebugInfo)
        {
            Debug.Log("🏹 [BowController] Arc tenu!");
        }
    }

    /// <summary>
    /// Relâcher l'arc
    /// </summary>
    void ReleaseBow()
    {
        isBowHeld = false;
        isDrawing = false;

        if (bowRenderer != null)
        {
            bowRenderer.material.color = originalBowColor;
        }

        // Détruire la flèche si elle existe
        if (currentArrow != null)
        {
            Destroy(currentArrow);
            currentArrow = null;
        }

        // Réinitialiser la corde
        if (bowString != null)
        {
            bowString.localPosition = initialStringPosition;
        }

        if (showDebugInfo)
        {
            Debug.Log("🏹 [BowController] Arc relâché");
        }
    }

    /// <summary>
    /// Commencer à bander l'arc
    /// </summary>
    void StartDrawing()
    {
        isDrawing = true;

        // Créer une flèche
        if (arrowPrefab != null && arrowSpawnPoint != null && bowString != null)
        {
            // Appliquer la correction de rotation
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            
            // Calculer la position : l'arrière de la flèche doit être sur la corde
            // Position de la corde dans l'espace monde
            Vector3 stringWorldPos = bowString.position;
            
            // Direction vers l'avant de l'arc
            Vector3 forwardDir = correctedRotation * Vector3.forward;
            
            // Position de la flèche : arrière sur la corde, pointe vers l'avant
            Vector3 arrowPosition = stringWorldPos + forwardDir * arrowLength;
            
            currentArrow = Instantiate(arrowPrefab, arrowPosition, correctedRotation);
            
            // Ne pas parenter pour un meilleur contrôle de position
            currentArrow.transform.SetParent(null);
        }

        if (audioSource != null && drawBowSound != null)
        {
            audioSource.PlayOneShot(drawBowSound);
        }

        if (showDebugInfo)
        {
            Debug.Log("🎯 [BowController] Bandage de l'arc commencé");
        }
    }

    /// <summary>
    /// Mettre à jour le bandage de l'arc
    /// </summary>
    void UpdateDrawing()
    {
        if (rightHandTransform == null) return;

        // Calculer la distance de traction
        float distance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);
        currentDrawAmount = Mathf.Clamp01(distance / maxDrawDistance);

        // Animer la corde
        if (bowString != null)
        {
            Vector3 stringPos = initialStringPosition;
            stringPos.z -= currentDrawAmount * maxDrawDistance * 0.5f; // Tirer vers l'arrière
            bowString.localPosition = stringPos;
        }

        // Positionner la flèche : l'arrière suit la corde
        if (currentArrow != null && bowString != null && arrowSpawnPoint != null)
        {
            // Position de la corde dans l'espace monde
            Vector3 stringWorldPos = bowString.position;
            
            // Direction de tir avec correction
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 forwardDir = correctedRotation * Vector3.forward;
            
            // L'arrière de la flèche est sur la corde, la pointe est devant
            Vector3 arrowPosition = stringWorldPos + forwardDir * arrowLength;
            
            currentArrow.transform.position = arrowPosition;
            currentArrow.transform.rotation = correctedRotation;
        }

        // Changer la couleur
        if (bowRenderer != null)
        {
            bowRenderer.material.color = Color.Lerp(bowHeldColor, bowDrawnColor, currentDrawAmount);
        }
    }

    /// <summary>
    /// Tirer la flèche
    /// </summary>
    void ReleaseArrow()
    {
        if (currentArrow == null)
        {
            isDrawing = false;
            return;
        }
        
        // Vérifier si la traction est suffisante
        float distance = Vector3.Distance(leftHandTransform.position, rightHandTransform.position);
        if (distance < minDrawDistanceToShoot)
        {
            if (showDebugInfo)
            {
                Debug.Log($"⚠️ [BowController] Traction insuffisante: {distance:F2}m (min: {minDrawDistanceToShoot:F2}m)");
            }
            
            // Détruire la flèche et annuler le tir
            Destroy(currentArrow);
            currentArrow = null;
            isDrawing = false;
            currentDrawAmount = 0f;
            
            // Réinitialiser la corde
            if (bowString != null)
            {
                bowString.localPosition = initialStringPosition;
            }
            
            if (bowRenderer != null)
            {
                bowRenderer.material.color = bowHeldColor;
            }
            return;
        }

        // Détacher la flèche
        currentArrow.transform.SetParent(null);

        // Ajouter un Rigidbody si nécessaire
        Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentArrow.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Réduire la gravité pour une trajectoire plus droite
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;

        // Calculer la force basée sur la traction
        float force = arrowForce * (1f + currentDrawAmount * forceMultiplier);
        
        // Calculer la direction de tir avec correction de rotation
        Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
        Vector3 shootDirection = correctedRotation * Vector3.forward;

        // Appliquer la force
        rb.linearVelocity = shootDirection * force;

        // Ne pas ajouter de rotation aléatoire pour une trajectoire droite
        rb.angularVelocity = Vector3.zero;
        
        // Ajouter le script pour suivre la vélocité
        if (arrowFollowsVelocity)
        {
            ArrowVelocityFollow followScript = currentArrow.GetComponent<ArrowVelocityFollow>();
            if (followScript == null)
            {
                followScript = currentArrow.AddComponent<ArrowVelocityFollow>();
            }
            followScript.gravityScale = gravityScale;
        }

        // Son de tir
        if (audioSource != null && releaseArrowSound != null)
        {
            audioSource.PlayOneShot(releaseArrowSound);
        }

        if (showDebugInfo)
        {
            Debug.Log($"🎯 [BowController] Flèche tirée! Force: {force:F1}, Traction: {currentDrawAmount:F2}");
        }

        // Réinitialiser
        currentArrow = null;
        isDrawing = false;
        currentDrawAmount = 0f;

        // Réinitialiser la corde
        if (bowString != null)
        {
            bowString.localPosition = initialStringPosition;
        }

        // Réinitialiser la couleur
        if (bowRenderer != null)
        {
            bowRenderer.material.color = bowHeldColor;
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Visualiser la zone de traction
        if (isBowHeld && leftHandTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftHandTransform.position, maxDrawDistance);
        }

        // Visualiser la direction de tir
        if (isDrawing && arrowSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(arrowSpawnPoint.position, arrowSpawnPoint.forward * 2f);
        }
    }
}
