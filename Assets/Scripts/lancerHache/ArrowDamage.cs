using UnityEngine;

/// <summary>
/// Script pour la flèche qui inflige des dégâts aux cibles
/// </summary>
public class ArrowDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Dégâts infligés à la cible")]
    public float damage = 1f;

    [Header("Stick to Target")]
    [Tooltip("La flèche reste plantée dans la cible")]
    public bool stickToTarget = true;

    [Header("Auto Destroy")]
    [Tooltip("Détruire la flèche après X secondes (0 = jamais)")]
    public float destroyAfterSeconds = 10f;

    [Header("Trajectory Visualization")]
    [Tooltip("Ajouter un Trail Renderer pour voir la trajectoire")]
    public bool addTrailEffect = true;
    
    [Tooltip("Durée du trail en secondes")]
    public float trailTime = 0.5f;
    
    [Tooltip("Largeur du trail")]
    public float trailWidth = 0.02f;
    
    [Tooltip("Couleur du trail")]
    public Color trailColor = Color.yellow;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool hasHit = false;
    private Rigidbody rb;
    private TrailRenderer trail;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Créer le Trail Renderer pour visualiser la trajectoire
        if (addTrailEffect)
        {
            SetupTrail();
        }
    }
    
    /// <summary>
    /// Configure le Trail Renderer pour voir la trajectoire
    /// </summary>
    void SetupTrail()
    {
        trail = gameObject.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }
        
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth * 0.1f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        
        // Configurer le gradient de couleur
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trail.colorGradient = gradient;
        
        if (showDebugInfo)
        {
            Debug.Log("[ArrowDamage] Trail Renderer ajouté pour visualiser la trajectoire");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ne traiter qu'une seule collision
        if (hasHit) return;

        hasHit = true;

        if (showDebugInfo)
        {
            Debug.Log($"[ArrowDamage] Flèche a touché : {collision.gameObject.name}");
        }

        // Vérifier si l'objet touché implémente IHealth
        IHealth healthComponent = collision.gameObject.GetComponent<IHealth>();
        if (healthComponent != null)
        {
            // Infliger des dégâts
            healthComponent.TakeDamage(damage);

            if (showDebugInfo)
            {
                Debug.Log($"[ArrowDamage] Dégâts infligés à {collision.gameObject.name}: {damage}");
            }
        }

        // Accrocher la flèche à la cible
        if (stickToTarget)
        {
            StickToSurface(collision);
        }
        
        // Désactiver le trail une fois la flèche plantée
        if (trail != null)
        {
            trail.emitting = false;
        }

        // Détruire après un certain temps
        if (destroyAfterSeconds > 0)
        {
            Destroy(gameObject, destroyAfterSeconds);
        }
    }

    /// <summary>
    /// Coller la flèche à la surface touchée
    /// </summary>
    void StickToSurface(Collision collision)
    {
        // Désactiver la physique
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Parenter la flèche à l'objet touché
        transform.SetParent(collision.transform);

        // Désactiver le collider pour éviter d'autres collisions
        Collider arrowCollider = GetComponent<Collider>();
        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }

        if (showDebugInfo)
        {
            Debug.Log("[ArrowDamage] Flèche plantée dans la cible");
        }
    }
}
