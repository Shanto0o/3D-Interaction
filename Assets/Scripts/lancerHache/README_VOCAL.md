# 🎤 Guide Complet - Reconnaissance Vocale pour Rappel de Hache

## ⚡ Configuration Rapide (Recommandé - Quest Standalone)

### Étapes principales:
1. **Installer Meta Voice SDK** dans Unity
2. **Créer app Wit.ai** avec intent `recall_axe`
3. **Créer AppVoiceExperience** dans la scène
4. **Configurer WitAxeRecall + VoiceActivationButton**
5. **Tester**: Bouton B → Dire "hache" → Profit! ⚔️

---

## 📋 Installation Détaillée

### 1️⃣ Installer Meta Voice SDK

**Package Manager:**
1. Window → Package Manager
2. `+` → Add package from git URL
3. Collez: `https://github.com/wit-ai/wit-unity.git`
4. Attendez l'installation

### 2️⃣ Créer l'app Wit.ai

1. Allez sur **https://wit.ai**
2. Connectez-vous (compte Meta/Facebook)
3. **Create New App** → Nommez: "AxeRecall"
4. **Understanding → Intents → Create Intent**:
   - Nom: `recall_axe`
   - Ajoutez ces phrases d'entraînement:
     ```
     hache
     rappelle la hache
     reviens hache
     axe
     retour
     viens ici
     rappelle
     ```
5. **Cliquez "Train"** (en haut à droite, attendez 30-60s)
6. **Settings → Server Access Token** → Copiez le token

### 3️⃣ Setup Unity - AppVoiceExperience

1. **Hierarchy → Clic droit → GameObject → Voice SDK → App Voice Experience**

2. Sélectionnez `AppVoiceExperience`:
   - **Runtime Configuration → Wit Configuration**:
     - Créez: `Assets → Create → Voice SDK → Values → Wit Configuration`
     - Nommez: `AxeRecallConfig`
     - **Collez votre Server Access Token** (de wit.ai)
   - Assignez `AxeRecallConfig` à AppVoiceExperience
   - ✅ Enable Console Logging
   - ✅ Send Transcription Events For Messages

### 4️⃣ Créer VoiceManager

1. **Hierarchy → Create Empty** → Nommez: `VoiceManager`

2. **Ajoutez WitAxeRecall.cs**:
   - Voice Experience: `AppVoiceExperience` (depuis scène)
   - Recall Intent Name: `recall_axe`
   - Min Confidence: `0.5`
   - Axe: Votre hache (avec XRThrowableWeapon.cs)
   - Right Hand: OVRHand droite
   - Right Hand Transform: Transform de la main
   - Recall Speed: `15`
   - Auto Catch Distance: `0.3`
   - Magnetic Force: `50`
   - ✅ Show Debug Info
   - ✅ Show Transcription

3. **Ajoutez VoiceActivationButton.cs** (même GameObject):
   - Wit Axe Recall: Auto-détecté
   - Activation Button: `Two` (Bouton Y/B)
   - Controller: `RTouch`
   - Push To Talk: ❌ (mode toggle)
   - ✅ Show Debug Info

### 5️⃣ Permissions Android (Quest)

**Project Settings → Player → Android → Other Settings:**
- ✅ Microphone dans Permissions

**Custom Main Manifest** (si demandé):
```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.INTERNET" />
```

### 6️⃣ Test Final

**Dans Unity Editor:**
1. Play
2. Appuyez sur Y/B (ou simulez)
3. Parlez: "hache"
4. Regardez la Console pour logs

**Sur Quest:**
1. Build & Run
2. Lancez la hache loin
3. **Appuyez sur Bouton B** (petite vibration)
4. **Dites "hache"** ou "rappelle la hache"
5. La hache vole vers vous! 🪓→✋

---

## 🎛️ Paramètres Ajustables

### WitAxeRecall

| Paramètre | Défaut | Description |
|-----------|--------|-------------|
| Min Confidence | 0.5 | Seuil de confiance (0-1) |
| Recall Speed | 15 | Vitesse de retour |
| Auto Catch Distance | 0.3 | Distance d'attraction auto (m) |
| Magnetic Force | 50 | Force d'attraction |
| Continuous Listening | false | Écoute en continu |
| Listening Interval | 2.0 | Délai entre écoutes (s) |

