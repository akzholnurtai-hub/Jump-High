using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Week 11 tools. Run "Week11 > 1. Setup Data + Saves" once (safe to run again).
//  1. Enemies placed in a level scene become prefabs + spawn entries in that level's LevelData,
//     then they are removed from the scene: LevelRunner spawns them from the data.
//  2. Every level scene gets a LevelRunner pointing to its LevelData asset.
//  3. Main Menu gets a Level Select panel (locked / unlocked from save.json)
//     and a "Reset progress" button with an in-game confirm panel.
// This file only wires things together - it has no balance numbers.
// All numbers are in Assets/Data and Assets/Resources/GameConfig.asset.
public static class Week11Setup
{
    const string ScenesDir = "Assets/Scenes/";
    const string EnemyPrefabDir = "Assets/Prefabs/Enemies";

    // UI look, same colors as RK2Setup
    static readonly Color Panel = new Color(0.07f, 0.09f, 0.16f, 0.95f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color Accent = new Color(1f, 0.55f, 0.1f, 1f);
    static readonly Color Glass = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color Danger = new Color(0.85f, 0.2f, 0.15f, 0.9f);
    static readonly Color LockedTint = new Color(0.35f, 0.35f, 0.4f, 0.55f);

    static TMP_FontAsset font;

    // =====================================================================
    [MenuItem("Week11/1. Setup Data + Saves")]
    static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Week11", "Stop Play mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
        if (cfg == null || cfg.player == null || cfg.items == null || cfg.boss == null || cfg.levels.Count == 0)
        {
            EditorUtility.DisplayDialog("Week11",
                "Assets/Resources/GameConfig.asset is missing or empty.\nCheck Assets/Data and the GameConfig fields.", "OK");
            return;
        }

        string log = "";
        foreach (LevelData lvl in cfg.levels)
        {
            if (lvl == null) continue;
            if (!SceneExists(lvl.sceneName)) { log += "! Scene not found: " + lvl.sceneName + "\n"; continue; }
            EditorSceneManager.OpenScene(ScenesDir + lvl.sceneName + ".unity");
            log += EnemiesToData(lvl);
            log += AddRunner(lvl);
            SaveScene();
        }

        if (SceneExists(cfg.menuScene))
        {
            EditorSceneManager.OpenScene(ScenesDir + cfg.menuScene + ".unity");
            log += BuildMainMenu();
            SaveScene();
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenesDir + cfg.menuScene + ".unity");
        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);

