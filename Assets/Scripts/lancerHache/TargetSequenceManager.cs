using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gère la séquence des cibles à toucher dans un ordre précis (2-1-4-3)
/// Affiche une animation "Bravo" si l'ordre est respecté
/// </summary>
public class TargetSequenceManager : MonoBehaviour
{
    [Header("Sequence Settings")]
    [Tooltip("L'ordre exact dans lequel les cibles doivent être touchées")]
    public int[] targetSequence = new int[] { 2, 1, 4, 3 };

    [Header("Targets")]
    [Tooltip("Les 4 cibles du jeu - Assurez-vous qu'elles ont des targetId de 1 à 4")]
    public List<Target> targets = new List<Target>();

    [Header("Success Animation")]
    [Tooltip("GameObject contenant le texte 'Bravo' (TextMeshPro ou Text)")]
    public GameObject bravoObject;
    
    [Tooltip("Durée d'affichage du message Bravo (en secondes)")]
    public float bravoDisplayDuration = 3f;
    
    [Tooltip("Animation de scale pour le texte Bravo")]
    public bool animateBravo = true;
    
    [Tooltip("Scale maximale de l'animation")]
    public float maxScale = 1.5f;

    [Header("Audio")]
    [Tooltip("Son quand on touche la bonne cible dans l'ordre")]
    public AudioClip correctHitSound;
    
    [Tooltip("Son quand on touche la mauvaise cible")]
    public AudioClip wrongHitSound;
    
    [Tooltip("Son de victoire (Bravo)")]
    public AudioClip bravoSound;

    [Header("Visual Feedback")]
    [Tooltip("Couleur des cibles réussies")]
    public Color successColor = Color.green;
    
    [Tooltip("Intensité d'émission pour les cibles réussies")]
    public float emissionIntensity = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private int currentSequenceIndex = 0;
    private AudioSource audioSource;
    private Vector3 bravoInitialScale;
    private bool isShowingBravo = false;

    public static TargetSequenceManager Instance { get; private set; }

    void Awake()
    {
        // Singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }

        // Sauvegarder l'échelle initiale du texte Bravo
        if (bravoObject != null)
        {
            bravoInitialScale = bravoObject.transform.localScale;
            bravoObject.SetActive(false);
        }
    }

    void Start()
    {
        // Vérifier qu'on a bien 4 cibles
        if (targets.Count != 4)
        {
            Debug.LogError($"[TargetSequenceManager] Il faut exactement 4 cibles ! Actuellement : {targets.Count}");
            return;
        }

        // Enregistrer ce manager sur chaque cible
        foreach (var target in targets)
        {
            if (target != null)
                target.SetSequenceManager(this);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[TargetSequenceManager] Ordre à respecter : {string.Join(" → ", targetSequence)}");
            Debug.Log($"[TargetSequenceManager] Prochaine cible : {targetSequence[currentSequenceIndex]}");
        }
    }

    /// <summary>
    /// Appelé quand une cible est touchée
    /// </summary>
    public void OnTargetHit(Target target)
    {
        int expectedTargetId = targetSequence[currentSequenceIndex];
        
        if (target.targetId == expectedTargetId)
        {
            // Bonne cible !
            OnCorrectTargetHit(target);
        }
        else
        {
            // Mauvaise cible !
            OnWrongTargetHit(target);
        }
    }

    /// <summary>
    /// Appelé quand la bonne cible est touchée
    /// </summary>
    void OnCorrectTargetHit(Target target)
    {
        // Son de succès
        if (audioSource != null && correctHitSound != null)
            audioSource.PlayOneShot(correctHitSound);

        // Marquer la cible en vert pour indiquer le succès
        target.UpdateVisual(successColor, emissionIntensity);

        if (showDebugInfo)
            Debug.Log($"[TargetSequenceManager] ✓ Bonne cible {target.targetId} ! Progression : {currentSequenceIndex + 1}/{targetSequence.Length}");

        // Avancer dans la séquence
        currentSequenceIndex++;

        // Vérifier si la séquence est complète
        if (currentSequenceIndex >= targetSequence.Length)
        {
            OnSequenceComplete();
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"[TargetSequenceManager] Prochaine cible : {targetSequence[currentSequenceIndex]}");
        }
    }

    /// <summary>
    /// Appelé quand la mauvaise cible est touchée
    /// </summary>
    void OnWrongTargetHit(Target target)
    {
        // Son d'erreur
        if (audioSource != null && wrongHitSound != null)
            audioSource.PlayOneShot(wrongHitSound);

        int expectedTargetId = targetSequence[currentSequenceIndex];
        
        if (showDebugInfo)
            Debug.Log($"[TargetSequenceManager] ✗ Mauvaise cible {target.targetId} ! Attendu : {expectedTargetId}. Séquence réinitialisée.");

        // Réinitialiser la séquence
        ResetSequence();
    }

    /// <summary>
    /// Appelé quand toute la séquence est complétée
    /// </summary>
    void OnSequenceComplete()
    {
        if (showDebugInfo)
            Debug.Log($"[TargetSequenceManager] 🎉 BRAVO ! Séquence complète : {string.Join(" → ", targetSequence)}");

        // Son de victoire
        if (audioSource != null && bravoSound != null)
            audioSource.PlayOneShot(bravoSound);

        // Afficher l'animation Bravo
        if (bravoObject != null && !isShowingBravo)
        {
            StartCoroutine(ShowBravoAnimation());
        }

        // Réinitialiser pour recommencer
        Invoke(nameof(ResetSequence), bravoDisplayDuration);
    }

    /// <summary>
    /// Animation du texte "Bravo"
    /// </summary>
    System.Collections.IEnumerator ShowBravoAnimation()
    {
        isShowingBravo = true;
        bravoObject.SetActive(true);

        if (animateBravo)
        {
            float elapsed = 0f;
            float animDuration = bravoDisplayDuration * 0.3f; // 30% du temps pour l'animation

            // Animation d'apparition (scale up)
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animDuration;
                float scale = Mathf.Lerp(0f, maxScale, t);
                bravoObject.transform.localScale = bravoInitialScale * scale;
                yield return null;
            }

            // Maintenir à l'échelle maximale
            yield return new WaitForSeconds(bravoDisplayDuration * 0.4f); // 40% du temps

            // Animation de disparition (scale down)
            elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animDuration;
                float scale = Mathf.Lerp(maxScale, 0f, t);
                bravoObject.transform.localScale = bravoInitialScale * scale;
                yield return null;
            }
        }
        else
        {
            // Juste afficher et masquer
            yield return new WaitForSeconds(bravoDisplayDuration);
        }

        bravoObject.SetActive(false);
        bravoObject.transform.localScale = bravoInitialScale;
        isShowingBravo = false;
    }

    /// <summary>
    /// Réinitialise la séquence
    /// </summary>
    public void ResetSequence()
    {
        currentSequenceIndex = 0;
        
        // Réinitialiser toutes les cibles à leur état initial
        foreach (var target in targets)
        {
            if (target != null)
                target.ResetTarget();
        }

        if (showDebugInfo)
            Debug.Log($"[TargetSequenceManager] Séquence réinitialisée. Prochaine cible : {targetSequence[currentSequenceIndex]}");
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // Afficher uniquement la progression
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 25;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string progressText = $"Cibles touchées : {currentSequenceIndex}/{targetSequence.Length}";
        GUI.Label(new Rect(20, 20, 400, 50), progressText, style);
    }
}
