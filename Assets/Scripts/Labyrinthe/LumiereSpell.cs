using UnityEngine;
using Oculus.Voice; // Meta Voice SDK
using Meta.WitAi.Json; // for WitResponseNode
using Oculus.Interaction.Input;

public enum LightGestureType
{
    Pinch,
    IndexPointing
}

public class LumiereSpell : MonoBehaviour
{
    public static bool hasBeenCastOnce = false; // Flag pour savoir si le sort a été lancé au moins une fois
    
    [Header("References")]
    public OVRHand hand;
    public ParticleSystem lightParticlePrefab; // Système de particules pour la lumière
    public AppVoiceExperience voice;
    
    [Tooltip("Door controller to open automatically when spell is cast")]
    public DoorControllerOpen doorToOpen;

    [Header("Gesture Type")]
    public LightGestureType gestureType = LightGestureType.Pinch;

    [Header("Gesture Settings")]
    [Tooltip("Seuil pour détecter que l'index est ouvert (pas de pinch)")]
    [Range(0.01f, 0.5f)]
    public float indexOpenThreshold = 0.08f;

    [Header("Positioning")]
    public float distanceFromFinger = 0.05f; // Distance depuis le bout du doigt
    public Vector3 rotationOffset = Vector3.zero; // Offset de rotation du particle system (en degrés)

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isIndexPointingUp = false;
    private bool wasIndexPointingUp = false;
    private bool wasIndexPointing = false; // Pour la gesture IndexPointing
    private bool isVoiceCommandReceived = false;
    private GameObject currentLight;

    private bool isListening = false;
    private float minRecordingTime = 0.5f; // Temps minimum avant de pouvoir arrêter l'enregistrement
    private float recordingStartTime = 0f;
    
    private bool wasPinching = false; // Pour détecter le début du pinch
    private bool isLightActive = false; // Indique si la lumière est actuellement active

