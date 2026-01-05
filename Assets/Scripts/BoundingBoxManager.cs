using UnityEngine;
using TMPro;

public class BoundingBoxManager : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    
    // BoundingBox : enfants automatiques
    private BoundingBox[] boundingBoxes;


    private int scenario = 0;

    void Start()
    {
        // Récupérer tous les BoundingBox enfants
        boundingBoxes = GetComponentsInChildren<BoundingBox>();
        
        // S'abonner aux événements de chaque BoundingBox
        foreach (BoundingBox box in boundingBoxes)
        {
            box.OnPlayerEnter += HandlePlayerEnter;
            box.OnPlayerExit += HandlePlayerExit;
        }
        
        // Afficher le texte au démarrage
        textDisplay.gameObject.SetActive(true);
        textDisplay.text = "Allez dans la cuisine.";
    }

    private void OnDestroy()
    {
        // Se désabonner des événements pour éviter les fuites mémoire
        if (boundingBoxes != null)
        {
            foreach (BoundingBox box in boundingBoxes)
            {
                if (box != null)
                {
                    box.OnPlayerEnter -= HandlePlayerEnter;
                    box.OnPlayerExit -= HandlePlayerExit;
                }
            }
        }
    }

    private void HandlePlayerEnter(BoundingBox box)
    {
        if (box.boxName == "kitchen" && scenario == 0)
        {
            textDisplay.text = "Allez au frigo.";
            textDisplay.gameObject.SetActive(true);
            scenario = 1;
        }
        if (box.boxName == "fridge" && scenario == 1)
        {
            textDisplay.text = "Touchez la table.";
            scenario = 2;
            textDisplay.gameObject.SetActive(true);
        }
    }

    private void HandlePlayerExit(BoundingBox box)
    {
        if (box.boxName == "kitchen" && scenario == 3)
        {   
            scenario = 4;
            textDisplay.text = "Bravo ! Vous avez terminé le scénario.";
            textDisplay.gameObject.SetActive(true);
        }
    }
}