        Debug.Log("[Week11]\n" + log);
        EditorUtility.DisplayDialog("Week11 - done", log +
            "\nBalance numbers: Assets/Data (Player, Items, Boss, Enemies, Levels)." +
            "\nSave file: Week11 > Save file > Open folder." +
            "\n\nPress Play in MainMenu.", "OK");
    }

    // ---------- 1. scene enemies -> prefab + LevelData entry ----------
    static string EnemiesToData(LevelData lvl)
    {
        EnemyBase[] found = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (found.Length == 0) return "";

        lvl.enemies.Clear();
        string log = "";
        foreach (EnemyBase e in found)
        {
            EnemyStats stats = StatsFor(e);
            if (stats == null) { log += "! No stats asset for " + e.name + " (" + e.GetType().Name + ")\n"; continue; }

            EnemySpawn spawn = new EnemySpawn
            {
                type = stats,
                position = e.transform.position,
                yaw = e.transform.eulerAngles.y
            };

            SerializedObject so = new SerializedObject(e);
            SerializedProperty pts = so.FindProperty("points");
            if (pts != null)
            {
                for (int i = 0; i < pts.arraySize; i++)
                {
                    Transform t = pts.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (t != null) spawn.route.Add(t.position);
                }
                pts.arraySize = 0;                         // a prefab can not point to scene objects
            }
            so.FindProperty("stats").objectReferenceValue = stats;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (stats.prefab == null)
            {
                EnsureFolder(EnemyPrefabDir);
                string path = EnemyPrefabDir + "/" + stats.name + ".prefab";
                stats.prefab = PrefabUtility.SaveAsPrefabAsset(e.gameObject, path);
                EditorUtility.SetDirty(stats);
                log += "  prefab " + path + "\n";
            }

            lvl.enemies.Add(spawn);
            Object.DestroyImmediate(e.gameObject);
        }

        EditorUtility.SetDirty(lvl);
        return "+ " + lvl.name + ": " + lvl.enemies.Count + " enemies moved from the scene into the data\n" + log;
    }

    static EnemyStats StatsFor(EnemyBase e)
    {
        if (e.Stats != null) return e.Stats;
        string type = e is PatrollerAI ? "PatrollerStats" : e is ShooterAI ? "ShooterStats" : e is ChargerAI ? "ChargerStats" : null;
        if (type == null) return null;
        string[] guids = AssetDatabase.FindAssets("t:" + type, new[] { "Assets/Data" });
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<EnemyStats>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }

    // ---------- 2. LevelRunner in every level ----------
    static string AddRunner(LevelData lvl)
    {
        LevelRunner runner = Object.FindAnyObjectByType<LevelRunner>(FindObjectsInactive.Include);
        if (runner == null) runner = new GameObject("LevelRunner").AddComponent<LevelRunner>();

        SerializedObject so = new SerializedObject(runner);
        so.FindProperty("level").objectReferenceValue = lvl;
        so.ApplyModifiedPropertiesWithoutUndo();
        return "+ " + lvl.sceneName + ": LevelRunner -> " + lvl.name + "\n";
    }

    // ---------- 3. Main Menu: level select + reset with confirm ----------
    static string BuildMainMenu()
    {
        MenuButtons mb = Object.FindAnyObjectByType<MenuButtons>(FindObjectsInactive.Include);
        Transform main = FindInScene("MainPanel");
        if (mb == null || main == null) return "! MainMenu: MenuButtons or MainPanel not found\n";

        Transform start = FindInScene("Btn_Start");
        font = start != null ? start.GetComponentInChildren<TextMeshProUGUI>(true).font : null;

        Transform parent = main.parent;                   // SafeArea
        RemoveInScene("LevelSelectPanel");
        RemoveInScene("ConfirmPanel");

        // ---- level select ----
        RectTransform select = NewUI("LevelSelectPanel", parent);
        Stretch(select);
        SetImage(select, null, Dim);

        Text(select, "Title", "SELECT LEVEL", 100, Accent, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(1200, 150));

        RectTransform row = NewUI("LevelRow", select);
        Place(row, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1700, 320));
        HorizontalLayoutGroup hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 36;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;

        Button template = NewButton("LevelButtonTemplate", row, "1", Accent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 300), 90, null);
        template.gameObject.SetActive(false);

        TextMeshProUGUI total = Text(select, "TotalText", "Best score: 0", 44, Color.white,
                                     new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(1600, 70));

        NewButton("Btn_Reset", select, "Reset progress", Danger, new Vector2(0.5f, 0.5f), new Vector2(-300, -370), new Vector2(520, 120), 46, mb.AskReset);
        NewButton("Btn_LevelBack", select, "Back", Glass, new Vector2(0.5f, 0.5f), new Vector2(300, -370), new Vector2(520, 120), 52, mb.CloseLevelSelect);

        LevelSelect ls = select.gameObject.AddComponent<LevelSelect>();
        SerializedObject lso = new SerializedObject(ls);
        lso.FindProperty("template").objectReferenceValue = template;
        lso.FindProperty("row").objectReferenceValue = row;
        lso.FindProperty("totalText").objectReferenceValue = total;
        lso.FindProperty("openColor").colorValue = Accent;
        lso.FindProperty("lockedColor").colorValue = LockedTint;
        lso.ApplyModifiedPropertiesWithoutUndo();
        select.gameObject.SetActive(false);

        // ---- confirm ----
        RectTransform confirm = NewUI("ConfirmPanel", parent);
        Stretch(confirm);
        SetImage(confirm, null, Dim);
        RectTransform box = NewUI("Box", confirm);
        Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 560));
        SetImage(box, Rounded(), Panel);
        TextMeshProUGUI q = Text(box, "Question",
            "RESET ALL PROGRESS?\n<size=60%>Unlocked levels, best scores, coins and deaths will be deleted. Settings stay.</size>",
            64, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1000, 300));
        q.textWrappingMode = TextWrappingModes.Normal;
        NewButton("Btn_Yes", box, "Yes, reset", Danger, new Vector2(0.5f, 0.5f), new Vector2(-240, -170), new Vector2(420, 120), 48, mb.ConfirmReset);
        NewButton("Btn_No", box, "No", Glass, new Vector2(0.5f, 0.5f), new Vector2(240, -170), new Vector2(420, 120), 48, mb.CancelReset);
        confirm.SetAsLastSibling();
        confirm.gameObject.SetActive(false);

        // ---- MenuButtons fields ----
        SerializedObject mso = new SerializedObject(mb);
        mso.FindProperty("levelSelectPanel").objectReferenceValue = select.gameObject;
        mso.FindProperty("confirmPanel").objectReferenceValue = confirm.gameObject;
        Transform settings = FindInScene("SettingsPanel");
        Slider slider = settings != null ? settings.GetComponentInChildren<Slider>(true) : null;
        mso.FindProperty("volumeSlider").objectReferenceValue = slider;
        mso.ApplyModifiedPropertiesWithoutUndo();

        return "+ MainMenu: Level Select + Reset progress (confirm panel)\n";
    }

    // =====================================================================
    // Save file helpers for the video tests
    [MenuItem("Week11/Save file/Open folder")]
    static void OpenFolder()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        EditorUtility.RevealInFinder(File.Exists(SaveSystem.FilePath) ? SaveSystem.FilePath : Application.persistentDataPath);
        Debug.Log("save.json: " + SaveSystem.FilePath);
    }

    [MenuItem("Week11/Save file/Print to Console")]
    static void Print()
    {
        Debug.Log(File.Exists(SaveSystem.FilePath) ? SaveSystem.FilePath + "\n" + File.ReadAllText(SaveSystem.FilePath)
                                                   : "No save.json yet: " + SaveSystem.FilePath);
    }

    [MenuItem("Week11/Save file/Break it (broken file test)")]
    static void Break()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Week11", "Stop Play mode first - the game saves on quit and would fix the file.", "OK");
            return;
        }
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(SaveSystem.FilePath, "{ \"version\": 1, \"unlocked\": [1, 2, \"bestScore\": ###");
        Debug.Log("save.json is now broken. Press Play: the game must start with defaults and not crash.");
    }

    [MenuItem("Week11/Save file/Delete it")]
    static void Delete()
    {
        if (File.Exists(SaveSystem.FilePath)) File.Delete(SaveSystem.FilePath);
        Debug.Log("save.json deleted: " + SaveSystem.FilePath);
    }

    // =====================================================================
    // UI helpers
    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static Image SetImage(Component c, Sprite sprite, Color color)
    {
        Image img = c.GetComponent<Image>();
        if (img == null) img = c.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 0.35f;
        img.color = color;
        return img;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
                                Vector2 anchor, Vector2 pos, Vector2 box)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, anchor, pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        Style(t, size, color);
        t.text = text;
        return t;
    }

    static void Style(TextMeshProUGUI t, float size, Color color)
    {
        if (font != null) t.font = font;
        t.enableAutoSizing = false;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
    }

    static Button NewButton(string name, Transform parent, string label, Color color,
                            Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, UnityAction action)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, anchor, pos, size);
        SetImage(rt, Rounded(), color);
        Button b = rt.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);

        RectTransform trt = NewUI("Text (TMP)", rt);
        Stretch(trt);
        TextMeshProUGUI t = trt.gameObject.AddComponent<TextMeshProUGUI>();
        Style(t, fontSize, Color.white);
        t.text = label;
        return b;
    }

    static Sprite Rounded()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    // =====================================================================
    // scene / asset helpers
    static bool SceneExists(string scene)
    {
        return !string.IsNullOrEmpty(scene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenesDir + scene + ".unity") != null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Transform FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }

    static void RemoveInScene(string name)
    {
        Transform t;
        while ((t = FindInScene(name)) != null) Object.DestroyImmediate(t.gameObject);
    }

    static void SaveScene()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }
}
