using UnityEngine;
using UnityEngine.InputSystem;
using Oculus.Voice;
using Meta.WitAi.Json;
using TMPro;

/// <summary>
/// Boss avec deux phases : Slime Vert puis Slime Rouge
/// Phase 1 : 5 coups → rétrécit → 5 coups → Phase 2
/// Phase 2 : Immunisé jusqu'à touche "T" → 5 coups → rétrécit → 5 coups → Mort
/// </summary>
public class SlimeBoss : MonoBehaviour
{
    [Header("Slime Prefabs")]
    [Tooltip("Prefab du slime vert (Phase 1)")]
    public GameObject greenSlimePrefab;
    [Tooltip("Prefab du slime rouge (Phase 2)")]
    public GameObject redSlimePrefab;
    
    [Header("Boss Settings")]
    [Tooltip("Nombre de coups par sous-phase (avant rétrécissement)")]
    public int hitsPerSubPhase = 5;
    
    [Header("Phase 2 - Immunité")]
    [Tooltip("AppVoiceExperience pour la commande vocale 'combat'")]
    public AppVoiceExperience voice;
    
    [Header("Player Knockback")]
    [Tooltip("PlayerController à repousser lors des collisions")]
    public Transform playerController;
    [Tooltip("Force de repoussement du joueur")]
    public float knockbackForce = 5f;
    [Tooltip("Direction verticale du repoussement (vers le haut)")]
    public float knockbackUpForce = 2f;
    
    [Header("Propulsion au Spawn")]
    [Tooltip("Distance minimale requise entre le joueur et le boss lors du spawn")]
    public float safetyDistance = 3f;
    [Tooltip("Force de propulsion appliquée si le joueur est trop proche au spawn")]
    public float spawnPropulsionForce = 15f;
    
    [Header("Visual Feedback")]
    [Tooltip("Activer les effets visuels lors des coups")]
    public bool showHitEffect = true;
    [Tooltip("Durée du flash lors d'un coup")]
    public float hitFlashDuration = 0.1f;
    [Tooltip("Caméra VR (OVRCameraRig CenterEyeAnchor) - le Canvas sera créé automatiquement comme enfant")]
    public Camera vrCamera;
    [Tooltip("Durée d'affichage du message d'immunité")]
    public float immuneTextDuration = 2f;
    [Tooltip("Couleur du flash pour feedback de coup")]
    public Color hitFlashColor = Color.white;
    [Tooltip("Durée du flash lors d'un changement de sous-phase")]
    public float phaseFlashDuration = 0.3f;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Son joué quand le boss est frappé")]
    public AudioClip hitSound;
    [Tooltip("Son joué quand le boss est immunisé")]
    public AudioClip immuneSound;
    [Tooltip("Son joué lors du changement de phase")]
    public AudioClip phaseChangeSound;
    [Tooltip("Son joué quand le boss meurt")]
    public AudioClip deathSound;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // État du boss
    private enum BossPhase
    {
        Phase1_Green_Normal,    // Slime vert, taille normale
        Phase1_Green_Small,     // Slime vert, rétréci
        Phase2_Red_Normal,      // Slime rouge, taille normale, immunisé
        Phase2_Red_Small,       // Slime rouge, rétréci, peut être frappé
        Dead                    // Boss mort
    }
    
    private BossPhase currentPhase = BossPhase.Phase1_Green_Normal;
    private int currentHits = 0;
    private GameObject currentSlimeObject;
    private Renderer[] slimeRenderers;
    private Color[] originalColors;
    private AudioSource audioSource;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private Color flashColor;
    private bool isPhase2Immune = true;
    private Rigidbody bossRigidbody;
    
    // Canvas overlay VR créé automatiquement
    private Canvas immuneCanvas;
    private TextMeshProUGUI immuneText;
    
