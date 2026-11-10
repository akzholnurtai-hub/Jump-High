using System.Collections;
using UnityEngine;

public class Shooter : MonoBehaviour
{
    public enum Pattern { Single, Spread, Burst }

    public GameObject bulletPrefab;
    public float cooldown = 0.3f;
    public AudioClip shootClip;

    [Header("Fire pattern")]
    public Pattern pattern = Pattern.Single;
    public int spreadCount = 3;          // bullets in the fan
    public float spreadAngle = 12f;      // degrees between bullets
    public int burstCount = 3;           // bullets in a burst
    public float burstDelay = 0.08f;     // s between burst bullets

    float nextShot;

    public void Shoot(Vector3 dir)
    {
        if (Time.time < nextShot) return;
        nextShot = Time.time + cooldown;

        switch (pattern)
        {
            case Pattern.Single:
                Spawn(dir);
                break;

            case Pattern.Spread:
                float start = -spreadAngle * (spreadCount - 1) / 2f;
                for (int i = 0; i < spreadCount; i++)
                    Spawn(Quaternion.Euler(0f, start + spreadAngle * i, 0f) * dir);
                break;

            case Pattern.Burst:
                StartCoroutine(Burst());
                return;                      // Burst plays its own sounds
        }
        PlaySound();
    }

    IEnumerator Burst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            Spawn(transform.forward);        // follows the player's turn during the burst
            PlaySound();
            yield return new WaitForSeconds(burstDelay);
        }
    }

    void Spawn(Vector3 dir)
    {
        Vector3 pos = transform.position + dir * 0.8f;
        Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
    }

    void PlaySound()
    {
        if (shootClip != null) AudioSource.PlayClipAtPoint(shootClip, transform.position);
    }
}
