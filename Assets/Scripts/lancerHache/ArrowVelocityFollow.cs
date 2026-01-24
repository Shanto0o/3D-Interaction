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
        }
    }

    void FixedUpdate()
    {
        if (hasHit || rb == null) return;

        // Appliquer une gravité réduite manuellement
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);

        // Orienter la flèche dans la direction de son mouvement
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Arrêter de suivre la vélocité après impact
        hasHit = true;
        
        // Désactiver ce script après la collision
        this.enabled = false;
    }
}
