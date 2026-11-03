using UnityEngine;

public class Shooter : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float cooldown = 0.3f;
    float nextShot;
    public AudioClip shootClip;

    public void Shoot(Vector3 dir)
    {
        if (Time.time < nextShot) return;
        nextShot = Time.time + cooldown;

        Vector3 pos = transform.position + dir * 0.8f;
        Instantiate(bulletPrefab, pos, Quaternion.LookRotation(dir));
        AudioSource.PlayClipAtPoint(shootClip, pos);
    }
}