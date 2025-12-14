using UnityEngine;
using System.Collections.Generic;
using Oculus.Voice; // Meta Voice SDK
using Meta.WitAi.Json; // for WitResponseNode
using Oculus.Interaction.Input;


//This is a very important script to learn how to use voice command and "pseudo" eye tracking


public class GazeScaleVoiceController : MonoBehaviour {
    [Header("References")]
    public Camera vrCamera;                 // Assign CenterEyeAnchor or MainCamera
    public AppVoiceExperience voice;        // Assign Wit.ai AppVoiceExperience prefab
    public float rayLength = 5f;
    public HandRef leftHand;                 // Assign one Hand (from Hand Interactions)
    private bool wasPinching = false;

    [Header("Visual & Behavior Toggles")]
    public bool rayVisible = true;          // toggle visibility of green line
    public bool gazeActive = true;          // toggle scaling/raycast behavior

    [Header("Scaling Settings")]
    [Tooltip("How much to scale up (e.g., 0.2 = +20%)")]
    public float scaleUpPercentage = 0.2f;
    [Tooltip("Time (in seconds) to reach target scale")]
    public float scaleDuration = 1f;

    private LineRenderer line;
    private GameObject currentTarget;
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private float currentScaleTime = 0f;
    void Awake(){
        if (vrCamera == null) vrCamera = Camera.main;
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.01f;
        line.endWidth = 0.005f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.green;
        line.endColor = Color.green;
    }

    void Start(){
        // Hook up voice events
        if (voice != null){
            voice.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            voice.VoiceEvents.OnError.AddListener(OnVoiceError);
            voice.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            voice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else{
            Debug.LogWarning("AppVoiceExperience (voice) not assigned. Voice commands won't work.");
        }
    }

    void Update(){
        if (leftHand == null || voice == null) return;

        //Check if left index finger is pinching
        bool isPinching = leftHand.GetFingerIsPinching(HandFinger.Index);

        //Detect pinch start
        if (isPinching && !wasPinching){
            Debug.Log("Left-hand pinch started voice.Activate()");
            voice.Activate();
        }

        //Detect pinch release to stop listening
        if (!isPinching && wasPinching){
            Debug.Log("Left-hand pinch released voice.Deactivate()");
            voice.Deactivate();
        }

        wasPinching = isPinching;
    }

    void LateUpdate(){
        if (vrCamera == null) return;

        Vector3 origin = vrCamera.transform.position;
        Vector3 direction = vrCamera.transform.rotation * Vector3.forward;
        Vector3 endPoint = origin + direction * rayLength;

        line.enabled = rayVisible;
        if (rayVisible){
            line.SetPosition(0, origin);
            line.SetPosition(1, endPoint);
            Debug.DrawLine(origin, endPoint, Color.red);
        }

        if (!gazeActive){
            ResetPreviousTarget();
            return;
        }

        //Raycast for scaling
        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayLength)){
            GameObject hitObj = hit.collider.gameObject;
            Rigidbody rb = hitObj.GetComponent<Rigidbody>();
            bool isMovableLayer = hitObj.layer == LayerMask.NameToLayer("Movable");

            if (rb != null && isMovableLayer)
            {
                if (currentTarget != hitObj)
                {
                    ResetPreviousTarget();
                    currentTarget = hitObj;
                    currentScaleTime = 0f;

                    if (!originalScales.ContainsKey(hitObj))
                        originalScales[hitObj] = hitObj.transform.localScale;
                }

                currentScaleTime += Time.deltaTime;
                float t = Mathf.Clamp01(currentScaleTime / scaleDuration);

                Vector3 baseScale = originalScales[currentTarget];
                Vector3 targetScale = baseScale * (1f + scaleUpPercentage);
                currentTarget.transform.localScale = Vector3.Lerp(baseScale, targetScale, t);

                return;
            }
        }

        ResetPreviousTarget();
    }

    private void ResetPreviousTarget(){
        if (currentTarget == null) return;

        Vector3 baseScale = originalScales[currentTarget];
        currentTarget.transform.localScale = Vector3.Lerp(
            currentTarget.transform.localScale,
            baseScale,
            Time.deltaTime * (1f / scaleDuration) * 2f
        );

        if (Vector3.Distance(currentTarget.transform.localScale, baseScale) < 0.001f){
            currentTarget.transform.localScale = baseScale;
            currentTarget = null;
            currentScaleTime = 0f;
        }
    }

    // Voice events
    private void OnVoiceResponse(WitResponseNode response){
        string text = response["text"];
        Debug.Log($"Heard: {text}");

        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower();

        if (text.Contains("on"))
        {
            rayVisible = true;
            Debug.Log("Voice command: Ray visible (ON)");
        }
        else if (text.Contains("of"))
        {
            rayVisible = false;
            Debug.Log("Voice command: Ray hidden (OFF)");
        }
    }

    private void OnVoiceError(string error, string message){
        Debug.LogError($"Voice Error: {error} - {message}");
    }

    private void OnPartialTranscription(string text){
        Debug.Log($"Partial transcription: {text}");
    }

    private void OnFullTranscription(string text){
        Debug.Log($"Full transcription: {text}");
    }
}
