using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Main Menu (and Win screen) buttons.
// Music volume is kept in save.json (SaveSystem), not in PlayerPrefs.
// Week 12: the music is played by AudioManager; the volume sliders are VolumeSlider components.
public class MenuButtons : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject helpPanel;

    [Header("Week 11")]
    public GameObject levelSelectPanel;   // PLAY opens it
    public GameObject confirmPanel;       // "Reset all progress?" Yes / No
    public Slider volumeSlider;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        ShowOnly(mainMenuPanel);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        // show the saved settings without firing the "changed" events
        SettingsData s = SaveSystem.Settings;
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(s.musicVolume);
    }

    void ShowOnly(GameObject panel)
    {
        foreach (GameObject p in new[] { mainMenuPanel, settingsPanel, helpPanel, levelSelectPanel })
            if (p != null) p.SetActive(p == panel);
    }

    public void SetVolume(float value)
    {
        SaveSystem.Settings.musicVolume = value;
        SaveSystem.SettingsChanged();                // -> AudioManager.ApplyVolumes (mixer, dB)                // saved a moment after the slider stops
    }

    // PLAY: level select (locked / unlocked from the save file)
    public void Play()
    {
        if (levelSelectPanel != null)
        {
            ShowOnly(levelSelectPanel);
            return;
        }
        // no level select in this scene: continue from the last level the player may play
        GameConfig cfg = GameConfig.Get();
        LevelData last = cfg.FindByNumber(SaveSystem.Data.lastLevel);
        LevelRunner.Load(last != null && SaveSystem.IsUnlocked(last.levelNumber) ? last : cfg.FirstLevel);
    }

    public void CloseLevelSelect()
    {
        ShowOnly(mainMenuPanel);
    }

    // ---- reset progress, with an in-game confirm ----

    public void AskReset()
    {
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    public void ConfirmReset()
    {
        SaveSystem.ResetProgress();                  // writes save.json at once; LevelSelect refreshes itself
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    public void CancelReset()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    public void Quit()
    {
        SaveSystem.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void Settings()
    {
        ShowOnly(settingsPanel);
    }

    public void Back()
    {
        ShowOnly(mainMenuPanel);
    }

    public void OpenHelp()
    {
        ShowOnly(helpPanel);
    }

    public void CloseHelp()
    {
        ShowOnly(mainMenuPanel);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(GameConfig.Get().menuScene);
    }
}