    void Start()
    {
        if (voice != null)
        {
            voice.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            voice.VoiceEvents.OnError.AddListener(OnVoiceError);
            voice.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            voice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else
        {
            Debug.LogWarning("AppVoiceExperience (voice) not assigned. Voice commands won't work.");
        }
    }

    void Update()
    {
        if (hand == null)
        {
            Debug.LogWarning("Hand not assigned!");
            return;
        }

        // Vérifier le geste : pinch de l'index
        UpdateGesture();
    }

    void UpdateGesture()
    {
        if (gestureType == LightGestureType.Pinch)
        {
            UpdatePinchGesture();
        }
        else if (gestureType == LightGestureType.IndexPointing)
        {
            UpdateIndexPointingGesture();
        }
    }

    void UpdatePinchGesture()
    {
        bool isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        // Détection du début du pinch - ACTIVER LA VOIX
        if (isPinching && !wasPinching)
        {
            // Activer l'écoute vocale quand on commence à pincer
            if (voice != null && !isListening)
            {
                voice.Activate();
                isListening = true;
                recordingStartTime = Time.time;
                if (showDebugInfo)
                {
                    Debug.Log("Index pinch started! Voice activated.");
                }
            }
        }

        // Si on est en train de pincer et qu'on a reçu la commande vocale "lumière"
        if (isPinching && isVoiceCommandReceived && !isLightActive)
        {
            CreateLight();
            isVoiceCommandReceived = false; // Reset pour la prochaine fois
            isLightActive = true;
        }

        // Maintenir la lumière active tant que le pinch est maintenu
        if (isPinching && isLightActive && currentLight != null)
        {
            // La lumière reste attachée au doigt et active
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log("Light is active and following finger.");
            }
        }
        
        // Vérification de sécurité : si la lumière est active mais qu'on ne pince plus, l'éteindre
        if (!isPinching && isLightActive && currentLight != null)
        {
            Destroy(currentLight);
            currentLight = null;
            isLightActive = false;
            if (showDebugInfo)
            {
                Debug.Log("Light destroyed - safety check (no pinch detected).");
            }
        }

        // Fin du pinch - DÉSACTIVER LA VOIX ET ÉTEINDRE LA LUMIÈRE
        if (!isPinching && wasPinching)
        {
            // Éteindre la lumière quand on relâche le pinch
            if (currentLight != null)
            {
                Destroy(currentLight);
                currentLight = null;
                isLightActive = false;
                if (showDebugInfo)
                {
                    Debug.Log("Light destroyed - pinch released.");
                }
            }
            
            // Désactiver l'écoute vocale quand on arrête de pincer
            if (voice != null && isListening)
            {
                float recordingDuration = Time.time - recordingStartTime;
                if (recordingDuration >= minRecordingTime)
                {
                    voice.Deactivate();
                    isListening = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Index pinch ended. Voice deactivated (recorded for {recordingDuration:F2}s).");
                    }
                }
                else
                {
                    // Si pas assez de temps, on annule simplement
                    voice.Deactivate();
                    isListening = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Recording too short ({recordingDuration:F2}s), cancelled.");
                    }
                }
            }
        }

        wasPinching = isPinching;
    }

    void UpdateIndexPointingGesture()
    {
        bool isPointing = CheckIndexPointing();

        // Début de la gesture - ACTIVER LA VOIX
        if (isPointing && !wasIndexPointing)
        {
            // Activer l'écoute vocale quand on commence la gesture
            if (voice != null && !isListening)
            {
                voice.Activate();
                isListening = true;
                recordingStartTime = Time.time;
                if (showDebugInfo)
                {
                    Debug.Log("Index pointing detected! Voice activated.");
                }
            }
        }

        // Si on est en train de pointer et qu'on a reçu la commande vocale "lumière"
        if (isPointing && isVoiceCommandReceived && !isLightActive)
        {
            CreateLight();
            isVoiceCommandReceived = false; // Reset pour la prochaine fois
            isLightActive = true;
        }

        // Maintenir la lumière active tant que la gesture est maintenue
        if (isPointing && isLightActive && currentLight != null)
        {
            // La lumière reste attachée au doigt et active
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log("Light is active and following finger.");
            }
        }

        // Fin de la gesture - DÉSACTIVER LA VOIX ET ÉTEINDRE LA LUMIÈRE
        if (!isPointing && wasIndexPointing)
        {
            // Éteindre la lumière quand on arrête la gesture
            if (currentLight != null)
            {
                Destroy(currentLight);
                currentLight = null;
                isLightActive = false;
                if (showDebugInfo)
                {
                    Debug.Log("Light destroyed - gesture ended.");
                }
            }
            
            // Désactiver l'écoute vocale quand on arrête la gesture
            if (voice != null && isListening)
            {
                float recordingDuration = Time.time - recordingStartTime;
                if (recordingDuration >= minRecordingTime)
                {
                    voice.Deactivate();
                    isListening = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Gesture ended. Voice deactivated (recorded for {recordingDuration:F2}s).");
                    }
                }
                else
                {
                    // Si pas assez de temps, on annule simplement
                    voice.Deactivate();
                    isListening = false;
                    if (showDebugInfo)
                    {
                        Debug.Log($"Recording too short ({recordingDuration:F2}s), cancelled.");
                    }
                }
            }
        }

        wasIndexPointing = isPointing;
    }

    bool CheckIndexPointing()
    {
        // Vérifier que l'index est ouvert (pas de pinch)
        bool indexOpen = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < indexOpenThreshold;
        
        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Index Open: {indexOpen}");
        }
        
        return indexOpen;
    }

    void CreateLight()
    {
        // Marquer que le sort a été lancé et ouvrir la porte si c'est la première fois
        if (!hasBeenCastOnce)
        {
            hasBeenCastOnce = true;
            
            if (doorToOpen != null)
            {
                doorToOpen.OpenDoor();
                if (showDebugInfo)
                {
                    Debug.Log("LumiereSpell: Door opened automatically after first cast!");
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Creating light at finger tip!");
        }

        // Vérifier que le prefab existe
        if (lightParticlePrefab == null)
        {
            Debug.LogWarning("Light particle prefab not assigned!");
            return;
        }

        // Obtenir le transform du bout de l'index
        Transform indexTip = GetIndexTipTransform();
        if (indexTip == null)
        {
            Debug.LogWarning("Could not find index tip bone!");
            return;
        }

        // Instancier directement le système de particules (qui contient déjà la lumière en enfant)
        ParticleSystem ps = Instantiate(lightParticlePrefab);
        
        // Attacher la lumière au doigt
        ps.transform.SetParent(indexTip);
        ps.transform.localPosition = Vector3.up * distanceFromFinger;
        ps.transform.localRotation = Quaternion.Euler(rotationOffset);

        // NE PAS détruire automatiquement - la lumière reste active tant que le pinch est maintenu
        // Elle sera détruite manuellement quand le pinch sera relâché

        // Stocker la référence à la lumière actuelle
        if (currentLight != null)
        {
            Destroy(currentLight);
        }
        currentLight = ps.gameObject;

        if (showDebugInfo)
        {
            Debug.Log($"Light attached to index tip!");
        }
    }

    Transform GetIndexTipTransform()
    {
        // Obtenir le transform du bout de l'index
        var skeleton = hand.GetComponent<OVRSkeleton>();
        if (skeleton != null && skeleton.Bones != null)
        {
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    return bone.Transform;
                }
            }
        }

        return null;
    }

    private void OnVoiceResponse(WitResponseNode response)
    {
        string text = response["text"];
        Debug.Log($"Heard: {text}");

        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower();

        // Vérifier si le mot "lumière" (ou variantes) est prononcé
        if (text.Contains("lumière") || text.Contains("lumiere") || text.Contains("light"))
        {
            isVoiceCommandReceived = true;
            if (showDebugInfo)
            {
                Debug.Log("Voice command 'lumière' received!");
            }
        }
    }

    private void OnVoiceError(string error, string message)
    {
        Debug.LogError($"Voice Error: {error} - {message}");
        isListening = false;
    }

    private void OnPartialTranscription(string text)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Partial transcription: {text}");
        }
    }

    private void OnFullTranscription(string text)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Full transcription: {text}");
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
    }
}
