using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// The one sound object of the game. Made before the first scene (like SaveSystem) and kept
// alive with DontDestroyOnLoad, so:
//  - the music does NOT restart when a level loads: the same track keeps playing;
//    a different track (menu <-> level) comes in with a crossfade (no click);
//  - a sound that starts right before a scene change (lose, win) is not cut off.
// Every AudioSource goes through the AudioMixer: music -> Music/Soundtrack, the rest -> SFX.
// Volumes: slider 0..1 -> decibels (Log10 x 20) -> exposed parameters MusicVol / SFXVol.
// Pause: snapshot Paused (Soundtrack quieter + lowpass). Clips and numbers: Resources/SoundLibrary.
public class AudioManager : MonoBehaviour
{
    const string MusicParam = "MusicVol";
    const string SfxParam = "SFXVol";
    public const float MinDb = -80f;           // the mixer's "silent"
    const int PoolSize = 16;                   // SFX that can play at the same time

    static AudioManager instance;

    public static SoundLibrary Lib => instance != null ? instance.lib : null;

    SoundLibrary lib;
    AudioMixerGroup musicGroup;
    AudioMixerGroup sfxGroup;
    AudioMixerSnapshot normal;
    AudioMixerSnapshot paused;

    readonly AudioSource[] music = new AudioSource[2];   // two, so one can fade out while the other fades in
    int current;
    Coroutine fade;

