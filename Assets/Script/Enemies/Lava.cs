using UnityEngine;

// Lava floor: hurts while you stand in it (Health's invincibility limits the hits)
// and throws the player up when they step in. Numbers in ItemValues.
public class Lava : MonoBehaviour
{
    static ItemValues Items => GameConfig.Get().items;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null) rb.linearVelocity = new Vector3(rb.linearVelocity.x, Items.lavaBounce, rb.linearVelocity.z);
        Hurt(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) Hurt(other);
    }

    void Hurt(Collider other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null) h.TakeDamage(Items.lavaDamage);
    }
}
