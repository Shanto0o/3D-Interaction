using UnityEngine;

public class DoorControllerClose : MonoBehaviour
{
    [Header("Door to control")]
    [Tooltip("The door GameObject with the Animator component")]
    public GameObject doorObject;
    
    [Header("Audio")]
    [Tooltip("AudioSource to play when door closes")]
    public AudioSource closeDoorAudio;

    private Animator animator;
    private DoorState doorState;

    void Start()
    {
        // Si doorObject est assigné, utiliser son animator, sinon utiliser celui de ce GameObject
        if (doorObject != null)
        {
            animator = doorObject.GetComponent<Animator>();
            doorState = doorObject.GetComponent<DoorState>();
            
            // Si DoorState n'existe pas, l'ajouter
            if (doorState == null)
            {
                doorState = doorObject.AddComponent<DoorState>();
            }
        }
        else
        {
            animator = GetComponent<Animator>();
            doorState = GetComponent<DoorState>();
            
            if (doorState == null)
            {
                doorState = gameObject.AddComponent<DoorState>();
            }
        }

    }

    // Méthode pour ouvrir la porte
    public void OpenDoor()
    {
        if (doorState != null && !doorState.isOpen)
        {
            Debug.Log("Opening door!"); 
            animator.SetTrigger("Open");
            doorState.isOpen = true;
        }
    }

    // Méthode pour fermer la porte (optionnel)
    public void CloseDoor()
    {
        if (doorState != null && doorState.isOpen)
        {
            Debug.Log("Closing door!"); 
            animator.SetTrigger("Close");
            doorState.isOpen = false;
            
            // Jouer le son de fermeture
            if (closeDoorAudio != null && !closeDoorAudio.isPlaying)
            {
                closeDoorAudio.Play();
            }
        }
    }

    // Ouvrir quand le joueur entre dans la zone
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Trigger entered by: " + other.gameObject.name);
            CloseDoor();
            
        }
    }
}
