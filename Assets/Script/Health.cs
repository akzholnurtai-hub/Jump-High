using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Max HP and the blink time come from PlayerStats, the death height from LevelData.
public class Health : MonoBehaviour
{
    public int hp;                       // current HP (HUDFx watches it)
    public int MaxHp => P.maxHp;

    static PlayerStats P => GameConfig.Get().player;

    public AudioClip hurtClip;
    public AudioClip healClip;

    bool invincible;
    bool dead;
    Renderer[] renderers;
    AudioSource src;
    HUD hud;

    void Start()
    {
        hp = MaxHp;

        renderers = GetComponentsInChildren<Renderer>();
        src = GetComponent<AudioSource>();

        hud = FindAnyObjectByType<HUD>();
        if (hud != null) hud.SetHearts(hp);

        Debug.Log("HP: " + hp);
    }

    void Update()
    {
        LevelData level = LevelRunner.CurrentLevel;
        if (level != null && transform.position.y < level.killHeight)
            GameOver();
    }

    public void TakeDamage(int dmg)
    {
        if (invincible) return;

        hp -= dmg;
        if (hp < 0) hp = 0;
        Debug.Log("OUCH! HP: " + hp);
        if (src != null && hurtClip != null) src.PlayOneShot(hurtClip);

        if (hud != null) hud.SetHearts(hp);

        if (hp <= 0)
        {
            GameOver();
            return;
        }
        StartCoroutine(Invincibility());
    }

    public void Heal(int amount)
    {
        hp = Mathf.Min(hp + amount, MaxHp);
        Debug.Log("HEAL! HP: " + hp);
        if (src != null && healClip != null) src.PlayOneShot(healClip);

        if (hud != null) hud.SetHearts(hp);
    }

    IEnumerator Invincibility()
    {
        invincible = true;
        bool visible = true;
        float t = 0f;
        while (t < P.invincibleTime)
        {
            visible = !visible;
            SetVisible(visible);
            float step = P.blinkInterval > 0f ? P.blinkInterval : P.invincibleTime;   // no endless loop on a 0
            yield return new WaitForSeconds(step);
            t += step;
        }
        SetVisible(true);
        invincible = false;
    }

    void SetVisible(bool v)
    {
        foreach (Renderer r in renderers)
            if (r != null) r.enabled = v;
    }

    void GameOver()
    {
        if (dead) return;               // count one death, not one per frame
        dead = true;
        Debug.Log("GAME OVER");
        SaveSystem.AddDeath();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}