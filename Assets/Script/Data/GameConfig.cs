using System.Collections.Generic;
using UnityEngine;

// The one asset that knows all the other data assets.
// It lives in Assets/Resources so any script can reach it with GameConfig.Get().
[CreateAssetMenu(fileName = "GameConfig", menuName = "Jump High/Game Config")]
public class GameConfig : ScriptableObject
{
    public PlayerStats player;
    public ItemValues items;
    public BossStats boss;

    [Header("Levels, in play order")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("Scenes")]
    public string menuScene;
    public string winScene;
    [Tooltip("s the score is shown before the next level loads")] public float levelEndDelay;

    [Header("Save file defaults (first start, broken file)")]
    public SettingsData defaultSettings = new SettingsData();

    static GameConfig instance;

    public static GameConfig Get()
    {
        if (instance == null)
        {
            instance = Resources.Load<GameConfig>("GameConfig");
            if (instance == null)
                Debug.LogError("Assets/Resources/GameConfig.asset is missing.");
        }
        return instance;
    }

    public LevelData FirstLevel => levels.Count > 0 ? levels[0] : null;

    public LevelData FindByNumber(int number)
    {
        foreach (LevelData l in levels)
            if (l != null && l.levelNumber == number) return l;
        return null;
    }

    public LevelData FindByScene(string scene)
    {
        foreach (LevelData l in levels)
            if (l != null && l.sceneName == scene) return l;
        return null;
    }
}
