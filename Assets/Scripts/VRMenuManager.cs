using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Gère le menu VR pour sélectionner et placer des objets dans la scène
/// </summary>
public class VRMenuManager : MonoBehaviour
{
    [Header("Menu Settings")]
    public GameObject menuPanel; // Le panneau UI du menu
    public Transform menuButtonContainer; // Container pour les boutons
    public GameObject menuButtonPrefab; // Prefab du bouton de menu
    
    [Header("Available Objects")]
    public List<PlaceableObjectData> availableObjects = new List<PlaceableObjectData>();
    
    [Header("Placement")]
    public ObjectPlacementSystem placementSystem; // Référence au système de placement
    
    [Header("Toggle Settings")]
    public UnityEngine.InputSystem.InputActionReference toggleMenuAction; // Action pour ouvrir/fermer le menu
    
    private bool isMenuOpen = false;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    
    void Start()
    {
        // Créer les boutons pour chaque objet disponible
        CreateMenuButtons();
        
        // Fermer le menu au démarrage
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
    
    void OnEnable()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
        {
            toggleMenuAction.action.Enable();
            toggleMenuAction.action.performed += OnToggleMenu;
        }
    }
    
    void OnDisable()
    {
        if (toggleMenuAction != null && toggleMenuAction.action != null)
        {
            toggleMenuAction.action.performed -= OnToggleMenu;
            toggleMenuAction.action.Disable();
        }
    }
    
    private void OnToggleMenu(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleMenu();
    }
    
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(isMenuOpen);
        }
        
        Debug.Log($"Menu VR {(isMenuOpen ? "ouvert" : "fermé")}");
    }
    
    void CreateMenuButtons()
    {
        if (menuButtonContainer == null || menuButtonPrefab == null)
        {
            Debug.LogWarning("Menu Button Container ou Prefab non assigné!");
            return;
        }
        
        // Détruire les anciens boutons
        foreach (GameObject btn in spawnedButtons)
        {
            Destroy(btn);
        }
        spawnedButtons.Clear();
        
        // Créer un bouton pour chaque objet
        foreach (PlaceableObjectData objData in availableObjects)
        {
            if (objData == null) continue;
            
            GameObject buttonObj = Instantiate(menuButtonPrefab, menuButtonContainer);
            spawnedButtons.Add(buttonObj);
            
            // Configurer le bouton
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                // Capturer la variable locale pour la closure
                PlaceableObjectData capturedData = objData;
                button.onClick.AddListener(() => OnObjectSelected(capturedData));
            }
            
            // Configurer le texte et l'icône
            Text buttonText = buttonObj.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = objData.objectName;
            }
            
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && objData.icon != null)
            {
                buttonImage.sprite = objData.icon;
            }
        }
        
        Debug.Log($"Menu créé avec {spawnedButtons.Count} boutons");
    }
    
    void OnObjectSelected(PlaceableObjectData objectData)
    {
        Debug.Log($"Objet sélectionné: {objectData.objectName}");
        
        if (placementSystem != null)
        {
            placementSystem.StartPlacing(objectData);
            // Optionnel: fermer le menu après sélection
            // ToggleMenu();
        }
        else
        {
            Debug.LogError("Placement System non assigné!");
        }
    }
    
    public void OpenMenu()
    {
        isMenuOpen = true;
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }
    
    public void CloseMenu()
    {
        isMenuOpen = false;
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
}
