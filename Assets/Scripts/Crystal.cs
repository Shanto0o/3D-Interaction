using UnityEngine;
using System;

/// <summary>
/// Cristal qui peut être détruit par le laser après 2 secondes de visée continue
/// </summary>
public class Crystal : MonoBehaviour
{
    [Header("Crystal Settings")]
    [Tooltip("Temps requis pour casser le cristal (en secondes)")]
    public float breakDuration = 2f;
    
    [Header("Visual Feedback")]
    [Tooltip("Effet visuel lors de la destruction")]
    public ParticleSystem breakEffect;
    [Tooltip("Matériau à appliquer pendant le chargement")]
    public Material chargingMaterial;
    
    [Header("Audio")]
    [Tooltip("Son joué pendant que le cristal se fait casser (loop)")]
    public AudioClip chargingSound;
    [Tooltip("Volume du son de chargement (0 à 5 pour amplification)")]
    [Range(0f, 5f)]
    public float chargingVolume = 5.0f;
    [Tooltip("Son joué quand le cristal casse")]
    public AudioClip breakSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État interne
    private float currentChargeTime = 0f;
    private bool isBeingCharged = false;
    private Renderer crystalRenderer;
    private Material originalMaterial;
    private bool isBroken = false;
    private AudioSource audioSource;
    private bool isPlayingChargingSound = false;
    
    // Event pour notifier la destruction du cristal
    public static event Action<Crystal> OnCrystalDestroyed;
    
    void Start()
    {
        crystalRenderer = GetComponent<Renderer>();
        if (crystalRenderer != null)
        {
            originalMaterial = crystalRenderer.material;
        }
        
        // Ajouter un AudioSource si des sons sont configurés
        if (chargingSound != null || breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Son 3D
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"💎 Cristal initialisé: {gameObject.name}");
        }
    }
    
    void Update()
    {
        // Si le cristal n'est plus visé, réduire progressivement le chargement
        if (!isBeingCharged && currentChargeTime > 0f)
        {
            currentChargeTime -= Time.deltaTime * 0.5f; // Décharge 2x plus lentement
            if (currentChargeTime < 0f)
            {
                currentChargeTime = 0f;
                RestoreOriginalMaterial();
            }
            
            // Arrêter le son de chargement si le cristal n'est plus visé
            if (isPlayingChargingSound && audioSource != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
                isPlayingChargingSound = false;
            }
            
            UpdateVisualFeedback();
        }
        
        // Reset le flag pour le prochain frame
        isBeingCharged = false;
    }
    
    /// <summary>
    /// Appelé par le laser quand il touche le cristal
    /// </summary>
    public void OnLaserHit()
    {
        if (isBroken) return;
        
        isBeingCharged = true;
        currentChargeTime += Time.deltaTime;
        
        // Démarrer le son de chargement si ce n'est pas déjà fait
        if (!isPlayingChargingSound && chargingSound != null && audioSource != null)
        {
            audioSource.clip = chargingSound;
            audioSource.loop = true;
            audioSource.volume = chargingVolume;
            audioSource.Play();
            isPlayingChargingSound = true;
        }
        
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"💎 {gameObject.name} - Charge: {currentChargeTime:F2}s / {breakDuration}s ({(currentChargeTime/breakDuration)*100:F0}%)");
        }
        
        UpdateVisualFeedback();
        
        // Vérifier si le temps de chargement est atteint
        if (currentChargeTime >= breakDuration)
        {
            BreakCrystal();
        }
    }
    
    void UpdateVisualFeedback()
    {
        if (crystalRenderer == null) return;
        
        // Calculer le pourcentage de charge
        float chargePercent = Mathf.Clamp01(currentChargeTime / breakDuration);
        
        // Changer progressivement la couleur ou utiliser le matériau de chargement
        if (chargingMaterial != null && chargePercent > 0f)
        {
            crystalRenderer.material = chargingMaterial;
            
            // Modifier l'émission selon le pourcentage
            if (chargingMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = Color.Lerp(Color.white, Color.red, chargePercent);
                chargingMaterial.SetColor("_EmissionColor", emissionColor * chargePercent * 2f);
            }
        }
        else if (originalMaterial != null && chargePercent == 0f)
        {
            crystalRenderer.material = originalMaterial;
        }
        
        // Faire pulser le cristal selon le chargement
        float scale = 1f + Mathf.Sin(Time.time * 10f * chargePercent) * 0.1f * chargePercent;
        transform.localScale = Vector3.one * scale;
    }
    
    void RestoreOriginalMaterial()
    {
        if (crystalRenderer != null && originalMaterial != null)
        {
            crystalRenderer.material = originalMaterial;
        }
    }
    
    void BreakCrystal()
    {
        if (isBroken) return;
        
        isBroken = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"💥💥💥 CRISTAL CASSÉ: {gameObject.name}");
        }
        
        // Arrêter le son de chargement
        if (isPlayingChargingSound && audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isPlayingChargingSound = false;
        }
        
        // Jouer le son de destruction
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
        
        // Déclencher l'effet de particules
        if (breakEffect != null)
        {
            ParticleSystem effect = Instantiate(breakEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 3f);
        }
        
        // Notifier tous les écouteurs (notamment le boss)
        OnCrystalDestroyed?.Invoke(this);
        
        // Détruire le cristal après un court délai pour laisser le son se jouer
        Destroy(gameObject, breakSound != null ? Mathf.Min(breakSound.length, 2f) : 0f);
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Afficher la progression du chargement
        Gizmos.color = Color.Lerp(Color.blue, Color.red, currentChargeTime / breakDuration);
        Gizmos.DrawWireSphere(transform.position, 0.5f + (currentChargeTime / breakDuration) * 0.3f);
    }
}