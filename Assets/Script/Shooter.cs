using System.Collections;
using UnityEngine;

// The player's gun. Pattern, cooldown and bullet numbers come from PlayerStats.
public class Shooter : MonoBehaviour
{
    public enum Pattern { Single, Spread, Burst }

    const float MuzzleOffset = 0.8f;     // m in front of the body, so the bullet does not hit the player

    public GameObject bulletPrefab;
    public AudioClip shootClip;

    static PlayerStats P => GameConfig.Get().player;

    float nextShot;

    public void Shoot(Vector3 dir)
    {
        if (Time.time < nextShot) return;
        nextShot = Time.time + P.fireCooldown;

        switch (P.firePattern)
        {
            case Pattern.Single:
                Spawn(dir);
                break;

            case Pattern.Spread:
                float start = -P.spreadAngle * (P.spreadCount - 1) / 2f;
                for (int i = 0; i < P.spreadCount; i++)
                    Spawn(Quaternion.Euler(0f, start + P.spreadAngle * i, 0f) * dir);
                break;

            case Pattern.Burst:
                StartCoroutine(Burst());
                return;                      // Burst plays its own sounds
        }
        PlaySound();
    }

    IEnumerator Burst()
    {
        for (int i = 0; i < P.burstCount; i++)
        {
            Spawn(transform.forward);        // follows the player's turn during the burst
            PlaySound();
            yield return new WaitForSeconds(P.burstDelay);
        }
    }

    void Spawn(Vector3 dir)
    {
        Vector3 pos = transform.position + dir * MuzzleOffset;
        GameObject go = Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        Bullet b = go.GetComponent<Bullet>();
        if (b != null)
        {
            b.speed = P.bulletSpeed;         // set before the bullet's Start() runs
            b.lifeTime = P.bulletLifetime;
        }
    }

    void PlaySound()
    {
        if (shootClip != null) AudioSource.PlayClipAtPoint(shootClip, transform.position);
    }
}