### VoiceActivationButton

| Paramètre | Options | Description |
|-----------|---------|-------------|
| Activation Button | One/Two/Three/Four | Bouton du controller |
| Controller | LTouch/RTouch/Any | Controller à utiliser |
| Push To Talk | true/false | Maintenir vs Toggle |

---

## 🐛 Dépannage

### ❌ "AppVoiceExperience non trouvé"
→ Créez-le via: GameObject → Voice SDK → App Voice Experience

### ❌ "Intent non détecté"
→ Vérifiez:
- Intent name = `recall_axe` (exactement)
- Modèle Wit.ai "Trained"
- Server Access Token correct
- Connexion internet active

### ❌ "Confiance trop faible"
→ Réduisez Min Confidence (ex: 0.3)  
→ Ajoutez plus d'exemples sur wit.ai  
→ Parlez plus clairement

### ❌ La hache ne revient pas
→ Vérifiez:
- Right Hand Transform assigné
- Hache a un Rigidbody
- Magnetic Force assez élevé (50+)

### ❌ Pas de son / micro ne fonctionne pas
→ Quest: Permissions micro activées  
→ Test micro dans système Quest  
→ Rebuild avec permissions

---

## 💡 Options Avancées

### Écoute Continue (Toujours Active)

Dans WitAxeRecall:
- ✅ **Continuous Listening**
- **Listening Interval**: `2` (écoute toutes les 2 secondes)

⚠️ Consomme plus de batterie et données!

### Mode Push-to-Talk

Dans VoiceActivationButton:
- ✅ **Push To Talk**
- **Maintenez** le bouton pendant que vous parlez
- Relâchez pour traiter

### Indicateur Visuel

Créez une sphère lumineuse:
1. Créez Sphere → Scale 0.1
2. Material émissif
3. Assignez à VoiceActivationButton → **Listening Indicator**
4. S'allume pendant l'écoute! 💡

### Cooldown (Éviter spam)

Ajoutez dans WitAxeRecall.cs:
```csharp
private float lastRecallTime = 0f;
public float cooldownTime = 5f;

void StartRecall()
{
    if (Time.time - lastRecallTime < cooldownTime)
    {
        Debug.Log("⏳ Cooldown actif!");
        return;
    }
    lastRecallTime = Time.time;
    // ... reste du code existant
}
```

### Distance Maximale

```csharp
public float maxRecallDistance = 20f;

void StartRecall()
{
    float dist = Vector3.Distance(axe.transform.position, rightHandTransform.position);
    if (dist > maxRecallDistance)
    {
        Debug.Log("🚫 Hache trop loin!");
        return;
    }
    // ... reste du code
}
```

---

## 📊 Logs Utiles

### Ce que vous devriez voir:

```
✅ [WitAxeRecall] Initialisé avec AppVoiceExperience
🎤 [VoiceActivationButton] Prêt! Appuyez sur Two (RTouch) pour parler
🎤 [WitAxeRecall] Écoute démarrée...
🎙️ [WitAxeRecall] Écoute: 'hache'
🎙️ [WitAxeRecall] Transcription complète: 'hache'
🎤 [WitAxeRecall] Intent détecté: 'recall_axe' (Confiance: 95%)
✅ [WitAxeRecall] Intent 'recall_axe' confirmé! Rappel de la hache...
⚔️ [WitAxeRecall] Rappel de la hache!
✋ [WitAxeRecall] Hache attrapée!
```

---

## 🎮 Commandes Vocales Reconnues

Une fois configuré, ces phrases fonctionnent:
- "hache" ⭐
- "rappelle la hache"
- "reviens hache"
- "axe"
- "retour"
- "viens ici"
- "rappelle"

**Ajoutez les vôtres sur wit.ai!**

---

## 📝 Résumé Quick Start

```
1. Installez Meta Voice SDK
2. Créez app wit.ai avec intent "recall_axe"
3. GameObject → Voice SDK → App Voice Experience
4. Configurez token wit.ai
5. Créez VoiceManager + WitAxeRecall + VoiceActivationButton
6. Assignez références (hache, main)
7. Build → Test → Bouton B → "hache" → WIN! 🎉
```

**Bon jeu et bon rappel de hache! 🪓⚡**
