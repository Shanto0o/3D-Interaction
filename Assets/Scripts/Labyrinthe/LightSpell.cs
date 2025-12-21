using UnityEngine;
using System.Collections.Generic;
using Oculus.Voice; // Meta Voice SDK
using Meta.WitAi.Json; // for WitResponseNode
using Oculus.Interaction.Input;


public class LightSpell : MonoBehaviour
{
    [Header("References")]
    public OVRHand hand;
    public ParticleSystem fireBallPrefab; // FX_Fire_03
    public AppVoiceExperience voice;

    [Header("Gesture Type")]
    public GestureType gestureType = GestureType.Pinch;

    [Header("Charge Settings")]
    public float chargeTime = 3f;
    public float launchForce = 15f;

    [Header("Open Hand Settings")]
    [Range(0.01f, 0.5f)]
    public float openHandThreshold = 0.08f;
    public float palmUpThreshold = 0.5f;
    public float palmDownThreshold = -0.5f;

    [Header("Visual Feedback")]
    public bool showChargingEffect = true;
    public float maxChargeScale = 0.3f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isPinching = false;
    private bool isHandOpen = false;
    private bool wasHandOpen = false;
    private float currentChargeTime = 0f;
    private bool isFullyCharged = false;

    private GameObject chargingFireBall;
    private Vector3 chargeStartPosition;
    private FireColor currentFireColor = FireColor.Rouge;

    private bool isVoiceActive = false;
    private float voiceCooldown = 0f;
    private const float VOICE_COOLDOWN_TIME = 1f;


    void Start()
    {
        if (voice != null)
        {
            voice.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            voice.VoiceEvents.OnError.AddListener(OnVoiceError);
            voice.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            voice.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }
        else
        {
            Debug.LogWarning("AppVoiceExperience (voice) not assigned. Voice commands won't work.");
        }
    }
    void Update()
    {
        if (hand == null || fireBallPrefab == null)
        {
            Debug.LogWarning("Hand or fireball prefab not assigned!");
            return;
        }

        // Gestion de l'activation continue de la voix
        if (voice != null)
        {
            if (voiceCooldown > 0f)
            {
                voiceCooldown -= Time.deltaTime;
            }

            if (!isVoiceActive && voiceCooldown <= 0f)
            {
                voice.Activate();
                isVoiceActive = true;
                if (showDebugInfo)
                {
                    Debug.Log("Voice activated - listening...");
                }
            }
        }

        // Gestion selon le type de geste choisi
        if (gestureType == GestureType.Pinch)
        {
            UpdatePinchGesture();
        }
        else if (gestureType == GestureType.OpenHand)
        {
            UpdateOpenHandGesture();
        }
    }

    void UpdatePinchGesture()
    {
        bool pinchActive = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        // Début du pinch - commence à charger
        if (pinchActive && !isPinching)
        {
            StartCharging();
        }

        // Pendant le pinch - continue de charger
        if (pinchActive && isPinching)
        {
            UpdateCharging();
        }

        // Fin du pinch - lance si chargé
        if (!pinchActive && isPinching)
        {
            if (isFullyCharged)
            {
                LaunchFireBall();
            }
            else
            {
                CancelCharging();
            }
            ResetCharge();
        }

        isPinching = pinchActive;
    }

    void UpdateOpenHandGesture()
    {
        bool handOpen = CheckOpenHand();

        // Détecter l'orientation de la main (paume vers le haut ou vers le bas)
        float palmOrientation = -hand.transform.up.y;
        bool palmUp = palmOrientation > palmUpThreshold;
        bool palmDown = palmOrientation < palmDownThreshold;

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Hand Open: {handOpen} | Palm Up: {palmUp} | Palm Down: {palmDown} | Orientation: {palmOrientation:F2}");
        }

        // Début main ouverte + paume vers le haut - commence à charger
        if (handOpen && palmUp && !wasHandOpen && !isHandOpen)
        {
            StartCharging();
        }

        // Pendant main ouverte + paume vers le haut - continue de charger
        if (handOpen && palmUp && isHandOpen)
        {
            UpdateCharging();
        }

