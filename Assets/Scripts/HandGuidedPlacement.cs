using UnityEngine;
using UnityEngine.InputSystem;

//Very important script to learn how to use and motion for diverse interaction
//You need to read and understand everything to build similar stuff for your project

public class HandGuidedPlacement : MonoBehaviour{
    public enum InteractionType{
        Disabled,
        IndexPinch,
        MiddlePinch,
        RingPinch,
        PinkyPinch,
        AllFingersPinch,
        FullOpenHand,
        FullyClosedHand,
        Poke,
        Grab
    }

    [Header("References")]
    public OVRHand hand;
    public GameObject cubePrefab;

    [Header("Primary Interaction (Place Beam)")]
    public InteractionType placeInteractionType = InteractionType.IndexPinch;

    [Header("Secondary Interaction (Grab Beam)")]
    public InteractionType grabInteractionType = InteractionType.Grab;

    [Header("Place Beam Settings")]
    public float spawnDistance = 0.3f;
    public float rayDistance = 5f;
    public float cubeSpeed = 2f;
    public float collisionCheckDistance = 0.2f;
    public LayerMask raycastLayers = ~0;

    [Header("Grab Beam Settings")]
    public float grabSpeed = 3f;

    [Header("Swipe Settings")]
    public float swipeThreshold = 0.2f;
    public float swipeTime = 0.15f;
    public float throwForceMultiplier = 5f;


    [Header("Custom Gesture Threshold")]
    [Range(0.5f, 0.9f)]
    public float allPinchThreshold = 0.7f;
    [Range(0.01f, 0.2f)]
    public float openHandThreshold = 0.08f;

    [Header("Closed Fist Thresholds (adjust per finger)")]
    [Range(0.01f, 1f)]
    public float indexClosedMin = 0.1f;
    [Range(0.01f, 1f)]
    public float middleClosedMin = 0.1f;
    [Range(0.01f, 1f)]
    public float ringClosedMin = 0.1f;
    [Range(0.01f, 1f)]
    public float pinkyClosedMin = 0.1f;

    [Header("Debug")]
    public bool showDebugValues = false;

    private GameObject spawnedCube;
    private Rigidbody cubeRigidbody;
    private LineRenderer placeRay;
    private LineRenderer grabRay;

    private bool wasActive = false;
    private bool cubeIsGuided = false;
    private Vector3 lastPosition;
    private float debugTimer = 0f;

    // Swipe tracking
    private Vector3 swipeStartPos;
    private float swipeStartTime;
    private bool trackingSwipe = false;

    private Rigidbody grabbedObject = null;

    void Start(){
        // place ray
        placeRay = gameObject.AddComponent<LineRenderer>();
        placeRay.startWidth = 0.01f;
        placeRay.endWidth = 0.01f;
        placeRay.material = new Material(Shader.Find("Sprites/Default"));
        placeRay.startColor = Color.green;
        placeRay.endColor = Color.green;
        placeRay.positionCount = 2;
        placeRay.enabled = false;

        // grab ray
        grabRay = new GameObject("GrabRay").AddComponent<LineRenderer>();
        grabRay.startWidth = 0.01f;
        grabRay.endWidth = 0.01f;
        grabRay.material = new Material(Shader.Find("Sprites/Default"));
        grabRay.startColor = Color.blue;
        grabRay.endColor = Color.blue;
        grabRay.positionCount = 2;
        grabRay.enabled = false;
    }

    void Update(){
        if (hand == null || cubePrefab == null){
            Debug.LogWarning("Hand or cube prefab not assigned!");
            return;
        }

        if (showDebugValues){
            debugTimer += Time.deltaTime;
            if (debugTimer > 0.5f)
            {
                debugTimer = 0f;
                float i = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                float m = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
                float r = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
                float p = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);
                float t = hand.GetFingerPinchStrength(OVRHand.HandFinger.Thumb);
                Debug.Log($"Fist - Index: {i:F2}, Middle: {m:F2}, Ring: {r:F2}, Pinky: {p:F2}, Thumb: {t:F2}");
            }
        }

        bool placeActive = CheckInteraction(placeInteractionType);
        bool grabActive = CheckInteraction(grabInteractionType);

        // Place beam
        if (placeActive && !wasActive && !cubeIsGuided){
            SpawnCube();
        }
        if (cubeIsGuided && spawnedCube != null && placeActive){
            GuideCube();
        }
        else if (!placeActive && cubeIsGuided){
            DropCube();
        }

        // Grab beam
        if (grabActive){
            HandleGrabBeam();
        }
        else{
            if (grabbedObject != null)
                grabbedObject = null;
            grabRay.enabled = false;
        }

