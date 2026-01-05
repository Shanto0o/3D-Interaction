using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Script pour rendre une hache/épée saisissable et lançable avec XR Interaction Toolkit
/// À attacher sur l'arme avec XRGrabInteractable
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class XRThrowableWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Dégâts infligés")]
    public float damage = 50f;
    
    [Tooltip("Vitesse minimale pour infliger des dégâts")]
    public float minVelocityForDamage = 2f;

    [Header("Throw Settings")]
    [Tooltip("Multiplicateur de force de lancer")]
    public float throwVelocityScale = 1.5f;
    
    [Tooltip("Multiplicateur de rotation")]
    public float throwAngularVelocityScale = 1.0f;

    [Header("Visual Feedback")]
    [Tooltip("Effet de particules lors de la saisie")]
    public ParticleSystem grabEffect;
    
    [Tooltip("Effet de particules lors de l'impact")]
    public ParticleSystem hitEffect;

    [Header("Audio")]
    [Tooltip("Son lors de la saisie")]
    public AudioClip grabSound;
    
    [Tooltip("Son lors du lancer")]
    public AudioClip throwSound;
    
    [Tooltip("Son lors de l'impact")]
    public AudioClip hitSound;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isGrabbed = false;
    private Vector3 velocity;
    private Vector3 lastPosition;

    void Awake()
    {
        // Récupérer les composants
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Configuration du Rigidbody
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (grabSound != null || throwSound != null || hitSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // S'abonner aux événements XR Grab
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculer la vélocité pour les dégâts
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (showDebugInfo && isGrabbed)
        {
            Debug.Log($"[XRThrowableWeapon] Velocity: {velocity.magnitude:F2}");
        }
    }

    /// <summary>
    /// Appelé quand l'arme est saisie
    /// </summary>
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        // Feedback visuel
        if (grabEffect != null)
        {
            grabEffect.Play();
        }

        // Feedback audio
        if (audioSource != null && grabSound != null)
        {
            audioSource.PlayOneShot(grabSound);
        }

        if (showDebugInfo)
            Debug.Log($"[XRThrowableWeapon] Weapon grabbed by {args.interactorObject.transform.name}");
    }

    /// <summary>
    /// Appelé quand l'arme est lâchée/lancée
    /// </summary>
    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Obtenir la vélocité de l'interactor (main/controller)
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
        {
            // Appliquer la vélocité de lancer
            if (interactor.attachTransform != null)
            {
                Vector3 throwVelocity = GetInteractorVelocity(interactor);
                Vector3 throwAngularVelocity = GetInteractorAngularVelocity(interactor);

                rb.linearVelocity = throwVelocity * throwVelocityScale;
                rb.angularVelocity = throwAngularVelocity * throwAngularVelocityScale;

                if (showDebugInfo)
                    Debug.Log($"[XRThrowableWeapon] Thrown with velocity: {throwVelocity.magnitude:F2}");
            }
        }

        // Son de lancer
        if (audioSource != null && throwSound != null)
        {
            audioSource.PlayOneShot(throwSound);
        }
    }

    /// <summary>
    /// Obtient la vélocité de l'interactor
    /// </summary>
    Vector3 GetInteractorVelocity(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor.attachTransform != null)
        {
            // XR Toolkit calcule automatiquement la vélocité
            if (interactor.TryGetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(out var controllerInteractor))
            {
                // Pour les controllers
                return controllerInteractor.GetComponent<Rigidbody>() != null 
                    ? controllerInteractor.GetComponent<Rigidbody>().linearVelocity 
                    : velocity;
            }
            else if (interactor.TryGetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>(out var directInteractor))
            {
                // Pour le hand tracking direct
                return velocity;
            }
        }
        return velocity;
    }

    /// <summary>
    /// Obtient la vélocité angulaire de l'interactor
    /// </summary>
    Vector3 GetInteractorAngularVelocity(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor.TryGetComponent<Rigidbody>(out var interactorRb))
        {
            return interactorRb.angularVelocity;
        }
        return Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignorer les collisions quand l'arme est tenue
        if (isGrabbed) return;

        // Vérifier la vélocité pour infliger des dégâts
        float impactVelocity = velocity.magnitude;

        if (impactVelocity >= minVelocityForDamage)
        {
            // Chercher un composant de santé
            var health = collision.gameObject.GetComponent<IHealth>();
            if (health != null)
            {
                float finalDamage = damage * (impactVelocity / minVelocityForDamage);
                health.TakeDamage(finalDamage);

                if (showDebugInfo)
                    Debug.Log($"[XRThrowableWeapon] Dealt {finalDamage:F1} damage to {collision.gameObject.name}");
            }

            // Effet d'impact
            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, collision.contacts[0].point, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, 2f);
            }

            // Son d'impact
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
    }

    void OnDestroy()
    {
        // Se désabonner des événements
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualiser la vélocité
        if (Application.isPlaying && velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, velocity.normalized * 0.5f);
        }
    }
}

/// <summary>
/// Interface pour les objets qui peuvent recevoir des dégâts
/// Ajoutez cette interface à vos cibles/ennemis
/// </summary>
public interface IHealth
{
    void TakeDamage(float damage);
}
