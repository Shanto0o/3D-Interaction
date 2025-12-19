using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Image_puzzle : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Sprite[] images; // Liste des sprites à faire défiler
    [SerializeField] private float scrollDuration = 5f;
    [SerializeField] private float scrollSpeed = 0.1f; // Temps entre chaque changement d'image
    [SerializeField] private bool autoStart = true; // Démarrage automatique
    
    private SpriteRenderer spriteRenderer;
    private Image imageComponent;
    private bool isScrolling = false;
    private int currentImageIndex = 0;
    
    void Start()
    {
        // Essayer de récupérer un SpriteRenderer ou un Image
        spriteRenderer = GetComponent<SpriteRenderer>();
        imageComponent = GetComponent<Image>();
        
        // Désactiver l'affichage au départ pour éviter le carré blanc
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        else if (imageComponent != null)
        {
            imageComponent.enabled = false;
        }
        else
        {
            Debug.LogError("Aucun SpriteRenderer ou Image trouvé sur cet objet !");
            return;
        }
        
        // Vérifier qu'on a bien des images dans la liste
        if (images == null || images.Length == 0)
        {
            Debug.LogError("La liste de sprites est vide ! Ajoute des sprites dans l'Inspector.");
            return;
        }
        
        // Démarrer le défilement automatiquement si activé
        if (autoStart)
        {
            StartScrolling();
        }
    }
    
    public void StartScrolling()
    {
        if (!isScrolling && images.Length > 0)
        {
            StartCoroutine(ScrollImages());
        }
    }
    
    private IEnumerator ScrollImages()
    {
        isScrolling = true;
        float elapsedTime = 0f;
        
        // Activer l'affichage et afficher la première image
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = images[0];
        }
        else if (imageComponent != null)
        {
            imageComponent.enabled = true;
            imageComponent.sprite = images[0];
        }
        
        currentImageIndex = 0;
        
        // Défilement pendant 5 secondes
        while (elapsedTime < scrollDuration)
        {
            // Changer l'image affichée
            currentImageIndex = (currentImageIndex + 1) % images.Length;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = images[currentImageIndex];
            }
            else if (imageComponent != null)
            {
                imageComponent.sprite = images[currentImageIndex];
            }
            
            // Attendre avant le prochain changement
            yield return new WaitForSeconds(scrollSpeed);
            elapsedTime += scrollSpeed;
        }
        
        // Se figer sur une image finale aléatoire
        currentImageIndex = Random.Range(0, images.Length);
        
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = images[currentImageIndex];
        }
        else if (imageComponent != null)
        {
            imageComponent.sprite = images[currentImageIndex];
        }
        
        isScrolling = false;
        Debug.Log($"Défilement terminé. Image finale : {currentImageIndex}");
    }
    
    // Méthode pour relancer le défilement manuellement
    public void Restart()
    {
        StopAllCoroutines();
        isScrolling = false;
        StartScrolling();
    }
}
