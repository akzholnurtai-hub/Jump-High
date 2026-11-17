using UnityEngine;

// Player bullet. Speed and life time are given by Shooter from PlayerStats.
public class Bullet : MonoBehaviour
{
    [System.NonSerialized] public float speed;
    [System.NonSerialized] public float lifeTime;

    void Start()
    {
        PlayerStats p = GameConfig.Get().player;
        if (speed <= 0f) speed = p.bulletSpeed;          // placed by hand, not by Shooter
        if (lifeTime <= 0f) lifeTime = p.bulletLifetime;

        Launch(transform.forward);
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 direction)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = direction.normalized * speed;
    }
}
