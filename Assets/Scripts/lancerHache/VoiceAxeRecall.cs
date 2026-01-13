using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Système de reconnaissance vocale pour rappeler la hache
/// Fonctionne avec Windows Speech Recognition (casque VR compatible)
/// Dites "hache" ou "reviens" pour rappeler la hache dans votre main
/// </summary>
public class VoiceAxeRecall : MonoBehaviour
{
    [Header("Voice Commands")]
    [Tooltip("Mots-clés pour rappeler la hache")]
    public string[] recallKeywords = new string[] { "hache", "reviens", "retour", "axe" };

    [Header("References")]
    [Tooltip("La hache à rappeler")]
    public XRThrowableWeapon axe;
    
    [Tooltip("Main droite OVR")]
    public OVRHand rightHand;
    
    [Tooltip("Transform de la main droite (controller ou hand)")]
    public Transform rightHandTransform;

    [Header("Recall Settings")]
    [Tooltip("Vitesse de rappel de la hache")]
    public float recallSpeed = 15f;
    
    [Tooltip("Distance minimale pour attraper automatiquement")]
    public float autoCatchDistance = 0.3f;
    
    [Tooltip("Force d'attraction magnétique")]
    public float magneticForce = 50f;

    [Header("Visual Feedback")]
    [Tooltip("Ligne visuelle montrant le chemin de la hache")]
    public LineRenderer recallLine;
    
    [Tooltip("Effet de particules lors du rappel")]
    public ParticleSystem recallEffect;
    
    [Tooltip("Couleur de la ligne de rappel")]
    public Color lineColor = new Color(0f, 1f, 1f, 0.5f);

    [Header("Audio Feedback")]
    [Tooltip("Son lors du rappel vocal")]
    public AudioClip recallSound;
    
    [Tooltip("Son quand la hache arrive")]
    public AudioClip catchSound;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private KeywordRecognizer keywordRecognizer;
    private bool isRecalling = false;
    private Rigidbody axeRigidbody;
    private AudioSource audioSource;
    private bool wasGravityEnabled;

    void Start()
    {
        // Vérifier les références
        if (axe == null)
        {
            Debug.LogError("❌ [VoiceAxeRecall] Aucune hache assignée!");
            enabled = false;
            return;
        }

        axeRigidbody = axe.GetComponent<Rigidbody>();

        // Trouver la main droite si non assignée
        if (rightHandTransform == null && rightHand != null)
        {
            rightHandTransform = rightHand.transform;
        }

        if (rightHandTransform == null)
        {
            Debug.LogError("❌ [VoiceAxeRecall] Aucune main droite assignée!");
            enabled = false;
            return;
        }

        // Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Son 2D pour les commandes
        }

        // Créer la ligne de rappel si non assignée
        if (recallLine == null)
        {
            GameObject lineObj = new GameObject("RecallLine");
            lineObj.transform.SetParent(transform);
            recallLine = lineObj.AddComponent<LineRenderer>();
            recallLine.startWidth = 0.02f;
            recallLine.endWidth = 0.02f;
            recallLine.material = new Material(Shader.Find("Sprites/Default"));
            recallLine.startColor = lineColor;
            recallLine.endColor = lineColor;
            recallLine.positionCount = 2;
            recallLine.enabled = false;
        }

