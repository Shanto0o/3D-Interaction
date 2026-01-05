using UnityEngine;

/// <summary>
/// Script pour un cube boss simplifié qui rétrécit à chaque changement de phase
/// et disparaît après plusieurs phases.
/// </summary>
public class BossCube : MonoBehaviour
{
    [Header("Boss Settings")]
    [Tooltip("Nombre de phases (rétrécissements)")]
    public int maxPhases = 3;
    [Tooltip("Nombre de coups nécessaires par phase")]
    public int hitsPerPhase = 5;
    
    [Header("Visual Feedback")]
    [Tooltip("Couleur initiale du boss")]
    public Color initialColor = Color.red;
    [Tooltip("Couleur quand le boss est presque détruit")]
    public Color finalColor = Color.yellow;
    [Tooltip("Activer les effets visuels lors des coups")]
    public bool showHitEffect = true;
    [Tooltip("Durée du flash noir lors d'un coup")]
    public float hitFlashDuration = 0.1f;
    [Tooltip("Durée du flash blanc lors d'un changement de phase")]
    public float phaseFlashDuration = 0.3f;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Son joué quand le boss est frappé")]
    public AudioClip hitSound;
    [Tooltip("Son joué quand le boss meurt")]
    public AudioClip deathSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État privé
    private int currentHits = 0;
    private int currentPhase = 0;
    private Renderer bossRenderer;
    private Color originalColor;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private Color flashColor;
    private Rigidbody bossRigidbody;
    
