using UnityEngine;

/// <summary>
/// Composant attaché à chaque objet placé dans la scène par l'utilisateur
/// Gère les données et les interactions avec l'objet placé
/// </summary>
public class PlacedObject : MonoBehaviour
{
    [Header("Object Data")]
    public PlaceableObjectData objectData;
    
    [Header("Media")]
    public Texture2D associatedImage; // Image du projet associée
    public string associatedVideoPath; // Chemin vers la vidéo
    
    [Header("Modification")]
    public bool isSelected = false;
    public bool canBeModified = true;
    
    private Renderer[] renderers;
    private Color[] originalColors;
    private Material[] originalMaterials;
    
    [Header("Selection Visual")]
    public Color selectionColor = new Color(1f, 0.8f, 0f, 1f); // Couleur jaune/orange
    public float selectionGlowIntensity = 1.5f;
    
    void Start()
    {
        // Sauvegarder les matériaux originaux
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null)
            {
                originalMaterials[i] = renderers[i].material;
                originalColors[i] = renderers[i].material.color;
            }
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (isSelected)
        {
            ApplySelectionVisual();
        }
        else
        {
            RemoveSelectionVisual();
        }
    }
    
    void ApplySelectionVisual()
    {
        // Appliquer un effet visuel pour indiquer que l'objet est sélectionné
        foreach (Renderer rend in renderers)
        {
            if (rend.material != null)
            {
                // Option 1: Changer légèrement la couleur
                rend.material.color = selectionColor;
                
                // Option 2: Ajouter de l'émission si le shader le supporte
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", selectionColor * selectionGlowIntensity);
                }
            }
        }
    }
    
    void RemoveSelectionVisual()
    {
        // Restaurer les matériaux originaux
        for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material.color = originalColors[i];
                
                if (renderers[i].material.HasProperty("_EmissionColor"))
                {
                    renderers[i].material.DisableKeyword("_EMISSION");
                    renderers[i].material.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
    
    public void AssociateImage(Texture2D image)
    {
        associatedImage = image;
        
        // Si c'est un cadre photo, appliquer l'image
        if (objectData != null && objectData.canHaveMedia)
        {
            ApplyImageToObject(image);
        }
    }
    
    void ApplyImageToObject(Texture2D image)
    {
        // Chercher un renderer avec un nom spécifique ou le premier renderer
        Renderer targetRenderer = GetComponentInChildren<Renderer>();
        
        if (targetRenderer != null && image != null)
        {
            targetRenderer.material.mainTexture = image;
            Debug.Log($"Image appliquée à {gameObject.name}");
        }
    }
    
    public void AssociateVideo(string videoPath)
    {
        associatedVideoPath = videoPath;
        Debug.Log($"Vidéo associée: {videoPath}");
        
        // TODO: Implémenter la lecture vidéo dans une prochaine étape
    }
    
    void OnDrawGizmosSelected()
    {
        // Afficher des gizmos pour le débogage
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
