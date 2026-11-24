using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A volume slider in Settings (Music or SFX).
// The slider is 0..1, the label shows the decibels that go to the mixer.
// The value is kept in save.json (SettingsData) and written 0.5 s after the slider stops.
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    public enum Channel { Music, SFX }

    const float PreviewGap = 0.2f;          // s between test sounds while the SFX slider is dragged

    public Channel channel;
    public TMP_Text label;
    public string title = "Music";

    Slider slider;
    float lastPreview;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    void Start()
    {
        Show();
        slider.onValueChanged.AddListener(Changed);
        SaveSystem.Changed += Show;
    }

    void OnDestroy()
    {
        SaveSystem.Changed -= Show;
    }

    float Saved => channel == Channel.Music ? SaveSystem.Settings.musicVolume : SaveSystem.Settings.sfxVolume;

    void Show()
    {
        slider.SetValueWithoutNotify(Saved);
        ShowLabel(Saved);
    }

    void Changed(float value)
    {
        if (channel == Channel.Music) SaveSystem.Settings.musicVolume = value;
        else SaveSystem.Settings.sfxVolume = value;
        SaveSystem.SettingsChanged();                  // mixer now, save.json a moment later
        ShowLabel(value);

        // let the player hear the new SFX level while dragging
        if (channel == Channel.SFX && Time.unscaledTime - lastPreview > PreviewGap)
        {
            lastPreview = Time.unscaledTime;
            AudioManager.PlayUI(Sfx.Pickup);
        }
    }

    void ShowLabel(float value)
    {
        if (label == null) return;
        float db = AudioManager.ToDb(value);
        label.text = title + "   " + (db <= AudioManager.MinDb ? "off" : db.ToString("0") + " dB");
    }
}
