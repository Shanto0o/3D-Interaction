using UnityEngine;

public class Target : MonoBehaviour, IHealth
{
    [Header("Visual")]
    [Tooltip("Renderer de la cible (pour changer la couleur)")]
    public Renderer targetRenderer;

    [Header("Particle Effect")]
    [Tooltip("Effet de particules lors de l'impact")]
    public ParticleSystem hitEffect;

    [Header("Colors")]
    [Tooltip("Couleur initiale (rouge)")]
    public Color initialColor = Color.red;
    
    [Tooltip("Couleur après impact (vert)")]
    public Color hitColor = Color.green;
    
    [Tooltip("Intensité d'émission")]
    public float emissionIntensity = 2f;

    private TargetGame gameManager;
    private bool hasBeenHit = false;
    private Material targetMaterial;

    void Awake()
    {
        // Obtenir le renderer automatiquement si non assigné
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        // Créer une copie du matériau pour pouvoir le modifier
        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
        }
    }

    void Start()
    {
        // Initialiser en rouge
        UpdateVisual(initialColor);
    }

    /// <summary>
    /// Enregistre le game manager
    /// </summary>
    public void SetGameManager(TargetGame manager)
    {
        gameManager = manager;
    }

    /// <summary>
    /// Met à jour l'apparence de la cible
    /// </summary>
    void UpdateVisual(Color color)
    {
        if (targetMaterial == null) return;

        targetMaterial.color = color;

        // Activer l'émission (glow) si le matériau le supporte
        if (targetMaterial.HasProperty("_EmissionColor"))
        {
            targetMaterial.EnableKeyword("_EMISSION");
            targetMaterial.SetColor("_EmissionColor", color * emissionIntensity);
        }
    }

    /// <summary>
    /// Implémentation de IHealth - appelé quand la hache touche
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Ne rien faire si déjà touchée
        if (hasBeenHit)
            return;

        // Marquer comme touchée
        hasBeenHit = true;

        // Changer la couleur en vert
        UpdateVisual(hitColor);

        // Effet de particules
        if (hitEffect != null)
        {
            hitEffect.Play();
        }

        // Notifier le game manager si présent
        if (gameManager != null)
        {
            gameManager.OnTargetHit(this);
        }

        Debug.Log($"[Target] {gameObject.name} touchée ! Passage au vert.");
    }

    void OnDestroy()
    {
        // Nettoyer le matériau
        if (targetMaterial != null)
            Destroy(targetMaterial);
    }
}