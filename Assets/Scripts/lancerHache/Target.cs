using UnityEngine;

public class Target : MonoBehaviour, IHealth
{
    [Header("Visual")]
    [Tooltip("Renderer de la cible (pour changer la couleur)")]
    public Renderer targetRenderer;

    [Header("Particle Effect")]
    [Tooltip("Effet de particules lors de l'impact")]
    public ParticleSystem hitEffect;

    private TargetGame gameManager;
    private bool isCorrectTarget = false;
    private Material targetMaterial;
    private Color originalColor;

    void Awake()
    {
        // Obtenir le renderer automatiquement si non assigné
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        // Créer une copie du matériau pour pouvoir le modifier
        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            originalColor = targetMaterial.color;
        }
    }

    /// <summary>
    /// Enregistre le game manager
    /// </summary>
    public void SetGameManager(TargetGame manager)
    {
        gameManager = manager;
    }

    /// <summary>
    /// Définit cette cible comme étant la bonne à toucher
    /// </summary>
    public void SetAsCorrectTarget(Color color, float emissionIntensity)
    {
        isCorrectTarget = true;
        UpdateVisual(color, emissionIntensity);
    }

    /// <summary>
    /// Définit cette cible comme étant une mauvaise cible
    /// </summary>
    public void SetAsWrongTarget(Color color, float emissionIntensity)
    {
        isCorrectTarget = false;
        UpdateVisual(color, emissionIntensity);
    }

    /// <summary>
    /// Met à jour l'apparence de la cible
    /// </summary>
    void UpdateVisual(Color color, float emissionIntensity)
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
        // Effet de particules
        if (hitEffect != null)
        {
            hitEffect.Play();
        }

        // Notifier le game manager
        if (gameManager != null)
        {
            gameManager.OnTargetHit(this);
        }

        Debug.Log($"[Target] {gameObject.name} touched! Is correct: {isCorrectTarget}");
    }

    void OnDestroy()
    {
        // Nettoyer le matériau
        if (targetMaterial != null)
            Destroy(targetMaterial);
    }
}