using UnityEngine;
using UnityEngine.SceneManagement;

// Level select buttons call Load("SceneName"). Works from the pause menu (un-pauses first).
public class LevelLoader : MonoBehaviour
{
    public void Load(string scene)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(scene);
    }
}
