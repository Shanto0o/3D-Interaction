using UnityEngine;

/// <summary>
/// Active/désactive le fog quand le joueur entre/sort d'une zone trigger
/// </summary>
public class FogTriggerZone : MonoBehaviour
{
    [Header("Fog Settings")]
    [Tooltip("Active le fog quand le joueur entre dans la zone")]
    public bool enableFogOnEnter = true;
    
    [Tooltip("Désactive le fog quand le joueur sort de la zone")]
    public bool disableFogOnExit = true;

    [Header("Fog Configuration")]
    public Color fogColor = Color.gray;
    public FogMode fogMode = FogMode.ExponentialSquared;
    
    [Tooltip("Densité du fog (pour ExponentialSquared ou Exponential)")]
    [Range(0f, 1f)]
    public float fogDensity = 0.01f;

    [Header("Player Detection")]
    [Tooltip("Tag du joueur (par défaut 'Player')")]
    public string playerTag = "Player";

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool playerInZone = false;
    private float originalAmbientIntensity;

    private void Start()
    {
        // Sauvegarder l'intensité ambiante d'origine
        originalAmbientIntensity = RenderSettings.ambientIntensity;

        // Désactiver le fog au démarrage
        RenderSettings.fog = false;

        // S'assurer que le Collider est en mode Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            if (showDebugInfo)
            {
                Debug.Log($"[FogTriggerZone] Collider configuré en mode Trigger sur {gameObject.name}");
            }
        }
        else if (col == null)
        {
            Debug.LogWarning($"[FogTriggerZone] Aucun Collider trouvé sur {gameObject.name}! Ajoutez un BoxCollider ou autre.");
        }

        if (showDebugInfo)
        {
            Debug.Log("[FogTriggerZone] Fog désactivé au démarrage");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur
        if (other.CompareTag(playerTag))
        {
            playerInZone = true;

            if (enableFogOnEnter)
            {
                ActivateFog();
            }

            if (showDebugInfo)
            {
                Debug.Log($"[FogTriggerZone] Joueur entré dans la zone: {other.gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;

            if (disableFogOnExit)
            {
                DeactivateFog();
            }

            if (showDebugInfo)
            {
                Debug.Log($"[FogTriggerZone] Joueur sorti de la zone: {other.gameObject.name}");
            }
        }
    }

    private void ActivateFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        
        // Réduire l'intensité de l'éclairage ambiant à 0
        RenderSettings.ambientIntensity = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"[FogTriggerZone] Fog activé - Mode: {fogMode}, Densité: {fogDensity}, Ambient Intensity: 0");
        }
    }

    private void DeactivateFog()
    {
        RenderSettings.fog = false;
        
        // Restaurer l'intensité de l'éclairage ambiant
        RenderSettings.ambientIntensity = originalAmbientIntensity;

        if (showDebugInfo)
        {
            Debug.Log($"[FogTriggerZone] Fog désactivé, Ambient Intensity restaurée: {originalAmbientIntensity}");
        }
    }

    // Méthodes publiques pour contrôle manuel
    public void EnableFog()
    {
        ActivateFog();
    }

    public void DisableFog()
    {
        DeactivateFog();
    }

    public bool IsPlayerInZone()
    {
        return playerInZone;
    }

    // Visualisation de la zone dans l'éditeur
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
    }
}