        // Initialiser la reconnaissance vocale
        InitializeVoiceRecognition();
    }

    void InitializeVoiceRecognition()
    {
        if (recallKeywords.Length == 0)
        {
            Debug.LogError("❌ [VoiceAxeRecall] Aucun mot-clé défini!");
            return;
        }

        try
        {
            // Créer le recognizer avec les mots-clés
            keywordRecognizer = new KeywordRecognizer(recallKeywords);
            keywordRecognizer.OnPhraseRecognized += OnKeywordRecognized;
            keywordRecognizer.Start();

            if (showDebugInfo)
            {
                Debug.Log($"✅ [VoiceAxeRecall] Reconnaissance vocale activée. Dites: {string.Join(", ", recallKeywords)}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [VoiceAxeRecall] Erreur d'initialisation: {e.Message}");
            Debug.LogWarning("💡 Assurez-vous que la reconnaissance vocale Windows est activée et que le micro fonctionne.");
        }
    }

    void OnKeywordRecognized(PhraseRecognizedEventArgs args)
    {
        if (showDebugInfo)
        {
            Debug.Log($"🎤 [VoiceAxeRecall] Commande détectée: '{args.text}' (Confiance: {args.confidence})");
        }

        // Déclencher le rappel
        StartRecall();
    }

    /// <summary>
    /// Démarre le rappel de la hache
    /// </summary>
    public void StartRecall()
    {
        if (axe == null || axeRigidbody == null || rightHandTransform == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("⚠️ [VoiceAxeRecall] Impossible de rappeler - références manquantes");
            return;
        }

        // Ne pas rappeler si déjà tenue
        if (Vector3.Distance(axe.transform.position, rightHandTransform.position) < autoCatchDistance)
        {
            if (showDebugInfo)
                Debug.Log("ℹ️ [VoiceAxeRecall] La hache est déjà proche");
            return;
        }

        isRecalling = true;
        wasGravityEnabled = axeRigidbody.useGravity;

        // Désactiver la gravité temporairement
        axeRigidbody.useGravity = false;
        axeRigidbody.linearVelocity = Vector3.zero;
        axeRigidbody.angularVelocity = Vector3.zero;

        // Activer les visuels
        if (recallLine != null)
        {
            recallLine.enabled = true;
        }

        if (recallEffect != null)
        {
            recallEffect.transform.position = axe.transform.position;
            recallEffect.Play();
        }

        // Son de rappel
        if (audioSource != null && recallSound != null)
        {
            audioSource.PlayOneShot(recallSound);
        }

        if (showDebugInfo)
        {
            Debug.Log("⚔️ [VoiceAxeRecall] Rappel de la hache activé!");
        }
    }

    void Update()
    {
        if (!isRecalling) return;

        if (axe == null || axeRigidbody == null || rightHandTransform == null)
        {
            StopRecall();
            return;
        }

        // Calculer la direction vers la main
        Vector3 direction = (rightHandTransform.position - axe.transform.position).normalized;
        float distance = Vector3.Distance(axe.transform.position, rightHandTransform.position);

        // Appliquer la force magnétique (plus fort quand proche)
        float force = Mathf.Lerp(magneticForce, magneticForce * 2f, 1f - Mathf.Clamp01(distance / 5f));
        axeRigidbody.AddForce(direction * force, ForceMode.Force);

        // Orienter la hache vers la main
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        axe.transform.rotation = Quaternion.Slerp(axe.transform.rotation, targetRotation, Time.deltaTime * 5f);

        // Mettre à jour la ligne visuelle
        if (recallLine != null && recallLine.enabled)
        {
            recallLine.SetPosition(0, rightHandTransform.position);
            recallLine.SetPosition(1, axe.transform.position);
        }

        // Vérifier si la hache est assez proche pour être attrapée
        if (distance < autoCatchDistance)
        {
            CatchAxe();
        }
    }

    /// <summary>
    /// Attrape automatiquement la hache
    /// </summary>
    void CatchAxe()
    {
        if (showDebugInfo)
        {
            Debug.Log("✋ [VoiceAxeRecall] Hache attrapée automatiquement!");
        }

        // Position finale dans la main
        axe.transform.position = rightHandTransform.position;
        axe.transform.rotation = rightHandTransform.rotation;

        // Arrêter le mouvement
        axeRigidbody.linearVelocity = Vector3.zero;
        axeRigidbody.angularVelocity = Vector3.zero;

        // Son d'attraction
        if (audioSource != null && catchSound != null)
        {
            audioSource.PlayOneShot(catchSound);
        }

        StopRecall();

        // Note: L'utilisateur devra ensuite faire un vrai grab avec XRGrabInteractable
        // pour vraiment tenir la hache
    }

    /// <summary>
    /// Arrête le rappel
    /// </summary>
    void StopRecall()
    {
        isRecalling = false;

        if (axeRigidbody != null)
        {
            axeRigidbody.useGravity = wasGravityEnabled;
        }

        if (recallLine != null)
        {
            recallLine.enabled = false;
        }

        if (recallEffect != null && recallEffect.isPlaying)
        {
            recallEffect.Stop();
        }
    }

    /// <summary>
    /// Méthode publique pour rappeler manuellement (utilisable avec des boutons UI)
    /// </summary>
    public void RecallAxe()
    {
        StartRecall();
    }

    void OnDestroy()
    {
        // Nettoyer le recognizer
        if (keywordRecognizer != null)
        {
            keywordRecognizer.OnPhraseRecognized -= OnKeywordRecognized;
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
    }

    void OnDrawGizmos()
    {
        // Visualiser la zone d'attraction automatique
        if (rightHandTransform != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(rightHandTransform.position, autoCatchDistance);
        }

        // Ligne vers la hache
        if (Application.isPlaying && isRecalling && axe != null && rightHandTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHandTransform.position, axe.transform.position);
        }
    }
}
