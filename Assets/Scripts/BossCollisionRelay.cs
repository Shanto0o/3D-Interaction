using UnityEngine;

/// <summary>
/// Script à ajouter sur les colliders enfants du boss pour relayer les collisions au parent
/// </summary>
public class BossCollisionRelay : MonoBehaviour
{
    private SlimeBoss parentBoss;
    
    void Start()
    {
        // Trouver le SlimeBoss dans les parents
        parentBoss = GetComponentInParent<SlimeBoss>();
        
        if (parentBoss == null)
        {
            Debug.LogError($"❌ BossCollisionRelay sur [{gameObject.name}] : Aucun SlimeBoss trouvé dans les parents!");
        }
        else
        {
            Debug.Log($"✅ BossCollisionRelay configuré sur [{gameObject.name}] → Parent: {parentBoss.gameObject.name}");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔁 BossCollisionRelay: OnCollisionEnter avec [{collision.gameObject.name}] → Relayé au boss");
        
        if (parentBoss != null)
        {
            parentBoss.OnChildCollisionEnter(collision);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔁 BossCollisionRelay: OnTriggerEnter avec [{other.gameObject.name}] → Relayé au boss");
        
        if (parentBoss != null)
        {
            parentBoss.OnChildTriggerEnter(other);
        }
    }
}
