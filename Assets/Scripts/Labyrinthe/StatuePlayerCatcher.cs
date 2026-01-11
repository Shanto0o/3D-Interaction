using UnityEngine;
using Pathfinding;

/// <summary>
/// Téléporte le joueur à l'entrée du labyrinthe si la statue le touche pendant qu'elle est en mouvement.
/// Attachez ce script à la statue ou à un collider enfant de la statue.
/// </summary>
public class StatuePlayerCatcher : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform where the player will be teleported (entrance of the maze)")]
    public Transform teleportDestination;
    
    [Tooltip("The player's VR rig or character controller to teleport")]
    public Transform playerRig;
    
    [Tooltip("Reference to the statue's AIPath component")]
    public AIPath statueAIPath;
    
    [Tooltip("AudioSource to play when catching the player")]
    public AudioSource statueAudioSource;
    
    [Header("Detection Settings")]
    [Tooltip("Tag to identify the player (default: 'Player')")]
    public string playerTag = "Player";
    
    [Tooltip("Minimum distance to trigger teleport if not using collision")]
    public float detectionDistance = 1.5f;
    
    [Tooltip("Use distance check instead of collision")]
    public bool useDistanceCheck = false;
    
    [Header("Teleport Settings")]
    [Tooltip("Delay before teleporting (optional, 0 for instant)")]
    public float teleportDelay = 0f;
    
    [Tooltip("Add a vertical offset when teleporting (y-axis)")]
    public float verticalOffset = 0f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebugMessages = true;
    
    private bool isTeleporting = false;
    private float teleportTimer = 0f;
    
    void Start()
    {
        // Auto-find AIPath component if not assigned
        if (statueAIPath == null)
        {
            statueAIPath = GetComponentInParent<AIPath>();
            if (statueAIPath == null)
            {
                Debug.LogError("StatuePlayerCatcher: No AIPath component found! Assign it manually or attach this script to the statue.");
            }
        }
        
        // Try to find player if not assigned
        if (playerRig == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerRig = player.transform;
            }
            else
            {
                Debug.LogWarning($"StatuePlayerCatcher: No GameObject with tag '{playerTag}' found. Player must be assigned manually.");
            }
        }
        
        // Validate teleport destination
        if (teleportDestination == null)
        {
            Debug.LogError("StatuePlayerCatcher: Teleport destination not assigned!");
        }
    }
    
    void Update()
    {
        // Distance check mode
        if (useDistanceCheck && playerRig != null && statueAIPath != null && teleportDestination != null)
        {
            float distance = Vector3.Distance(transform.position, playerRig.position);
            
            // Check if statue is close to player AND statue is moving
            if (distance <= detectionDistance && statueAIPath.canMove && !isTeleporting)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"StatuePlayerCatcher: Player caught by statue at distance {distance:F2}m!");
                }
                TriggerTeleport();
            }
        }
        
        // Handle teleport delay
        if (isTeleporting)
        {
            teleportTimer += Time.deltaTime;
            if (teleportTimer >= teleportDelay)
            {
                ExecuteTeleport();
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Only works if not using distance check mode
        if (useDistanceCheck) return;
        
        // Check if the object that entered is the player
        if (other.CompareTag(playerTag) || (playerRig != null && other.transform == playerRig))
        {
            // Check if statue is currently moving (not frozen by gaze)
            if (statueAIPath != null && statueAIPath.canMove && !isTeleporting)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"StatuePlayerCatcher: Player caught by moving statue! Teleporting to entrance...");
                }
                TriggerTeleport();
            }
            else if (showDebugMessages && statueAIPath != null && !statueAIPath.canMove)
            {
                Debug.Log("StatuePlayerCatcher: Statue is frozen, not teleporting player.");
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Backup for rigidbody collisions
        if (useDistanceCheck) return;
        
        if (collision.gameObject.CompareTag(playerTag) || (playerRig != null && collision.transform == playerRig))
        {
            if (statueAIPath != null && statueAIPath.canMove && !isTeleporting)
            {
                if (showDebugMessages)
                {
                    Debug.Log($"StatuePlayerCatcher: Player caught by moving statue (collision)! Teleporting to entrance...");
                }
                TriggerTeleport();
            }
        }
    }
    
    /// <summary>
    /// Initializes the teleport process
    /// </summary>
    private void TriggerTeleport()
    {
        if (teleportDestination == null)
        {
            Debug.LogError("StatuePlayerCatcher: Cannot teleport - destination not set!");
            return;
        }
        
        if (playerRig == null)
        {
            Debug.LogError("StatuePlayerCatcher: Cannot teleport - player rig not found!");
            return;
        }
        
        isTeleporting = true;
        teleportTimer = 0f;
        
        // Play capture sound
        if (statueAudioSource != null && !statueAudioSource.isPlaying)
        {
            statueAudioSource.Play();
            if (showDebugMessages)
            {
                Debug.Log("StatuePlayerCatcher: Playing capture sound");
            }
        }
        
        if (teleportDelay <= 0f)
        {
            ExecuteTeleport();
        }
    }
    
    /// <summary>
    /// Executes the actual teleport
    /// </summary>
    private void ExecuteTeleport()
    {
        if (playerRig != null && teleportDestination != null)
        {
            Vector3 newPosition = teleportDestination.position;
            newPosition.y += verticalOffset;
            
            // Stop capture sound
            if (statueAudioSource != null && statueAudioSource.isPlaying)
            {
                statueAudioSource.Stop();
                if (showDebugMessages)
                {
                    Debug.Log("StatuePlayerCatcher: Stopped capture sound");
                }
            }
            
            // Teleport the player
            playerRig.position = newPosition;
            
            // Optional: match rotation
            // playerRig.rotation = teleportDestination.rotation;
            
            if (showDebugMessages)
            {
                Debug.Log($"StatuePlayerCatcher: Player teleported to {newPosition}");
            }
        }
        
        isTeleporting = false;
        teleportTimer = 0f;
    }
    
    // Visualize detection radius in editor
    void OnDrawGizmosSelected()
    {
        if (useDistanceCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionDistance);
        }
        
        if (teleportDestination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(teleportDestination.position, 0.5f);
            Gizmos.DrawLine(transform.position, teleportDestination.position);
        }
    }
}
