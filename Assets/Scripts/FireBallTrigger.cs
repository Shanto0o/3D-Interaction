using UnityEngine;

public class FireBallTrigger : MonoBehaviour
{
    [Header("Particle System")]
    [Tooltip("Le Particle System enfant à activer (laissez vide pour auto-détection)")]
    public ParticleSystem targetParticleSystem;

    [Header("Light")]
    [Tooltip("La Light enfant à activer (laissez vide pour auto-détection)")]
    public Light targetLight;

    [Header("Color Filter")]
    [Tooltip("La couleur de boule de feu requise pour activer le trigger")]
    public FireColor requiredColor = FireColor.Rouge;

    [Header("Settings")]
    [Tooltip("Active automatiquement le loop du Particle System")]
    public bool enableLoop = true;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // État de la torche
    private bool isLit = false;
    public bool IsLit { get { return isLit; } }

    private void Start()
    {
        // Si aucun particle system n'est assigné, chercher dans les enfants
        if (targetParticleSystem == null)
        {
            targetParticleSystem = GetComponentInChildren<ParticleSystem>();
            
            if (targetParticleSystem == null)
            {
                Debug.LogWarning($"[FireBallTrigger] Aucun Particle System trouvé sur {gameObject.name} ou ses enfants!");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Particle System auto-détecté: {targetParticleSystem.name}");
            }
        }

        // Si aucune light n'est assignée, chercher dans les enfants
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>();
            
            if (targetLight == null)
            {
                Debug.LogWarning($"[FireBallTrigger] Aucune Light trouvée sur {gameObject.name} ou ses enfants!");
            }
            else if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Light auto-détectée: {targetLight.name}");
            }
        }

        // S'assurer que le Collider est en mode Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Collider configuré en mode Trigger sur {gameObject.name}");
            }
        }

        // Désactiver le particle system au démarrage
        if (targetParticleSystem != null)
        {
            targetParticleSystem.Stop();
            
            // Configurer le loop si demandé
            if (enableLoop)
            {
                var main = targetParticleSystem.main;
                main.loop = true;
            }
        }

        // Désactiver la light au démarrage
        if (targetLight != null)
        {
            targetLight.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est une boule de feu (GameObject nommé "FireBall" ou "ChargingFireBall")
        if (other.gameObject.name.Contains("FireBall"))
        {
            // Vérifier la couleur de la boule de feu
            FireBallColorTag colorTag = other.GetComponent<FireBallColorTag>();
            
            if (colorTag == null)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[FireBallTrigger] Boule de feu sans tag de couleur: {other.gameObject.name}");
                }
                return;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Boule de feu détectée: {other.gameObject.name} - Couleur: {colorTag.color} (Requise: {requiredColor})");
            }

            // Vérifier si la couleur correspond
            if (colorTag.color == requiredColor)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[FireBallTrigger] Couleur correcte! Activation...");
                }
                ActivateParticleSystem();
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[FireBallTrigger] Mauvaise couleur. {colorTag.color} != {requiredColor}");
                }
            }
        }
    }

    private void ActivateParticleSystem()
    {
        if (targetParticleSystem != null)
        {
            // Activer le loop si nécessaire
            if (enableLoop)
            {
                var main = targetParticleSystem.main;
                main.loop = true;
            }

            // Démarrer le particle system
            targetParticleSystem.Play();

            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Particle System activé: {targetParticleSystem.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[FireBallTrigger] Impossible d'activer le Particle System - non assigné!");
        }

        // Activer la light
        if (targetLight != null)
        {
            targetLight.enabled = true;

            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Light activée: {targetLight.name}");
            }
        }

        // Marquer comme allumé
        isLit = true;

        if (showDebugInfo)
        {
            Debug.Log($"[FireBallTrigger] Torche allumée!");
        }
    }

    // Méthode publique pour désactiver le particle system si besoin
    public void DeactivateParticleSystem()
    {
        if (targetParticleSystem != null)
        {
            targetParticleSystem.Stop();
            
            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Particle System désactivé: {targetParticleSystem.name}");
            }
        }

        // Désactiver la light
        if (targetLight != null)
        {
            targetLight.enabled = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"[FireBallTrigger] Light désactivée: {targetLight.name}");
            }
        }

        // Marquer comme éteint
        isLit = false;

        if (showDebugInfo)
        {
            Debug.Log($"[FireBallTrigger] Torche éteinte!");
        }
    }
}
