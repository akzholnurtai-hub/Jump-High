using UnityEngine;

// Anything that hurts the player on touch. A plain Hazard is a trap in the level
// (spikes, moving blocks, spin bars): trap damage, never disappears.
// Rock / Heart / BombHazard / Fireball change Damage and LifeTime - the numbers are in ItemValues.
public class Hazard : MonoBehaviour
{
    public bool destroyOnHit = true;

    protected static ItemValues Items => GameConfig.Get().items;

    protected virtual int Damage => Items.trapDamage;
    protected virtual float LifeTime => 0f;          // 0 = stays in the level

    protected virtual void Start()
    {
        if (LifeTime > 0f)
            Destroy(gameObject, LifeTime);
    }

    protected virtual void OnCollisionEnter(Collision c)
    {
        TryHit(c.gameObject);
    }

    protected virtual void OnCollisionStay(Collision c)
    {
        if (!destroyOnHit)
            TryHit(c.gameObject);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        TryHit(other.gameObject);
    }

    void TryHit(GameObject other)
    {
        if (!other.CompareTag("Player")) return;

        Health h = other.GetComponent<Health>();
        if (h == null) h = other.GetComponentInParent<Health>();
        if (h == null) return;

        OnHitPlayer(h);

        if (destroyOnHit)
            Destroy(gameObject);
    }

    protected virtual void OnHitPlayer(Health player)
    {
        player.TakeDamage(Damage);
    }
}