        DetectSwipe();
    }

    bool CheckInteraction(InteractionType type){
        if (type == InteractionType.Disabled)
            return false;

        switch (type){
            case InteractionType.IndexPinch:
                return hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            case InteractionType.MiddlePinch:
                return hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
            case InteractionType.RingPinch:
                return hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);
            case InteractionType.PinkyPinch:
                return hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);
            //The previous hand interaction were already defined by the library
            //The following ones are defined manually by me ... you can do the same !
            case InteractionType.AllFingersPinch:
                return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > allPinchThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > allPinchThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > allPinchThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) > allPinchThreshold;
            case InteractionType.FullOpenHand:
                return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < openHandThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) < openHandThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) < openHandThreshold &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) < openHandThreshold;
            case InteractionType.FullyClosedHand:
                return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > indexClosedMin &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > middleClosedMin &&
                       hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > ringClosedMin;
            case InteractionType.Poke:
                bool indexExtended = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < 0.5f;
                bool middlePoke = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) > 0.4f;
                bool ringPoke = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) > 0.4f;
                bool keyboardPoke = Keyboard.current.pKey.isPressed;
                return (indexExtended && middlePoke && ringPoke) || keyboardPoke;
            case InteractionType.Grab:
                bool indexGrab = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                bool keyboardGrab = Keyboard.current.gKey.isPressed;
                return indexGrab || keyboardGrab;
            default:
                return false;
        }
    }

    void HandleGrabBeam(){
        if (!grabbedObject)
            grabbedObject = null;

        Vector3 rayStart = hand.transform.position;
        Vector3 rayDir = hand.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDir, out hit, rayDistance, raycastLayers)){
            Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.linearVelocity = (hand.transform.position - targetRb.position) * grabSpeed;
            }
        }

        if (grabbedObject != null){
            Vector3 targetPos = hand.transform.position + hand.transform.forward * spawnDistance;
            grabbedObject.linearVelocity = (targetPos - grabbedObject.position) * grabSpeed;
        }

        // Draw ray for visualization
        grabRay.SetPosition(0, rayStart);
        grabRay.SetPosition(1, rayStart + rayDir * rayDistance);
        grabRay.enabled = true;
    }

    void DetectSwipe(){
        Vector3 handPos = hand.transform.position;

        if (!trackingSwipe){
            trackingSwipe = true;
            swipeStartPos = handPos;
            swipeStartTime = Time.time;
        }
        else{
            float swipeDist = Vector3.Distance(handPos, swipeStartPos);
            float swipeDuration = Time.time - swipeStartTime;

            if (swipeDist >= swipeThreshold && swipeDuration <= swipeTime){
                Vector3 swipeDirection = (handPos - swipeStartPos).normalized;
                SpawnAndThrowCube(swipeDirection);
                trackingSwipe = false;
            }
            else if (swipeDuration > swipeTime){
                // reset if too slow
                trackingSwipe = false;
            }
        }
    }

    void SpawnAndThrowCube(Vector3 direction){
        Vector3 spawnPosition = hand.transform.position + hand.transform.forward * spawnDistance;
        GameObject cube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null) rb = cube.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.linearVelocity = direction * throwForceMultiplier;
        rb.angularVelocity = Vector3.zero;
    }

    void SpawnCube(){
        Vector3 spawnPosition = hand.transform.position + hand.transform.forward * spawnDistance;
        spawnedCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);

        cubeRigidbody = spawnedCube.GetComponent<Rigidbody>();
        if (cubeRigidbody == null){
            cubeRigidbody = spawnedCube.AddComponent<Rigidbody>();
        }

        cubeRigidbody.useGravity = false;
        cubeRigidbody.isKinematic = true;
        cubeRigidbody.interpolation = RigidbodyInterpolation.None;

        if (spawnedCube.GetComponent<Collider>() == null){
            spawnedCube.AddComponent<BoxCollider>();
        }

        placeRay.enabled = true;
        cubeIsGuided = true;
        lastPosition = spawnPosition;
    }

    void GuideCube(){
        Vector3 rayStart = hand.transform.position + hand.transform.forward * 0.05f;
        Vector3 rayDirection = hand.transform.forward;
        Vector3 rayEnd = rayStart + rayDirection * rayDistance;

        placeRay.SetPosition(0, rayStart);
        placeRay.SetPosition(1, rayEnd);

        Vector3 targetPosition;
        RaycastHit hit;

        int cubeLayerMask = 1 << spawnedCube.layer;
        int finalLayerMask = raycastLayers & ~cubeLayerMask;

        if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, finalLayerMask)){
            targetPosition = hit.point;

            float distanceToHit = Vector3.Distance(spawnedCube.transform.position, hit.point);
            if (distanceToHit < collisionCheckDistance)
            {
                StopGuiding();
                return;
            }
        }
        else{
            targetPosition = rayEnd;
        }

        Vector3 newPosition = Vector3.MoveTowards(
            spawnedCube.transform.position,
            targetPosition,
            cubeSpeed * Time.deltaTime
        );

        spawnedCube.transform.position = newPosition;
        lastPosition = newPosition;
    }

    void DropCube(){
        if (cubeRigidbody != null){
            cubeRigidbody.isKinematic = false;
            cubeRigidbody.linearVelocity = Vector3.zero;
            cubeRigidbody.angularVelocity = Vector3.zero;
            cubeRigidbody.useGravity = true;
            cubeRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
        placeRay.enabled = false;
        cubeIsGuided = false;
    }

    void StopGuiding(){
        if (cubeRigidbody != null){
            cubeRigidbody.isKinematic = false;
            cubeRigidbody.linearVelocity = Vector3.zero;
            cubeRigidbody.angularVelocity = Vector3.zero;
            cubeRigidbody.useGravity = true;
        }

        placeRay.enabled = false;
        cubeIsGuided = false;

        Debug.Log("Cube stopped");
    }
}
