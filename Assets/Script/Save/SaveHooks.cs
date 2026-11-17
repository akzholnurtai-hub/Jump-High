using UnityEngine;

// Created by SaveSystem before the first scene and kept alive between scenes.
// A phone app can be killed at any moment after it goes to the background,
// so OnApplicationPause(true) is the last safe moment to write the file.
public class SaveHooks : MonoBehaviour
{
    const float SettleTime = 0.5f;   // s after the last settings change before writing (slider drags)

    void Update()
    {
        if (SaveSystem.IsDirty && Time.unscaledTime - SaveSystem.LastChangeTime >= SettleTime)
            SaveSystem.Save();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveSystem.Save();
    }

    void OnApplicationQuit()
    {
        SaveSystem.Save();
    }
}
