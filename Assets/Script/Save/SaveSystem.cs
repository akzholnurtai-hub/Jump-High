using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// save.json in Application.persistentDataPath (on Android: the app's private files folder).
//  - JsonUtility only (never BinaryFormatter).
//  - Missing file -> defaults. Broken file -> a copy is kept as save.broken.json, the game uses defaults.
//    Load never throws, so a bad file can not crash the game.
//  - Writes to save.tmp first, then swaps it in, so a crash during a write does not leave half a file.
// When it saves: level finished (CompleteLevel), progress reset, settings changed (RequestSave),
// app goes to background or quits (SaveHooks: OnApplicationPause(true) / OnApplicationQuit).
public static class SaveSystem
{
    public const int Version = 1;
    const string FileName = "save.json";
    const string TempName = "save.tmp";
    const string BrokenName = "save.broken.json";
    const int AudioSettingsVersion = 1;
    const int Campaign = 2;                             // monster stages replaced the old levels                 // Week 12: sfxVolume added to SettingsData

    static SaveData data;
    static bool dirty;
    static float lastChange;

    public static event Action Changed;                 // UI listens (level select, settings)

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    public static string BrokenPath => Path.Combine(Application.persistentDataPath, BrokenName);
    public static bool IsDirty => dirty;
    public static float LastChangeTime => lastChange;

    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    // Runs once before the first scene: load the file and create the object that saves on pause/quit.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        data = null;
        dirty = false;
        Load();

        GameObject hooks = new GameObject("SaveSystem");
        hooks.AddComponent<SaveHooks>();
        UnityEngine.Object.DontDestroyOnLoad(hooks);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (!Application.isMobilePlatform && !Application.isEditor)
            Screen.fullScreen = data.settings.fullscreen;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAudio();

    // ---------------- load / save ----------------

