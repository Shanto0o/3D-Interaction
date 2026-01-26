using UnityEngine;

/// <summary>
/// Fait pointer la flèche dans la direction de son mouvement pour une trajectoire visuelle droite
/// </summary>
public class ArrowVelocityFollow : MonoBehaviour
{
    [Tooltip("Échelle de gravité (0-1) - plus petit = trajectoire plus droite")]
    [Range(0f, 1f)]
    public float gravityScale = 0.3f;
    
    private Rigidbody rb;
    private bool hasHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Appliquer une gravité réduite pour une trajectoire plus droite
            rb.useGravity = false; // On va gérer la gravité manuellement
            rb.maxAngularVelocity = 0.1f; // limiter la rotation physique initiale
        }
    }

    void FixedUpdate()
    {
        if (hasHit || rb == null) return;

        // Appliquer une gravité réduite manuellement
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        // Orienter la flèche dans la direction de son mouvement
        Vector3 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(vel.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.fixedDeltaTime * 10f);
        }

        // Réduire la rotation physique résiduelle
        rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Récupérer le Rigidbody (au cas où)
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Assurer qu'il y a un collider non trigger pour s'accrocher
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Ajouter un BoxCollider approximatif
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                bc.size = rend.bounds.size;
            }
            col = bc;
        }
        if (col != null) col.isTrigger = false;

        // Préserver la position de contact et orienter/attacher la flèche
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point;
        Vector3 contactNormal = contact.normal;

        // Positionner légèrement la flèche enfoncée dans la surface pour l'effet "planté"
        transform.position = contactPoint - transform.forward * 0.02f;

        // Rendre le rigidbody kinematic pour qu'il ne traverse plus rien
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // Attacher la flèche à l'objet touché
        transform.SetParent(collision.transform, true);

        // Désactiver ce script
        this.enabled = false;
    }
}
