using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Système pour placer des objets dans la scène VR
/// L'utilisateur peut prévisualiser l'objet et le placer avec un bouton
/// </summary>
public class ObjectPlacementSystem : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform rightHandTransform; // Main/contrôleur droit
    public float placementDistance = 1.5f; // Distance de placement devant la main
    public LayerMask placementLayer; // Layer pour détecter où placer (sols, murs, etc.)
    
    [Header("Preview Settings")]
    public Material previewMaterial; // Matériau semi-transparent pour la prévisualisation
    public Color validPlacementColor = new Color(0, 1, 0, 0.5f);
    public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
    
    [Header("Input Actions")]
    public InputActionReference placeObjectAction; // Action pour placer l'objet
    public InputActionReference cancelPlacementAction; // Action pour annuler
    
    [Header("Placed Objects")]
    public Transform placedObjectsContainer; // Parent pour les objets placés
    
    // État privé
    private GameObject previewObject;
    private PlaceableObjectData currentObjectData;
    private bool isPlacing = false;
    private bool isValidPlacement = false;
    private Vector3 targetPlacementPosition;
    private Quaternion targetPlacementRotation;
    
    void OnEnable()
    {
        if (placeObjectAction != null && placeObjectAction.action != null)
        {
            placeObjectAction.action.Enable();
            placeObjectAction.action.performed += OnPlaceObject;
        }
        
        if (cancelPlacementAction != null && cancelPlacementAction.action != null)
        {
            cancelPlacementAction.action.Enable();
            cancelPlacementAction.action.performed += OnCancelPlacement;
        }
    }
    
    void OnDisable()
    {
        if (placeObjectAction != null && placeObjectAction.action != null)
        {
            placeObjectAction.action.performed -= OnPlaceObject;
            placeObjectAction.action.Disable();
        }
        
        if (cancelPlacementAction != null && cancelPlacementAction.action != null)
        {
            cancelPlacementAction.action.performed -= OnCancelPlacement;
            cancelPlacementAction.action.Disable();
        }
    }
    
    void Update()
    {
        if (isPlacing && previewObject != null)
        {
            UpdatePreviewPosition();
        }
    }
    
    public void StartPlacing(PlaceableObjectData objectData)
    {
        if (objectData == null || objectData.prefab == null)
        {
            Debug.LogWarning("Objet invalide à placer!");
            return;
        }
        
        // Annuler le placement précédent si en cours
        if (isPlacing)
        {
            CancelPlacement();
        }
        
        currentObjectData = objectData;
        isPlacing = true;
        
        // Créer l'objet de prévisualisation
        previewObject = Instantiate(objectData.prefab);
        previewObject.name = $"Preview_{objectData.objectName}";
        
        // Désactiver les colliders sur la preview
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Appliquer le matériau de prévisualisation
        ApplyPreviewMaterial(previewObject);
        
        Debug.Log($"Placement commencé pour: {objectData.objectName}");
    }
    
    void UpdatePreviewPosition()
    {
        if (rightHandTransform == null) return;
        
        // Calculer la position cible devant la main
        Vector3 handForward = rightHandTransform.forward;
        Vector3 handPosition = rightHandTransform.position;
        
        // Raycast pour trouver une surface
        Ray ray = new Ray(handPosition, handForward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, placementDistance * 2f, placementLayer))
        {
            // Surface trouvée
            targetPlacementPosition = hit.point;
            targetPlacementRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, 0, 0);
            isValidPlacement = true;
        }
        else
        {
            // Pas de surface, placer à distance fixe
            targetPlacementPosition = handPosition + handForward * placementDistance;
            targetPlacementRotation = Quaternion.LookRotation(handForward);
            isValidPlacement = true; // Peut placer dans l'air
        }
        
        // Mettre à jour la position de la preview
        previewObject.transform.position = targetPlacementPosition;
        previewObject.transform.rotation = targetPlacementRotation;
        
        // Mettre à jour la couleur selon la validité
        UpdatePreviewColor(isValidPlacement);
    }
    
    void ApplyPreviewMaterial(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (previewMaterial != null)
                {
                    mats[i] = previewMaterial;
                }
                else
                {
                    // Créer un matériau transparent par défaut
                    mats[i] = new Material(rend.materials[i]);
                    mats[i].SetFloat("_Mode", 3); // Transparent mode
                    mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mats[i].SetInt("_ZWrite", 0);
                    mats[i].DisableKeyword("_ALPHATEST_ON");
                    mats[i].EnableKeyword("_ALPHABLEND_ON");
                    mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mats[i].renderQueue = 3000;
                    
                    Color col = mats[i].color;
                    col.a = 0.5f;
                    mats[i].color = col;
                }
            }
            rend.materials = mats;
        }
    }
    
    void UpdatePreviewColor(bool isValid)
    {
        Color targetColor = isValid ? validPlacementColor : invalidPlacementColor;
        
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.color = targetColor;
            }
        }
    }
    
    void OnPlaceObject(InputAction.CallbackContext context)
    {
        if (!isPlacing || !isValidPlacement) return;
        
        PlaceObject();
    }
    
    void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (isPlacing)
        {
            CancelPlacement();
        }
    }
    
    void PlaceObject()
    {
        if (currentObjectData == null || currentObjectData.prefab == null)
        {
            Debug.LogWarning("Pas d'objet à placer!");
            return;
        }
        
        // Créer l'objet réel
        GameObject placedObject = Instantiate(
            currentObjectData.prefab,
            targetPlacementPosition,
            targetPlacementRotation
        );
        
        placedObject.name = currentObjectData.objectName;
        
        // Appliquer l'échelle par défaut
        placedObject.transform.localScale = currentObjectData.defaultScale;
        
        // Mettre dans le container
        if (placedObjectsContainer != null)
        {
            placedObject.transform.SetParent(placedObjectsContainer);
        }
        
        // Ajouter le composant PlacedObject pour la gestion ultérieure
        PlacedObject placedComponent = placedObject.AddComponent<PlacedObject>();
        placedComponent.objectData = currentObjectData;
        
        Debug.Log($"Objet placé: {currentObjectData.objectName} à {targetPlacementPosition}");
        
        // Nettoyer
        CancelPlacement();
    }
    
    void CancelPlacement()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        
        previewObject = null;
        currentObjectData = null;
        isPlacing = false;
        isValidPlacement = false;
        
        Debug.Log("Placement annulé");
    }
}
