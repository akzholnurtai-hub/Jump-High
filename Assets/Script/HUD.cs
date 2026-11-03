using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("Жүректер (сол жақ жоғарыда)")]
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    [Header("Санауыш (оң жақ жоғарыда)")]
    public TextMeshProUGUI countText;

    [Header("Хабарлама (ортада)")]
    public TextMeshProUGUI messageText;
    public float messageTime = 2f;

    Coroutine messageRoutine;

    void Awake()
    {
        if (messageText != null) messageText.text = "";
    }

    public void SetHearts(int hp)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            bool full = i < hp;

            if (emptyHeart != null)
            {
                hearts[i].enabled = true;
                hearts[i].sprite = full ? fullHeart : emptyHeart;
            }
            else
            {
                hearts[i].enabled = full;
            }
        }
    }

    public void SetCount(int current, int total)
    {
        if (countText != null)
            countText.text = current + " / " + total;
    }

    public void ShowMessage(string message)
    {
        if (messageText == null) return;

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageCoroutine(message));
    }

    IEnumerator MessageCoroutine(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(messageTime);
        messageText.text = "";
    }
}