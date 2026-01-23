using UnityEngine;

/// <summary>
/// ScriptableObject pour définir les objets que l'utilisateur peut placer dans la scène
/// </summary>
[CreateAssetMenu(fileName = "PlaceableObject", menuName = "VR Resume/Placeable Object", order = 1)]
public class PlaceableObjectData : ScriptableObject
{
    [Header("Object Info")]
    public string objectName = "New Object";
    [TextArea(2, 4)]
    public string description = "Description of the object";
    
    [Header("Prefab")]
    public GameObject prefab;
    
    [Header("Visual")]
    public Sprite icon; // Icône pour le menu
    
    [Header("Category")]
    public ObjectCategory category = ObjectCategory.Frame;
    
    [Header("Default Settings")]
    public Vector3 defaultScale = Vector3.one;
    public bool canHaveMedia = true; // Peut-on associer une image/vidéo?
}

public enum ObjectCategory
{
    Frame,          // Cadre photo
    VideoScreen,    // Écran vidéo
    Trophy,         // Trophée
    Decoration,     // Décoration
    Furniture,      // Meuble
    Other          // Autre
}
