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
    [Header("Auto-Aim (Cheat)")]
    [Tooltip("Activer un auto-aim simple: si une cible est proche de la pointe, on ajuste la direction pour toucher.")]
    public bool enableAutoAim = true;
    [Tooltip("Rayon de recherche (m) pour l'auto-aim")]
    public float autoAimRadius = 5f;
    [Tooltip("Angle max (degrés) entre la pointe de la flèche et la cible pour que l'auto-aim s'active")]
    public float autoAimMaxAngle = 30f;
    [Tooltip("Layers à considérer pour l'auto-aim")]
    public LayerMask autoAimLayerMask = ~0;
    [Tooltip("Tag attendu sur les cibles (laisser vide pour ignorer le filtrage par tag)")]
    public string autoAimTag = "Target";
    [Tooltip("Afficher la trajectoire prévue avant de tirer")]
    public bool showTrajectoryPreview = true;
    [Tooltip("Tracer la trajectoire de la flèche après le tir")]
    public bool showArrowTrail = false;
    [Tooltip("Longueur de la ligne de visée (en mètres)")]
    public float aimLineLength = 10f;
    [Tooltip("Couleur de la ligne de visée")]
    public Color aimLineColor = Color.red;
    [Tooltip("Utiliser une trajectoire en cloche (parabolique) pour l'aperçu et le tir")]
    public bool useArcTrajectory = true;
    [Tooltip("Angle de lancement (degrés) pour la trajectoire parabolique")]
    [Range(5f, 85f)]
    public float launchAngleDegrees = 45f;
    [Tooltip("Nombre d'échantillons pour tracer la parabole")]
    [Range(6, 60)]
    public int trajectoryResolution = 30;

    private bool isBowHeld = false;
    private bool isDrawing = false;
    private GameObject currentArrow;
    private Vector3 initialStringPosition;
    private float currentDrawAmount = 0f;
    private Color originalBowColor;
    private bool wasLeftPinching = false;
    private LineRenderer trajectoryLine;

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

        // Créer la ligne de trajectoire
        if (showTrajectoryPreview)
        {
            GameObject trajectoryObj = new GameObject("TrajectoryLine");
            trajectoryObj.transform.SetParent(transform);
            trajectoryLine = trajectoryObj.AddComponent<LineRenderer>();
            trajectoryLine.startWidth = 0.01f;
            trajectoryLine.endWidth = 0.01f;
            
            // Créer un matériau simple
            Material lineMat = new Material(Shader.Find("Unlit/Color"));
            if (lineMat.shader == null)
            {
                lineMat = new Material(Shader.Find("Standard"));
            }
            trajectoryLine.material = lineMat;
            trajectoryLine.startColor = aimLineColor;
            trajectoryLine.endColor = aimLineColor;
            trajectoryLine.positionCount = 0;
            trajectoryLine.enabled = false;
        }

        // Si le script est attaché à un prefab (arc non tenu), placer l'arc à x=0
        if (!isBowHeld)
        {
            Vector3 pos = transform.position;
            pos.x = 0f;
            transform.position = pos;
            if (showDebugInfo)
            {
                Debug.Log($"[BowController] Arc placé à x=0 (position initiale prefab)");
            }
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
            // Nouvelle orientation : corde vers la paume, flèche vers l'avant
            OrientBowToPalm(leftHandTransform);
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
    /// Oriente l'arc pour que la corde soit face à la paume et la flèche vers l'avant de la main
    /// </summary>
    void OrientBowToPalm(Transform handTransform)
    {
        // Axe de la paume : handTransform.up (Y)
        // Axe avant de la main : handTransform.forward (Z)
        // On veut que la corde soit alignée avec la paume (main ouverte)
        // et la flèche vers l'avant de la main

        // Calcul de la rotation cible :
        // - Z de l'arc = forward de la main (direction de tir)
        // - Y de l'arc = up de la main (paume)
        // - X de l'arc = right de la main
        Quaternion targetRotation = Quaternion.LookRotation(handTransform.forward, handTransform.up);
        // Appliquer l'offset de rotation pour ajuster le modèle si besoin
        Quaternion offsetRotation = Quaternion.Euler(bowRotationOffset);
        transform.rotation = targetRotation * offsetRotation;
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

        // Placer l'arc à x=0 lors du pinch
        Vector3 pos = transform.position;
        pos.x = 0f;
        transform.position = pos;
        if (showDebugInfo)
        {
            Debug.Log($"[BowController] Arc placé à x=0 (GrabBow)");
        }

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

        // Cacher la ligne de trajectoire
        if (trajectoryLine != null)
        {
            trajectoryLine.enabled = false;
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
        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            // Appliquer la correction de rotation
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 correctedForward = correctedRotation * Vector3.forward;
            Vector3 correctedUp = correctedRotation * Vector3.up;
            // Placer la flèche au arrowSpawnPoint en utilisant la rotation corrigée pour forward/up
            Vector3 arrowPosition = arrowSpawnPoint.position + correctedRotation * arrowPositionOffset;
            currentArrow = Instantiate(arrowPrefab, arrowPosition, Quaternion.LookRotation(correctedForward, correctedUp));
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
            stringPos.z -= currentDrawAmount * maxDrawDistance * 0.5f;
            bowString.localPosition = stringPos;
        }

        // Positionner la flèche : elle reste au niveau du arrowSpawnPoint
        // mais recule avec la main droite
        if (currentArrow != null && arrowSpawnPoint != null)
        {
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 correctedForward = correctedRotation * Vector3.forward;
            // Calculer le recul de la flèche en fonction du bandage (utiliser forward corrigé)
            Vector3 pullbackOffset = -correctedForward * currentDrawAmount * maxDrawDistance;
            Vector3 arrowPosition = arrowSpawnPoint.position + pullbackOffset + correctedRotation * arrowPositionOffset;

            currentArrow.transform.position = arrowPosition;
            currentArrow.transform.rotation = Quaternion.LookRotation(correctedForward, correctedRotation * Vector3.up);
        }

        // Afficher la ligne de visée
        if (showTrajectoryPreview && trajectoryLine != null && arrowSpawnPoint != null)
        {
            trajectoryLine.enabled = true;
            
            // CORRECTION : Utiliser arrowSpawnPoint.forward avec correction
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 correctedForward = correctedRotation * Vector3.forward;
            Vector3 correctedUp = correctedRotation * Vector3.up;
            Vector3 shootDirection = correctedForward;

            // Décalage demandé : tourner la direction de -90° autour de l'axe 'up' (gauche)
            Quaternion leftRot = Quaternion.AngleAxis(-90f, correctedUp);
            Vector3 aimDir = leftRot * shootDirection;

            // Calculer le point cible (point jaune) à distance 'aimLineLength'
            Vector3 aimPoint = arrowSpawnPoint.position + aimDir * aimLineLength;

            if (useArcTrajectory)
            {
                // Résoudre la vitesse initiale nécessaire pour frapper aimPoint avec l'angle demandé
                Vector3 initVel;
                float totalTime;
                float g = Physics.gravity.y * gravityScale; // note: g est négatif
                bool ok = SolveBallisticArc(arrowSpawnPoint.position, aimPoint, launchAngleDegrees, g, out initVel, out totalTime);
                if (ok)
                {
                    trajectoryLine.positionCount = trajectoryResolution + 1;
                    for (int i = 0; i <= trajectoryResolution; i++)
                    {
                        float t = (totalTime * i) / (float)trajectoryResolution;
                        // s(t) = s0 + v0 * t + 0.5 * g * t^2
                        Vector3 pos = arrowSpawnPoint.position + initVel * t + 0.5f * Vector3.up * Physics.gravity.y * gravityScale * t * t;
                        trajectoryLine.SetPosition(i, pos);
                    }
                    // Marquer le point cible (jaune) par un petit rayon
                    Debug.DrawRay(aimPoint, Vector3.up * 0.05f, Color.yellow);
                }
                else
                {
                    // fallback : ligne droite si l'équation échoue
                    trajectoryLine.positionCount = 2;
                    trajectoryLine.SetPosition(0, arrowSpawnPoint.position);
                    trajectoryLine.SetPosition(1, aimPoint);
                }
            }
            else
            {
                // Ligne droite simple
                trajectoryLine.positionCount = 2;
                trajectoryLine.SetPosition(0, arrowSpawnPoint.position);
                trajectoryLine.SetPosition(1, aimPoint);
            }
            
            // Changer la couleur selon la force
            Color lineColor = Color.Lerp(Color.yellow, Color.red, currentDrawAmount);
            trajectoryLine.startColor = lineColor;
            trajectoryLine.endColor = lineColor;
        }

        // Changer la couleur
        if (bowRenderer != null)
        {
            bowRenderer.material.color = Color.Lerp(bowHeldColor, bowDrawnColor, currentDrawAmount);
        }

        // Debug info
        if (showDebugInfo && arrowSpawnPoint != null)
        {
            // Recalculer la direction effective (décalée à gauche) pour le debug (évite variable hors-scope)
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 correctedForward = correctedRotation * Vector3.forward;
            Vector3 correctedUp = correctedRotation * Vector3.up;
            Vector3 shootDirection = correctedForward;
            Quaternion leftRot = Quaternion.AngleAxis(-90f, correctedUp);
            Vector3 debugAimDir = leftRot * shootDirection;
            Debug.DrawRay(arrowSpawnPoint.position, debugAimDir * 5f, Color.cyan);
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
            if (trajectoryLine != null) trajectoryLine.enabled = false;
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
            Destroy(currentArrow);
            currentArrow = null;
            isDrawing = false;
            currentDrawAmount = 0f;
            if (bowString != null) bowString.localPosition = initialStringPosition;
            if (bowRenderer != null) bowRenderer.material.color = bowHeldColor;
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            return;
        }

        // Détacher la flèche
        currentArrow.transform.SetParent(null);

        // Ajouter un Rigidbody si nécessaire
        Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
        if (rb == null) rb = currentArrow.AddComponent<Rigidbody>();

        // CONFIGURATION PHYSIQUE OPTIMISÉE
        rb.mass = 0.05f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;

        // Calculer la force basée sur la traction
        float force = arrowForce * (1f + currentDrawAmount * forceMultiplier);

        // Direction réelle de la flèche
        Vector3 shootDirection = currentArrow.transform.forward;
        bool autoAimed = false;

        // Auto-aim : si activé, chercher une cible proche de la pointe et ajuster la direction
        if (enableAutoAim)
        {
            Vector3 arrowPos = currentArrow.transform.position;
            Collider[] hits = Physics.OverlapSphere(arrowPos, autoAimRadius, autoAimLayerMask);
            Collider best = null;
            float bestScore = float.MaxValue;
            foreach (var col in hits)
            {
                if (!string.IsNullOrEmpty(autoAimTag) && !col.CompareTag(autoAimTag)) continue;
                Vector3 closest = col.ClosestPoint(arrowPos);
                Vector3 toTarget = closest - arrowPos;
                float dist = toTarget.magnitude;
                if (dist < 0.001f) continue;
                float angle = Vector3.Angle(currentArrow.transform.forward, toTarget);
                if (angle > autoAimMaxAngle) continue;
                // score : prefère petit angle puis courte distance
                float score = angle + dist * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = col;
                }
            }
            if (best != null)
            {
                Vector3 targetPoint = best.ClosestPoint(arrowPos);
                shootDirection = (targetPoint - arrowPos).normalized;
                autoAimed = true;
                // orienter visuellement la flèche vers la cible
                currentArrow.transform.rotation = Quaternion.LookRotation(shootDirection);
            }
        }

        // Calculer la rotation corrigée du spawn pour axes cohérents
        Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
        Vector3 correctedUp = correctedRotation * Vector3.up;
        // Si on n'a PAS auto-aimé la direction, appliquer le décalage de -90° vers la gauche
        Quaternion leftRot = Quaternion.AngleAxis(-90f, correctedUp);
        if (!autoAimed)
        {
            // Utiliser la direction 'forward' corrigée comme base
            Vector3 baseForward = correctedRotation * Vector3.forward;
            shootDirection = leftRot * baseForward;
            // ré-orienter la flèche localement pour correspondre à la direction
            currentArrow.transform.rotation = Quaternion.LookRotation(shootDirection);
        }

        // Si on utilise la trajectoire en cloche, calculer la vitesse initiale vers le point jaune
        if (useArcTrajectory)
        {
            // Calculer l'aimPoint à partir du spawn point et de sa rotation corrigée
            Quaternion correctedRotation2 = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 correctedUp2 = correctedRotation2 * Vector3.up;
            Vector3 aimPoint = arrowSpawnPoint.position + (Quaternion.AngleAxis(-90f, correctedUp2) * (correctedRotation2 * Vector3.forward)) * aimLineLength;
            // Note: utiliser la même logique que dans UpdateDrawing pour g
            Vector3 initVel;
            float totalTime;
            float g = Physics.gravity.y * gravityScale;
            bool ok = SolveBallisticArc(arrowSpawnPoint.position, aimPoint, launchAngleDegrees, g, out initVel, out totalTime);
            if (ok)
            {
                rb.useGravity = false; // ArrowVelocityFollow gère la gravité manuellement
                rb.linearVelocity = initVel;
            }
            else
            {
                rb.linearVelocity = shootDirection * force;
            }
        }
        else
        {
            rb.linearVelocity = shootDirection * force;
        }
        // Éviter les rotations physiques indésirables
        rb.angularVelocity = Vector3.zero;
        rb.maxAngularVelocity = 0.1f;

        // Scripts utilitaires
        if (arrowFollowsVelocity)
        {
            ArrowVelocityFollow followScript = currentArrow.GetComponent<ArrowVelocityFollow>();
            if (followScript == null) followScript = currentArrow.AddComponent<ArrowVelocityFollow>();
            followScript.gravityScale = gravityScale;
        }

        // Traînée visuelle désactivée (évite l'erreur de shader); activer manuellement si nécessaire

        if (showDebugInfo)
        {
            if (currentArrow.GetComponent<ArrowDebugger>() == null) currentArrow.AddComponent<ArrowDebugger>();
        }

        if (audioSource != null && releaseArrowSound != null) audioSource.PlayOneShot(releaseArrowSound);

        if (showDebugInfo)
        {
            Debug.Log($"🎯 [BowController] Flèche tirée! Force: {force:F1}, Direction: {shootDirection}, Traction: {currentDrawAmount:F2}");
        }

        if (trajectoryLine != null) trajectoryLine.enabled = false;

        // Réinitialiser
        currentArrow = null;
        isDrawing = false;
        currentDrawAmount = 0f;
        if (bowString != null) bowString.localPosition = initialStringPosition;
        if (bowRenderer != null) bowRenderer.material.color = bowHeldColor;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (isBowHeld && leftHandTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftHandTransform.position, maxDrawDistance);
        }
        if (isDrawing && arrowSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Quaternion correctedRotation = arrowSpawnPoint.rotation * Quaternion.Euler(arrowRotationCorrection);
            Vector3 shootDirection = correctedRotation * Vector3.forward;
            // appliquer le décalage gauche -90° pour correspondre au comportement runtime
            Quaternion leftRot = Quaternion.AngleAxis(-90f, arrowSpawnPoint.up);
            Vector3 aimDir = leftRot * shootDirection;
            Gizmos.DrawRay(arrowSpawnPoint.position, aimDir * 5f);
        }
    }

    /// <summary>
    /// Résout la vitesse initiale nécessaire pour toucher "target" depuis "start"
    /// avec un angle de lancement donné (en degrés) et une gravité donnée (valeur négative attendue).
    /// Retourne la vitesse initiale et le temps total de vol.
    /// </summary>
    bool SolveBallisticArc(Vector3 start, Vector3 target, float angleDeg, float gravity, out Vector3 initialVelocity, out float timeToTarget)
    {
        initialVelocity = Vector3.zero;
        timeToTarget = 0f;
        float theta = Mathf.Deg2Rad * angleDeg;
        Vector3 diff = target - start;
        Vector3 diffXZ = new Vector3(diff.x, 0f, diff.z);
        float dx = diffXZ.magnitude;
        float dy = diff.y;
        float g = Mathf.Abs(gravity);
        if (dx < 0.001f) return false;
        float cos = Mathf.Cos(theta);
        float sin = Mathf.Sin(theta);
        float denom = dx * Mathf.Tan(theta) - dy;
        if (denom <= 0f) return false;
        float v2 = (g * dx * dx) / (2f * cos * cos * denom);
        if (v2 <= 0f) return false;
        float v = Mathf.Sqrt(v2);
        Vector3 v0 = diffXZ.normalized * v * cos + Vector3.up * v * sin;
        float t = dx / (v * cos);
        initialVelocity = v0;
        timeToTarget = t;
        return true;
    }

}