    void Start()
    {
        Debug.Log("🎮 ===== BOSSCUBE START =====");
        
        // Ajouter un Rigidbody kinematic pour détecter les collisions avec l'épée kinematic
        bossRigidbody = GetComponent<Rigidbody>();
        if (bossRigidbody == null)
        {
            bossRigidbody = gameObject.AddComponent<Rigidbody>();
            Debug.Log("➕ Rigidbody ajouté au BossCube");
        }
        bossRigidbody.isKinematic = true;
        bossRigidbody.useGravity = false;
        Debug.Log($"✅ BossCube Rigidbody configuré (kinematic: {bossRigidbody.isKinematic})");
        
        // Configurer les colliders : un physique (non-trigger) + un trigger pour détecter les coups
        SetupColliders();
        
        // Récupérer le renderer pour changer les couleurs
        bossRenderer = GetComponent<Renderer>();
        if (bossRenderer != null)
        {
            originalColor = initialColor;
            bossRenderer.material.color = initialColor;
            Debug.Log($"✅ Renderer configuré avec couleur: {initialColor}");
        }
        
        // Ajouter un AudioSource si des sons sont configurés
        if (hitSound != null || deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Son 3D
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"🎮 BossCube initialisé - {maxPhases} phases × {hitsPerPhase} coups = {maxPhases * hitsPerPhase} coups total");
        }
    }
    
    void SetupColliders()
    {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        
        if (colliders.Length == 0)
        {
            // Aucun collider : créer les deux
            BoxCollider physicsCollider = gameObject.AddComponent<BoxCollider>();
            physicsCollider.isTrigger = false;
            Debug.Log("➕ Collider physique (NON-trigger) créé - bloque l'épée");
            
            BoxCollider triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = Vector3.one * 1.05f; // Légèrement plus grand
            Debug.Log("➕ Collider trigger créé - détecte les coups");
        }
        else if (colliders.Length == 1)
        {
            // Un seul collider : le mettre en physique et ajouter un trigger
            colliders[0].isTrigger = false;
            Debug.Log($"✅ Collider existant configuré en physique (NON-trigger)");
            
            BoxCollider triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = colliders[0].size * 1.05f; // Légèrement plus grand
            Debug.Log("➕ Collider trigger ajouté pour détection des coups");
        }
        else
        {
            // Plusieurs colliders : s'assurer qu'un est physique et un est trigger
            bool hasPhysics = false;
            bool hasTrigger = false;
            
            foreach (BoxCollider col in colliders)
            {
                if (col.isTrigger)
                {
                    hasTrigger = true;
                    Debug.Log($"✅ Collider trigger trouvé");
                }
                else
                {
                    hasPhysics = true;
                    Debug.Log($"✅ Collider physique trouvé");
                }
            }
            
            if (!hasPhysics)
            {
                colliders[0].isTrigger = false;
                Debug.Log("⚠️ Aucun collider physique : premier collider configuré en NON-trigger");
            }
            
            if (!hasTrigger && colliders.Length > 1)
            {
                colliders[1].isTrigger = true;
                colliders[1].size = colliders[0].size * 1.05f;
                Debug.Log("⚠️ Aucun trigger : deuxième collider configuré en trigger");
            }
        }
    }
    
    void Update()
    {
        // Gérer l'effet de flash
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                isFlashing = false;
                if (bossRenderer != null)
                {
                    bossRenderer.material.color = originalColor;
                }
            }
            else
            {
                // Clignotement pendant le flash
                if (bossRenderer != null)
                {
                    bossRenderer.material.color = flashColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Appelé par OnCollisionEnter depuis l'épée ou directement
    /// </summary>
    public void TakeHit(Vector3 hitPoint)
    {
        currentHits++;
        int hitsInCurrentPhase = currentHits % hitsPerPhase;
        if (hitsInCurrentPhase == 0) hitsInCurrentPhase = hitsPerPhase;
        
        if (showDebugInfo)
        {
            Debug.Log($"💥 Boss frappé! Phase {currentPhase + 1}/{maxPhases} | Coups dans cette phase: {hitsInCurrentPhase}/{hitsPerPhase} | Total: {currentHits}/{maxPhases * hitsPerPhase}");
        }
        
        // Effet visuel du coup (flash noir)
        if (showHitEffect)
        {
            ShowHitEffect();
        }
        
        // Jouer le son de coup
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Vérifier si on change de phase (5 coups atteints)
        if (currentHits % hitsPerPhase == 0)
        {
            currentPhase++;
            AdvancePhase();
        }
    }
    
    /// <summary>
    /// Change de phase : réduit le scale et change la couleur
    /// </summary>
    void AdvancePhase()
    {
        if (showDebugInfo)
        {
            Debug.Log($"🔄 CHANGEMENT DE PHASE! Phase {currentPhase}/{maxPhases}");
        }
        
        // Diviser le scale par 2
        transform.localScale = transform.localScale * 0.5f;
        
        // Effet visuel de changement de phase (flash blanc)
        if (showHitEffect)
        {
            ShowPhaseChangeEffect();
        }
        
        // Mettre à jour la couleur en fonction de la phase
        UpdateColor();
        
        // Vérifier si le boss doit être détruit
        if (currentPhase >= maxPhases)
        {
            DestroySelf();
        }
    }
    
    void ShowHitEffect()
    {
        if (bossRenderer != null)
        {
            // Flash noir pour un coup normal
            flashColor = Color.black;
            bossRenderer.material.color = flashColor;
            isFlashing = true;
            flashTimer = hitFlashDuration;
        }
    }
    
    void ShowPhaseChangeEffect()
    {
        if (bossRenderer != null)
        {
            // Flash blanc pour un changement de phase
            flashColor = Color.white;
            bossRenderer.material.color = flashColor;
            isFlashing = true;
            flashTimer = phaseFlashDuration;
        }
    }
    
    void UpdateColor()
    {
        if (bossRenderer == null) return;
        
        // Interpoler entre la couleur initiale et finale en fonction de la phase
        float progress = (float)currentPhase / maxPhases;
        originalColor = Color.Lerp(initialColor, finalColor, progress);
        
        if (!isFlashing)
        {
            bossRenderer.material.color = originalColor;
        }
    }
    
    void DestroySelf()
    {
        if (showDebugInfo)
        {
            Debug.Log("💀 Boss détruit!");
        }
        
        // Jouer le son de mort
        if (deathSound != null && audioSource != null)
        {
            // Créer un GameObject temporaire pour jouer le son après la destruction
            GameObject tempAudio = new GameObject("TempAudio");
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = deathSound;
            tempSource.spatialBlend = 0f; // Son 2D pour qu'on l'entende partout
            tempSource.Play();
            Destroy(tempAudio, deathSound.length);
        }
        
        // Détruire le cube
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Détecte les collisions avec l'épée
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔔 BOSSCUBE: OnCollisionEnter avec [{collision.gameObject.name}]");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {collision.gameObject.tag}");
        Debug.Log($"   └─ Contacts: {collision.contacts.Length}");
        
        // Vérifier si c'est l'épée qui a frappé
        SwordDamageDealer sword = collision.gameObject.GetComponent<SwordDamageDealer>();
        if (sword == null)
        {
            Debug.LogWarning($"⚠️ Pas de SwordDamageDealer sur [{collision.gameObject.name}]");
            
            // Vérifier dans les parents
            sword = collision.gameObject.GetComponentInParent<SwordDamageDealer>();
            if (sword != null)
            {
                Debug.Log($"✅ SwordDamageDealer trouvé dans le parent!");
            }
            else
            {
                Debug.LogWarning($"❌ Aucun SwordDamageDealer trouvé même dans les parents");
                return;
            }
        }
        else
        {
            Debug.Log($"✅ SwordDamageDealer trouvé sur [{collision.gameObject.name}]!");
        }
        
        if (sword.CanDealDamage())
        {
            Debug.Log("✅ L'épée PEUT infliger des dégâts!");
            // Obtenir le point de contact
            Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            
            TakeHit(hitPoint);
            
            // Notifier l'épée qu'elle a frappé
            sword.OnHitConfirmed();
        }
        else
        {
            Debug.LogWarning("⚠️ L'épée NE PEUT PAS infliger des dégâts (cooldown ou vélocité)");
        }
    }
    
    /// <summary>
    /// Détecte les triggers avec l'épée (FONCTIONNE AVEC KINEMATIC!)
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔔 BOSSCUBE: OnTriggerEnter avec [{other.gameObject.name}]");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {other.gameObject.tag}");
        
        // Vérifier si c'est l'épée qui a frappé
        SwordDamageDealer sword = other.gameObject.GetComponent<SwordDamageDealer>();
        if (sword == null)
        {
            Debug.LogWarning($"⚠️ Pas de SwordDamageDealer sur [{other.gameObject.name}]");
            
            // Vérifier dans les parents
            sword = other.gameObject.GetComponentInParent<SwordDamageDealer>();
            if (sword != null)
            {
                Debug.Log($"✅ SwordDamageDealer trouvé dans le parent!");
            }
            else
            {
                Debug.LogWarning($"❌ Aucun SwordDamageDealer trouvé même dans les parents");
                return;
            }
        }
        else
        {
            Debug.Log($"✅ SwordDamageDealer trouvé sur [{other.gameObject.name}]!");
        }
        
        if (sword.CanDealDamage())
        {
            Debug.Log("✅ L'épée PEUT infliger des dégâts!");
            // Pour un trigger, utiliser la position du collider
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            
            TakeHit(hitPoint);
            
            // Notifier l'épée qu'elle a frappé
            sword.OnHitConfirmed();
        }
        else
        {
            Debug.LogWarning("⚠️ L'épée NE PEUT PAS infliger des dégâts (cooldown ou vélocité)");
        }
    }
    
    void OnDrawGizmos()
    {
        // Dessiner une sphère autour du boss pour visualiser sa zone
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
