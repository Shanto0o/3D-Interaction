using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gère le jeu : sélectionne la bonne cible et compte les réussites
/// À attacher sur un GameObject vide "GameManager"
/// </summary>
public class TargetGame : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Les 4 cibles du jeu")]
    public List<Target> targets = new List<Target>();

    [Header("Game Settings")]
    [Tooltip("Nombre de cibles à toucher d'affilé pour gagner")]
    public int requiredHits = 4;

    [Header("Visual Feedback")]
    [Tooltip("Couleur de la cible à toucher")]
    public Color correctTargetColor = Color.green;
    
    [Tooltip("Couleur des mauvaises cibles")]
    public Color wrongTargetColor = Color.red;
    
    [Tooltip("Intensité d'émission (glow)")]
    public float emissionIntensity = 2f;

    [Header("Audio")]
    [Tooltip("Son quand on touche la bonne cible")]
    public AudioClip correctHitSound;
    
    [Tooltip("Son quand on touche la mauvaise cible")]
    public AudioClip wrongHitSound;
    
    [Tooltip("Son quand on gagne")]
    public AudioClip winSound;

    [Header("UI")]
    [Tooltip("Afficher le score à l'écran")]
    public bool showScore = true;
    
    [Header("Door Control")]
    [Tooltip("Porte à ouvrir quand le joueur gagne")]
    public DoorController doorToOpen;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Target currentCorrectTarget;
    private int consecutiveHits = 0;
    private AudioSource audioSource;

    public static TargetGame Instance { get; private set; }

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
            audioSource.spatialBlend = 0f; // 2D sound pour le feedback
        }
    }

    void Start()
    {
        // Vérifier qu'on a bien 4 cibles
        if (targets.Count != 4)
        {
            Debug.LogError($"[TargetGame] Il faut exactement 4 cibles ! Actuellement : {targets.Count}");
            return;
        }

        // Enregistrer ce manager sur chaque cible
        foreach (var target in targets)
        {
            if (target != null)
                target.SetGameManager(this);
        }

        // Démarrer le jeu
        SelectNewTarget();
    }

    /// <summary>
    /// Sélectionne aléatoirement une nouvelle cible
    /// </summary>
    void SelectNewTarget()
    {
        // Choisir une cible au hasard
        currentCorrectTarget = targets[Random.Range(0, targets.Count)];

        if (showDebugInfo)
            Debug.Log($"[TargetGame] Nouvelle cible : {currentCorrectTarget.name}");
    }

    /// <summary>
    /// Appelé quand une cible est touchée
    /// </summary>
    public void OnTargetHit(Target target)
    {
        if (target == currentCorrectTarget)
        {
            // Bonne cible !
            OnCorrectTargetHit();
        }
        else
        {
            // Mauvaise cible !
            OnWrongTargetHit();
        }
    }

    /// <summary>
    /// Appelé quand la bonne cible est touchée
    /// </summary>
    void OnCorrectTargetHit()
    {
        consecutiveHits++;

        // Son de succès
        if (audioSource != null && correctHitSound != null)
            audioSource.PlayOneShot(correctHitSound);

        if (showDebugInfo)
            Debug.Log($"[TargetGame] ✓ Bonne cible ! Score : {consecutiveHits}/{requiredHits}");

        // Vérifier la victoire
        if (consecutiveHits >= requiredHits)
        {
            OnGameWon();
        }
        else
        {
            // Sélectionner une nouvelle cible
            SelectNewTarget();
        }
    }

    /// <summary>
    /// Appelé quand la mauvaise cible est touchée
    /// </summary>
    void OnWrongTargetHit()
    {
        // Son d'erreur
        if (audioSource != null && wrongHitSound != null)
            audioSource.PlayOneShot(wrongHitSound);

        if (showDebugInfo)
            Debug.Log($"[TargetGame] ✗ Mauvaise cible ! Score réinitialisé.");

        // Réinitialiser le score
        consecutiveHits = 0;

        // Sélectionner une nouvelle cible
        SelectNewTarget();
    }

    /// <summary>
    /// Appelé quand le joueur gagne
    /// </summary>
    void OnGameWon()
    {
        if (showDebugInfo)
            Debug.Log($"[TargetGame] 🎉 VICTOIRE ! {requiredHits} cibles touchées !");

        // Son de victoire
        if (audioSource != null && winSound != null)
            audioSource.PlayOneShot(winSound);

        // Ouvrir la porte
        if (doorToOpen != null)
        {
            doorToOpen.OpenDoor();
            if (showDebugInfo)
                Debug.Log("[TargetGame] Porte ouverte !");
        }

        // Réinitialiser et recommencer
        consecutiveHits = 0;
        SelectNewTarget();

        // Ici vous pouvez ajouter d'autres actions (afficher un message, etc.)
    }

    /// <summary>
    /// Réinitialise le jeu
    /// </summary>
    public void ResetGame()
    {
        consecutiveHits = 0;
        SelectNewTarget();
    }

    void OnGUI()
    {
        if (!showScore) return;

        // Afficher le score en haut à gauche
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        string scoreText = $"Score : {consecutiveHits} / {requiredHits}";
        GUI.Label(new Rect(20, 20, 400, 50), scoreText, style);

        // Afficher quelle cible toucher
        if (currentCorrectTarget != null)
        {
            style.fontSize = 20;
            style.normal.textColor = correctTargetColor;
            GUI.Label(new Rect(20, 70, 400, 40), $"➜ Touchez : {currentCorrectTarget.name}", style);
        }
    }
}
