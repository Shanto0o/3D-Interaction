using UnityEngine;
using Oculus.Voice; // Meta Voice SDK
using Meta.WitAi.Json; // for WitResponseNode
using Oculus.Interaction.Input;

public class LumiereSpell : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public ParticleSystem lightParticlePrefab; // Système de particules pour la lumière
    public AppVoiceExperience voice;

    [Header("Gesture Settings")]
    [Range(0.01f, 0.5f)]
    public float fingerClosedThreshold = 0.2f; // Seuil pour détecter doigts fermés
    [Range(0.01f, 0.5f)]
    public float indexOpenThreshold = 0.08f; // Seuil pour détecter index ouvert
    [Range(0f, 1f)]
    public float upAlignmentThreshold = 0.3f; // Seuil pour détecter index vers le haut

    [Header("Light Settings")]
    public float lightDuration = 10f; // Durée de vie de la lumière

    [Header("Positioning")]
    public float distanceFromFinger = 0.05f; // Distance depuis le bout du doigt

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isIndexPointingUp = false;
    private bool wasIndexPointingUp = false;
    private bool isVoiceCommandReceived = false;
    private GameObject currentLight;

    private bool isVoiceActive = false;
    private float voiceCooldown = 0f;
    private const float VOICE_COOLDOWN_TIME = 1f;

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
                if (showDebugInfo)
                {
                    Debug.Log("Voice activated - listening...");
                }
            }
        }

        // Vérifier le geste : index levé, autres doigts fermés
        UpdateGesture();
    }

    void UpdateGesture()
    {
        bool indexPointing = CheckIndexPointingUp();

        // Détection du début du geste
        if (indexPointing && !wasIndexPointingUp)
        {
            isIndexPointingUp = true;
            if (showDebugInfo)
            {
                Debug.Log("Index pointing up detected!");
            }
        }

        // Si le geste est actif et qu'on a reçu la commande vocale "lumière"
        if (isIndexPointingUp && isVoiceCommandReceived)
        {
            CreateLight();
            isVoiceCommandReceived = false; // Reset pour la prochaine fois
        }

        // Fin du geste
        if (!indexPointing && wasIndexPointingUp)
        {
            isIndexPointingUp = false;
            if (showDebugInfo)
            {
                Debug.Log("Index pointing gesture ended");
            }
        }

        wasIndexPointingUp = indexPointing;
    }

    bool CheckIndexPointingUp()
    {
        // L'index doit être ouvert (pas pincé)
        bool indexOpen = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < indexOpenThreshold;

        // Les autres doigts doivent être fermés (le pinky est optionnel car difficile à tracker)
        bool middleClosed = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > fingerClosedThreshold;
        bool ringClosed = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > fingerClosedThreshold;
        // Le pinky est optionnel - on vérifie s'il est au moins partiellement fermé
        float pinkyStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);
        bool pinkyOk = pinkyStrength > 0.05f; // Très permissif pour le pinky


        bool gestureDetected = indexOpen && middleClosed && ringClosed && pinkyOk;

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            float pinkyStrength2 = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);
            Debug.Log($"Index Open: {indexOpen} | Middle: {middleClosed} | Ring: {ringClosed} | Pinky: {pinkyOk} (strength: {pinkyStrength2:F2}) | Gesture: {gestureDetected}");
        }

        return gestureDetected;
    }

    void CreateLight()
    {
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
        ps.transform.localRotation = Quaternion.identity;

        // Détruire après la durée spécifiée
        Destroy(ps.gameObject, lightDuration);

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

        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;

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
        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;
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
