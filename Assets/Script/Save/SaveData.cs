using System;
using System.Collections.Generic;

// Everything that goes into save.json. Only what JsonUtility can write:
// plain public fields, Lists and [Serializable] classes. No Dictionary, no properties.
[Serializable]
public class SaveData
{
    public int version;
    public int campaign;                                        // 2 = the monster stages (old level progress is reset once)
    public List<int> unlocked = new List<int>();               // level numbers the player may start
    public List<LevelRecord> records = new List<LevelRecord>();
    public int bestScore;                                       // sum of the best score of every level
    public int lastLevel;                                       // last level started
    public int totalCoins;                                      // coins picked up, all time
    public int deaths;                                          // all time
    public SettingsData settings = new SettingsData();
}

[Serializable]
public class LevelRecord
{
    public int level;
    public int bestScore;
    public float bestTime;                                      // s, 0 = not cleared yet
    public int clears;
}

[Serializable]
public class SettingsData
{
    public float musicVolume;                                   // 0..1 (slider), the mixer gets Log10 x 20 dB
    public float sfxVolume;                                     // 0..1 (slider), Week 12
    public int audioVersion;                                    // 0 = save from before Week 12 (no sfxVolume yet)
    public bool musicOn;
    public bool fullscreen;
    public string playerName;
    public int playerColor;

    public SettingsData Copy() => (SettingsData)MemberwiseClone();
}
