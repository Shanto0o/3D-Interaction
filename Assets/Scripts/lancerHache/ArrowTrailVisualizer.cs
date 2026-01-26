using UnityEngine;

/// <summary>
/// Affiche une traînée visuelle derrière la flèche pour suivre sa trajectoire
/// </summary>
public class ArrowTrailVisualizer : MonoBehaviour
{
    private TrailRenderer trail;

    void Start()
    {
        // Ajouter un TrailRenderer
        trail = gameObject.AddComponent<TrailRenderer>();
        if (trail == null)
        {
            Debug.LogError("[ArrowTrailVisualizer] TrailRenderer non créé, la traînée ne sera pas visible.");
            return;
        }

        trail.time = 2f; // Durée de la traînée
        trail.startWidth = 0.02f;
        trail.endWidth = 0.005f;
        trail.startColor = Color.yellow;
        trail.endColor = new Color(1f, 0f, 0f, 0.5f);
        trail.numCornerVertices = 5;
        trail.numCapVertices = 5;

        // Créer un matériau simple et robuste
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
        if (shader == null)
        {
            Debug.LogWarning("[ArrowTrailVisualizer] Shader 'Legacy Shaders/Particles/Alpha Blended Premultiply' introuvable, utilisation du shader Standard.");
            shader = Shader.Find("Standard");
        }
        if (shader != null)
        {
            Material trailMat = new Material(shader);
            if (trailMat != null)
            {
                trail.material = trailMat;
            }
            else
            {
                Debug.LogWarning("[ArrowTrailVisualizer] Le matériel de traînée n'a pas pu être créé.");
            }
        }
        else
        {
            Debug.LogWarning("[ArrowTrailVisualizer] Aucun shader compatible trouvé, la traînée sera invisible.");
        }
        trail.enabled = true;
    }
}
