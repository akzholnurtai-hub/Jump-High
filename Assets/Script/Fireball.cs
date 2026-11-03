using UnityEngine;

public class Fireball : Hazard
{
    public float speed = 8f;

    protected override void Start()
    {
        base.Start();

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = transform.forward * speed;
    }
}
