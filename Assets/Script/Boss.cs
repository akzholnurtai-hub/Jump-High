using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Boss: fan of fireballs, then a rock from the sky on the player's spot. Faster in phase 2.
// Every number is in BossStats (GameConfig > boss).
public class Boss : MonoBehaviour
{
    public Slider hpBar;
    public Transform player;
    public Transform firePoint;
    public GameObject fireballPrefab;
    public GameObject warningPrefab;
    public GameObject rockPrefab;
    public float groundY;                // world Y of the arena floor (map layout)
    public Animator anim;

    static BossStats S => GameConfig.Get().boss;

    int hp;
    bool dead;

    void Start()
    {
        hp = S.maxHp;
        hpBar.maxValue = S.maxHp;
        hpBar.value = hp;

        if (anim != null) anim.SetBool("Grounded", true);

        StartCoroutine(AttackLoop());
    }

    void Update()
    {
        if (dead) return;
        Vector3 look = player.position - transform.position;
        look.y = 0;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(look);
    }

    bool SecondPhase()
    {
        return hp <= S.maxHp * S.phase2At;
    }

    float Pause()
    {
        return SecondPhase() ? S.pause * S.phase2PauseScale : S.pause;
    }

    // The boss falls over backwards like the other monsters, then the level is won.
    IEnumerator FallThenWin()
    {
        const float FallTime = 0.8f, LieTime = 0.8f;
        if (anim != null) anim.enabled = false;
        foreach (Hazard h in GetComponents<Hazard>()) h.enabled = false;

        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.AngleAxis(-90f, transform.right) * start;
        for (float t = 0f; t < FallTime; t += Time.deltaTime)
        {
            float k = t / FallTime;
            transform.rotation = Quaternion.Slerp(start, end, k * k);
            yield return null;
        }
        transform.rotation = end;
        yield return new WaitForSeconds(LieTime);

        if (LevelRunner.Current != null)
        {
            LevelRunner.Current.AddScore(S.killScore);
            LevelRunner.Current.Complete();      // score, save, then the Win scene
        }
        else UnityEngine.SceneManagement.SceneManager.LoadScene(GameConfig.Get().winScene);
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(S.startDelay);

        while (!dead)
        {
            yield return StartCoroutine(FanAttack());
            yield return new WaitForSeconds(Pause());

            yield return StartCoroutine(RainAttack());
            yield return new WaitForSeconds(Pause());
        }
    }

    IEnumerator FanAttack()
    {
        // wind-up: a low growl first, the fireballs come fanWarnTime later
        AudioManager.PlayAt(Sfx.BossWindUp, transform.position);
        yield return new WaitForSeconds(S.fanWarnTime);

        int count = SecondPhase() ? S.fanCountPhase2 : S.fanCount;
        float step = S.fanAngleStep;
        float start = -step * (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            Quaternion rot = transform.rotation * Quaternion.Euler(0, start + step * i, 0);
            GameObject go = Instantiate(fireballPrefab, firePoint.position, rot);
            Fireball fb = go.GetComponent<Fireball>();
            if (fb != null) fb.speed = S.fireballSpeed;
        }
        yield return new WaitForSeconds(S.fanRecover);
    }

    IEnumerator RainAttack()
    {
        Vector3 pos = new Vector3(player.position.x, groundY, player.position.z);

        GameObject mark = Instantiate(warningPrefab, pos, Quaternion.identity);
        AudioManager.PlayAt(Sfx.EnemyWindUp, pos);          // the red mark and the sound come together
        yield return new WaitForSeconds(S.rainWarnTime);
        Destroy(mark);

        Instantiate(rockPrefab, pos + Vector3.up * S.rockDropHeight, Quaternion.identity);
    }

    void OnTriggerEnter(Collider other)
    {
        if (dead) return;
        if (other.GetComponent<Bullet>() == null) return;

        Destroy(other.gameObject);
        hp--;
        hpBar.value = hp;

        if (hp <= 0)
        {
            dead = true;
            AudioManager.PlayAt(Sfx.EnemyDeath, transform.position);
            StopAllCoroutines();
            StartCoroutine(FallThenWin());
            return;
        }
        AudioManager.PlayAt(Sfx.EnemyHit, transform.position);
    }
}