    AudioSource ui;                                      // UI clicks: still heard in the pause menu
    readonly List<AudioSource> pool = new List<AudioSource>();
    int next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SoundLibrary l = Resources.Load<SoundLibrary>("SoundLibrary");
        if (l == null)
        {
            Debug.LogWarning("[Audio] Assets/Resources/SoundLibrary.asset is missing. Run Week12 > Setup Sound Pass.");
            return;
        }
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioManager>();
        instance.Init(l);
    }

    void Init(SoundLibrary l)
    {
        lib = l;
        if (lib.mixer != null)
        {
            lib.mixer.updateMode = AudioMixerUpdateMode.UnscaledTime;   // snapshots still move when timeScale = 0
            musicGroup = FindGroup("Soundtrack");
            if (musicGroup == null) musicGroup = FindGroup("Music");
            sfxGroup = FindGroup("SFX");
            normal = lib.mixer.FindSnapshot("Normal");
            paused = lib.mixer.FindSnapshot("Paused");
        }
        else Debug.LogWarning("[Audio] SoundLibrary has no mixer. Sounds play, but the volume sliders do nothing.");

        for (int i = 0; i < music.Length; i++)
        {
            music[i] = NewSource("Music " + (i == 0 ? "A" : "B"), musicGroup);
            music[i].loop = true;
            music[i].ignoreListenerPause = true;         // the pause menu pauses the game, not the music
        }
        ui = NewSource("UI", sfxGroup);
        ui.ignoreListenerPause = true;
        for (int i = 0; i < PoolSize; i++) pool.Add(NewSource("SFX " + i, sfxGroup));

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    // The mixer ignores SetFloat in Awake, so the saved volumes are applied in Start.
    // The first scene is set up here too: with "Enter Play Mode Options" (no scene reload)
    // sceneLoaded is not called for it. If it was called, CrossfadeTo sees the same track and does nothing.
    void Start()
    {
        TidySceneSources();
        AddClickSounds();
        CrossfadeTo(MusicFor(SceneManager.GetActiveScene().name));
        ApplyVolumes();
    }

    AudioMixerGroup FindGroup(string groupName)
    {
        foreach (AudioMixerGroup g in lib.mixer.FindMatchingGroups(groupName))
            if (g.name == groupName) return g;
        return null;
    }

    AudioSource NewSource(string sourceName, AudioMixerGroup group)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.outputAudioMixerGroup = group;
        return s;
    }

    // ------------------------------------------------------------------ scenes

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetPaused(false);
        TidySceneSources();
        AddClickSounds();
        CrossfadeTo(MusicFor(scene.name));
        ApplyVolumes();
    }

    // Menu and Win get the menu track, every level from GameConfig gets the level track.
    AudioClip MusicFor(string sceneName)
    {
        GameConfig cfg = GameConfig.Get();
        bool isLevel = cfg != null && cfg.FindByScene(sceneName) != null;
        return isLevel ? lib.levelMusic : lib.menuMusic;
    }

    // Safety net for scenes that were not set up with Week12 > Setup Sound Pass:
    // an old "Music" source in the scene would play a second copy of the music,
    // and a source with no mixer group would ignore the SFX slider.
    void TidySceneSources()
    {
        foreach (AudioSource s in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
        {
            if (s.transform.IsChildOf(transform)) continue;                 // my own sources
            bool isMusic = s.name == "Music" || (s.clip != null && (s.clip == lib.menuMusic || s.clip == lib.levelMusic));
            if (isMusic) { s.Stop(); s.enabled = false; }
            else if (s.outputAudioMixerGroup == null) s.outputAudioMixerGroup = sfxGroup;
        }
    }

    // Every button, toggle and dropdown clicks. Week12 setup adds ClickSound in the editor;
    // this adds it to anything that was missed.
    static void AddClickSounds()
    {
        foreach (Selectable s in FindObjectsByType<Selectable>(FindObjectsInactive.Include))
            if ((s is Button || s is Toggle || s is TMP_Dropdown) && s.GetComponent<ClickSound>() == null)
                s.gameObject.AddComponent<ClickSound>();
    }

    // ------------------------------------------------------------------ music

    public static void PlayMusic(AudioClip clip)
    {
        if (instance != null) instance.CrossfadeTo(clip);
    }

    void CrossfadeTo(AudioClip clip)
    {
        AudioSource now = music[current];
        if (clip == null) return;
        if (now.clip == clip && now.isPlaying) return;                      // same track: keep playing, no restart
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(Crossfade(clip));
    }

    // Equal-power crossfade: sin / cos curves keep the loudness even in the middle.
    // Unscaled time, so it also works while the game is paused.
    IEnumerator Crossfade(AudioClip clip)
    {
        AudioSource from = music[current];
        current = 1 - current;
        AudioSource to = music[current];

        to.Stop();
        to.clip = clip;
        to.volume = 0f;
        to.Play();

        float fromStart = from.volume;
        float time = Mathf.Max(0.01f, lib.crossfadeTime);
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            to.volume = Mathf.Sin(k * Mathf.PI * 0.5f);
            from.volume = fromStart * Mathf.Cos(k * Mathf.PI * 0.5f);
            yield return null;
        }
        to.volume = 1f;
        from.volume = 0f;
        from.Stop();
        fade = null;
    }

    // ------------------------------------------------------------------ sound effects

    // 2D: player sounds, win / lose
    public static void Play(Sfx s)
    {
        if (instance != null) instance.PlayEvent(instance.lib.Get(s), false, Vector3.zero, instance.NextSource());
    }

    // 3D at a point: enemies, boss
    public static void PlayAt(Sfx s, Vector3 position)
    {
        if (instance != null) instance.PlayEvent(instance.lib.Get(s), true, position, instance.NextSource());
    }

    // UI: plays even in the pause menu (AudioListener.pause does not stop it)
    public static void PlayUI(Sfx s)
    {
        if (instance != null) instance.PlayEvent(instance.lib.Get(s), false, Vector3.zero, instance.ui);
    }

    // Instead of AudioSource.PlayClipAtPoint, which makes a source with NO mixer group
    // (it would ignore the SFX slider). For clips that belong to one object (bomb explosion).
    public static void PlayClipAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (instance == null || clip == null) return;
        AudioSource src = instance.NextSource();
        instance.Setup3D(src, true, position);
        src.clip = clip;
        src.volume = volume;
        src.pitch = 1f;
        src.Play();
    }

    void PlayEvent(SoundEvent e, bool is3D, Vector3 position, AudioSource src)
    {
        if (e == null) return;
        AudioClip clip = e.Pick();
        if (clip == null) return;

        Setup3D(src, is3D, position);
        src.clip = clip;
        src.volume = e.volume;
        src.pitch = e.RandomPitch();
        src.Play();
    }

    void Setup3D(AudioSource src, bool is3D, Vector3 position)
    {
        src.transform.position = position;
        src.spatialBlend = is3D ? lib.spatialBlend : 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = lib.minDistance;
        src.maxDistance = lib.maxDistance;
    }

    // a free source; if all 16 are busy, the oldest one is reused
    AudioSource NextSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            AudioSource s = pool[(next + i) % pool.Count];
            if (!s.isPlaying) { next = (next + i + 1) % pool.Count; return s; }
        }
        AudioSource oldest = pool[next];
        next = (next + 1) % pool.Count;
        return oldest;
    }

    // ------------------------------------------------------------------ mixer

    public static void SetPaused(bool isPaused)
    {
        if (instance == null) return;
        AudioMixerSnapshot snap = isPaused ? instance.paused : instance.normal;
        if (snap != null) snap.TransitionTo(instance.lib.pauseFadeTime);
    }

    // Settings -> mixer. Called at start, on every scene and when a slider moves.
    public static void ApplyVolumes()
    {
        if (instance == null || instance.lib.mixer == null) return;
        SettingsData s = SaveSystem.Settings;
        instance.lib.mixer.SetFloat(MusicParam, s.musicOn ? ToDb(s.musicVolume) : MinDb);
        instance.lib.mixer.SetFloat(SfxParam, ToDb(s.sfxVolume));
    }

    // The ear hears loudness in decibels, not in a straight line: half the slider = -6 dB, not "half".
    // 1 -> 0 dB, 0.5 -> -6 dB, 0.1 -> -20 dB, 0 -> -80 dB (silent; Log10(0) would be minus infinity).
    public static float ToDb(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDb;
    }
}
