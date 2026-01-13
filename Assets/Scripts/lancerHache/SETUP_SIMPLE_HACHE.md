# 🪓 Rappel de Hache Ultra-Simple - Configuration en 5 Minutes

## 🎯 Résultat Final
**Dites "hache" à n'importe quel moment → La hache vient dans votre main. C'est tout.** ✨

---

## ✅ Étapes de Configuration

### 1. Installer Meta Voice SDK (Une seule fois)
```
Window → Package Manager → + → Add from git URL
https://github.com/wit-ai/wit-unity.git
```
Attendez l'installation (2-3 minutes)

### 2. Créer App Wit.ai (Une seule fois)
1. Allez sur https://wit.ai
2. Connectez-vous (compte Meta/Facebook)
3. **Create New App** → Nom: `"MonJeu"`
4. **Settings** (roue dentée) → Copiez le **Server Access Token**

**C'EST TOUT pour Wit.ai !** Pas besoin d'intent, pas besoin d'entraînement.

### 3. Setup Unity (5 minutes)

#### A. Créer AppVoiceExperience
1. **Hierarchy → Clic droit → GameObject → Voice SDK → App Voice Experience**
2. Sélectionnez `AppVoiceExperience`
3. **Runtime Configuration → Wit Configuration**:
   - Créez: `Assets → Create → Voice SDK → Values → Wit Configuration`
   - **Collez votre Server Access Token** (de wit.ai)
   - Assignez cette config
4. ✅ Cochez **Enable Console Logging**

#### B. Créer VoiceManager
1. **Hierarchy → Create Empty** → Nom: `VoiceManager`
2. **Ajoutez le script `WitAxeRecall.cs`**
3. **Configurez dans l'Inspector**:
   ```
   Voice Experience: [Glissez AppVoiceExperience]
   Axe: [Glissez votre hache avec XRThrowableWeapon.cs]
   Right Hand: [Glissez OVRHand droite ou RightHandAnchor]
   Right Hand Transform: [Même transform]
   
   ✅ Continuous Listening: TRUE (déjà coché par défaut)
   Listening Interval: 1.5
   Recall Cooldown: 3
   
   ✅ Show Debug Info: TRUE
   ✅ Show Transcription: TRUE
   ```

### 4. Permissions Android
**Edit → Project Settings → Player → Android → Other Settings**
- ✅ Cochez **Microphone**

### 5. Tester !
1. **Build & Run** sur Quest
2. Lancez la hache
3. **Dites "hache"** 🎤
4. La hache vole vers vous! ⚔️

---

## 🎤 Mots qui Fonctionnent

Par défaut, ces mots rappellent la hache:
- **"hache"** ⭐ (le plus simple)
- "axe"
- "rappelle"
- "reviens"

**Vous pouvez modifier la liste** dans `WitAxeRecall → Trigger Words`

---

## 🔧 C'est Tout !

Le système écoute **automatiquement en continu**.
- ✅ Pas de bouton à presser
- ✅ Pas d'intent à configurer sur wit.ai
- ✅ Juste dire "hache" et ça marche

---

## 🐛 Problèmes Courants

### ❌ La hache ne revient pas
**Vérifiez:**
1. AppVoiceExperience a le bon Server Access Token
2. Right Hand Transform est assigné
3. Internet actif sur le Quest
4. Permissions micro activées
5. Console Unity: voyez-vous "Entendu: hache" ?

### ❌ Rien ne se passe quand je parle
**Solutions:**
1. Regardez la Console Unity (logs en temps réel)
2. Testez votre micro Quest (Paramètres → Audio)
3. Vérifiez que `Continuous Listening` est ✅
4. Attendez 2-3 secondes après le lancement du jeu

### ❌ Trop de rappels / La hache revient trop souvent
**Augmentez le Recall Cooldown:**
- Dans WitAxeRecall → Recall Cooldown: `5` ou `10` secondes

---

## 💡 Paramètres Optionnels

### Changer l'intervalle d'écoute
Plus court = réactivité  
Plus long = économie batterie

```
Listening Interval: 1.5 (défaut, bon équilibre)
Listening Interval: 0.5 (très réactif, consomme plus)
Listening Interval: 3.0 (économise batterie)
```

### Ajouter vos propres mots
Dans `WitAxeRecall → Trigger Words`, ajoutez:
- "come back"
- "ici"
- "viens"
- etc.

### Distance de rappel
```
Magnetic Force: 50 (défaut)
Plus élevé = rappel plus rapide

Auto Catch Distance: 0.3 (défaut)
Plus élevé = attrape de plus loin
```

---

## 📊 Logs Normaux (Console Unity)

Quand tout fonctionne, vous devriez voir:
```
✅ [WitAxeRecall] Initialisé avec AppVoiceExperience
🔄 [WitAxeRecall] Écoute continue activée - Dites 'hache' à tout moment!
🎤 [WitAxeRecall] Écoute démarrée...
🎙️ [WitAxeRecall] Entendu: 'hache'
🎤 [WitAxeRecall] Transcription: 'hache'
✅ [WitAxeRecall] Mot déclencheur détecté: 'hache'
⚔️ [WitAxeRecall] Rappel de la hache!
✋ [WitAxeRecall] Hache attrapée!
```

---

## ✨ Résumé

**Vous:** 🗣️ "hache"  
**Le jeu:** 🪓→✋ (La hache vole vers votre main)

**C'est magique et ça marche à tout moment.** 🎮
