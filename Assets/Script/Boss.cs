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
    public AudioClip hitClip;
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

        if (hitClip != null)
            AudioSource.PlayClipAtPoint(hitClip, transform.position);

        if (hp <= 0)
        {
            dead = true;
            StopAllCoroutines();
            if (LevelRunner.Current != null)
            {
                LevelRunner.Current.AddScore(S.killScore);
                LevelRunner.Current.Complete();      // score, save, then the Win scene
            }
            else UnityEngine.SceneManagement.SceneManager.LoadScene(GameConfig.Get().winScene);
        }
    }
}
