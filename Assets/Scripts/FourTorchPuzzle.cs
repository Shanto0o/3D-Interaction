using UnityEngine;

/// <summary>
/// Surveille 4 torches FireBallTrigger et active une animation quand elles sont toutes allumées
/// </summary>
public class FourTorchPuzzle : MonoBehaviour
{
    [Header("Torches")]
    [Tooltip("Les 4 torches à surveiller")]
    public FireBallTrigger[] torches = new FireBallTrigger[4];

    [Header("Animation")]
    [Tooltip("L'Animator Controller à activer")]
    public Animator targetAnimator;
    
    [Tooltip("Le nom du trigger d'animation à activer (ex: 'open')")]
    public string animationTriggerName = "Open";

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool puzzleSolved = false;

    private void Start()
    {
        // Vérifier que toutes les torches sont assignées
        if (torches.Length != 4)
        {
            Debug.LogError($"[FourTorchPuzzle] Vous devez assigner exactement 4 torches! (Actuellement: {torches.Length})");
            return;
        }

        for (int i = 0; i < torches.Length; i++)
        {
            if (torches[i] == null)
            {
                Debug.LogError($"[FourTorchPuzzle] La torche à l'index {i} n'est pas assignée!");
            }
        }

        if (targetAnimator == null)
        {
            Debug.LogWarning($"[FourTorchPuzzle] Animator non assigné!");
        }
    }

    private void Update()
    {
        // Si le puzzle est déjà résolu, ne rien faire
        if (puzzleSolved)
            return;

        // Vérifier si toutes les torches sont allumées
        if (AreAllTorchesLit())
        {
            TriggerAnimation();
        }
    }

    private bool AreAllTorchesLit()
    {
        // Vérifier que toutes les torches sont assignées et allumées
        for (int i = 0; i < torches.Length; i++)
        {
            if (torches[i] == null || !torches[i].IsLit)
            {
                return false;
            }
        }

        return true;
    }

    private void TriggerAnimation()
    {
        puzzleSolved = true;

        if (showDebugInfo)
        {
            Debug.Log($"[FourTorchPuzzle] Toutes les torches sont allumées! Activation de l'animation '{animationTriggerName}'");
        }

        if (targetAnimator != null)
        {
            // Activer le trigger d'animation
            targetAnimator.SetTrigger(animationTriggerName);

            if (showDebugInfo)
            {
                Debug.Log($"[FourTorchPuzzle] Animation déclenchée!");
            }
        }
        else
        {
            Debug.LogWarning($"[FourTorchPuzzle] Impossible de déclencher l'animation - Animator non assigné!");
        }
    }

    // Méthode publique pour réinitialiser le puzzle si besoin
    public void ResetPuzzle()
    {
        puzzleSolved = false;

        if (showDebugInfo)
        {
            Debug.Log($"[FourTorchPuzzle] Puzzle réinitialisé");
        }
    }
}