    public static void Load()
    {
        data = null;
        bool broken = false;
        string path = FilePath;

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);   // throws on bad JSON
                if (loaded == null) throw new InvalidDataException("the file is empty");
                if (loaded.version != Version) throw new InvalidDataException("unknown version " + loaded.version);
                data = loaded;
                Debug.Log("[Save] Loaded " + path);
            }
            catch (Exception e)
            {
                broken = true;
                Debug.LogWarning("[Save] save.json is broken (" + e.Message + "). Starting with defaults.");
                KeepBrokenCopy(path);
            }
        }
        else
        {
            Debug.Log("[Save] No save file yet. Starting with defaults: " + path);
        }

        if (data == null) data = NewSave();
        Sanitize(data);
        if (broken) Save();                              // replace the bad file with clean defaults
        Changed?.Invoke();
    }

    public static bool Save()
    {
        if (data == null) return false;
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            string tmp = Path.Combine(Application.persistentDataPath, TempName);
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
            dirty = false;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Save] Could not write save.json: " + e.Message);
            return false;
        }
    }

    // For things that change many times in a row (volume slider): SaveHooks writes the file
    // a moment after the last change instead of on every frame.
    public static void RequestSave()
    {
        dirty = true;
        lastChange = Time.unscaledTime;
    }

    // ---------------- progress ----------------

    public static bool IsUnlocked(int level) => Data.unlocked.Contains(level);

    public static LevelRecord GetRecord(int level)
    {
        foreach (LevelRecord r in Data.records)
            if (r.level == level) return r;
        return null;
    }

    public static int BestScore(int level)
    {
        LevelRecord r = GetRecord(level);
        return r != null ? r.bestScore : 0;
    }

    public static void StartLevel(LevelData level)
    {
        if (level != null) Data.lastLevel = level.levelNumber;
    }

    // Called once when a level is won. Returns true if it is a new best score.
    public static bool CompleteLevel(LevelData level, int score, float time)
    {
        LevelRecord r = GetRecord(level.levelNumber);
        if (r == null)
        {
            r = new LevelRecord { level = level.levelNumber };
            Data.records.Add(r);
        }

        bool newBest = score > r.bestScore;
        r.clears++;
        if (newBest) r.bestScore = score;
        if (r.bestTime <= 0f || time < r.bestTime) r.bestTime = time;

        if (level.nextLevel != null && !IsUnlocked(level.nextLevel.levelNumber))
            Data.unlocked.Add(level.nextLevel.levelNumber);

        RecountBest(Data);
        Save();
        Changed?.Invoke();
        return newBest;
    }

    public static void AddCoin() => Data.totalCoins++;
    public static void AddDeath() => Data.deaths++;

    // Reset button: progress back to a new game, settings stay.
    public static void ResetProgress()
    {
        SettingsData keep = Data.settings;
        data = NewSave();
        data.settings = keep;
        Sanitize(data);
        Save();
        Changed?.Invoke();
    }

    // ---------------- settings ----------------

    public static SettingsData Settings => Data.settings;

    public static void SettingsChanged()
    {
        ApplyAudio();
        RequestSave();
        Changed?.Invoke();
    }

    // Music and SFX volume go to the AudioMixer (AudioManager), in decibels.
    public static void ApplyAudio()
    {
        AudioManager.ApplyVolumes();
    }

    // ---------------- helpers ----------------

    static SaveData NewSave()
    {
        GameConfig cfg = GameConfig.Get();
        SaveData d = new SaveData { version = Version, campaign = Campaign };
        if (cfg != null)
        {
            d.settings = cfg.defaultSettings.Copy();
            if (cfg.FirstLevel != null) d.unlocked.Add(cfg.FirstLevel.levelNumber);
        }
        return d;
    }

    // A file edited by hand can have anything in it. Make every value usable.
    static void Sanitize(SaveData d)
    {
        GameConfig cfg = GameConfig.Get();

        if (d.unlocked == null) d.unlocked = new List<int>();
        if (d.records == null) d.records = new List<LevelRecord>();
        if (d.settings == null) d.settings = cfg != null ? cfg.defaultSettings.Copy() : new SettingsData();

        // The levels are new (monster stages), so old unlocks and best scores mean nothing: start them over.
        // Settings, coins and deaths are kept.
        if (d.campaign < Campaign)
        {
            d.unlocked.Clear();
            d.records.Clear();
            d.lastLevel = 0;
            d.campaign = Campaign;
        }

        List<int> clean = new List<int>();
        foreach (int n in d.unlocked)
            if (!clean.Contains(n) && (cfg == null || cfg.FindByNumber(n) != null)) clean.Add(n);
        if (cfg != null && cfg.FirstLevel != null && !clean.Contains(cfg.FirstLevel.levelNumber))
            clean.Add(cfg.FirstLevel.levelNumber);
        clean.Sort();
        d.unlocked = clean;

        d.records.RemoveAll(r => r == null || (cfg != null && cfg.FindByNumber(r.level) == null));
        foreach (LevelRecord r in d.records)
        {
            r.bestScore = Mathf.Max(0, r.bestScore);
            r.clears = Mathf.Max(0, r.clears);
            if (float.IsNaN(r.bestTime) || float.IsInfinity(r.bestTime) || r.bestTime < 0f) r.bestTime = 0f;
        }

        SettingsData s = d.settings;
        if (float.IsNaN(s.musicVolume) || float.IsInfinity(s.musicVolume))
            s.musicVolume = cfg != null ? cfg.defaultSettings.musicVolume : 0f;
        s.musicVolume = Mathf.Clamp01(s.musicVolume);

        // A save from Week 11 has no sfxVolume: JsonUtility reads it as 0, which would mute every sound.
        // audioVersion 0 tells us the file is that old, so it gets the default SFX volume once.
        float defaultSfx = cfg != null && cfg.defaultSettings.sfxVolume > 0f ? cfg.defaultSettings.sfxVolume : 0.8f;
        if (s.audioVersion < AudioSettingsVersion || float.IsNaN(s.sfxVolume) || float.IsInfinity(s.sfxVolume))
        {
            s.sfxVolume = defaultSfx;
            s.audioVersion = AudioSettingsVersion;
        }
        s.sfxVolume = Mathf.Clamp01(s.sfxVolume);
        if (s.playerName == null) s.playerName = "";
        s.playerColor = Mathf.Max(0, s.playerColor);

        d.totalCoins = Mathf.Max(0, d.totalCoins);
        d.deaths = Mathf.Max(0, d.deaths);
        RecountBest(d);
    }

    static void RecountBest(SaveData d)
    {
        int sum = 0;
        foreach (LevelRecord r in d.records) sum += r.bestScore;
        d.bestScore = sum;
    }

    static void KeepBrokenCopy(string path)
    {
        try { File.Copy(path, BrokenPath, true); }
        catch (Exception e) { Debug.LogWarning("[Save] Could not keep a copy of the broken file: " + e.Message); }
    }
}
