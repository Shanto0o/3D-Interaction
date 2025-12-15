using UnityEngine;

//This is a bugged version of HandGuidedPlacement, its not really useful, but its fun so ... I kept it
//You can skip it at the beginning, but take a look when you have time

public class HandForceThrow : MonoBehaviour
{
    public enum TriggerType { Pinch, IndexTrigger }

    [Header("Settings")]
    public TriggerType triggerType = TriggerType.Pinch;
    public OVRHand hand; // Assign your OVRHand (Left or Right)
    public GameObject objectPrefab;
    public float handRayOffset = 0.05f;
    public float cubeSpeed = 10f;
    public float maxDistance = 10f;

    private bool isActive = false;
    private GameObject activeCube;
    private Rigidbody cubeRb;
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        // check if hand movement
        if (hand == null || objectPrefab == null)
            return;

        bool triggerPressed = false;
        switch (triggerType)
        {
            case TriggerType.Pinch:
                triggerPressed = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                break;
            case TriggerType.IndexTrigger:
                triggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger);
                break;
        }

        // start / stop beams
        if (triggerPressed && !isActive)
        {
            StartBeam();
        }
        else if (!triggerPressed && isActive)
        {
            StopBeam();
        }

        if (isActive && activeCube != null)
        {
            UpdateBeam();
        }
    }

    //Placement beam
    void StartBeam()
    {
        isActive = true;

        // Spawn cube at hand and apply an impulse forward
        Vector3 spawnPos = hand.transform.position + hand.transform.forward * handRayOffset;
        activeCube = Instantiate(objectPrefab, spawnPos, Quaternion.identity);

        cubeRb = activeCube.GetComponent<Rigidbody>();
        if (cubeRb == null)
            cubeRb = activeCube.AddComponent<Rigidbody>();

        // don't modify gravity, use the prefab’s own setting
        cubeRb.linearVelocity = hand.transform.forward * cubeSpeed;

        lineRenderer.enabled = true;
    }

    //Drop the placement
    void StopBeam()
    {
        isActive = false;
        lineRenderer.enabled = false;
        activeCube = null;
        cubeRb = null;
    }

    void UpdateBeam()
    {
        if (!activeCube) return;

        Vector3 rayStart = hand.transform.position + hand.transform.forward * handRayOffset;
        Vector3 direction = hand.transform.forward;
        lineRenderer.SetPosition(0, rayStart);
        lineRenderer.SetPosition(1, rayStart + direction * maxDistance);

        if (cubeRb != null && cubeRb.linearVelocity.sqrMagnitude > 0.01f)
        {
            if (Physics.Raycast(rayStart, direction, out RaycastHit hit, maxDistance))
            {
                if (Vector3.Distance(activeCube.transform.position, hit.point) < 0.1f)
                {
                    StopBeam();
                }
            }
        }
    }
}
