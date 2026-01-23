using UnityEngine;

/// <summary>
/// Zone qui joue une musique d'ambiance quand le joueur entre dedans.
/// À placer sur un GameObject avec un BoxCollider (ou autre) en mode Trigger.
/// </summary>
public class AmbientSoundZone : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("Musique d'ambiance à jouer (loop)")]
    public AudioClip ambientMusic;
    
    [Tooltip("Volume de la musique (0 à 1)")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    
    [Tooltip("Spatialisation du son (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 0.3f;
    
    [Tooltip("Démarrer la musique automatiquement au Start")]
    public bool autoPlay = false;
    
    [Tooltip("Arrêter la musique quand le joueur sort de la zone")]
    public bool stopOnExit = true;
    
    [Header("Player Detection")]
    [Tooltip("Tag du joueur à détecter (par défaut 'Player')")]
    public string playerTag = "Player";
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private AudioSource audioSource;
    private bool isPlayerInZone = false;
    
    void Start()
    {
        // Vérifier qu'il y a bien un collider en mode trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("❌ AmbientSoundZone: Aucun Collider trouvé! Ajoutez un BoxCollider en mode Trigger.");
            return;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning("⚠️ AmbientSoundZone: Le Collider n'est pas en mode Trigger! Activation automatique...");
            col.isTrigger = true;
        }
        
        // Créer l'AudioSource
        if (ambientMusic != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = ambientMusic;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = spatialBlend;
            
            if (autoPlay)
            {
                audioSource.Play();
                if (showDebugInfo)
                {
                    Debug.Log($"🎵 Musique d'ambiance démarrée automatiquement: {ambientMusic.name}");
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"✅ Zone d'ambiance initialisée: {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ AmbientSoundZone: Aucun clip audio assigné!");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
            
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
                
                if (showDebugInfo)
                {
                    Debug.Log($"🎵 Joueur entré dans la zone - Musique démarrée: {ambientMusic.name}");
                }
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = false;
            
            if (stopOnExit && audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                
                if (showDebugInfo)
                {
                    Debug.Log("🔇 Joueur sorti de la zone - Musique arrêtée");
                }
            }
        }
    }
    
    // Méthodes publiques pour contrôler la musique depuis d'autres scripts
    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
    
    void OnDrawGizmos()
    {
        // Dessiner la zone dans l'éditeur
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
