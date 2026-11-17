using UnityEngine;

// Fireball of the Shooter enemy and the Boss. They set the speed from their own stats;
// if nobody does, ItemValues.fireballSpeed is used.
// The collider is a trigger (so it can hurt the player), and a trigger flies through everything.
// So the fireball checks for solid objects itself: a wall / pillar / island in its way destroys it.
public class Fireball : Hazard
{
    [System.NonSerialized] public float speed;

    protected override int Damage => Items.fireballDamage;
    protected override float LifeTime => Items.fireballLifetime;

    Rigidbody rb;
    float radius;

    protected override void Start()
    {
        base.Start();
        if (speed <= 0f) speed = Items.fireballSpeed;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = transform.forward * speed;

        SphereCollider sc = GetComponent<SphereCollider>();
        radius = sc != null ? sc.radius * transform.lossyScale.x : 0f;
    }

    // A fast ball can jump over a thin wall between two physics steps,
    // so look ahead along the path every step.
    void FixedUpdate()
    {
        if (rb == null) return;
        Vector3 v = rb.linearVelocity;
        float dist = v.magnitude * Time.fixedDeltaTime;
        if (dist <= 0f) return;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, v / v.magnitude, dist + radius,
                                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (IsBlocking(h.collider)) { Destroy(gameObject); return; }
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        base.OnTriggerEnter(other);                             // hurts the player
        if (this != null && IsBlocking(other)) Destroy(gameObject);
    }

    // Solid level geometry. Not the player (OnTriggerEnter hurts him), not the shooter itself,
    // not other hazards, not the floor under the ball.
    bool IsBlocking(Collider c)
    {
        if (c.isTrigger) return false;
        if (c.CompareTag("Player") || c.GetComponentInParent<Health>() != null) return false;
        if (c.GetComponentInParent<Boss>() != null) return false;
        if (c.GetComponentInParent<EnemyBase>() != null) return false;
        if (c.GetComponentInParent<Hazard>() != null) return false;
        return c.bounds.max.y > transform.position.y;           // floor below the ball does not stop it
    }
}
