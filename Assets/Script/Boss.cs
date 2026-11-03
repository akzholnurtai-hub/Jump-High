using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Boss : MonoBehaviour
{
    public int maxHp = 10;
    public Slider hpBar;
    public Transform player;
    public Transform firePoint;
    public GameObject fireballPrefab;
    public GameObject warningPrefab;
    public GameObject rockPrefab;
    public float groundY = 0f;
    public float pause = 2f;
    public AudioClip hitClip;
    public Animator anim;

    int hp;
    bool dead;

    void Start()
    {
        hp = maxHp;
        hpBar.maxValue = maxHp;
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
        return hp <= maxHp / 2;
    }

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(2f);

        while (!dead)
        {
            yield return StartCoroutine(FanAttack());
            yield return new WaitForSeconds(SecondPhase() ? pause / 2 : pause);

            yield return StartCoroutine(RainAttack());
            yield return new WaitForSeconds(SecondPhase() ? pause / 2 : pause);
        }
    }

    IEnumerator FanAttack()
    {
        int count = SecondPhase() ? 5 : 3;
        float step = 15f;
        float start = -step * (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            Quaternion rot = transform.rotation * Quaternion.Euler(0, start + step * i, 0);
            Instantiate(fireballPrefab, firePoint.position, rot);
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator RainAttack()
    {
        Vector3 pos = new Vector3(player.position.x, groundY, player.position.z);

        GameObject mark = Instantiate(warningPrefab, pos, Quaternion.identity);
        yield return new WaitForSeconds(1f);
        Destroy(mark);

        Instantiate(rockPrefab, pos + Vector3.up * 8f, Quaternion.identity);
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
            SceneManager.LoadScene("Win");
        }
    }
}
