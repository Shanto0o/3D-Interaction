using UnityEngine;
using Oculus.Voice;
using Meta.WitAi;
using Meta.WitAi.Json;

/// <summary>
/// Système de reconnaissance vocale pour rappeler la hache avec Meta Voice SDK (AppVoiceExperience)
/// NÉCESSITE: Meta Voice SDK installé et AppVoiceExperience configuré
/// Configuration: Créez une app Wit.ai sur wit.ai et configurez l'intent "recall_axe"
/// </summary>
public class WitAxeRecall : MonoBehaviour
{
    [Header("Voice Experience")]
    [Tooltip("Référence au AppVoiceExperience (créez-le via GameObject > Voice SDK > App Voice Experience)")]
    public AppVoiceExperience voiceExperience;
    
    [Header("Wit.ai Settings")]
    [Tooltip("Mots à détecter pour rappeler la hache (vérification simple dans transcription)")]
    public string[] triggerWords = new string[] { "hache", "axe", "rappelle", "reviens","h" };
    
    [Tooltip("Active automatiquement l'écoute en continu (RECOMMANDÉ pour mains uniquement)")]
    public bool continuousListening = true;
    
    [Tooltip("Délai entre les écoutes automatiques (secondes)")]
    public float listeningInterval = 1.5f;
    
    [Tooltip("Cooldown entre deux rappels (évite les spam)")]
    public float recallCooldown = 3f;

    [Header("References")]
    [Tooltip("La hache à rappeler")]
    public XRThrowableWeapon axe;
    
    [Tooltip("Caméra du joueur (pour savoir où est 'devant')")]
    public Transform playerCamera;
    
    [Tooltip("Main droite OVR")]
    public OVRHand rightHand;
    
    [Tooltip("Transform de la main droite")]
    public Transform rightHandTransform;

    [Header("Recall Settings")]
    [Tooltip("Distance devant le joueur où la hache apparaît")]
    public float spawnDistanceInFront = 1.5f;
    
    [Tooltip("Hauteur au-dessus de la caméra")]
    public float spawnHeightAboveCamera = 0.3f;
    
    [Tooltip("Vitesse de rappel de la hache")]
    public float recallSpeed = 15f;
    
    [Tooltip("Distance minimale pour attraper automatiquement")]
    public float autoCatchDistance = 0.3f;
    
    [Tooltip("Force d'attraction magnétique")]
    public float magneticForce = 50f;

    [Header("Visual Feedback")]
    [Tooltip("Ligne visuelle montrant le chemin")]
    public LineRenderer recallLine;
    
    [Tooltip("Effet de particules lors du rappel")]
    public ParticleSystem recallEffect;

    [Header("Audio Feedback")]
    public AudioClip recallSound;
    public AudioClip catchSound;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showTranscription = true;

    private bool isRecalling = false;
    private Rigidbody axeRigidbody;
    private AudioSource audioSource;
    private bool wasGravityEnabled;
    private bool isListening = false;
    private float lastRecallTime = -999f;

