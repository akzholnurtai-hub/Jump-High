using UnityEngine;

public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public bool destroyOnHit = true;
    public float lifeTime = 15f;

    protected virtual void Start()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
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
        player.TakeDamage(damage);
    }
}