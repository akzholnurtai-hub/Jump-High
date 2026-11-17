using System.Collections;
using UnityEngine;

// Player bullets (Bullet.cs, trigger collider) damage the enemy.
// Max HP and the kill score come from the enemy's EnemyStats asset.
public class EnemyHealth : MonoBehaviour
{
    // short "I got hit" squash, visual only
    const float SquashTime = 0.1f;                    // s
    static readonly Vector3 SquashScale = new Vector3(1.2f, 0.8f, 1.2f);

    [SerializeField] AudioClip hitClip;
    [SerializeField] AudioClip deathClip;

    EnemyStats stats;
    int hp;
    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    // Start, not Awake: LevelRunner gives the enemy its stats between the two.
    void Start()
    {
        EnemyBase enemy = GetComponent<EnemyBase>();
        stats = enemy != null ? enemy.Stats : null;
        hp = stats != null ? stats.maxHp : 1;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Bullet>() == null) return;
        Destroy(other.gameObject);
        TakeHit();
    }

    public void TakeHit()
    {
        if (hp <= 0) return;                          // already dying (two Spread bullets in one frame)
        hp--;
        if (hp <= 0)
        {
            if (deathClip != null) AudioSource.PlayClipAtPoint(deathClip, transform.position);
            if (stats != null && LevelRunner.Current != null) LevelRunner.Current.AddScore(stats.killScore);
            Destroy(gameObject);
            return;
        }
        if (hitClip != null) AudioSource.PlayClipAtPoint(hitClip, transform.position);
        StopAllCoroutines();
        StartCoroutine(Squash());
    }

    IEnumerator Squash()
    {
        transform.localScale = Vector3.Scale(baseScale, SquashScale);
        yield return new WaitForSeconds(SquashTime);
        transform.localScale = baseScale;
    }
}