    void Start()
    {
        // Vérifier AppVoiceExperience
        if (voiceExperience == null)
        {
            voiceExperience = FindFirstObjectByType<AppVoiceExperience>();
            if (voiceExperience == null)
            {
                Debug.LogError("❌ [WitAxeRecall] AppVoiceExperience non trouvé! Créez-en un via: GameObject > Voice SDK > App Voice Experience");
                enabled = false;
                return;
            }
            else
            {
                Debug.Log("✅ [WitAxeRecall] AppVoiceExperience trouvé automatiquement");
            }
        }

        // Vérifier les références
        if (axe == null)
        {
            Debug.LogError("❌ [WitAxeRecall] Aucune hache assignée!");
            enabled = false;
            return;
        }

        axeRigidbody = axe.GetComponent<Rigidbody>();
        
        // S'abonner aux événements de grab de la hache
        var grabInteractable = axe.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnAxeGrabbed);
        }

        // Chercher la caméra automatiquement si non assignée
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null)
            {
                Debug.LogWarning("⚠️ [WitAxeRecall] Aucune caméra trouvée, cherche OVRCameraRig...");
                OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    playerCamera = cameraRig.centerEyeAnchor;
                }
            }
        }

        if (rightHandTransform == null && rightHand != null)
        {
            rightHandTransform = rightHand.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError("❌ [WitAxeRecall] Aucune caméra du joueur trouvée!");
            enabled = false;
            return;
        }

        // Désactiver la hache au départ
        if (axe != null)
        {
            axe.gameObject.SetActive(false);
            if (showDebugInfo)
            {
                Debug.Log("👻 [WitAxeRecall] Hache désactivée au démarrage");
            }
        }

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // S'abonner aux événements Voice
        voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        voiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        voiceExperience.VoiceEvents.OnResponse.AddListener(OnWitResponse);
        voiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartListening);
        voiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        voiceExperience.VoiceEvents.OnError.AddListener(OnError);

        if (showDebugInfo)
        {
            Debug.Log($"✅ [WitAxeRecall] Initialisé avec AppVoiceExperience.");
            Debug.Log($"💡 [WitAxeRecall] Écoute continue: {continuousListening}");
            Debug.Log($"🎤 [WitAxeRecall] Dites simplement: {string.Join(", ", triggerWords)}");
        }

        // Créer la ligne visuelle
        CreateRecallLine();

        // Démarrer l'écoute continue automatiquement
        if (continuousListening)
        {
            InvokeRepeating(nameof(StartListening), 0.5f, listeningInterval);
            if (showDebugInfo)
            {
                Debug.Log("🔄 [WitAxeRecall] Écoute continue activée - Dites 'hache' à tout moment!");
            }
        }
    }

    void CreateRecallLine()
    {
        if (recallLine == null)
        {
            GameObject lineObj = new GameObject("WitRecallLine");
            lineObj.transform.SetParent(transform);
            recallLine = lineObj.AddComponent<LineRenderer>();
            recallLine.startWidth = 0.02f;
            recallLine.endWidth = 0.02f;
            recallLine.material = new Material(Shader.Find("Sprites/Default"));
            recallLine.startColor = new Color(0f, 1f, 1f, 0.5f);
            recallLine.endColor = new Color(0f, 1f, 1f, 0.5f);
            recallLine.positionCount = 2;
            recallLine.enabled = false;
        }
    }

    /// <summary>
    /// Callback quand l'écoute démarre
    /// </summary>
    void OnStartListening()
    {
        isListening = true;
        if (showDebugInfo)
            Debug.Log("🎤 [WitAxeRecall] Écoute démarrée...");
    }

    /// <summary>
    /// Callback quand l'écoute s'arrête
    /// </summary>
    void OnStoppedListening()
    {
        isListening = false;
        if (showDebugInfo)
            Debug.Log("🔇 [WitAxeRecall] Écoute arrêtée");
    }

    /// <summary>
    /// Callback en cas d'erreur
    /// </summary>
    void OnError(string error, string message)
    {
        Debug.Log($"❌ [WitAxeRecall] Erreur: {error} - {message}");
    }

    /// <summary>
    /// Callback quand Wit renvoie une réponse
    /// </summary>
    void OnWitResponse(WitResponseNode response)
    {
        if (response == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("⚠️ [WitAxeRecall] Réponse vide de Wit.ai");
            return;
        }

        // Récupérer la transcription directement
        string transcription = response["text"]?.Value;
        
        if (string.IsNullOrEmpty(transcription))
        {
            // Essayer aussi avec _text
            transcription = response["_text"]?.Value;
        }

        if (string.IsNullOrEmpty(transcription))
        {
            if (showDebugInfo)
                Debug.Log("ℹ️ [WitAxeRecall] Aucune transcription détectée");
            return;
        }

        transcription = transcription.ToLower();

        if (showDebugInfo)
        {
            Debug.Log($"🎤 [WitAxeRecall] Transcription: '{transcription}'");
        }

        // Vérifier si un des mots déclencheurs est présent
        bool wordFound = false;
        foreach (string word in triggerWords)
        {
            if (transcription.Contains(word.ToLower()))
            {
                wordFound = true;
                if (showDebugInfo)
                {
                    Debug.Log($"✅ [WitAxeRecall] Mot déclencheur détecté: '{word}'");
                }
                break;
            }
        }

        if (wordFound)
        {
            // Vérifier le cooldown
            if (Time.time - lastRecallTime < recallCooldown)
            {
                float remaining = recallCooldown - (Time.time - lastRecallTime);
                if (showDebugInfo)
                {
                    Debug.Log($"⏳ [WitAxeRecall] Cooldown actif ({remaining:F1}s restantes)");
                }
                return;
            }

            lastRecallTime = Time.time;
            StartRecall();
        }
    }

    /// <summary>
    /// Affiche la transcription partielle (optionnel)
    /// </summary>
    void OnPartialTranscription(string transcription)
    {
        if (showTranscription && !string.IsNullOrEmpty(transcription))
        {
            Debug.Log($"🎙️ [WitAxeRecall] Écoute: '{transcription}'");
        }
    }

    /// <summary>
    /// Affiche la transcription complète
    /// </summary>
    void OnFullTranscription(string transcription)
    {
        if (showTranscription && !string.IsNullOrEmpty(transcription))
        {
            Debug.Log($"🎙️ [WitAxeRecall] Entendu: '{transcription}'");
        }
    }

    /// <summary>
    /// Méthode publique pour activer manuellement l'écoute (optionnel)
    /// </summary>
    public void StartListening()
    {
        if (voiceExperience == null)
        {
            Debug.LogError("❌ [WitAxeRecall] AppVoiceExperience manquant!");
            return;
        }

        // Ne pas relancer si déjà en écoute ou en train de rappeler
        if (isListening || isRecalling)
        {
            return;
        }

        // Activer l'écoute avec AppVoiceExperience
        voiceExperience.Activate();
    }

    /// <summary>
    /// Arrête l'écoute manuellement
    /// </summary>
    public void StopListening()
    {
        if (voiceExperience != null && isListening)
        {
            voiceExperience.Deactivate();
            if (showDebugInfo)
                Debug.Log("🔇 [WitAxeRecall] Écoute arrêtée manuellement");
        }
    }

    /// <summary>
    /// Démarre le rappel de la hache - Apparition instantanée devant le joueur
    /// </summary>
    void StartRecall()
    {
        if (axe == null || axeRigidbody == null || playerCamera == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("⚠️ [WitAxeRecall] Impossible de rappeler");
            return;
        }

        // Activer la hache si elle est désactivée
        if (!axe.gameObject.activeSelf)
        {
            axe.gameObject.SetActive(true);
        }

        // Calculer la position devant le joueur à la même hauteur
        Vector3 spawnPosition = playerCamera.position + playerCamera.forward * spawnDistanceInFront;
        spawnPosition.y += spawnHeightAboveCamera; // Un peu plus haut
        
        // Positionner la hache
        axe.transform.position = spawnPosition;
        axe.transform.rotation = playerCamera.rotation;

        // Réinitialiser la physique avec gravité activée
        axeRigidbody.linearVelocity = Vector3.zero;
        axeRigidbody.angularVelocity = Vector3.zero;
        axeRigidbody.useGravity = true;
        axeRigidbody.isKinematic = false; // Physique active pour que la gravité fonctionne

        // Effets visuels et sonores
        if (recallEffect != null)
        {
            recallEffect.transform.position = spawnPosition;
            recallEffect.Play();
        }

        if (audioSource != null && recallSound != null)
            audioSource.PlayOneShot(recallSound);

        if (audioSource != null && catchSound != null)
            audioSource.PlayOneShot(catchSound);

        if (showDebugInfo)
            Debug.Log("⚔️ [WitAxeRecall] Hache apparue devant le joueur!");
    }

    /// <summary>
    /// Appelé quand le joueur attrape la hache - Réactive la physique
    /// </summary>
    void OnAxeGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        // La physique est déjà active, pas besoin de la réactiver
        if (showDebugInfo)
            Debug.Log("✋ [WitAxeRecall] Hache attrapée!");
    }

    void Update()
    {
        if (!isRecalling) return;

        if (axe == null || axeRigidbody == null || rightHandTransform == null)
        {
            StopRecall();
            return;
        }

        Vector3 direction = (rightHandTransform.position - axe.transform.position).normalized;
        float distance = Vector3.Distance(axe.transform.position, rightHandTransform.position);

        // Force magnétique
        float force = Mathf.Lerp(magneticForce, magneticForce * 2f, 1f - Mathf.Clamp01(distance / 5f));
        axeRigidbody.AddForce(direction * force, ForceMode.Force);

        // Rotation vers la main
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        axe.transform.rotation = Quaternion.Slerp(axe.transform.rotation, targetRotation, Time.deltaTime * 5f);

        // Ligne visuelle
        if (recallLine != null && recallLine.enabled)
        {
            recallLine.SetPosition(0, rightHandTransform.position);
            recallLine.SetPosition(1, axe.transform.position);
        }

        // Attraper si assez proche
        if (distance < autoCatchDistance)
        {
            CatchAxe();
        }
    }

    void CatchAxe()
    {
        if (showDebugInfo)
            Debug.Log("✋ [WitAxeRecall] Hache attrapée!");

        axe.transform.position = rightHandTransform.position;
        axe.transform.rotation = rightHandTransform.rotation;

        axeRigidbody.linearVelocity = Vector3.zero;
        axeRigidbody.angularVelocity = Vector3.zero;

        if (audioSource != null && catchSound != null)
            audioSource.PlayOneShot(catchSound);

        StopRecall();
    }

    void StopRecall()
    {
        isRecalling = false;

        if (axeRigidbody != null)
            axeRigidbody.useGravity = wasGravityEnabled;

        if (recallLine != null)
            recallLine.enabled = false;

        if (recallEffect != null && recallEffect.isPlaying)
            recallEffect.Stop();
    }

    void OnDestroy()
    {
        // Désabonner des événements de la hache
        if (axe != null)
        {
            var grabInteractable = axe.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnAxeGrabbed);
            }
        }
        
        if (voiceExperience != null && voiceExperience.VoiceEvents != null)
        {
            voiceExperience.VoiceEvents.OnResponse.RemoveListener(OnWitResponse);
            voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            voiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartListening);
            voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            voiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
        }

        if (continuousListening)
        {
            CancelInvoke(nameof(StartListening));
        }
    }

    void OnDrawGizmos()
    {
        if (rightHandTransform != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(rightHandTransform.position, autoCatchDistance);
        }

        if (Application.isPlaying && isRecalling && axe != null && rightHandTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHandTransform.position, axe.transform.position);
        }
    }
}
