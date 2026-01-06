using UnityEngine;

public enum LaserGestureType
{
    Pinch,
    Triangle
}

public class TriangleLaser : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    [Header("Gesture Type")]
    public LaserGestureType gestureType = LaserGestureType.Triangle;
    public OVRHand pinchHand; // Main utilisée pour le pinch (peut être left ou right)

    [Header("Laser Settings")]
    public ParticleSystem laserPrefab; // explosion_light
    public float laserDistance = 10f;
    public float laserWidth = 0.05f;
    public LayerMask laserHitLayers = ~0;

    [Header("Triangle Detection")]
    [Range(0.01f, 0.15f)]
    public float touchDistanceThreshold = 0.08f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDebugGizmos = true;

    private bool isLaserActive = false;
    private GameObject laserObject;
    private ParticleSystem laserEffect;
    private LineRenderer laserBeam;
    private bool wasPinching = false;

    void Start()
    {
        // Créer le LineRenderer pour le faisceau laser
        GameObject beamObj = new GameObject("LaserBeam");
        beamObj.transform.parent = transform;
        laserBeam = beamObj.AddComponent<LineRenderer>();
        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth;
        laserBeam.material = new Material(Shader.Find("Sprites/Default"));
        laserBeam.startColor = Color.cyan;
        laserBeam.endColor = Color.white;
        laserBeam.positionCount = 2;
        laserBeam.enabled = false;
    }

    void Update()
    {
        if (gestureType == LaserGestureType.Triangle)
        {
            if (leftHand == null || rightHand == null)
            {
                Debug.LogWarning("Left or Right hand not assigned!");
                return;
            }

            bool shouldActivateLaser = CheckHandsTouchingGesture();

            if (shouldActivateLaser && !isLaserActive)
            {
                ActivateLaser();
            }
            else if (!shouldActivateLaser && isLaserActive)
            {
                DeactivateLaser();
            }

            if (isLaserActive)
            {
                UpdateLaser();
            }
        }
        else if (gestureType == LaserGestureType.Pinch)
        {
            if (pinchHand == null)
            {
                Debug.LogWarning("Pinch hand not assigned!");
                return;
            }

            UpdatePinchGesture();
        }
    }

    void UpdatePinchGesture()
    {
        bool pinchActive = pinchHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        // Début du pinch - active le laser
        if (pinchActive && !wasPinching)
        {
            ActivateLaser();
        }

        // Pendant le pinch - met à jour le laser
        if (pinchActive && isLaserActive)
        {
            UpdateLaserPinch();
        }

        // Fin du pinch - désactive le laser
        if (!pinchActive && wasPinching)
        {
            DeactivateLaser();
        }

        wasPinching = pinchActive;
    }

    bool CheckHandsTouchingGesture()
    {
        // Vérifier que tous les doigts des deux mains se touchent
        OVRHand.HandFinger[] fingers = new OVRHand.HandFinger[] 
        { 
            OVRHand.HandFinger.Thumb, 
            OVRHand.HandFinger.Index, 
            OVRHand.HandFinger.Middle, 
            OVRHand.HandFinger.Ring, 
            OVRHand.HandFinger.Pinky 
        };

        foreach (var finger in fingers)
        {
            Vector3 leftFingerTip = GetFingerTipPosition(leftHand, finger);
            Vector3 rightFingerTip = GetFingerTipPosition(rightHand, finger);
            float distance = Vector3.Distance(leftFingerTip, rightFingerTip);

            if (distance > touchDistanceThreshold)
            {
                if (showDebugInfo && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"{finger} not touching: distance = {distance:F3}m");
                }
                return false;
            }
        }

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log("All fingers touching - gesture detected!");
        }

        return true;
    }

    Vector3 GetFingerTipPosition(OVRHand hand, OVRHand.HandFinger finger)
    {
        // Utiliser la position de la bone du bout du doigt
        OVRSkeleton skeleton = hand.GetComponent<OVRSkeleton>();
        if (skeleton != null && skeleton.Bones != null)
        {
            // Mapping des doigts aux bones
            int boneIndex = -1;
            switch (finger)
            {
                case OVRHand.HandFinger.Thumb:
                    boneIndex = (int)OVRSkeleton.BoneId.Hand_ThumbTip;
                    break;
                case OVRHand.HandFinger.Index:
                    boneIndex = (int)OVRSkeleton.BoneId.Hand_IndexTip;
                    break;
                case OVRHand.HandFinger.Middle:
                    boneIndex = (int)OVRSkeleton.BoneId.Hand_MiddleTip;
                    break;
                case OVRHand.HandFinger.Ring:
                    boneIndex = (int)OVRSkeleton.BoneId.Hand_RingTip;
                    break;
                case OVRHand.HandFinger.Pinky:
                    boneIndex = (int)OVRSkeleton.BoneId.Hand_PinkyTip;
                    break;
            }

            if (boneIndex >= 0 && boneIndex < skeleton.Bones.Count)
            {
                Transform bone = skeleton.Bones[boneIndex].Transform;
                if (bone != null)
                {
                    return bone.position;
                }
            }
        }

        // Fallback: utiliser la position de la main
        return hand.transform.position;
    }

    void ActivateLaser()
    {
        isLaserActive = true;

        // Déterminer la position initiale selon le type de geste
        Vector3 initialPosition = (gestureType == LaserGestureType.Triangle) 
            ? (leftHand.transform.position + rightHand.transform.position) / 2f
            : pinchHand.transform.position + pinchHand.transform.forward * 0.05f;

        // Créer l'objet laser
        if (laserPrefab != null)
        {
            laserObject = new GameObject("TriangleLaser");
            laserObject.transform.position = initialPosition;

            // Instancier l'effet de particules
            laserEffect = Instantiate(laserPrefab, laserObject.transform);
            laserEffect.transform.localPosition = Vector3.zero;
            laserEffect.Play();
        }

        // Activer le faisceau laser
        laserBeam.enabled = true;

        if (showDebugInfo)
        {
            Debug.Log("Laser activated!");
        }
    }

    void UpdateLaser()
    {
        // Calculer la position centrale entre les deux mains
        Vector3 handsCenter = (leftHand.transform.position + rightHand.transform.position) / 2f;

        // Mettre à jour la position de l'objet laser
        if (laserObject != null)
        {
            laserObject.transform.position = handsCenter;

            // Calculer la direction du laser vers l'avant des index
            Vector3 leftIndexTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Index);
            Vector3 rightIndexTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Index);
            Vector3 indexCenter = (leftIndexTip + rightIndexTip) / 2f;
            
            // Direction depuis le centre des mains vers le centre des index
            Vector3 laserDirection = (indexCenter - handsCenter).normalized;

            laserObject.transform.rotation = Quaternion.LookRotation(laserDirection);
        }

        // Mettre à jour le faisceau laser (raycast pour détecter les collisions)
        Vector3 laserStart = handsCenter;
        Vector3 laserDir = laserObject != null ? laserObject.transform.forward : Camera.main.transform.forward;
        Vector3 laserEnd = laserStart + laserDir * laserDistance;

        RaycastHit hit;
        if (Physics.Raycast(laserStart, laserDir, out hit, laserDistance, laserHitLayers))
        {
            laserEnd = hit.point;

            // Optionnel: ajouter un effet d'impact
            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"Laser hit: {hit.collider.gameObject.name}");
            }
        }

        // Mettre à jour le LineRenderer
        laserBeam.SetPosition(0, laserStart);
        laserBeam.SetPosition(1, laserEnd);
    }

    void UpdateLaserPinch()
    {
        // Position et direction du laser depuis la main qui pince
        Vector3 laserStart = pinchHand.transform.position + pinchHand.transform.forward * 0.05f;
        Vector3 laserDir = pinchHand.transform.forward;

        // Mettre à jour la position de l'objet laser
        if (laserObject != null)
        {
            laserObject.transform.position = laserStart;
            laserObject.transform.rotation = Quaternion.LookRotation(laserDir);
        }

        // Calculer le point final du laser avec raycast
        Vector3 laserEnd = laserStart + laserDir * laserDistance;
        RaycastHit hit;
        if (Physics.Raycast(laserStart, laserDir, out hit, laserDistance, laserHitLayers))
        {
            laserEnd = hit.point;

            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"Laser hit: {hit.collider.gameObject.name}");
            }
        }

        // Mettre à jour le LineRenderer
        laserBeam.SetPosition(0, laserStart);
        laserBeam.SetPosition(1, laserEnd);
    }

    void DeactivateLaser()
    {
        isLaserActive = false;

        // Détruire l'objet laser
        if (laserObject != null)
        {
            if (laserEffect != null)
            {
                laserEffect.Stop();
            }
            Destroy(laserObject, 1f); // Délai pour laisser les particules disparaître
            laserObject = null;
            laserEffect = null;
        }

        // Désactiver le faisceau
        laserBeam.enabled = false;

        if (showDebugInfo)
        {
            Debug.Log("Laser deactivated!");
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || leftHand == null || rightHand == null)
            return;

        // Dessiner les positions de tous les doigts
        OVRHand.HandFinger[] fingers = new OVRHand.HandFinger[] 
        { 
            OVRHand.HandFinger.Thumb, 
            OVRHand.HandFinger.Index, 
            OVRHand.HandFinger.Middle, 
            OVRHand.HandFinger.Ring, 
            OVRHand.HandFinger.Pinky 
        };

        foreach (var finger in fingers)
        {
            Vector3 leftFingerTip = GetFingerTipPosition(leftHand, finger);
            Vector3 rightFingerTip = GetFingerTipPosition(rightHand, finger);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(leftFingerTip, 0.008f);
            Gizmos.DrawWireSphere(rightFingerTip, 0.008f);

            // Dessiner une ligne entre les doigts correspondants
            if (isLaserActive)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(leftFingerTip, rightFingerTip);
            }
        }

        // Dessiner le centre entre les mains
        if (isLaserActive)
        {
            Vector3 handsCenter = (leftHand.transform.position + rightHand.transform.position) / 2f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(handsCenter, 0.02f);
        }
    }

    void OnDestroy()
    {
        if (laserObject != null)
        {
            Destroy(laserObject);
        }
    }
}