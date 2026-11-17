using UnityEngine;
using TMPro;

// Shows the player name from save.json in the level HUD.
public class PlayerProfile : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Color[] colors = { Color.red, Color.blue, Color.green };

    void Start()
    {
        string playerName = SaveSystem.Settings.playerName;
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        if (nameText != null)
        {
            nameText.text = playerName;
        }
    }
}
