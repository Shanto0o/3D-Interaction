using UnityEngine;
using Pathfinding; // A* Pathfinding Project namespace

/// <summary>
/// Contrôle le mouvement d'une statue avec AIPath basé sur le regard du joueur (raycast depuis le casque VR).
/// La statue s'arrête quand le joueur la regarde et reprend après 0.5s quand il détourne le regard.
/// </summary>
public class StatueGazeController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign CenterEyeAnchor or MainCamera from VR rig")]
    public Camera vrCamera;
    
    [Tooltip("The statue GameObject that has the AIPath component")]
    public GameObject statueObject;
    
    [Tooltip("The trigger collider (child of statue) used for detection. Leave empty to detect statue directly.")]
    public GameObject detectionTrigger;
    
    [Header("Raycast Settings")]
    [Tooltip("Maximum distance for raycast detection")]
    public float rayLength = 20f;
    
    [Tooltip("Delay before statue can move again after player looks away")]
    public float resumeMovementDelay = 0.5f;
    
    [Header("Visual Debug")]
    [Tooltip("Show debug raycast line")]
    public bool showDebugRay = true;
    
    // Private variables
    private AIPath aiPath;
    private bool isLookingAtStatue = false;
    private float lookAwayTimer = 0f;
    private LineRenderer debugLine;
    
    void Awake()
    {
        // Get main camera if not assigned
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
        }
        
        // Get AIPath component from statue
        if (statueObject != null)
        {
            aiPath = statueObject.GetComponent<AIPath>();
            if (aiPath == null)
            {
                Debug.LogError($"StatueGazeController: No AIPath component found on {statueObject.name}!");
            }
        }
        else
        {
            Debug.LogWarning("StatueGazeController: Statue object not assigned!");
        }
        
        // Setup debug line renderer
        if (showDebugRay)
        {
            debugLine = gameObject.AddComponent<LineRenderer>();
            debugLine.positionCount = 2;
            debugLine.useWorldSpace = true;
            debugLine.startWidth = 0.02f;
            debugLine.endWidth = 0.01f;
            debugLine.material = new Material(Shader.Find("Sprites/Default"));
            debugLine.startColor = Color.cyan;
            debugLine.endColor = Color.cyan;
        }
    }
    
    void Update()
    {
        if (vrCamera == null || aiPath == null) return;
        
        // Raycast from VR camera
        Vector3 origin = vrCamera.transform.position;
        Vector3 direction = vrCamera.transform.forward;
        
        // Perform raycast
        RaycastHit hit;
        bool hitStatue = false;
        Vector3 endPoint = origin + direction * rayLength;
        
        if (Physics.Raycast(origin, direction, out hit, rayLength))
        {
            // Ray stops at the hit point (wall, object, or statue)
            endPoint = hit.point;
            
            // Check if we hit the detection trigger or statue
            GameObject hitObject = hit.collider.gameObject;
            
            // If a specific detection trigger is assigned, check for it
            if (detectionTrigger != null)
            {
                if (hitObject == detectionTrigger)
                {
                    hitStatue = true;
                }
            }
            else
            {
                // Fallback: check if we hit the statue or any of its children
                if (hitObject == statueObject || hitObject.transform.IsChildOf(statueObject.transform))
                {
                    hitStatue = true;
                }
            }
        }
        
        // Update debug visualization (ray stops at collision)
        if (showDebugRay && debugLine != null)
        {
            debugLine.enabled = true;
            debugLine.SetPosition(0, origin);
            debugLine.SetPosition(1, endPoint);
            
            // Change color based on whether we're looking at statue
            debugLine.startColor = isLookingAtStatue ? Color.red : Color.cyan;
            debugLine.endColor = isLookingAtStatue ? Color.yellow : Color.cyan;
        }
        
        // Handle state changes
        if (hitStatue)
        {
            // Player is looking at statue
            if (!isLookingAtStatue)
            {
                OnStartLookingAtStatue();
            }
            isLookingAtStatue = true;
            lookAwayTimer = 0f; // Reset timer while looking
        }
        else
        {
            // Player is NOT looking at statue
            if (isLookingAtStatue)
            {
                // Just looked away, start timer
                isLookingAtStatue = false;
                lookAwayTimer = 0f;
            }
            
            // Increment timer if we're not looking
            if (!isLookingAtStatue && !aiPath.canMove)
            {
                lookAwayTimer += Time.deltaTime;
                
                // After delay, allow statue to move again
                if (lookAwayTimer >= resumeMovementDelay)
                {
                    OnStopLookingAtStatue();
                }
            }
        }
    }
    
    /// <summary>
    /// Called when player starts looking at the statue
    /// </summary>
    private void OnStartLookingAtStatue()
    {
        if (aiPath != null)
        {
            aiPath.canMove = false;
            Debug.Log($"Statue frozen! Player is looking at {statueObject.name}");
        }
    }
    
    /// <summary>
    /// Called after player looks away and delay has passed
    /// </summary>
    private void OnStopLookingAtStatue()
    {
        if (aiPath != null && !isLookingAtStatue)
        {
            aiPath.canMove = true;
            Debug.Log($"Statue can move again! Player looked away from {statueObject.name}");
        }
    }
    
    void OnDisable()
    {
        // Make sure statue can move when script is disabled
        if (aiPath != null)
        {
            aiPath.canMove = true;
        }
    }
    
    // Optional: Gizmos for editor visualization
    void OnDrawGizmos()
    {
        if (vrCamera == null) return;
        
        Vector3 origin = vrCamera.transform.position;
        Vector3 direction = vrCamera.transform.forward;
        
        Gizmos.color = isLookingAtStatue ? Color.red : Color.green;
        Gizmos.DrawRay(origin, direction * rayLength);
    }
}
