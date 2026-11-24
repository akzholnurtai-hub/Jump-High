using System.Collections;
using UnityEngine;

// Player bullets (Bullet.cs, trigger collider) damage the enemy.
// Max HP and the kill score come from the enemy's EnemyStats asset.
public class EnemyHealth : MonoBehaviour
{
    // short "I got hit" squash, visual only
    const float SquashTime = 0.1f;                    // s
    static readonly Vector3 SquashScale = new Vector3(1.2f, 0.8f, 1.2f);

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
            AudioManager.PlayAt(Sfx.EnemyDeath, transform.position);
            if (stats != null && LevelRunner.Current != null) LevelRunner.Current.AddScore(stats.killScore);
            if (LevelRunner.Current != null) LevelRunner.Current.MonsterKilled();
            StopAllCoroutines();
            StartCoroutine(FallAndDie());
            return;
        }
        AudioManager.PlayAt(Sfx.EnemyHit, transform.position);
        StopAllCoroutines();
        StartCoroutine(Squash());
    }

    // Shot dead: stop thinking, fall over backwards (faster and faster, like gravity),
    // lie a moment, sink into the floor, then disappear. Visual only, so the numbers stay here.
    IEnumerator FallAndDie()
    {
        const float FallTime = 0.45f, LieTime = 1.2f, SinkTime = 1f, SinkDepth = 1.2f;

        EnemyBase ai = GetComponent<EnemyBase>();
        if (ai != null) ai.Die();
        foreach (EnemyAnimator a in GetComponentsInChildren<EnemyAnimator>()) a.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (LineRenderer l in GetComponentsInChildren<LineRenderer>()) l.enabled = false;
        if (TryGetComponent(out UnityEngine.AI.NavMeshAgent agent)) agent.enabled = false;
        if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        transform.localScale = baseScale;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.AngleAxis(-90f, transform.right) * start;   // top goes backwards
        for (float t = 0f; t < FallTime; t += Time.deltaTime)
        {
            float k = t / FallTime;
            transform.rotation = Quaternion.Slerp(start, end, k * k);
            yield return null;
        }
        transform.rotation = end;
        yield return new WaitForSeconds(LieTime);

        Vector3 from = transform.position;
        for (float t = 0f; t < SinkTime; t += Time.deltaTime)
        {
            transform.position = from + Vector3.down * SinkDepth * (t / SinkTime);
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Squash()
    {
        transform.localScale = Vector3.Scale(baseScale, SquashScale);
        yield return new WaitForSeconds(SquashTime);
        transform.localScale = baseScale;
    }
}