        // Retourne la main (paume vers le bas) - lance si chargé
        if (palmDown && isHandOpen)
        {
            if (isFullyCharged)
            {
                LaunchFireBall();
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Cannot launch: not fully charged ({currentChargeTime:F1}s / {chargeTime}s)");
                }
                CancelCharging();
            }
            ResetCharge();
        }
        // Ferme la main sans retourner - annule
        else if (!handOpen && isHandOpen && !palmDown)
        {
            CancelCharging();
            ResetCharge();
        }

        wasHandOpen = handOpen;
    }

    bool CheckOpenHand()
    {
        // Vérifie que tous les doigts sont ouverts
        return hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring) < openHandThreshold &&
               hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky) < openHandThreshold;
    }

    void StartCharging()
    {
        if (gestureType == GestureType.Pinch)
        {
            isPinching = true;
        }
        else
        {
            isHandOpen = true;
        }

        currentChargeTime = 0f;
        isFullyCharged = false;
        chargeStartPosition = hand.transform.position + hand.transform.forward * 0.2f;

        if (showChargingEffect)
        {
            // Créer un GameObject parent avec Rigidbody
            chargingFireBall = new GameObject("ChargingFireBall");
            chargingFireBall.transform.position = chargeStartPosition;
            chargingFireBall.transform.rotation = Quaternion.identity;
            chargingFireBall.transform.localScale = Vector3.zero;

            // Ajouter le Rigidbody
            Rigidbody rb = chargingFireBall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Ajouter un SphereCollider pour la détection
            SphereCollider collider = chargingFireBall.AddComponent<SphereCollider>();
            collider.radius = 0.15f; // Ajuster selon la taille de la boule

            // Ajouter le tag de couleur
            FireBallColorTag colorTag = chargingFireBall.AddComponent<FireBallColorTag>();
            colorTag.color = currentFireColor;

            // Instancier le ParticleSystem comme enfant
            ParticleSystem ps = Instantiate(fireBallPrefab, chargingFireBall.transform);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;

            // Appliquer la couleur
            ApplyColorToParticleSystem(ps, currentFireColor);
        }

        if (showDebugInfo)
        {
            Debug.Log("Charging started...");
        }
    }

    void UpdateCharging()
    {
        currentChargeTime += Time.deltaTime;

        // Calculer le pourcentage de charge
        float chargePercent = Mathf.Clamp01(currentChargeTime / chargeTime);

        if (currentChargeTime >= chargeTime && !isFullyCharged)
        {
            isFullyCharged = true;
            if (showDebugInfo)
            {
                Debug.Log("Fireball fully charged!");
            }
        }

        // Mise à jour visuelle de la boule pendant la charge
        if (showChargingEffect && chargingFireBall != null)
        {
            // Positionner la boule devant la main
            Vector3 targetPos = hand.transform.position + hand.transform.forward * 0.2f;
            chargingFireBall.transform.position = Vector3.Lerp(
                chargingFireBall.transform.position,
                targetPos,
                Time.deltaTime * 10f
            );

            // Faire grandir la boule pendant la charge
            float scale = chargePercent * maxChargeScale;
            chargingFireBall.transform.localScale = Vector3.Lerp(
                chargingFireBall.transform.localScale,
                Vector3.one * scale,
                Time.deltaTime * 5f
            );

            // Rotation pour plus d'effet
            chargingFireBall.transform.Rotate(Vector3.up, Time.deltaTime * 100f);
        }

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Charge: {chargePercent * 100f:F0}%");
        }
    }

    void LaunchFireBall()
    {
        Vector3 launchPosition = hand.transform.position + hand.transform.forward * 0.2f;
        Vector3 launchDirection = hand.transform.forward;

        GameObject fireBall;

        if (showChargingEffect && chargingFireBall != null)
        {
            // Utiliser la boule déjà créée
            fireBall = chargingFireBall;
            fireBall.transform.localScale = Vector3.one * maxChargeScale;
        }
        else
        {
            // Créer un nouveau GameObject parent avec Rigidbody
            fireBall = new GameObject("FireBall");
            fireBall.transform.position = launchPosition;
            fireBall.transform.rotation = Quaternion.identity;
            fireBall.transform.localScale = Vector3.one * maxChargeScale;

            // Ajouter le Rigidbody
            fireBall.AddComponent<Rigidbody>();

            // Ajouter un SphereCollider pour la détection
            SphereCollider collider = fireBall.AddComponent<SphereCollider>();
            collider.radius = 0.15f; // Ajuster selon la taille de la boule

            // Ajouter le tag de couleur
            FireBallColorTag colorTag = fireBall.AddComponent<FireBallColorTag>();
            colorTag.color = currentFireColor;

            // Instancier le ParticleSystem comme enfant
            ParticleSystem ps = Instantiate(fireBallPrefab, fireBall.transform);
            ps.transform.localPosition = Vector3.zero;
            ps.transform.localRotation = Quaternion.identity;

            // Appliquer la couleur
            ApplyColorToParticleSystem(ps, currentFireColor);
        }

        // Activer la physique
        Rigidbody rb = fireBall.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = launchDirection * launchForce;
        rb.angularVelocity = Random.insideUnitSphere * 2f;

        // Détruire la boule après quelques secondes
        Destroy(fireBall, 5f);

        if (showDebugInfo)
        {
            Debug.Log($"Fireball launched! Direction: {launchDirection}");
        }

        chargingFireBall = null;
    }

    void CancelCharging()
    {
        if (showChargingEffect && chargingFireBall != null)
        {
            // Faire disparaître la boule progressivement
            Destroy(chargingFireBall, 0.2f);
            chargingFireBall = null;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Charging cancelled (only {currentChargeTime:F1}s / {chargeTime}s)");
        }
    }

    void ResetCharge()
    {
        isPinching = false;
        isHandOpen = false;
        currentChargeTime = 0f;
        isFullyCharged = false;
    }

    private void OnVoiceResponse(WitResponseNode response)
    {
        string text = response["text"];
        Debug.Log($"Heard: {text}");

        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;

        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower();

        FireColor previousColor = currentFireColor;
        bool colorChanged = false;

        if (text.Contains("rouge"))
        {
            currentFireColor = FireColor.Rouge;
            colorChanged = true;
            Debug.Log("Couleur changée : Rouge");
        }
        else if (text.Contains("vert"))
        {
            currentFireColor = FireColor.Vert;
            colorChanged = true;
            Debug.Log("Couleur changée : Vert");
        }
        else if (text.Contains("bleu"))
        {
            currentFireColor = FireColor.Bleu;
            colorChanged = true;
            Debug.Log("Couleur changée : Bleu");
        }
        else if (text.Contains("violet"))
        {
            currentFireColor = FireColor.Violet;
            colorChanged = true;
            Debug.Log("Couleur changée : Violet");
        }

        // Si la couleur a changé et qu'une boule est en cours de charge, la mettre à jour
        if (colorChanged && previousColor != currentFireColor && chargingFireBall != null)
        {
            ParticleSystem ps = chargingFireBall.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ApplyColorToParticleSystem(ps, currentFireColor);
                if (showDebugInfo)
                {
                    Debug.Log("Couleur appliquée à la boule de feu en cours");
                }
            }

            // Mettre à jour le tag de couleur aussi
            FireBallColorTag colorTag = chargingFireBall.GetComponent<FireBallColorTag>();
            if (colorTag != null)
            {
                colorTag.color = currentFireColor;
                if (showDebugInfo)
                {
                    Debug.Log($"Tag de couleur mis à jour : {currentFireColor}");
                }
            }
        }
    }

    private void OnVoiceError(string error, string message)
    {
        Debug.LogError($"Voice Error: {error} - {message}");
        isVoiceActive = false;
        voiceCooldown = VOICE_COOLDOWN_TIME;
    }

    private void OnPartialTranscription(string text)
    {
        Debug.Log($"Partial transcription: {text}");
    }

    private void OnFullTranscription(string text)
    {
        Debug.Log($"Full transcription: {text}");
    }

    private void GetFireColor(FireColor color, out Color startColor, out Color midColor, out Color endColor)
    {
        switch (color)
        {
            case FireColor.Rouge:
                startColor = new Color(1f, 0.949f, 0.09f); // FFF217
                midColor = new Color(1f, 0f, 0f);          // FF0000
                endColor = new Color(0.784f, 0f, 0f);      // C80000
                break;

            case FireColor.Vert:
                startColor = new Color(0.969f, 1f, 0.09f); // F7FF17
                midColor = new Color(0f, 1f, 0f);          // 00FF00
                endColor = new Color(0f, 0.784f, 0f);      // 00C800
                break;

            case FireColor.Bleu:
                startColor = new Color(0.09f, 0.969f, 1f); // 17F7FF
                midColor = new Color(0f, 0f, 1f);          // 0000FF
                endColor = new Color(0f, 0f, 0.784f);      // 0000C8
                break;

            case FireColor.Violet:
                startColor = new Color(1f, 0.09f, 1f);     // FF17FF
                midColor = new Color(1f, 0f, 1f);          // FF00FF
                endColor = new Color(0.784f, 0f, 0.784f);  // C800C8
                break;

            default:
                startColor = new Color(1f, 0.949f, 0.09f);
                midColor = new Color(1f, 0f, 0f);
                endColor = new Color(0.784f, 0f, 0f);
                break;
        }
    }

    private void RGBToHSV(Color color, out float h, out float s, out float v)
    {
        float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
        float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
        float delta = max - min;

        // Value
        v = max;

        // Saturation
        s = (max != 0f) ? (delta / max) : 0f;

        // Hue
        if (delta == 0f)
        {
            h = 0f;
        }
        else if (max == color.r)
        {
            h = ((color.g - color.b) / delta) % 6f;
        }
        else if (max == color.g)
        {
            h = ((color.b - color.r) / delta) + 2f;
        }
        else
        {
            h = ((color.r - color.g) / delta) + 4f;
        }

        h *= 60f;
        if (h < 0f) h += 360f;
        h /= 360f; // Normaliser entre 0 et 1
    }

    private Color HSVToRGB(float h, float s, float v, float a)
    {
        h *= 360f;
        float c = v * s;
        float x = c * (1f - Mathf.Abs((h / 60f) % 2f - 1f));
        float m = v - c;

        float r = 0f, g = 0f, b = 0f;

        if (h >= 0f && h < 60f)
        {
            r = c; g = x; b = 0f;
        }
        else if (h >= 60f && h < 120f)
        {
            r = x; g = c; b = 0f;
        }
        else if (h >= 120f && h < 180f)
        {
            r = 0f; g = c; b = x;
        }
        else if (h >= 180f && h < 240f)
        {
            r = 0f; g = x; b = c;
        }
        else if (h >= 240f && h < 300f)
        {
            r = x; g = 0f; b = c;
        }
        else
        {
            r = c; g = 0f; b = x;
        }

        return new Color(r + m, g + m, b + m, a);
    }

    private bool IsGrayscaleColor(Color color)
    {
        float h, s, v;
        RGBToHSV(color, out h, out s, out v);
        // Si la saturation est très faible, c'est du noir/blanc/gris
        return s < 0.15f;
    }

    private Color AdjustColorHue(Color originalColor, FireColor targetColor)
    {
        float h, s, v;
        RGBToHSV(originalColor, out h, out s, out v);

        // Si c'est une couleur grise/blanche/noire (faible saturation), ne pas la changer
        if (s < 0.15f)
        {
            return originalColor;
        }

        // Obtenir la nouvelle teinte selon la couleur cible
        float newHue;
        switch (targetColor)
        {
            case FireColor.Rouge:
                newHue = 0f / 360f; // Rouge = 0°
                break;
            case FireColor.Vert:
                newHue = 120f / 360f; // Vert = 120°
                break;
            case FireColor.Bleu:
                newHue = 240f / 360f; // Bleu = 240°
                break;
            case FireColor.Violet:
                newHue = 300f / 360f; // Magenta/Violet = 300°
                break;
            default:
                newHue = 0f / 360f;
                break;
        }

        // Appliquer la nouvelle teinte en gardant saturation et luminosité
        return HSVToRGB(newHue, s, v, originalColor.a);
    }

    private void ApplyColorToSingleParticleSystem(ParticleSystem ps, FireColor color)
    {
        if (ps == null)
            return;

        var col = ps.colorOverLifetime;

        // Si le module n'est pas activé, on active avec les couleurs par défaut
        if (!col.enabled)
        {
            Color startColor, midColor, endColor;
            GetFireColor(color, out startColor, out midColor, out endColor);

            col.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(midColor, 0.615f),
                    new GradientColorKey(endColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.615f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);
            return;
        }

        // Récupérer le gradient existant
        Gradient existingGradient = null;
        if (col.color.mode == ParticleSystemGradientMode.Gradient)
        {
            existingGradient = col.color.gradient;
        }
        else if (col.color.mode == ParticleSystemGradientMode.TwoGradients)
        {
            existingGradient = col.color.gradientMax;
        }

        // Si pas de gradient, on ne peut rien faire
        if (existingGradient == null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"No gradient found on: {ps.name}");
            }
            return;
        }

        // Le gradient a des couleurs, on les ajuste selon la couleur cible
        GradientColorKey[] colorKeys = existingGradient.colorKeys;
        GradientAlphaKey[] alphaKeys = existingGradient.alphaKeys;

        bool hasColorChanges = false;

        for (int i = 0; i < colorKeys.Length; i++)
        {
            Color newColor = AdjustColorHue(colorKeys[i].color, color);
            if (newColor != colorKeys[i].color)
            {
                colorKeys[i].color = newColor;
                hasColorChanges = true;
            }
        }

        if (hasColorChanges)
        {
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(colorKeys, alphaKeys);
            col.color = new ParticleSystem.MinMaxGradient(newGradient);

            if (showDebugInfo)
            {
                Debug.Log($"Applied color to: {ps.name}");
            }
        }
        else if (showDebugInfo)
        {
            Debug.Log($"Skipped grayscale particle system: {ps.name}");
        }
    }

    private void ApplyColorToParticleSystem(ParticleSystem ps, FireColor color)
    {
        if (ps == null)
            return;

        // Appliquer au ParticleSystem principal
        ApplyColorToSingleParticleSystem(ps, color);

        // Récupérer tous les ParticleSystems enfants
        ParticleSystem[] childParticleSystems = ps.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem childPS in childParticleSystems)
        {
            // Éviter de traiter le parent une deuxième fois
            if (childPS != ps)
            {
                ApplyColorToSingleParticleSystem(childPS, color);
            }
        }
    }
}