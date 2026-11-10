using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Little HUD animations. Watches values, so Movement/Health/HUD do not need changes:
//  - heart pops when you lose or gain HP, the last heart beats
//  - red screen flash when you get hit
//  - carrot icon bounces and the progress bar fills when you pick up a coin
//  - timer box turns red and pulses in the last 15 seconds
public class HUDFx : MonoBehaviour
{
    public RectTransform[] hearts;
    public Image damageFlash;
    public RectTransform carrotIcon;
    public TMP_Text coinText;
    public Image coinFill;               // Image Type = Filled, Horizontal
    public Image timeBox;
    public TMP_Text timerText;
    public float dangerTime = 15f;       // s

    Health health;
    int lastHp = -1;
    string lastCoins, lastTime;
    float[] heartPop;
    float iconPop;
    Color timeBoxColor;

    void Start()
    {
        health = FindAnyObjectByType<Health>();
        heartPop = new float[hearts != null ? hearts.Length : 0];
        if (timeBox != null) timeBoxColor = timeBox.color;
        if (damageFlash != null) damageFlash.color = new Color(1f, 0f, 0f, 0f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // ---- hearts ----
        if (health != null)
        {
            int hp = health.hp;
            if (lastHp >= 0 && hp != lastHp)
            {
                int idx = Mathf.Clamp(hp < lastHp ? hp : hp - 1, 0, heartPop.Length - 1);
                if (heartPop.Length > 0) heartPop[idx] = 1f;
                if (hp < lastHp && damageFlash != null) damageFlash.color = new Color(1f, 0f, 0f, 0.45f);
            }
            lastHp = hp;

            for (int i = 0; i < heartPop.Length; i++)
            {
                if (hearts[i] == null) continue;
                heartPop[i] = Mathf.Max(0f, heartPop[i] - dt * 3f);
                float s = 1f + Mathf.Sin(heartPop[i] * Mathf.PI) * 0.45f;
                if (hp == 1 && i == 0) s *= 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.15f;   // last heart beats
                hearts[i].localScale = Vector3.one * s;
            }
        }

        if (damageFlash != null && damageFlash.color.a > 0f)
            damageFlash.color = new Color(1f, 0f, 0f, Mathf.Max(0f, damageFlash.color.a - dt * 1.5f));

        // ---- coins ----
        if (coinText != null && coinText.text != lastCoins)
        {
            if (lastCoins != null) iconPop = 1f;
            lastCoins = coinText.text;
            string[] parts = lastCoins.Split('/');
            if (coinFill != null && parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out int got) && int.TryParse(parts[1].Trim(), out int total) && total > 0)
                coinFill.fillAmount = (float)got / total;
        }
        if (carrotIcon != null)
        {
            iconPop = Mathf.Max(0f, iconPop - dt * 2.5f);
            float k = Mathf.Sin(iconPop * Mathf.PI);
            carrotIcon.localScale = Vector3.one * (1f + k * 0.5f);
            carrotIcon.localEulerAngles = new Vector3(0, 0, Mathf.Sin(iconPop * 20f) * 20f * iconPop);
        }

        // ---- timer ----
        if (timerText != null && timeBox != null)
        {
            if (timerText.text != lastTime) lastTime = timerText.text;
            string digits = System.Text.RegularExpressions.Regex.Match(lastTime, @"\d+").Value;
            bool danger = int.TryParse(digits, out int sec) && sec <= dangerTime;
            if (danger)
            {
                float p = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                timeBox.color = Color.Lerp(new Color(0.6f, 0.05f, 0.05f, 0.9f), new Color(1f, 0.2f, 0.1f, 0.95f), p);
                timeBox.rectTransform.localScale = Vector3.one * (1f + p * 0.06f);
            }
            else
            {
                timeBox.color = timeBoxColor;
                timeBox.rectTransform.localScale = Vector3.one;
            }
        }
    }
}
