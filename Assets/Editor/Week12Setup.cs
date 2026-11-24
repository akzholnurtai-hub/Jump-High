using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Week 12 - Sound Pass. Run "Week12 > 1. Setup Sound Pass" once (safe to run again).
//  1. Audio import settings: music streams (Vorbis), short sounds are decompressed on load.
//  2. Assets/Resources/SoundLibrary.asset: the mixer, 2 music tracks, 12 sound events
//     (only empty events are filled, so changes made in the Inspector are kept).
//  3. Every scene in Build Settings: the old "Music" sources are removed (AudioManager plays the
//     music now), every other AudioSource goes to the SFX group, every button gets ClickSound,
//     Settings gets a Music and an SFX slider with dB labels.
//  4. Every AudioSource in Assets/Prefabs goes to the SFX group.
// "Week12 > 2. Check" prints what is still missing.
public static class Week12Setup
{
    const string AudioDir = "Assets/Audio";
    const string MusicDir = "Assets/Audio/Music";
    const string SfxDir = "Assets/Audio/SFX/";
    const string MixerPath = "Assets/Audio/MainMixer.mixer";
    const string LibPath = "Assets/Resources/SoundLibrary.asset";
    const string PrefabDir = "Assets/Prefabs";

    // =====================================================================
    [MenuItem("Week12/1. Setup Sound Pass")]
    static void SetupMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Week12", "Stop Play mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string log = Run();
        string check = Check();
        Debug.Log("[Week12]\n" + log + "\n" + check);
        EditorUtility.DisplayDialog("Week12 - done", log + "\n" + check +
            "\nOpen Assets/Audio/MainMixer (Window > Audio > Audio Mixer) for the screenshot." +
            "\nPress Play in MainMenu.", "OK");
    }

    [MenuItem("Week12/2. Check")]
    static void CheckMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string check = Check();
        Debug.Log("[Week12]\n" + check);
        EditorUtility.DisplayDialog("Week12 - check", check, "OK");
    }

    [MenuItem("Week12/Open Audio Mixer")]
    static void OpenMixer()
    {
        Object mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        Selection.activeObject = mixer;
        EditorGUIUtility.PingObject(mixer);
        EditorApplication.ExecuteMenuItem("Window/Audio/Audio Mixer");
    }

    // For the command line: Unity -batchmode -executeMethod Week12Setup.RunBatch
    public static void RunBatch()
    {
        string log = Run();
        string check = Check();
        Debug.Log("[Week12]\n" + log + "\n" + check);
        EditorApplication.Exit(check.Contains("FAIL") ? 1 : 0);
    }

    static string Run()
    {
        StringBuilder log = new StringBuilder();
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null) return "! " + MixerPath + " is missing.";
        AudioMixerGroup sfx = FindGroup(mixer, "SFX");

        log.AppendLine(ImportSettings());
        SoundLibrary lib = MakeLibrary(mixer, log);
        SetDefaults(log);

        string first = null;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null) continue;
            if (first == null) first = s.path;
            Scene scene = EditorSceneManager.OpenScene(s.path);
            log.AppendLine(scene.name + ": " + SetupScene(scene, lib, sfx));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        log.AppendLine(SetupPrefabs(sfx));
        AssetDatabase.SaveAssets();
        if (first != null) EditorSceneManager.OpenScene(first);
        return log.ToString();
    }

    // =====================================================================
    // 1. import settings
    static string ImportSettings()
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;

            bool isMusic = path.StartsWith(MusicDir);
            AudioImporterSampleSettings s = imp.defaultSampleSettings;
            AudioClipLoadType load = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            if (s.loadType == load && imp.forceToMono == !isMusic) continue;

            s.loadType = load;                                   // music: read from disk while playing (little RAM)
            if (isMusic) s.compressionFormat = AudioCompressionFormat.Vorbis;
            imp.defaultSampleSettings = s;
            imp.forceToMono = !isMusic;                          // short sounds: mono is enough, half the size
            imp.SaveAndReimport();
            changed++;
        }
        return "Audio import settings changed: " + changed;
    }

    // =====================================================================
    // 2. sound library
    static SoundLibrary MakeLibrary(AudioMixer mixer, StringBuilder log)
    {
        SoundLibrary lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(lib, LibPath);
            log.AppendLine("Created " + LibPath);
        }

        lib.mixer = mixer;
        if (lib.menuMusic == null) lib.menuMusic = Clip(MusicDir + "/menu_theme.wav");
        if (lib.levelMusic == null) lib.levelMusic = Clip(MusicDir + "/tatamusic-game-game-music-599001.mp3");

        int filled = 0;
        filled += Fill(lib.jump, 0.7f, 0.92f, 1.08f, "jump.wav", "jump_2.wav");
        filled += Fill(lib.hurt, 0.9f, 0.92f, 1.08f, "freesound_community-breaking-small-bone-finger-88085.mp3", "hurt_2.wav");
        filled += Fill(lib.pickup, 0.7f, 0.95f, 1.10f, "lost-money-on-the-game-account.mp3", "coin_2.wav");
        filled += Fill(lib.heal, 0.8f, 0.95f, 1.05f, "freesound_community-084373_heal-36672.mp3");
        filled += Fill(lib.shoot, 0.5f, 0.90f, 1.10f, "freesound_community-072782_bullet-39717.mp3");
        filled += Fill(lib.enemyWindUp, 1.0f, 0.95f, 1.05f, "enemy_windup_1.wav", "enemy_windup_2.wav");
        filled += Fill(lib.bossWindUp, 1.0f, 0.95f, 1.05f, "boss_windup.wav");
        filled += Fill(lib.enemyHit, 0.7f, 0.90f, 1.10f, "enemy_hit_1.wav", "enemy_hit_2.wav");
        filled += Fill(lib.enemyDeath, 0.9f, 0.92f, 1.08f, "enemy_death.wav");
        filled += Fill(lib.uiClick, 0.6f, 0.95f, 1.05f, "ui_click.wav");
        filled += Fill(lib.win, 0.9f, 1f, 1f, "win.wav");           // jingles: no pitch change
        filled += Fill(lib.lose, 0.9f, 1f, 1f, "lose.wav");

        EditorUtility.SetDirty(lib);
        log.AppendLine("SoundLibrary: " + filled + " empty sound events filled");
        return lib;
    }

    static int Fill(SoundEvent e, float volume, float minPitch, float maxPitch, params string[] files)
    {
        if (e.clips != null && e.clips.Length > 0) return 0;     // keep what was set by hand
        List<AudioClip> clips = new List<AudioClip>();
        foreach (string f in files)
        {
            AudioClip c = Clip(SfxDir + f);
            if (c != null) clips.Add(c);
            else Debug.LogWarning("[Week12] Missing sound " + SfxDir + f);
        }
        e.clips = clips.ToArray();
        e.volume = volume;
        e.minPitch = minPitch;
        e.maxPitch = maxPitch;
        return 1;
    }

    static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

    static void SetDefaults(StringBuilder log)
    {
        GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
        if (cfg == null) { log.AppendLine("! GameConfig not found"); return; }
        if (cfg.defaultSettings.sfxVolume <= 0f) cfg.defaultSettings.sfxVolume = 0.8f;
        cfg.defaultSettings.audioVersion = 1;
        EditorUtility.SetDirty(cfg);
        if (cfg.boss != null && cfg.boss.fanWarnTime <= 0f)
        {
            cfg.boss.fanWarnTime = 0.6f;
            EditorUtility.SetDirty(cfg.boss);
        }
        log.AppendLine("Default SFX volume " + cfg.defaultSettings.sfxVolume +
                       (cfg.boss != null ? ", boss fan wind-up " + cfg.boss.fanWarnTime + " s" : ""));
    }

    // =====================================================================
    // 3. scenes
    static string SetupScene(Scene scene, SoundLibrary lib, AudioMixerGroup sfx)
    {
        int removed = 0, routed = 0, clicks = 0;

        foreach (AudioSource src in All<AudioSource>(scene))
        {
            if (src == null) continue;
            if (IsMusic(src, lib))
            {
                GameObject go = src.gameObject;
                bool onlySound = go.transform.childCount == 0 && go.GetComponents<Component>().Length == 2;   // Transform + AudioSource
                if (onlySound) Object.DestroyImmediate(go);
                else Object.DestroyImmediate(src);
                removed++;
            }
            else if (src.outputAudioMixerGroup != sfx)
            {
                src.outputAudioMixerGroup = sfx;
                routed++;
            }
        }

        foreach (Selectable s in All<Selectable>(scene))
            if ((s is Button || s is Toggle || s is TMP_Dropdown) && s.GetComponent<ClickSound>() == null)
            {
                s.gameObject.AddComponent<ClickSound>();
                clicks++;
            }

        string settings = "";
        foreach (Transform t in All<Transform>(scene))
            if (t.name == "SettingsPanel") settings += SetupSettings(t);

        return "music sources removed " + removed + ", sources -> SFX " + routed + ", click sounds " + clicks + settings;
    }

    static bool IsMusic(AudioSource src, SoundLibrary lib)
    {
        return src.name == "Music" || (src.clip != null && (src.clip == lib.menuMusic || src.clip == lib.levelMusic));
    }

    // Settings panel: Music slider (the old one) + a new SFX slider, both with a dB label.
    static string SetupSettings(Transform panel)
    {
        Transform musicT = Child(panel, "Slider");
        Slider music = musicT != null ? musicT.GetComponent<Slider>() : null;
        if (music == null) return ", settings: no Slider found";

        // the old listener (MenuButtons.SetVolume) is replaced by VolumeSlider
        for (int i = music.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(music.onValueChanged, i);

        Transform labelT = Child(panel, "Text (TMP)");
        TMP_Text musicLabel = labelT != null ? labelT.GetComponent<TMP_Text>() : null;

        Transform sfxT = Child(panel, "Slider_SFX");
        if (sfxT == null)
        {
            sfxT = Object.Instantiate(music.gameObject, music.transform.parent).transform;
            sfxT.name = "Slider_SFX";
        }

        TMP_Text sfxLabel = null;
        Transform sfxLabelT = Child(panel, "Label_SFX");
        if (sfxLabelT == null && musicLabel != null)
        {
            sfxLabelT = Object.Instantiate(musicLabel.gameObject, musicLabel.transform.parent).transform;
            sfxLabelT.name = "Label_SFX";
        }
        if (sfxLabelT != null) sfxLabel = sfxLabelT.GetComponent<TMP_Text>();

        Wire(music.gameObject, VolumeSlider.Channel.Music, "Music", musicLabel);
        Wire(sfxT.gameObject, VolumeSlider.Channel.SFX, "SFX", sfxLabel);

        // layout (same column as RK2Setup: Toggle_Music at y 90, Back button at the bottom)
        SetY(labelT, 25f);
        SetY(music.transform, -30f);
        SetY(sfxLabelT, -90f);
        SetY(sfxT, -145f);
        SetY(Child(panel, "Btn_Back"), -275f);
        return ", settings: Music + SFX sliders";
    }

    static void Wire(GameObject go, VolumeSlider.Channel channel, string title, TMP_Text label)
    {
        VolumeSlider v = go.GetComponent<VolumeSlider>();
        if (v == null) v = go.AddComponent<VolumeSlider>();
        v.channel = channel;
        v.title = title;
        v.label = label;
        if (label != null) label.text = title;
        EditorUtility.SetDirty(v);
    }

    static void SetY(Transform t, float y)
    {
        if (t == null) return;
        RectTransform rt = t as RectTransform;
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }

    // =====================================================================
    // 4. prefabs
    static string SetupPrefabs(AudioMixerGroup sfx)
    {
        int changed = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool dirty = false;
            foreach (AudioSource src in root.GetComponentsInChildren<AudioSource>(true))
                if (src.outputAudioMixerGroup != sfx) { src.outputAudioMixerGroup = sfx; dirty = true; }
            if (dirty) { PrefabUtility.SaveAsPrefabAsset(root, path); changed++; }
            PrefabUtility.UnloadPrefabContents(root);
        }
        return "Prefabs with AudioSources -> SFX: " + changed;
    }

    // =====================================================================
    // check (also for the screenshot / video checklist)
    static string Check()
    {
        StringBuilder r = new StringBuilder();
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        Line(r, mixer != null, "Mixer " + MixerPath);
        if (mixer != null)
        {
            foreach (string g in new[] { "Master", "Music", "Soundtrack", "SFX" })
                Line(r, FindGroup(mixer, g) != null, "Group " + g);
            foreach (string s in new[] { "Normal", "Paused" })
                Line(r, mixer.FindSnapshot(s) != null, "Snapshot " + s);

            List<string> exposed = new List<string>();
            SerializedProperty ep = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
            if (ep != null)
                for (int i = 0; i < ep.arraySize; i++) exposed.Add(ep.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            Line(r, exposed.Contains("MusicVol") && exposed.Contains("SFXVol"), "Exposed MusicVol, SFXVol (" + string.Join(", ", exposed) + ")");

            bool lowpass = false;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(MixerPath))
                if (o != null && o.GetType().Name == "AudioMixerEffectController")
                {
                    SerializedProperty n = new SerializedObject(o).FindProperty("m_EffectName");
                    if (n != null && n.stringValue == "Lowpass") lowpass = true;
                }
            Line(r, lowpass, "Lowpass on Soundtrack (Paused snapshot)");
        }

        SoundLibrary lib = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibPath);
        Line(r, lib != null, "SoundLibrary " + LibPath);
        if (lib != null)
        {
            Line(r, lib.mixer == mixer && mixer != null, "SoundLibrary uses MainMixer");
            Line(r, lib.menuMusic != null && lib.levelMusic != null && lib.menuMusic != lib.levelMusic, "2 music tracks (menu, level)");
            int events = 0, varied = 0;
            List<string> empty = new List<string>();
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx)))
            {
                SoundEvent e = lib.Get(s);
                if (e != null && e.clips != null && e.clips.Length > 0 && e.clips[0] != null) events++;
                else empty.Add(s.ToString());
                if (e != null && (e.clips.Length > 1 || e.maxPitch > e.minPitch)) varied++;
            }
            Line(r, events >= 8 && empty.Count == 0, events + " sound events with clips" + (empty.Count > 0 ? ", empty: " + string.Join(", ", empty) : ""));
            Line(r, varied > 0, varied + " events with pitch or clip variation");
        }

        AudioMixerGroup sfxGroup = mixer != null ? FindGroup(mixer, "SFX") : null;
        Scene open = SceneManager.GetActiveScene();
        string openPath = open.path;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (!s.enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path) == null) continue;
            Scene scene = EditorSceneManager.OpenScene(s.path);
            int noGroup = 0, musicSrc = 0, noClick = 0, sliders = 0;
            foreach (AudioSource src in All<AudioSource>(scene))
            {
                if (lib != null && IsMusic(src, lib)) musicSrc++;
                else if (src.outputAudioMixerGroup == null) noGroup++;
            }
            foreach (Selectable sel in All<Selectable>(scene))
                if ((sel is Button || sel is Toggle || sel is TMP_Dropdown) && sel.GetComponent<ClickSound>() == null) noClick++;
            foreach (VolumeSlider v in All<VolumeSlider>(scene)) sliders++;
            Line(r, noGroup == 0 && musicSrc == 0 && noClick == 0,
                 scene.name + ": unrouted " + noGroup + ", old music " + musicSrc + ", no click " + noClick +
                 (sliders > 0 ? ", volume sliders " + sliders : ""));
        }
        if (!string.IsNullOrEmpty(openPath)) EditorSceneManager.OpenScene(openPath);
        return r.ToString();
    }

    static void Line(StringBuilder r, bool ok, string text) => r.AppendLine((ok ? "OK    " : "FAIL  ") + text);

    // =====================================================================
    // helpers
    static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
    {
        foreach (AudioMixerGroup g in mixer.FindMatchingGroups(groupName))
            if (g.name == groupName) return g;
        return null;
    }

    static List<T> All<T>(Scene scene) where T : Component
    {
        List<T> list = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<T>(true));
        return list;
    }

    static Transform Child(Transform parent, string childName)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == childName && t != parent) return t;
        return null;
    }
}
