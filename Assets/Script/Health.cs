using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Max HP and the blink time come from PlayerStats, the death height from LevelData.
public class Health : MonoBehaviour
{
    public int hp;                       // current HP (HUDFx watches it)
    public int MaxHp => P.maxHp;

    static PlayerStats P => GameConfig.Get().player;

    bool invincible;
    bool dead;
    Renderer[] renderers;
    HUD hud;

    void Start()
    {
        hp = MaxHp;

        renderers = GetComponentsInChildren<Renderer>();

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
        AudioManager.Play(Sfx.Hurt);

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
        AudioManager.Play(Sfx.Heal);

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
        AudioManager.Play(Sfx.Lose);    // AudioManager lives between scenes, so the reload does not cut it
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}