using UnityEngine;

/// <summary>
/// Script de debug pour voir ce qui se passe avec la flèche en temps réel
/// </summary>
public class ArrowDebugger : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 lastPosition;
    private float lastLogTime;
    private int frameCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        lastLogTime = Time.time;
        
        Debug.Log($"🎯 [ArrowDebugger] Flèche lancée à {Time.time:F2}s");
        Debug.Log($"   Position initiale: {transform.position}");
        Debug.Log($"   Rotation initiale: {transform.rotation.eulerAngles}");
        if (rb != null)
        {
            Debug.Log($"   Vélocité initiale: {rb.linearVelocity}");
        }
    }

    void Update()
    {
        if (rb == null) return;

        frameCount++;

        // Logger toutes les 0.2 secondes
        if (Time.time - lastLogTime > 0.2f)
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 direction = (transform.position - lastPosition).normalized;
            float speed = velocity.magnitude;
            
            Debug.Log($"🎯 Frame {frameCount} - T:{Time.time - lastLogTime:F2}s");
            Debug.Log($"   Position: {transform.position}");
            Debug.Log($"   Vélocité: {velocity} (vitesse: {speed:F2})");
            Debug.Log($"   Direction déplacement: {direction}");
            Debug.Log($"   Forward de la flèche: {transform.forward}");
            Debug.Log($"   Rotation: {transform.rotation.eulerAngles}");
            
            lastPosition = transform.position;
            lastLogTime = Time.time;
        }

        // Dessiner la trajectoire en temps réel (visible dans la Scene view)
        Debug.DrawRay(transform.position, rb.linearVelocity.normalized * 2f, Color.cyan, 0.1f);
        Debug.DrawRay(transform.position, transform.forward * 2f, Color.yellow, 0.1f);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"💥 [ArrowDebugger] Impact avec {collision.gameObject.name}");
        Debug.Log($"   Position finale: {transform.position}");
        Debug.Log($"   Vélocité à l'impact: {rb.linearVelocity}");
    }
}
