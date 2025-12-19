using UnityEngine;

public class TriangleLaser : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand leftHand;
    public OVRHand rightHand;

    [Header("Laser Settings")]
    public ParticleSystem laserPrefab; // explosion_light
    public float laserDistance = 10f;
    public float laserWidth = 0.05f;
    public LayerMask laserHitLayers = ~0;

    [Header("Triangle Detection")]
    [Range(0.01f, 0.2f)]
    public float openHandThreshold = 0.08f;
    [Range(0.01f, 0.15f)]
    public float touchDistanceThreshold = 0.08f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDebugGizmos = true;

    private bool isLaserActive = false;
    private GameObject laserObject;
    private ParticleSystem laserEffect;
    private LineRenderer laserBeam;
    private Vector3 triangleCenter;

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
        if (leftHand == null || rightHand == null)
        {
            Debug.LogWarning("Left or Right hand not assigned!");
            return;
        }

        bool shouldActivateLaser = CheckTriangleGesture();

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

    bool CheckTriangleGesture()
    {
        // Vérifier que les deux mains sont ouvertes
        if (!IsHandOpen(leftHand) || !IsHandOpen(rightHand))
        {
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log("Hands not fully open");
            }
            return false;
        }

        // Obtenir les positions des pouces et index
        Vector3 leftThumbTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Thumb);
        Vector3 rightThumbTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Thumb);
        Vector3 leftIndexTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Index);
        Vector3 rightIndexTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Index);

        // Vérifier que les pouces se touchent
        float thumbDistance = Vector3.Distance(leftThumbTip, rightThumbTip);
        if (thumbDistance > touchDistanceThreshold)
        {
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"Thumbs not touching: distance = {thumbDistance:F3}m");
            }
            return false;
        }

        // Vérifier que les index se touchent
        float indexDistance = Vector3.Distance(leftIndexTip, rightIndexTip);
        if (indexDistance > touchDistanceThreshold)
        {
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"Index fingers not touching: distance = {indexDistance:F3}m");
            }
            return false;
        }

        // Calculer le centre du triangle
        Vector3 thumbCenter = (leftThumbTip + rightThumbTip) / 2f;
        Vector3 indexCenter = (leftIndexTip + rightIndexTip) / 2f;
        triangleCenter = (thumbCenter + indexCenter) / 2f;

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Triangle gesture detected! Center: {triangleCenter}");
        }

        return true;
    }

    bool IsHandOpen(OVRHand hand)
    {
        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) < openHandThreshold;
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

        // Créer l'objet laser au centre du triangle
        if (laserPrefab != null)
        {
            laserObject = new GameObject("TriangleLaser");
            laserObject.transform.position = triangleCenter;

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
        // Recalculer le centre du triangle à chaque frame
        Vector3 leftThumbTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Thumb);
        Vector3 rightThumbTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Thumb);
        Vector3 leftIndexTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Index);
        Vector3 rightIndexTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Index);

        Vector3 thumbCenter = (leftThumbTip + rightThumbTip) / 2f;
        Vector3 indexCenter = (leftIndexTip + rightIndexTip) / 2f;
        triangleCenter = (thumbCenter + indexCenter) / 2f;

        // Mettre à jour la position de l'objet laser
        if (laserObject != null)
        {
            laserObject.transform.position = triangleCenter;

            // Calculer la direction du laser (depuis le centre vers l'avant)
            // On utilise la normale du triangle formé par les 4 points
            Vector3 toIndex = indexCenter - thumbCenter;
            Vector3 toLeft = leftIndexTip - leftThumbTip;
            Vector3 laserDirection = Vector3.Cross(toIndex, toLeft).normalized;

            // S'assurer que le laser pointe vers l'avant (pas vers l'utilisateur)
            Vector3 headForward = Camera.main.transform.forward;
            if (Vector3.Dot(laserDirection, headForward) < 0)
            {
                laserDirection = -laserDirection;
            }

            laserObject.transform.rotation = Quaternion.LookRotation(laserDirection);
        }

        // Mettre à jour le faisceau laser (raycast pour détecter les collisions)
        Vector3 laserStart = triangleCenter;
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

        // Dessiner les positions des doigts
        Vector3 leftThumbTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Thumb);
        Vector3 rightThumbTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Thumb);
        Vector3 leftIndexTip = GetFingerTipPosition(leftHand, OVRHand.HandFinger.Index);
        Vector3 rightIndexTip = GetFingerTipPosition(rightHand, OVRHand.HandFinger.Index);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftThumbTip, 0.01f);
        Gizmos.DrawWireSphere(rightThumbTip, 0.01f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(leftIndexTip, 0.01f);
        Gizmos.DrawWireSphere(rightIndexTip, 0.01f);

        // Dessiner le triangle
        if (isLaserActive)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftThumbTip, rightThumbTip);
            Gizmos.DrawLine(leftIndexTip, rightIndexTip);
            Gizmos.DrawLine(leftThumbTip, leftIndexTip);
            Gizmos.DrawLine(rightThumbTip, rightIndexTip);

            // Dessiner le centre
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(triangleCenter, 0.02f);
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