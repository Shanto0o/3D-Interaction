using UnityEngine;

public class PiegeController : MonoBehaviour
{
    [Header("PiegeController to control")]
    [Tooltip("The piege GameObject with the Animator component")]
    public GameObject doorObject;

    private Animator animator;
    private bool isOpen = false;

    void Start()
    {
        // Si doorObject est assigné, utiliser son animator, sinon utiliser celui de ce GameObject
        if (doorObject != null)
        {
            animator = doorObject.GetComponent<Animator>();
        }
        else
        {
            animator = GetComponent<Animator>();
        }
    }

    // M�thode pour ouvrir la porte
    public void OpenDoor()
    {
        if (!isOpen)
        {
            Debug.Log("Opening door!"); animator.SetTrigger("piege");
            isOpen = true;
        }
    }

    // M�thode pour fermer la porte (optionnel)
    public void CloseDoor()
    {
        if (isOpen)
        {
            animator.SetTrigger("Close");
            isOpen = false;
        }
    }

    // Ouvrir quand le joueur entre dans la zone
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name);
        OpenDoor();
    }
}
