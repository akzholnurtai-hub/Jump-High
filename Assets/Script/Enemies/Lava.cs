using UnityEngine;

// Lava floor: hurts while you stand in it (Health's invincibility limits it to one hit per 2 s)
// and throws the player up when they step in.
public class Lava : MonoBehaviour
{
    public int damage = 1;
    public float bounce = 6f;           // m/s upward kick

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null) rb.linearVelocity = new Vector3(rb.linearVelocity.x, bounce, rb.linearVelocity.z);
        Hurt(other);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) Hurt(other);
    }

    void Hurt(Collider other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null) h.TakeDamage(damage);
    }
}
