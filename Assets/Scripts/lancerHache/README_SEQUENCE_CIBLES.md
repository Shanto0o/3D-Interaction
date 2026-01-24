# Guide de Configuration - Système de Séquence de Cibles

## Aperçu
Ce système permet de créer un jeu où les cibles doivent être touchées dans un ordre précis (2-1-4-3).
Lorsque l'ordre est respecté, une animation "BRAVO" apparaît.

## Configuration dans Unity

### Étape 1 : Créer le GameObject Manager
1. Créez un GameObject vide : `GameObject > Create Empty`
2. Nommez-le "TargetSequenceManager"
3. Ajoutez le script `TargetSequenceManager.cs` sur ce GameObject

### Étape 2 : Préparer les Cibles
1. Assurez-vous que vos 4 cibles ont le script `Target.cs`
2. Sur chaque cible, définissez le `Target ID` :
   - Cible 1 : Target ID = 1
   - Cible 2 : Target ID = 2
   - Cible 3 : Target ID = 3
   - Cible 4 : Target ID = 4

### Étape 3 : Créer le Texte "BRAVO"
Avec TextMeshPro (recommandé) :
1. `GameObject > UI > Text - TextMeshPro` (Canvas sera créé automatiquement)
2. Nommez-le "BravoText"
3. Dans le composant TextMeshPro :
   - Text: "BRAVO !"
   - Font Size: 100-150
   - Alignment: Center
   - Color: Jaune ou Or
4. Positionnez le texte au centre de l'écran
5. **Important** : Le GameObject doit être désactivé par défaut (décochez en haut de l'Inspector)

### Étape 4 : Configurer le TargetSequenceManager
Dans l'Inspector du TargetSequenceManager :

**Sequence Settings:**
- Target Sequence: [2, 1, 4, 3] (ordre à respecter)

**Targets:**
- Size: 4
- Element 0: Glissez la Cible 1
- Element 1: Glissez la Cible 2
- Element 2: Glissez la Cible 3
- Element 3: Glissez la Cible 4

**Success Animation:**
- Bravo Object: Glissez le GameObject "BravoText" ici
- Bravo Display Duration: 3 secondes
- Animate Bravo: ✓ Coché
- Max Scale: 1.5

**Audio (Optionnel):**
- Correct Hit Sound: Son de succès quand bonne cible
- Wrong Hit Sound: Son d'erreur
- Bravo Sound: Son de victoire

**Visual Feedback:**
- Next Target Color: Jaune (couleur de la prochaine cible)
- Completed Target Color: Vert (cibles déjà touchées)
- Inactive Target Color: Gris (cibles non actives)
- Emission Intensity: 2

**Debug:**
- Show Debug Info: ✓ Coché pour voir les messages

## Ordre de Fonctionnement

1. **Au démarrage** :
   - La cible 2 brille en jaune (première à toucher)
   - Les autres sont grisées

2. **Quand la cible 2 est touchée** :
   - Elle devient verte
   - La cible 1 brille maintenant en jaune

3. **Quand la cible 1 est touchée** :
   - Elle devient verte
   - La cible 4 brille en jaune

4. **Quand la cible 4 est touchée** :
   - Elle devient verte
   - La cible 3 brille en jaune

5. **Quand la cible 3 est touchée** :
   - BRAVO s'affiche avec une animation
   - Son de victoire
   - Après 3 secondes, tout se réinitialise

## Si une mauvaise cible est touchée
- Son d'erreur
- Toutes les cibles se réinitialisent
- La séquence recommence à 0 (cible 2)

## Personnalisation

### Changer l'ordre des cibles
Dans l'Inspector du TargetSequenceManager, modifiez `Target Sequence` :
- Exemple : [1, 2, 3, 4] pour l'ordre normal
- Exemple : [4, 3, 2, 1] pour l'ordre inverse

### Changer la durée d'affichage du Bravo
Modifiez `Bravo Display Duration` (en secondes)

### Désactiver l'animation du texte
Décochez `Animate Bravo` - le texte apparaîtra/disparaîtra instantanément

### Affichage de Debug à l'écran
Avec `Show Debug Info` coché, vous verrez en haut à gauche :
- La progression (ex: "Progression : 2/4")
- L'ordre avec la cible actuelle en jaune

## Matériaux des Cibles

Pour que l'effet d'émission (glow) fonctionne bien :
1. Le matériau des cibles doit être un matériau Standard ou similaire
2. Il doit avoir la propriété "Emission"
3. Dans le matériau, activez "Emission" dans l'Inspector

## Compatibilité

- Compatible avec le système de flèches (ArrowDamage.cs)
- Compatible avec le système de hache
- Les cibles doivent implémenter l'interface `IHealth` (déjà le cas avec Target.cs)

## Dépannage

**Le texte Bravo ne s'affiche pas :**
- Vérifiez que le GameObject BravoText est bien assigné dans le manager
- Vérifiez que le Canvas contient le texte
- Vérifiez que la caméra voit le Canvas (Canvas Render Mode: Screen Space - Overlay)

**Les cibles ne changent pas de couleur :**
- Vérifiez que les cibles ont le script Target.cs
- Vérifiez que chaque cible a un Target ID unique (1, 2, 3, 4)
- Vérifiez que les cibles ont un Renderer

**L'ordre n'est pas respecté :**
- Vérifiez que les Target ID correspondent bien aux cibles
- Vérifiez l'ordre dans Target Sequence du manager
- Activez Show Debug Info pour voir les logs
