using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Settings panel: name, color, fullscreen, music on/off.
// Every change goes into save.json (SaveSystem) and is written a moment later.
public class ProfileSettings : MonoBehaviour
{
    public Toggle fullscreenToggle;
    public Toggle musicToggle;
    public AudioSource bgSound;
    public TMP_InputField nameField;
    public TMP_Dropdown colorDropdown;

    void Start()
    {
        Show();
        SaveSystem.Changed += Show;
    }

    void OnDestroy()
    {
        SaveSystem.Changed -= Show;
    }

    // put the saved values into the UI without firing onValueChanged
    void Show()
    {
        SettingsData s = SaveSystem.Settings;
        if (nameField != null) nameField.SetTextWithoutNotify(s.playerName);
        if (colorDropdown != null) colorDropdown.SetValueWithoutNotify(s.playerColor);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);
        if (musicToggle != null) musicToggle.SetIsOnWithoutNotify(s.musicOn);
    }

    public void SetFullscreen(bool isOn)
    {
        Screen.fullScreen = isOn;
        SaveSystem.Settings.fullscreen = isOn;
        SaveSystem.SettingsChanged();
    }

    public void SetMusic(bool isOn)
    {
        SaveSystem.Settings.musicOn = isOn;
        SaveSystem.ApplyAudio(bgSound);
        SaveSystem.SettingsChanged();
    }

    public void SaveName(string value)
    {
        SaveSystem.Settings.playerName = value;
        SaveSystem.SettingsChanged();
    }

    public void SaveColor(int index)
    {
        SaveSystem.Settings.playerColor = index;
        SaveSystem.SettingsChanged();
    }
}