    void Start()
    {
        Debug.Log("🐙 ===== SLIMEBOSS START =====");
        Debug.Log($"   └─ GameObject: {gameObject.name}");
        Debug.Log($"   └─ Position: {transform.position}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(gameObject.layer)}");
        Debug.Log($"   └─ Tag: {gameObject.tag}");
        
        // Vérifier les prefabs
        if (greenSlimePrefab == null)
        {
            Debug.LogError("❌ Green Slime Prefab non assigné!");
            return;
        }
        if (redSlimePrefab == null)
        {
            Debug.LogError("❌ Red Slime Prefab non assigné!");
            return;
        }
        
        Debug.Log($"✅ Green Slime Prefab: {greenSlimePrefab.name}");
        Debug.Log($"✅ Red Slime Prefab: {redSlimePrefab.name}");
        
        // Ajouter un AudioSource si des sons sont configurés
        if (hitSound != null || immuneSound != null || phaseChangeSound != null || deathSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Son 3D
        }
        
        // Instancier le premier slime (vert)
        SpawnSlime(greenSlimePrefab, Vector3.one);
        
        // S'abonner aux événements vocaux
        if (voice != null)
        {
            voice.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            Debug.Log("🎤 Commande vocale 'combat' activée pour désactiver l'immunité en Phase 2");
        }
        else
        {
            Debug.LogWarning("⚠️ AppVoiceExperience non assigné! Commande vocale désactivée.");
        }
        
        // Créer le Canvas overlay VR
        CreateVROverlayCanvas();
        
        if (showDebugInfo)
        {
            Debug.Log($"🐙 SlimeBoss initialisé - Phase 1 : Slime Vert");
            Debug.Log($"   └─ {hitsPerSubPhase} coups → rétrécit → {hitsPerSubPhase} coups → Phase 2");
        }
    }
    
