using UnityEngine;

/// <summary>
/// Active la reconnaissance vocale en appuyant sur un bouton du controller VR
/// Attachez ce script sur un GameObject dans la scène
/// </summary>
public class VoiceActivationButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Le script WitAxeRecall à activer")]
    public WitAxeRecall witAxeRecall;

    [Header("Button Settings")]
    [Tooltip("Bouton pour activer l'écoute")]
    public OVRInput.Button activationButton = OVRInput.Button.Two; // Bouton Y ou B par défaut
    
    [Tooltip("Controller à utiliser (Left/Right/Any)")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    [Tooltip("Maintenir le bouton pour parler (Push-to-Talk)")]
    public bool pushToTalk = false;

    [Header("Visual Feedback")]
    [Tooltip("Afficher un indicateur visuel pendant l'écoute")]
    public GameObject listeningIndicator;
    
    [Tooltip("Particules lors de l'activation")]
    public ParticleSystem activationEffect;

    [Header("Audio Feedback")]
    [Tooltip("Son de début d'écoute")]
    public AudioClip startListeningSound;
    
    [Tooltip("Son de fin d'écoute")]
    public AudioClip stopListeningSound;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private AudioSource audioSource;
    private bool isListening = false;

    void Start()
    {
        if (witAxeRecall == null)
        {
            witAxeRecall = FindFirstObjectByType<WitAxeRecall>();
            if (witAxeRecall == null)
            {
                Debug.LogError("❌ [VoiceActivationButton] WitAxeRecall non trouvé!");
                enabled = false;
                return;
            }
        }

        // Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (startListeningSound != null || stopListeningSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        // Cacher l'indicateur au départ
        if (listeningIndicator != null)
        {
            listeningIndicator.SetActive(false);
        }

        if (showDebugInfo)
        {
            string buttonName = activationButton.ToString();
            Debug.Log($"✅ [VoiceActivationButton] Prêt! Appuyez sur {buttonName} ({controller}) pour parler");
        }
    }

    void Update()
    {
        if (witAxeRecall == null) return;

        if (pushToTalk)
        {
            // Mode Push-to-Talk: maintenir le bouton
            if (OVRInput.Get(activationButton, controller))
            {
                if (!isListening)
                {
                    StartVoiceListening();
                }
            }
            else if (isListening)
            {
                StopVoiceListening();
            }
        }
        else
        {
            // Mode Toggle: appuyer pour activer/désactiver
            if (OVRInput.GetDown(activationButton, controller))
            {
                if (!isListening)
                {
                    StartVoiceListening();
                }
                else
                {
                    StopVoiceListening();
                }
            }
        }
    }

    void StartVoiceListening()
    {
        isListening = true;
        witAxeRecall.StartListening();

        // Feedback visuel
        if (listeningIndicator != null)
        {
            listeningIndicator.SetActive(true);
        }

        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        // Feedback audio
        if (audioSource != null && startListeningSound != null)
        {
            audioSource.PlayOneShot(startListeningSound);
        }

        // Vibration du controller
        OVRInput.SetControllerVibration(0.2f, 0.2f, controller);
        Invoke(nameof(StopVibration), 0.1f);

        if (showDebugInfo)
        {
            Debug.Log("🎤 [VoiceActivationButton] Écoute activée - Parlez maintenant!");
        }
    }

    void StopVoiceListening()
    {
        isListening = false;
        witAxeRecall.StopListening();

        // Feedback visuel
        if (listeningIndicator != null)
        {
            listeningIndicator.SetActive(false);
        }

        // Feedback audio
        if (audioSource != null && stopListeningSound != null)
        {
            audioSource.PlayOneShot(stopListeningSound);
        }

        if (showDebugInfo)
        {
            Debug.Log("🔇 [VoiceActivationButton] Écoute arrêtée");
        }
    }

    void StopVibration()
    {
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    void OnDisable()
    {
        // Nettoyer
        if (isListening)
        {
            StopVoiceListening();
        }
    }
}
