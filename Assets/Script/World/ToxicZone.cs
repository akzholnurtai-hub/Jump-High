using UnityEngine;

// A toxic pool: hurts the player for as long as they stand in it (trap damage from ItemValues).
// Health's invincibility time decides how often, so it is a hit about every second, not every frame.
[RequireComponent(typeof(Collider))]
public class ToxicZone : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Health h = other.GetComponentInParent<Health>();
        if (h != null) h.TakeDamage(GameConfig.Get().items.trapDamage);
    }
}