    void CreateVROverlayCanvas()
    {
        if (vrCamera == null)
        {
            Debug.LogWarning("⚠️ VR Camera non assignée, le texte d'immunité ne s'affichera pas");
            return;
        }
        
        // Créer un GameObject pour le Canvas
        GameObject canvasObj = new GameObject("ImmuneOverlayCanvas");
        canvasObj.transform.SetParent(vrCamera.transform, false);
        
        // Configurer le Canvas
        immuneCanvas = canvasObj.AddComponent<Canvas>();
        immuneCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        immuneCanvas.worldCamera = vrCamera;
        immuneCanvas.planeDistance = 1.5f; // Distance devant la caméra
        
        // Ajouter CanvasScaler
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ajouter GraphicRaycaster (optionnel mais recommandé)
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Créer le texte
        GameObject textObj = new GameObject("ImmuneText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        immuneText = textObj.AddComponent<TextMeshProUGUI>();
        immuneText.text = "Boss immunisé!";
        immuneText.fontSize = 80;
        immuneText.color = new Color(1f, 0.3f, 0f, 1f); // Orange vif
        immuneText.alignment = TextAlignmentOptions.Center;
        immuneText.fontStyle = FontStyles.Bold;
        
        // Positionner le texte au centre de l'écran
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(800, 200);
        
        // Désactiver le Canvas par défaut
        canvasObj.SetActive(false);
        
        Debug.Log("✅ Canvas overlay VR créé et attaché à la caméra");
    }
    
    void Update()
    {
        // Gérer l'effet de flash
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                isFlashing = false;
                RestoreOriginalColors();
            }
            else
            {
                ApplyFlashColor(flashColor);
            }
        }
    }
    
    void SpawnSlime(GameObject slimePrefab, Vector3 scale)
    {
        // Détruire l'ancien slime s'il existe
        if (currentSlimeObject != null)
        {
            Destroy(currentSlimeObject);
        }
        
        // Instancier le nouveau slime
        currentSlimeObject = Instantiate(slimePrefab, transform.position, transform.rotation, transform);
        currentSlimeObject.name = "ActiveSlime";
        currentSlimeObject.transform.localScale = scale;
        
        // Configurer les colliders sur le slime
        SetupCollidersOnSlime();
        
        // Récupérer tous les renderers pour les effets visuels
        slimeRenderers = currentSlimeObject.GetComponentsInChildren<Renderer>();
        StoreOriginalColors();
        
        if (showDebugInfo)
        {
            Debug.Log($"✨ Slime instancié : {slimePrefab.name} | Scale: {scale}");
        }
    }
    
    void SetupCollidersOnSlime()
    {
        if (currentSlimeObject == null) return;
        
        Debug.Log("🔧 SetupCollidersOnSlime() appelé");
        
        // Ajouter un Rigidbody au parent (ce GameObject) s'il n'existe pas
        bossRigidbody = GetComponent<Rigidbody>();
        if (bossRigidbody == null)
        {
            bossRigidbody = gameObject.AddComponent<Rigidbody>();
            Debug.Log("➕ Rigidbody ajouté au parent boss");
        }
        bossRigidbody.isKinematic = true;
        bossRigidbody.useGravity = false;
        Debug.Log($"✅ Boss Rigidbody: isKinematic={bossRigidbody.isKinematic}, useGravity={bossRigidbody.useGravity}");
        
        // Chercher les colliders existants sur le slime ou ajouter un BoxCollider
        Collider[] existingColliders = currentSlimeObject.GetComponentsInChildren<Collider>();
        Debug.Log($"🔍 Colliders trouvés sur le slime: {existingColliders.Length}");
        
        if (existingColliders.Length == 0)
        {
            // Pas de collider : en ajouter un
            BoxCollider physicsCollider = currentSlimeObject.AddComponent<BoxCollider>();
            physicsCollider.isTrigger = false;
            physicsCollider.size = Vector3.one * 3f; // AUGMENTÉ pour test
            
            BoxCollider triggerCollider = currentSlimeObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = Vector3.one * 4f; // TRÈS LARGE pour test
            
            Debug.Log("➕ Colliders ajoutés au slime (physique + trigger) - TAILLE AUGMENTÉE POUR TEST");
        }
        else
        {
            // S'assurer qu'il y a au moins un trigger
            bool hasTrigger = false;
            bool hasPhysics = false;
            
            foreach (Collider col in existingColliders)
            {
                if (col.isTrigger) hasTrigger = true;
                else hasPhysics = true;
            }
            
            if (!hasPhysics)
            {
                existingColliders[0].isTrigger = false;
                Debug.Log("✅ Premier collider configuré en physique");
            }
            
            if (!hasTrigger)
            {
                BoxCollider triggerCollider = currentSlimeObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = Vector3.one * 1.1f;
                Debug.Log("➕ Collider trigger ajouté");
            }
            
            Debug.Log($"📊 Résumé colliders: {existingColliders.Length} colliders (Physics: {hasPhysics}, Trigger: {hasTrigger})");
        }
        
        // Log final de tous les colliders
        Collider[] finalColliders = GetComponentsInChildren<Collider>();
        Debug.Log($"📋 COLLIDERS FINAUX SUR BOSS (incluant enfants): {finalColliders.Length}");
        foreach (Collider col in finalColliders)
        {
            Debug.Log($"   └─ {col.gameObject.name}: {col.GetType().Name} | IsTrigger: {col.isTrigger} | Enabled: {col.enabled}");
            
            // Ajouter le BossCollisionRelay sur chaque collider enfant pour relayer les collisions au parent
            if (col.gameObject != gameObject) // Pas sur le parent lui-même
            {
                BossCollisionRelay relay = col.gameObject.GetComponent<BossCollisionRelay>();
                if (relay == null)
                {
                    relay = col.gameObject.AddComponent<BossCollisionRelay>();
                    Debug.Log($"➕ BossCollisionRelay ajouté sur {col.gameObject.name}");
                }
            }
        }
    }
    
    void StoreOriginalColors()
    {
        if (slimeRenderers == null) return;
        
        originalColors = new Color[slimeRenderers.Length];
        for (int i = 0; i < slimeRenderers.Length; i++)
        {
            if (slimeRenderers[i] != null && slimeRenderers[i].material != null)
            {
                originalColors[i] = slimeRenderers[i].material.color;
            }
        }
    }
    
    void RestoreOriginalColors()
    {
        if (slimeRenderers == null || originalColors == null) return;
        
        for (int i = 0; i < slimeRenderers.Length; i++)
        {
            if (slimeRenderers[i] != null && slimeRenderers[i].material != null)
            {
                slimeRenderers[i].material.color = originalColors[i];
            }
        }
    }
    
    void ApplyFlashColor(Color color)
    {
        if (slimeRenderers == null) return;
        
        foreach (Renderer renderer in slimeRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }
    
    /// <summary>
    /// Appelé quand le boss est frappé
    /// </summary>
    public void TakeHit(Vector3 hitPoint)
    {
        // Vérifier l'immunité en Phase 2
        if (currentPhase == BossPhase.Phase2_Red_Normal && isPhase2Immune)
        {
            ShowImmuneEffect();
            return;
        }
        
        currentHits++;
        
        if (showDebugInfo)
        {
            Debug.Log($"💥 Slime frappé! Phase: {currentPhase} | Coups: {currentHits}/{hitsPerSubPhase}");
        }
        
        // Effet visuel du coup
        if (showHitEffect)
        {
            ShowHitEffect();
        }
        
        // Jouer le son de coup
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Vérifier si on change de sous-phase
        if (currentHits >= hitsPerSubPhase)
        {
            AdvancePhase();
        }
    }
    
    void ShowImmuneEffect()
    {
        if (showDebugInfo)
        {
            Debug.Log("🛡️ Boss immunisé! Dites 'combat' pour désactiver l'immunité");
        }
        
        // Afficher le message à l'écran
        if (immuneCanvas != null)
        {
            immuneCanvas.gameObject.SetActive(true);
            CancelInvoke(nameof(HideImmuneText));
            Invoke(nameof(HideImmuneText), immuneTextDuration);
        }
        
        // Effet visuel d'immunité (flash jaune/orange)
        if (showHitEffect)
        {
            flashColor = new Color(1f, 0.5f, 0f, 1f); // Orange
            isFlashing = true;
            flashTimer = hitFlashDuration;
        }
        
        // Jouer le son d'immunité
        if (immuneSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(immuneSound);
        }
    }
    
    void HideImmuneText()
    {
        if (immuneCanvas != null)
        {
            immuneCanvas.gameObject.SetActive(false);
        }
        else if (immuneText != null)
        {
            immuneText.gameObject.SetActive(false);
        }
    }
    
    void ShowHitEffect()
    {
        flashColor = hitFlashColor;
        isFlashing = true;
        flashTimer = hitFlashDuration;
    }
    
    void ShowPhaseChangeEffect()
    {
        flashColor = Color.white;
        isFlashing = true;
        flashTimer = phaseFlashDuration;
    }
    
    void AdvancePhase()
    {
        currentHits = 0;
        
        switch (currentPhase)
        {
            case BossPhase.Phase1_Green_Normal:
                // Rétrécir le slime vert
                currentPhase = BossPhase.Phase1_Green_Small;
                ShrinkSlime();
                if (showDebugInfo)
                {
                    Debug.Log("🔄 Phase 1 → Slime vert rétréci");
                }
                break;
                
            case BossPhase.Phase1_Green_Small:
                // Passer au slime rouge (Phase 2)
                currentPhase = BossPhase.Phase2_Red_Normal;
                isPhase2Immune = true;
                SpawnSlime(redSlimePrefab, Vector3.one);
                
                // Propulser le joueur s'il est trop proche
                PropulsePlayerIfTooClose();
                
                PlayPhaseChangeSound();
                if (showDebugInfo)
                {
                    Debug.Log("🔴 PHASE 2 : Slime Rouge apparaît (IMMUNISÉ)");
                    Debug.Log("   └─ Dites 'combat' pour désactiver l'immunité (simulation destruction des objets)");
                }
                break;
                
            case BossPhase.Phase2_Red_Normal:
                // Rétrécir le slime rouge
                currentPhase = BossPhase.Phase2_Red_Small;
                ShrinkSlime();
                if (showDebugInfo)
                {
                    Debug.Log("🔄 Phase 2 → Slime rouge rétréci");
                }
                break;
                
            case BossPhase.Phase2_Red_Small:
                // Boss mort
                currentPhase = BossPhase.Dead;
                Die();
                break;
        }
    }
    
    void ShrinkSlime()
    {
        if (currentSlimeObject != null)
        {
            currentSlimeObject.transform.localScale *= 0.5f;
            ShowPhaseChangeEffect();
        }
    }
    
    /// <summary>
    /// Appelé quand la reconnaissance vocale reçoit une réponse
    /// </summary>
    void OnVoiceResponse(WitResponseNode response)
    {
        Debug.Log("🎤🎤🎤 SLIMEBOSS OnVoiceResponse APPELÉ!");
        
        if (response == null)
        {
            Debug.LogWarning("❌ Response est NULL!");
            return;
        }
        
        // Log de la réponse complète pour voir la structure
        Debug.Log($"📋 Response complète: {response}");
        
        string transcription = response["text"]?.Value?.ToLower() ?? "";
        Debug.Log($"🎤 Transcription extraite: '{transcription}'");
        Debug.Log($"📊 État actuel: Phase={currentPhase}, Immunisé={isPhase2Immune}");
        
        // Détecter les variantes de "combat"
        bool containsCombat = transcription.Contains("combat") || transcription.Contains("combats");
        bool containsAttaque = transcription.Contains("attaque") || transcription.Contains("attaquer");
        Debug.Log($"🔍 Contient 'combat': {containsCombat}, Contient 'attaque': {containsAttaque}");
        
        if (containsCombat || containsAttaque)
        {
            Debug.Log($"✅ Mot-clé détecté! Vérification des conditions...");
            Debug.Log($"   └─ currentPhase == Phase2_Red_Normal? {currentPhase == BossPhase.Phase2_Red_Normal}");
            Debug.Log($"   └─ isPhase2Immune? {isPhase2Immune}");
            
            if (currentPhase == BossPhase.Phase2_Red_Normal && isPhase2Immune)
            {
                Debug.Log("⚔️⚔️⚔️ Commande 'combat' détectée! Désactivation de l'immunité...");
                RemoveImmunity();
            }
            else
            {
                Debug.LogWarning($"❌ Conditions non remplies! Phase: {currentPhase}, Immunisé: {isPhase2Immune}");
            }
        }
        else
        {
            Debug.Log($"❌ Mot-clé non trouvé dans '{transcription}'");
        }
    }
    
    void RemoveImmunity()
    {
        isPhase2Immune = false;
        
        if (showDebugInfo)
        {
            Debug.Log("✅ Immunité retirée! Le boss peut maintenant être frappé!");
        }
        
        // Effet visuel (flash vert pour confirmer)
        flashColor = Color.green;
        isFlashing = true;
        flashTimer = phaseFlashDuration;
        
        // Son de confirmation
        if (phaseChangeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(phaseChangeSound);
        }
    }
    
    void PlayPhaseChangeSound()
    {
        if (phaseChangeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(phaseChangeSound);
        }
    }
    
    void Die()
    {
        if (showDebugInfo)
        {
            Debug.Log("💀 Boss détruit!");
        }
        
        // Jouer le son de mort
        if (deathSound != null && audioSource != null)
        {
            GameObject tempAudio = new GameObject("TempAudio");
            AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
            tempSource.clip = deathSound;
            tempSource.spatialBlend = 0f;
            tempSource.Play();
            Destroy(tempAudio, deathSound.length);
        }
        
        // Détruire le boss
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Repousse le joueur loin du boss
    /// </summary>
    void KnockbackPlayer(Vector3 collisionPoint)
    {
        if (playerController == null) return;
        
        // Calculer la direction de repoussement (du boss vers le joueur)
        Vector3 knockbackDirection = (playerController.position - transform.position).normalized;
        knockbackDirection.y = 0; // Garder horizontal
        
        // Ajouter une composante verticale
        Vector3 knockbackVector = knockbackDirection * knockbackForce + Vector3.up * knockbackUpForce;
        
        // Chercher le CharacterController sur le joueur
        CharacterController characterController = playerController.GetComponent<CharacterController>();
        if (characterController != null)
        {
            // Appliquer le mouvement via CharacterController
            characterController.Move(knockbackVector * Time.fixedDeltaTime);
            Debug.Log($"💨 Joueur repoussé! Direction: {knockbackDirection}, Force: {knockbackForce}");
        }
        else
        {
            // Chercher un Rigidbody si pas de CharacterController
            Rigidbody playerRb = playerController.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.AddForce(knockbackVector, ForceMode.Impulse);
                Debug.Log($"💨 Joueur repoussé (Rigidbody)! Direction: {knockbackDirection}, Force: {knockbackForce}");
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerController n'a ni CharacterController ni Rigidbody!");
            }
        }
    }
    
    /// <summary>    /// Propulse le joueur s'il est trop proche lors du spawn du boss
    /// </summary>
    void PropulsePlayerIfTooClose()
    {
        if (playerController == null)
        {
            Debug.Log("⚠️ playerController non assigné, propulsion ignorée");
            return;
        }
        
        float distance = Vector3.Distance(playerController.position, transform.position);
        Debug.Log($"🚀 Distance joueur-boss: {distance:F2}m (sécurité: {safetyDistance}m)");
        
        if (distance < safetyDistance)
        {
            // Calculer la direction de propulsion (du boss vers le joueur)
            Vector3 propulsionDirection = (playerController.position - transform.position).normalized;
            propulsionDirection.y = 0; // Garder horizontal
            
            // Ajouter une composante verticale pour un effet de "pop"
            Vector3 propulsionVector = propulsionDirection * spawnPropulsionForce + Vector3.up * (spawnPropulsionForce * 0.5f);
            
            Debug.Log($"🚀🚀🚀 PROPULSION! Direction={propulsionDirection}, Force={spawnPropulsionForce}");
            
            // Essayer CharacterController
            CharacterController cc = playerController.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log("🚀 Propulsion via CharacterController.Move()");
                cc.Move(propulsionVector * Time.deltaTime);
            }
            else
            {
                // Fallback: Rigidbody
                Rigidbody rb = playerController.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Debug.Log("🚀 Propulsion via Rigidbody.AddForce()");
                    rb.AddForce(propulsionVector, ForceMode.VelocityChange);
                }
                else
                {
                    Debug.LogWarning("⚠️ Aucun CharacterController ou Rigidbody trouvé");
                }
            }
        }
        else
        {
            Debug.Log($"✅ Joueur à distance sécuritaire ({distance:F2}m)");
        }
    }
    
    /// <summary>    /// Appelé par le BossCollisionRelay quand un collider enfant détecte une collision
    /// </summary>
    public void OnChildCollisionEnter(Collision collision)
    {
        Debug.Log($"🔔🔔🔔 SLIMEBOSS OnChildCollisionEnter (relayé) APPELÉ!");
        Debug.Log($"   └─ Objet collision: {collision.gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {collision.gameObject.tag}");
        Debug.Log($"   └─ Contact points: {collision.contacts.Length}");
        
        if (currentPhase == BossPhase.Dead)
        {
            Debug.Log("❌ Boss déjà mort, ignoré");
            return;
        }
        
        SwordDamageDealer sword = collision.gameObject.GetComponent<SwordDamageDealer>();
        Debug.Log($"🔍 Recherche SwordDamageDealer sur {collision.gameObject.name}: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        
        if (sword == null)
        {
            sword = collision.gameObject.GetComponentInParent<SwordDamageDealer>();
            Debug.Log($"🔍 Recherche dans parent: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        }
        
        if (sword != null)
        {
            Debug.Log($"⚔️ SwordDamageDealer trouvé! Test CanDealDamage()...");
            if (sword.CanDealDamage())
            {
                Debug.Log("✅✅✅ Coup valide! TakeHit() appelé");
                Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                TakeHit(hitPoint);
                sword.OnHitConfirmed();
                
                // Repousser le joueur
                KnockbackPlayer(hitPoint);
            }
            else
            {
                Debug.Log("❌ CanDealDamage() = false");
            }
        }
        else
        {
            Debug.Log("❌ Aucun SwordDamageDealer trouvé sur l'objet en collision");
        }
    }
    
    /// <summary>
    /// Appelé par le BossCollisionRelay quand un collider enfant détecte un trigger
    /// </summary>
    public void OnChildTriggerEnter(Collider other)
    {
        Debug.Log($"🔔🔔🔔 SLIMEBOSS OnChildTriggerEnter (relayé) APPELÉ!");
        Debug.Log($"   └─ Objet trigger: {other.gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {other.gameObject.tag}");
        Debug.Log($"   └─ IsTrigger: {other.isTrigger}");
        
        if (currentPhase == BossPhase.Dead)
        {
            Debug.Log("❌ Boss déjà mort, ignoré");
            return;
        }
        
        SwordDamageDealer sword = other.gameObject.GetComponent<SwordDamageDealer>();
        Debug.Log($"🔍 Recherche SwordDamageDealer sur {other.gameObject.name}: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        
        if (sword == null)
        {
            sword = other.gameObject.GetComponentInParent<SwordDamageDealer>();
            Debug.Log($"🔍 Recherche dans parent: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        }
        
        if (sword != null)
        {
            Debug.Log($"⚔️ SwordDamageDealer trouvé! Test CanDealDamage()...");
            if (sword.CanDealDamage())
            {
                Debug.Log("✅✅✅ Coup valide! TakeHit() appelé");
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                TakeHit(hitPoint);
                sword.OnHitConfirmed();
                
                // Repousser le joueur
                KnockbackPlayer(hitPoint);
            }
            else
            {
                Debug.Log("❌ CanDealDamage() = false");
            }
        }
        else
        {
            Debug.Log("❌ Aucun SwordDamageDealer trouvé sur l'objet en trigger");
        }
    }
    
    /// <summary>
    /// Détecte les collisions avec l'épée (sur le parent SlimeBoss directement)
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔔🔔🔔 SLIMEBOSS OnCollisionEnter APPELÉ!");
        Debug.Log($"   └─ Objet collision: {collision.gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {collision.gameObject.tag}");
        Debug.Log($"   └─ Contact points: {collision.contacts.Length}");
        
        if (currentPhase == BossPhase.Dead)
        {
            Debug.Log("❌ Boss déjà mort, ignoré");
            return;
        }
        
        SwordDamageDealer sword = collision.gameObject.GetComponent<SwordDamageDealer>();
        Debug.Log($"🔍 Recherche SwordDamageDealer sur {collision.gameObject.name}: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        
        if (sword == null)
        {
            sword = collision.gameObject.GetComponentInParent<SwordDamageDealer>();
            Debug.Log($"🔍 Recherche dans parent: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        }
        
        if (sword != null)
        {
            Debug.Log($"⚔️ SwordDamageDealer trouvé! Test CanDealDamage()...");
            if (sword.CanDealDamage())
            {
                Debug.Log("✅✅✅ Coup valide! TakeHit() appelé");
                Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                TakeHit(hitPoint);
                sword.OnHitConfirmed();
                
                // Repousser le joueur
                KnockbackPlayer(hitPoint);
            }
            else
            {
                Debug.Log("❌ CanDealDamage() = false");
            }
        }
        else
        {
            Debug.Log("❌ Aucun SwordDamageDealer trouvé sur l'objet en collision");
        }
    }
    
    /// <summary>
    /// Détecte les triggers avec l'épée
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔔🔔🔔 SLIMEBOSS OnTriggerEnter APPELÉ!");
        Debug.Log($"   └─ Objet trigger: {other.gameObject.name}");
        Debug.Log($"   └─ Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        Debug.Log($"   └─ Tag: {other.gameObject.tag}");
        Debug.Log($"   └─ IsTrigger: {other.isTrigger}");
        
        if (currentPhase == BossPhase.Dead)
        {
            Debug.Log("❌ Boss déjà mort, ignoré");
            return;
        }
        
        SwordDamageDealer sword = other.gameObject.GetComponent<SwordDamageDealer>();
        Debug.Log($"🔍 Recherche SwordDamageDealer sur {other.gameObject.name}: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        
        if (sword == null)
        {
            sword = other.gameObject.GetComponentInParent<SwordDamageDealer>();
            Debug.Log($"🔍 Recherche dans parent: {(sword != null ? "TROUVÉ" : "NON TROUVÉ")}");
        }
        
        if (sword != null)
        {
            Debug.Log($"⚔️ SwordDamageDealer trouvé! Test CanDealDamage()...");
            if (sword.CanDealDamage())
            {
                Debug.Log("✅✅✅ Coup valide! TakeHit() appelé");
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                TakeHit(hitPoint);
                sword.OnHitConfirmed();
                
                // Repousser le joueur
                KnockbackPlayer(hitPoint);
            }
            else
            {
                Debug.Log("❌ CanDealDamage() = false");
            }
        }
        else
        {
            Debug.Log("❌ Aucun SwordDamageDealer trouvé sur l'objet en trigger");
        }
    }
    
    void OnDrawGizmos()
    {
        // Dessiner une sphère pour visualiser la zone du boss
        Gizmos.color = currentPhase == BossPhase.Phase2_Red_Normal && isPhase2Immune ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
