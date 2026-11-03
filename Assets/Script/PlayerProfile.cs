using UnityEngine;
using TMPro;

public class PlayerProfile : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Color[] colors = { Color.red, Color.blue, Color.green };

    void Start()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");

        if (nameText != null)
        {
            nameText.text = playerName;
        }

    }
}