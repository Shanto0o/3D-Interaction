using UnityEngine;
using Pathfinding;
using System.Collections;

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
    
    [Tooltip("Door controller to reset when player is caught")]
    public DoorControllerClose doorController;
    
    [Tooltip("Lumiere spell script to disable when player is caught")]
    public LumiereSpell lumiereSpell;
    
    [Tooltip("Scene light to enable when player is caught")]
    public GameObject sceneLight;
    
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
    
    [Header("Fade Effect")]
    [Tooltip("Enable fog fade to black effect")]
    public bool enableFogFade = true;
    
    [Tooltip("Speed of fade to black (higher = faster)")]
    public float fadeToBlackSpeed = 5f;
    
    [Tooltip("Maximum fog density for black screen")]
    public float maxFogDensity = 0.5f;
    
    [Tooltip("Color of the fog fade (usually black)")]
    public Color fadeColor = Color.black;
    
    [Header("Debug")]
    [Tooltip("Show debug messages")]
    public bool showDebugMessages = true;
    
    private bool isTeleporting = false;
    private float teleportTimer = 0f;
    private Vector3 statueInitialPosition;
    private Quaternion statueInitialRotation;
    private Coroutine fadeCoroutine;
    private float originalFogDensity;
    private Color originalFogColor;
    private bool originalFogState;
    
    void Start()
    {
        // Save statue's initial position and rotation
        statueInitialPosition = transform.position;
        statueInitialRotation = transform.rotation;
        
        // Save original fog settings
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColor = RenderSettings.fogColor;
        originalFogState = RenderSettings.fog;
        
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
        
        // Activer la lumière de scène immédiatement lors de la capture
        if (sceneLight != null)
        {
            sceneLight.SetActive(true);
            if (showDebugMessages)
            {
                Debug.Log("StatuePlayerCatcher: Scene light enabled during capture");
            }
        }
        
        // Forcer la désactivation de la lumière du LumiereSpell
        if (lumiereSpell != null)
        {
            // Détruire la lumière active si elle existe
            var currentLightField = lumiereSpell.GetType().GetField("currentLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (currentLightField != null)
            {
                GameObject currentLight = currentLightField.GetValue(lumiereSpell) as GameObject;
                if (currentLight != null)
                {
                    Destroy(currentLight);
                    currentLightField.SetValue(lumiereSpell, null); // Reset à null
                    if (showDebugMessages)
                    {
                        Debug.Log("StatuePlayerCatcher: Destroyed active light from LumiereSpell");
                    }
                }
            }
            
            // Réinitialiser les états internes du LumiereSpell
            var isLightActiveField = lumiereSpell.GetType().GetField("isLightActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isLightActiveField != null)
            {
                isLightActiveField.SetValue(lumiereSpell, false);
            }
            
            var wasPinchingField = lumiereSpell.GetType().GetField("wasPinching", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wasPinchingField != null)
            {
                wasPinchingField.SetValue(lumiereSpell, false);
            }
            
            var wasIndexPointingField = lumiereSpell.GetType().GetField("wasIndexPointing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wasIndexPointingField != null)
            {
                wasIndexPointingField.SetValue(lumiereSpell, false);
            }
            
            var isVoiceCommandReceivedField = lumiereSpell.GetType().GetField("isVoiceCommandReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isVoiceCommandReceivedField != null)
            {
                isVoiceCommandReceivedField.SetValue(lumiereSpell, false);
            }
        }
        
        // Start fade to black effect
        if (enableFogFade)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeToBlackAndTeleport());
        }
        else if (teleportDelay <= 0f)
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
            StartCoroutine(PerformTeleport());
        }
        
        isTeleporting = false;
        teleportTimer = 0f;
    }
    
    private IEnumerator PerformTeleport()
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
        
        // Désactiver tous les composants qui peuvent interférer
        CharacterController characterController = playerRig.GetComponent<CharacterController>();
        Rigidbody rb = playerRig.GetComponent<Rigidbody>();
        
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Attendre un frame pour que les changements prennent effet
        yield return null;
        
        // Teleport the player
        playerRig.position = newPosition;
        playerRig.rotation = teleportDestination.rotation;
        
        // Attendre un autre frame
        yield return null;
        
        // Forcer la position une deuxième fois pour être sûr
        playerRig.position = newPosition;
        playerRig.rotation = teleportDestination.rotation;
        
        // Réactiver les composants
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"StatuePlayerCatcher: Player teleported to {newPosition} with rotation {teleportDestination.rotation.eulerAngles}");
        }
        
        // Return statue to initial position
        transform.position = statueInitialPosition;
        transform.rotation = statueInitialRotation;
        
        // Reset statue's pathfinding if needed
        if (statueAIPath != null)
        {
            statueAIPath.Teleport(statueInitialPosition);
            if (showDebugMessages)
            {
                Debug.Log($"StatuePlayerCatcher: Statue returned to initial position {statueInitialPosition}");
            }
        }
        
        // Reset door to idle (open) state
        if (doorController != null)
        {
            doorController.OpenDoor();
            if (showDebugMessages)
            {
                Debug.Log("StatuePlayerCatcher: Door reset to idle state");
            }
        }
        
        // Désactiver la lumière de scène après téléportation
        if (sceneLight != null)
        {
            sceneLight.SetActive(false);
            if (showDebugMessages)
            {
                Debug.Log("StatuePlayerCatcher: Scene light disabled after teleport");
            }
        }
    }
    
    /// <summary>
    /// Coroutine pour faire un fondu au noir, téléporter, puis fondu inverse
    /// </summary>
    private IEnumerator FadeToBlackAndTeleport()
    {
        // Activer le fog et configurer la couleur
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fadeColor;
        
        float startDensity = RenderSettings.fogDensity;
        float t = 0f;
        
        // Phase 1: Fondu au noir (augmenter la densité)
        if (showDebugMessages)
        {
            Debug.Log("StatuePlayerCatcher: Fade to black starting...");
        }
        
        while (t < 1f)
        {
            t += Time.deltaTime * fadeToBlackSpeed;
            t = Mathf.Clamp01(t);
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, maxFogDensity, t);
            yield return null;
        }
        
        // Phase 2: Attendre le teleportDelay configuré (écran noir)
        if (teleportDelay > 0f)
        {
            yield return new WaitForSeconds(teleportDelay);
        }
        
        // Phase 3: Téléporter le joueur
        ExecuteTeleport();
        
        // Phase 4: Restaurer immédiatement les paramètres de fog originaux
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fog = originalFogState;
        
        if (showDebugMessages)
        {
            Debug.Log("StatuePlayerCatcher: Teleport complete, fog restored");
        }
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
