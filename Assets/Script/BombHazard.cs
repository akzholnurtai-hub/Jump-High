using UnityEngine;

// Falls, waits (fuse), explodes. Numbers in ItemValues.
public class BombHazard : Hazard
{
    const float FxLifetime = 3f;         // s the explosion effect object lives (visual only)

    public GameObject explosionPrefab;
    public AudioClip explodeClip;

    bool armed;

    protected override int Damage => Items.bombDamage;
    protected override float LifeTime => Items.pickupLifetime;

    protected override void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("Player"))
        {
            base.OnCollisionEnter(c);
            PlayFx();
            return;
        }

        if (!armed)
        {
            armed = true;
            Invoke(nameof(Explode), Items.bombFuse);
        }
    }

    void Explode()
    {
        if (Items.bombBlastRadius > 0f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Items.bombBlastRadius);
            foreach (Collider col in hits)
            {
                if (!col.CompareTag("Player")) continue;
                Health h = col.GetComponentInParent<Health>();
                if (h != null) { h.TakeDamage(Items.bombBlastDamage); break; }
            }
        }
        PlayFx();
        Destroy(gameObject);
    }

    void PlayFx()
    {
        if (explosionPrefab != null)
        {
            GameObject fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(fx, FxLifetime);
        }
        if (explodeClip != null)
            AudioSource.PlayClipAtPoint(explodeClip, transform.position);
    }
}
