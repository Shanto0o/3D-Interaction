using UnityEngine;

/// <summary>
/// Script à ajouter sur l'épée pour qu'elle puisse infliger des dégâts au boss.
/// Gère le cooldown entre les coups pour éviter les collisions multiples.
/// </summary>
public class SwordDamageDealer : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Temps minimum entre deux coups (pour éviter les collisions répétées)")]
    public float damageCooldown = 0.5f;
    
    [Tooltip("Vitesse minimale requise pour infliger des dégâts (détecte les vrais coups)")]
    public float minimumVelocity = 1.0f;
    
    [Header("Visual Feedback")]
    [Tooltip("Activer le feedback visuel lors d'un coup réussi")]
    public bool showHitFeedback = true;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Son joué quand l'épée frappe quelque chose")]
    public AudioClip swingHitSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État privé
    private float lastDamageTime = 0f;
    private Rigidbody swordRigidbody;
    private AudioSource audioSource;
    
    // Tracking manuel de vélocité (nécessaire quand kinematic)
    private Vector3 lastPosition;
    private Vector3 manualVelocity;
    
    void Start()
    {
        Debug.Log("⚔️ ===== SWORDDAMAGEDEALER START =====");
        Debug.Log($"   └─ GameObject: {gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"   └─ Tag: {gameObject.tag}");
        
        // Récupérer le Rigidbody pour calculer la vélocité
        swordRigidbody = GetComponent<Rigidbody>();
        if (swordRigidbody == null)
        {
            Debug.LogWarning("⚠️ SwordDamageDealer: Aucun Rigidbody trouvé sur l'épée!");
        }
        else
        {
            Debug.Log($"✅ Rigidbody trouvé - isKinematic: {swordRigidbody.isKinematic}, useGravity: {swordRigidbody.useGravity}");
        }
        
        // Vérifier les colliders
        Collider[] colliders = GetComponents<Collider>();
        Debug.Log($"✅ Colliders sur l'épée: {colliders.Length}");
        foreach (Collider col in colliders)
        {
            Debug.Log($"   └─ {col.GetType().Name} | IsTrigger: {col.isTrigger} | Enabled: {col.enabled}");
        }
        
        // Ajouter un AudioSource si un son est configuré
        if (swingHitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Son 3D
        }
        
        Debug.Log("✅ SwordDamageDealer initialisé");
        
        // Initialiser le tracking de position
        lastPosition = transform.position;
    }
    
    void Update()
    {
        // Calculer la vélocité manuelle (important quand kinematic)
        manualVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }
    
    /// <summary>
    /// Vérifie si l'épée peut infliger des dégâts (cooldown respecté et vélocité suffisante)
    /// </summary>
    public bool CanDealDamage()
    {
        Debug.Log($"🔍 CanDealDamage() appelé");
        
        // Vérifier le cooldown
        float timeSinceLastDamage = Time.time - lastDamageTime;
        Debug.Log($"   └─ Temps depuis dernier coup: {timeSinceLastDamage:F2}s | Cooldown: {damageCooldown}s");
        
        if (timeSinceLastDamage < damageCooldown)
        {
            Debug.Log($"❌ Cooldown en cours: {(damageCooldown - timeSinceLastDamage):F2}s restantes");
            return false;
        }
        
        // Vérifier la vélocité (utiliser manualVelocity si kinematic, sinon rigidbody)
        float currentVelocity = 0f;
        bool isKinematic = swordRigidbody != null && swordRigidbody.isKinematic;
        
        if (swordRigidbody != null)
        {
            // Toujours utiliser la vélocité du rigidbody si disponible
            float rigidbodyVel = swordRigidbody.linearVelocity.magnitude;
            float manualVel = manualVelocity.magnitude;
            
            // Prendre la plus grande des deux vélocités
            currentVelocity = Mathf.Max(rigidbodyVel, manualVel);
            
            if (isKinematic)
            {
                Debug.Log($"   └─ Mode KINEMATIC - Vélocité Rigidbody: {rigidbodyVel:F2} | Manuelle: {manualVel:F2} | Utilisée: {currentVelocity:F2} m/s");
            }
            else
            {
                Debug.Log($"   └─ Mode PHYSIQUE - Vélocité Rigidbody: {rigidbodyVel:F2} | Manuelle: {manualVel:F2} | Utilisée: {currentVelocity:F2} m/s");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Pas de Rigidbody - utiliser vélocité manuelle par défaut");
            currentVelocity = manualVelocity.magnitude;
        }
        
        Debug.Log($"   └─ Vélocité minimum requise: {minimumVelocity} m/s");
        
        if (currentVelocity < minimumVelocity)
        {
            Debug.Log($"❌ Vélocité trop faible: {currentVelocity:F2} m/s (min: {minimumVelocity} m/s)");
            return false;
        }
        
        Debug.Log($"✅ Vélocité de frappe suffisante: {currentVelocity:F2} m/s - Coup valide!");
        
        return true;
    }
    
    /// <summary>
    /// Appelé par le BossCube quand un coup réussit
    /// </summary>
    public void OnHitConfirmed()
    {
        lastDamageTime = Time.time;
        
        if (showDebugInfo)
        {
            Debug.Log("✅ Coup confirmé! Cooldown activé.");
        }
        
        // Jouer le son de frappe
        if (swingHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(swingHitSound);
        }
        
        // Feedback visuel (optionnel - peut être une brève lueur sur l'épée)
        if (showHitFeedback)
        {
            ShowHitFeedback();
        }
    }
    
    void ShowHitFeedback()
    {
        // Vous pouvez ajouter ici un effet visuel sur l'épée
        // Par exemple, un trail renderer, des particules, etc.
        
        // Pour l'instant, juste un log
        if (showDebugInfo)
        {
            Debug.Log("✨ Effet de frappe!");
        }
    }
    
    /// <summary>
    /// Détecte les collisions pour le debug
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"⚔️ ===== ÉPÉE: OnCollisionEnter avec [{collision.gameObject.name}] =====");
        
        float velocity = swordRigidbody != null ? swordRigidbody.linearVelocity.magnitude : manualVelocity.magnitude;
        Debug.Log($"   └─ Vélocité: {velocity:F2} m/s");
        Debug.Log($"   └─ Contacts: {collision.contacts.Length}");
        Debug.Log($"   └─ Layer collision: {LayerMask.LayerToName(collision.gameObject.layer)}");
        Debug.Log($"   └─ Tag collision: {collision.gameObject.tag}");
        
        if (swordRigidbody != null)
        {
            Debug.Log($"   └─ Épée isKinematic: {swordRigidbody.isKinematic}");
            Debug.Log($"   └─ Épée useGravity: {swordRigidbody.useGravity}");
        }
        
        // Vérifier si l'objet touché a un BossCube ou SlimeBoss
        BossCube boss = collision.gameObject.GetComponent<BossCube>();
        SlimeBoss slimeBoss = collision.gameObject.GetComponent<SlimeBoss>();
        if (boss != null)
        {
            Debug.Log($"✅ BossCube détecté sur [{collision.gameObject.name}]!");
        }
        else if (slimeBoss != null)
        {
            Debug.Log($"✅ SlimeBoss détecté sur [{collision.gameObject.name}]!");
        }
        else
        {
            Debug.Log($"ℹ️ Pas de BossCube/SlimeBoss sur [{collision.gameObject.name}]");
            // Chercher dans les parents
            boss = collision.gameObject.GetComponentInParent<BossCube>();
            slimeBoss = collision.gameObject.GetComponentInParent<SlimeBoss>();
            if (boss != null)
            {
                Debug.Log($"✅ BossCube trouvé dans parent!");
            }
            else if (slimeBoss != null)
            {
                Debug.Log($"✅ SlimeBoss trouvé dans parent!");
            }
            else
            {
                Debug.Log($"❌ Aucun boss trouvé (ni sur objet ni dans parents)");
            }
        }
    }
    
    /// <summary>
    /// Détecte les triggers (FONCTIONNE AVEC KINEMATIC!)
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"⚔️⚔️⚔️ SWORD OnTriggerEnter APPELÉ!");
        Debug.Log($"   └─ Objet trigger: {other.gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {other.gameObject.tag}");
        Debug.Log($"   └─ IsTrigger: {other.isTrigger}");
        Debug.Log($"   └─ Vélocité manuelle: {manualVelocity.magnitude:F2} m/s");
        
        if (swordRigidbody != null)
        {
            Debug.Log($"   └─ Épée isKinematic: {swordRigidbody.isKinematic}");
        }
        
        // Vérifier si l'objet touché a un BossCube ou SlimeBoss
        BossCube boss = other.gameObject.GetComponent<BossCube>();
        SlimeBoss slimeBoss = other.gameObject.GetComponent<SlimeBoss>();
        if (boss != null)
        {
            Debug.Log($"✅ BossCube détecté sur [{other.gameObject.name}]!");
        }
        else if (slimeBoss != null)
        {
            Debug.Log($"✅ SlimeBoss détecté sur [{other.gameObject.name}]!");
        }
        else
        {
            Debug.Log($"ℹ️ Pas de BossCube/SlimeBoss sur [{other.gameObject.name}]");
            // Chercher dans les parents
            boss = other.gameObject.GetComponentInParent<BossCube>();
            slimeBoss = other.gameObject.GetComponentInParent<SlimeBoss>();
            if (boss != null)
            {
                Debug.Log($"✅ BossCube trouvé dans parent!");
            }
            else if (slimeBoss != null)
            {
                Debug.Log($"✅ SlimeBoss trouvé dans parent!");
            }
            else
            {
                Debug.Log($"❌ Aucun boss trouvé (ni sur objet ni dans parents)");
            }
        }
    }
}
