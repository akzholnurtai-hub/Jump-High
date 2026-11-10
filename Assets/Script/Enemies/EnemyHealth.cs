using System.Collections;
using UnityEngine;

// Player bullets (Bullet.cs, trigger collider) damage the enemy.
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 3;
    [SerializeField] AudioClip hitClip;
    [SerializeField] AudioClip deathClip;

    int hp;
    Vector3 baseScale;

    void Awake()
    {
        hp = maxHp;
        baseScale = transform.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Bullet>() == null) return;
        Destroy(other.gameObject);
        TakeHit();
    }

    public void TakeHit()
    {
        hp--;
        if (hp <= 0)
        {
            if (deathClip != null) AudioSource.PlayClipAtPoint(deathClip, transform.position);
            Destroy(gameObject);
            return;
        }
        if (hitClip != null) AudioSource.PlayClipAtPoint(hitClip, transform.position);
        StopAllCoroutines();
        StartCoroutine(Squash());
    }

    IEnumerator Squash()          // short "I got hit" feedback
    {
        transform.localScale = new Vector3(baseScale.x * 1.2f, baseScale.y * 0.8f, baseScale.z * 1.2f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale = baseScale;
    }
}
