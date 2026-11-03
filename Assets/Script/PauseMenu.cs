using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    bool paused;

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (paused) Resume();
            else Pause();
        }
    }

    void OnApplicationPause(bool appPaused)
    {
        if (appPaused && !paused) Pause();
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }
}
