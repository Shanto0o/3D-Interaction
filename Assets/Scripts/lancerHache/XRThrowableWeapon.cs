using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Script pour rendre une hache saisissable et lançable avec XR Interaction Toolkit
/// Inclut rotation réaliste et système de plantation dans les surfaces
/// À attacher sur la hache avec XRGrabInteractable
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class XRThrowableWeapon : MonoBehaviour
{
    [Header("Axe Settings")]
    [Tooltip("Dégâts infligés")]
    public float damage = 75f;
    
    [Tooltip("Vitesse minimale pour infliger des dégâts")]
    public float minVelocityForDamage = 3f;
    
    [Tooltip("Masse de la hache (kg)")]
    public float axeMass = 1.5f;

    [Header("Throw Settings")]
    [Tooltip("Multiplicateur de force de lancer")]
    public float throwVelocityScale = 1.5f;
    
    [Tooltip("Multiplicateur de rotation de la hache")]
    public float throwAngularVelocityScale = 2.5f;
    
    [Tooltip("Force de rotation automatique pour simulation de lancer de hache")]
    public float axeSpinForce = 10f;
    
    [Tooltip("Axe de rotation de la hache (local space)")]
    public Vector3 spinAxis = Vector3.right;

    [Header("Stick Settings")]
    [Tooltip("La hache peut-elle se planter dans les surfaces?")]
    public bool canStickToSurfaces = true;
    
    [Tooltip("Vitesse minimale pour se planter")]
    public float minStickVelocity = 4f;
    
    [Tooltip("Tags des objets où la hache peut se planter")]
    public string[] stickableTags = new string[] { "Enemy", "Wood", "Target", "Wall" };

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

    [Header("Return Settings")]
    [Tooltip("La hache revient-elle automatiquement après X secondes?")]
    public bool autoReturn = false;
    
    [Tooltip("Temps avant retour automatique (secondes)")]
    public float returnTime = 3f;
    
    [Tooltip("Vitesse de retour")]
    public float returnSpeed = 10f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool isGrabbed = false;
    private bool isStuck = false;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private Transform stuckParent;
    private Vector3 stuckLocalPosition;
    private Quaternion stuckLocalRotation;
    private float throwTime = 0f;
    private Transform ownerHand;

    void Awake()
    {
        // Récupérer les composants
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        // Configuration du Rigidbody pour une hache
        rb.mass = axeMass;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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
        if (isStuck)
        {
            // Maintenir la position si plantée
            MaintainStuckPosition();
            
            // Retour automatique
            if (autoReturn && Time.time - throwTime > returnTime)
            {
                ReturnToHand();
            }
            return;
        }

        // Calculer la vélocité pour les dégâts
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        if (showDebugInfo && isGrabbed)
        {
            Debug.Log($"[XRThrowableAxe] Velocity: {velocity.magnitude:F2} m/s");
        }
    }

    /// <summary>
    /// Appelé quand la hache est saisie
    /// </summary>
    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Détacher si plantée
        if (isStuck)
        {
            Unstick();
        }

        isGrabbed = true;
        ownerHand = args.interactorObject.transform;

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
            Debug.Log($"[XRThrowableAxe] Axe grabbed by {args.interactorObject.transform.name}");
    }

    /// <summary>
    /// Appelé quand la hache est lâchée/lancée
    /// </summary>
    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        throwTime = Time.time;

        // Obtenir la vélocité de l'interactor (main/controller)
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
        {
            // Appliquer la vélocité de lancer
            if (interactor.attachTransform != null)
            {
                Vector3 throwVelocity = GetInteractorVelocity(interactor);
                Vector3 throwAngularVelocity = GetInteractorAngularVelocity(interactor);

                // Appliquer vélocité linéaire
                rb.linearVelocity = throwVelocity * throwVelocityScale;
                
                // Appliquer rotation de hache (spin)
                Vector3 axeSpinVector = transform.TransformDirection(spinAxis) * axeSpinForce;
                rb.angularVelocity = (throwAngularVelocity * throwAngularVelocityScale) + axeSpinVector;

                if (showDebugInfo)
                    Debug.Log($"[XRThrowableAxe] Thrown with velocity: {throwVelocity.magnitude:F2} m/s, spin: {axeSpinVector.magnitude:F2}");
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
        // Ignorer les collisions quand la hache est tenue
        if (isGrabbed || isStuck) return;

        // Vérifier la vélocité pour infliger des dégâts
        float impactVelocity = velocity.magnitude;

        if (showDebugInfo)
            Debug.Log($"[XRThrowableAxe] Impact with {collision.gameObject.name} at {impactVelocity:F2} m/s");

        if (impactVelocity >= minVelocityForDamage)
        {
            // Chercher un composant de santé
            var health = collision.gameObject.GetComponent<IHealth>();
            if (health != null)
            {
                float finalDamage = damage * (impactVelocity / minVelocityForDamage);
                health.TakeDamage(finalDamage);

                if (showDebugInfo)
                    Debug.Log($"[XRThrowableAxe] Dealt {finalDamage:F1} damage to {collision.gameObject.name}");
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

            // Vérifier si la hache peut se planter
            if (canStickToSurfaces && impactVelocity >= minStickVelocity)
            {
                if (CanStickToObject(collision.gameObject))
                {
                    StickToSurface(collision);
                }
            }
        }
    }

    /// <summary>
    /// Vérifie si la hache peut se planter dans cet objet
    /// </summary>
    bool CanStickToObject(GameObject obj)
    {
        foreach (string tag in stickableTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Plante la hache dans la surface
    /// </summary>
    void StickToSurface(Collision collision)
    {
        isStuck = true;
        stuckParent = collision.transform;

        // Désactiver la physique
        rb.isKinematic = true;
        rb.useGravity = false;

        // Sauvegarder la position relative
        stuckLocalPosition = stuckParent.InverseTransformPoint(transform.position);
        stuckLocalRotation = Quaternion.Inverse(stuckParent.rotation) * transform.rotation;

        // Parenter à l'objet
        transform.SetParent(stuckParent);

        if (showDebugInfo)
            Debug.Log($"[XRThrowableAxe] Stuck to {stuckParent.name}");
    }

    /// <summary>
    /// Détache la hache de la surface
    /// </summary>
    void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        transform.SetParent(null);

        // Réactiver la physique
        rb.isKinematic = false;
        rb.useGravity = true;

        stuckParent = null;

        if (showDebugInfo)
            Debug.Log($"[XRThrowableAxe] Unstuck from surface");
    }

    /// <summary>
    /// Maintient la position quand plantée
    /// </summary>
    void MaintainStuckPosition()
    {
        if (stuckParent == null)
        {
            Unstick();
            return;
        }

        // Suivre la position/rotation du parent
        transform.position = stuckParent.TransformPoint(stuckLocalPosition);
        transform.rotation = stuckParent.rotation * stuckLocalRotation;
    }

    /// <summary>
    /// Retour automatique vers la main
    /// </summary>
    void ReturnToHand()
    {
        if (ownerHand == null) return;

        Unstick();

        // Désactiver temporairement la gravité pour le retour
        rb.useGravity = false;
        
        // Direction vers la main
        Vector3 direction = (ownerHand.position - transform.position).normalized;
        rb.linearVelocity = direction * returnSpeed;

        if (showDebugInfo)
            Debug.Log($"[XRThrowableAxe] Returning to hand");

        // Réactiver la gravité après un court délai
        Invoke(nameof(ReenableGravity), 0.5f);
    }

    void ReenableGravity()
    {
        if (rb != null && !isGrabbed)
        {
            rb.useGravity = true;
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

        // Visualiser l'axe de rotation
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.TransformDirection(spinAxis) * 0.3f);
        }

        // Visualiser si plantée
        if (Application.isPlaying && isStuck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
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
