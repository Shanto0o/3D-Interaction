using UnityEngine;
using Oculus.Voice; // Meta Voice SDK
using Meta.WitAi.Json; // for WitResponseNode

/// <summary>
/// Sort de bouclier activé par la voix quand l'épée est équipée.
/// Instancie un prefab de bouclier devant le joueur pendant 3 secondes.
/// </summary>
public class ShieldSpell : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Référence au script SwordGrabInteraction de l'épée")]
    public SwordGrabInteraction swordScript;
    [Tooltip("Référence à l'AppVoiceExperience pour la reconnaissance vocale")]
    public AppVoiceExperience voice;
    [Tooltip("Transform du joueur (généralement la caméra/OVRCameraRig)")]
    public Transform playerTransform;
    [Tooltip("Prefab du bouclier (ForceShield)")]
    public GameObject shieldPrefab;
    
    [Header("Shield Settings")]
    [Tooltip("Durée du bouclier en secondes")]
    public float shieldDuration = 3f;
    [Tooltip("Scale du bouclier")]
    public float shieldScale = 1.5f;
    [Tooltip("Distance devant le joueur")]
    public float forwardDistance = 1f;
    [Tooltip("Offset vertical du bouclier")]
    public float verticalOffset = 0f;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Son joué à l'activation du bouclier")]
    public AudioClip activationSound;
    [Tooltip("Son joué à la désactivation du bouclier")]
    public AudioClip deactivationSound;
    
    [Header("Cooldown")]
    [Tooltip("Temps de recharge entre deux utilisations")]
    public float cooldownTime = 5f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État privé
    private GameObject shieldObject;
    private bool isShieldActive = false;
    private float shieldTimer = 0f;
    private bool isVoiceCommandReceived = false;
    private bool isVoiceActive = false;
    private float voiceCooldown = 0f;
    private const float VOICE_COOLDOWN_TIME = 0.5f;
    private float voiceActiveTimer = 0f;
    private const float MAX_VOICE_DURATION = 10f; // Timeout de 10 secondes
    private float spellCooldown = 0f;
    private AudioSource audioSource;
    
    void Start()
    {
        // Configurer la reconnaissance vocale
        if (voice != null)
        {
            voice.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            voice.VoiceEvents.OnError.AddListener(OnVoiceError);
            voice.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            voice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else
        {
            Debug.LogWarning("⚠️ AppVoiceExperience non assigné! Les commandes vocales ne fonctionneront pas.");
        }
        
        // Vérifier les références
        if (swordScript == null)
        {
            Debug.LogError("❌ SwordGrabInteraction non assigné!");
        }
        
        if (shieldPrefab == null)
        {
            Debug.LogError("❌ Shield Prefab non assigné!");
        }
        
        if (playerTransform == null)
        {
            // Essayer de trouver automatiquement le joueur (OVRCameraRig)
            OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
            {
                playerTransform = cameraRig.trackingSpace;
                Debug.Log("✅ PlayerTransform trouvé automatiquement (OVRCameraRig)");
            }
            else
            {
                Debug.LogError("❌ PlayerTransform non assigné et impossible de le trouver automatiquement!");
            }
        }
        
        // Ajouter un AudioSource si des sons sont configurés
        if (activationSound != null || deactivationSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Son 2D
        }
        
        if (showDebugInfo)
        {
            Debug.Log("🛡️ ShieldSpell initialisé - Dites 'Bouclier' avec l'épée équipée!");
        }
    }
    
    void Update()
    {
        // Gestion de l'activation continue de la voix
        if (voice != null)
        {
            if (voiceCooldown > 0f)
            {
                voiceCooldown -= Time.deltaTime;
            }

            if (!isVoiceActive && voiceCooldown <= 0f)
            {
                voice.Activate();
                isVoiceActive = true;
                voiceActiveTimer = 0f;
            }
            
            // Désactiver automatiquement après le timeout
            if (isVoiceActive)
            {
                voiceActiveTimer += Time.deltaTime;
                if (voiceActiveTimer >= MAX_VOICE_DURATION)
                {
                    voice.Deactivate();
                    isVoiceActive = false;
                    voiceCooldown = VOICE_COOLDOWN_TIME;
                    if (showDebugInfo)
                    {
                        Debug.Log("⏱️ Voice deactivated - timeout reached");
                    }
                }
            }
        }
        
        // Gérer le cooldown du sort
        if (spellCooldown > 0f)
        {
            spellCooldown -= Time.deltaTime;
        }
        
        // Vérifier si la commande vocale a été reçue
        if (isVoiceCommandReceived)
        {
            TryActivateShield();
            isVoiceCommandReceived = false;
        }
        
        // Gérer le bouclier actif
        if (isShieldActive)
        {
            UpdateShield();
        }
    }
    
    void TryActivateShield()
    {
        // Vérifier si l'épée est équipée
        if (swordScript == null || !IsSwordEquipped())
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("⚠️ Impossible d'activer le bouclier : épée non équipée!");
            }
            return;
        }
        
        // Vérifier le cooldown
        if (spellCooldown > 0f)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"⚠️ Bouclier en cooldown : {spellCooldown:F1}s restantes");
            }
            return;
        }
        
        // Vérifier si un bouclier est déjà actif
        if (isShieldActive)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("⚠️ Un bouclier est déjà actif!");
            }
            return;
        }
        
        // Activer le bouclier
        ActivateShield();
    }
    
    bool IsSwordEquipped()
    {
        // Accéder au champ privé isAttached via reflection ou une méthode publique
        // Pour l'instant, on vérifie si l'épée n'a pas de gravité (signe qu'elle est attachée)
        Rigidbody swordRb = swordScript.GetComponent<Rigidbody>();
        if (swordRb != null)
        {
            return !swordRb.useGravity; // Si pas de gravité, elle est équipée
        }
        return false;
    }
    
    void ActivateShield()
    {
        if (showDebugInfo)
        {
            Debug.Log($"🛡️ BOUCLIER ACTIVÉ pour {shieldDuration} secondes!");
        }
        
        // Créer le bouclier depuis le prefab
        CreateShieldMesh();
        
        isShieldActive = true;
        shieldTimer = shieldDuration;
        spellCooldown = cooldownTime;
        
        // Jouer le son d'activation
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }
    
    void CreateShieldMesh()
    {
        if (playerTransform == null)
        {
            Debug.LogError("❌ PlayerTransform non disponible!");
            return;
        }
        
        if (shieldPrefab == null)
        {
            Debug.LogError("❌ Shield Prefab non assigné!");
            return;
        }
        
        // Calculer la position devant le joueur
        Vector3 spawnPosition = playerTransform.position + playerTransform.forward * forwardDistance + Vector3.up * verticalOffset;
        
        // Instancier le prefab du bouclier devant le joueur
        shieldObject = Instantiate(shieldPrefab, spawnPosition, Quaternion.identity);
        shieldObject.name = "ActiveShield";
        
        // Faire face au joueur
        shieldObject.transform.LookAt(playerTransform);
        shieldObject.transform.Rotate(0, 180f, 0); // Tourner pour que le bouclier fasse face au joueur
        
        // Dimensionner le bouclier
        shieldObject.transform.localScale = Vector3.one * shieldScale;
        
        // Attacher le bouclier au joueur pour qu'il le suive
        shieldObject.transform.SetParent(playerTransform);
        
        if (showDebugInfo)
        {
            Debug.Log($"✨ Bouclier instancié devant le joueur! Distance: {forwardDistance}m, Scale: {shieldScale}");
        }
    }
    
    void UpdateShield()
    {
        shieldTimer -= Time.deltaTime;
        
        // Suivre le joueur
        if (shieldObject != null && playerTransform != null)
        {
            // Le bouclier suit automatiquement car il est enfant du playerTransform
        }
        
        // Désactiver le bouclier quand le timer expire
        if (shieldTimer <= 0f)
        {
            DeactivateShield();
        }
    }
    
    void DeactivateShield()
    {
        if (showDebugInfo)
        {
            Debug.Log("🛡️ Bouclier désactivé");
        }
        
        if (shieldObject != null)
        {
            Destroy(shieldObject);
        }
        
        isShieldActive = false;
        
        // Jouer le son de désactivation
        if (deactivationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deactivationSound);
        }
    }
    
    // ========== GESTION DE LA RECONNAISSANCE VOCALE ==========
    
    private void OnVoiceResponse(WitResponseNode response)
    {
        string text = response["text"];
        
        if (showDebugInfo)
        {
            Debug.Log($"🎤 Entendu: {text}");
        }

        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;

        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower();

        // Vérifier si le mot "bouclier" (ou variantes) est prononcé
        if (text.Contains("bouclier") || text.Contains("shield") || text.Contains("protection"))
        {
            isVoiceCommandReceived = true;
            if (showDebugInfo)
            {
                Debug.Log("✅ Commande vocale 'bouclier' reçue!");
            }
        }
    }

    private void OnVoiceError(string error, string message)
    {
        Debug.LogError($"❌ Erreur vocale: {error} - {message}");
        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;
    }

    private void OnPartialTranscription(string text)
    {
        if (showDebugInfo)
        {
            Debug.Log($"📝 Transcription partielle: {text}");
        }
    }

    private void OnFullTranscription(string text)
    {
        if (showDebugInfo)
        {
            Debug.Log($"📄 Transcription complète: {text}");
        }
    }

    void OnDestroy()
    {
        // Nettoyer les listeners
        if (voice != null)
        {
            voice.VoiceEvents.OnResponse.RemoveListener(OnVoiceResponse);
            voice.VoiceEvents.OnError.RemoveListener(OnVoiceError);
            voice.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            voice.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        }
        
        // Détruire le bouclier s'il existe encore
        if (shieldObject != null)
        {
            Destroy(shieldObject);
        }
    }
}
