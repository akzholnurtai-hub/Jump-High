using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Menu: Week10 > Add Level Select (menu + pause)
// Adds level buttons to the pause card of every level and a LEVELS column to the MainMenu.
// Safe to run many times (old buttons are replaced).
public static class Week10Levels
{
    const string S = "Assets/Scenes/";
    static readonly string[] Scenes = { "Level1", "Level_Enemies", "Level2", "Level3_Boss" };
    static readonly string[] Labels = { "Level 1", "Enemies", "Level 2", "Boss" };

    static readonly Color Panel = new Color(0.07f, 0.09f, 0.16f, 0.9f);
    static readonly Color Accent = new Color(1f, 0.55f, 0.1f, 1f);
    static readonly Color Glass = new Color(1f, 1f, 1f, 0.22f);

    static TMP_FontAsset font;

    [MenuItem("Week10/Add Level Select (menu + pause)")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int n = 0;
        foreach (string lvl in Scenes)
        {
            if (!Exists(lvl)) continue;
            EditorSceneManager.OpenScene(S + lvl + ".unity");
            if (AddToPause(lvl)) n++;
            Save();
        }

        bool menu = false;
        if (Exists("MainMenu"))
        {
            EditorSceneManager.OpenScene(S + "MainMenu.unity");
            menu = AddToMainMenu();
            Save();
            EditorSceneManager.OpenScene(S + "MainMenu.unity");
        }

        EditorUtility.DisplayDialog("Week10",
            "Level buttons added to the pause menu in " + n + " levels" + (menu ? " and to the Main Menu." : ".") +
            "\n\nOn the phone: tap II (pause) and choose a level.\nRebuild the APK: Week10 > Build Android APK.", "OK");
    }

    // ---------- pause card ----------
    static bool AddToPause(string current)
    {
        PauseMenu pm = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pm == null || pm.pausePanel == null) return false;
        Transform card = Find(pm.pausePanel.transform, "Card");
        if (card == null) return false;
        font = AnyFont();

        RectTransform crt = card.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(760, 940);

        Remove(card, "LevelsTitle");
        Remove(card, "LevelRow");

        LevelLoader ll = Get<LevelLoader>(pm.gameObject);

        MakeText(card, "LevelsTitle", "LEVELS", 40, new Vector2(0, -285), new Vector2(600, 60));

        RectTransform row = UI("LevelRow", card);
        Rect(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -375), new Vector2(720, 100));

        int count = 0;
        for (int i = 0; i < Scenes.Length; i++) if (Exists(Scenes[i])) count++;
        float w = 170f, gap = 10f, x = -(count * w + (count - 1) * gap) / 2f + w / 2f;
        for (int i = 0; i < Scenes.Length; i++)
        {
            if (!Exists(Scenes[i])) continue;
            MakeButton(row, "Btn_" + Scenes[i], Labels[i], Scenes[i] == current ? Accent : Glass,
                   new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(w, 100), 34, ll, Scenes[i]);
            x += w + gap;
        }
        return true;
    }

    // ---------- main menu: LEVELS column on the right ----------
    static bool AddToMainMenu()
    {
        Transform main = FindInScene("MainPanel");
        if (main == null) return false;
        font = AnyFont();

        Remove(main, "LevelColumn");
        MenuButtons mb = Object.FindAnyObjectByType<MenuButtons>(FindObjectsInactive.Include);
        LevelLoader ll = Get<LevelLoader>(mb != null ? mb.gameObject : main.gameObject);

        RectTransform col = UI("LevelColumn", main);
        Rect(col, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-50, -40), new Vector2(360, 640));
        Image bg = col.gameObject.AddComponent<Image>();
        SetSprite(bg, Panel);
        bg.raycastTarget = false;

        MakeText(col, "LevelsTitle", "LEVELS", 48, new Vector2(0, 250), new Vector2(320, 70));
        float y = 140f;
        for (int i = 0; i < Scenes.Length; i++)
        {
            if (!Exists(Scenes[i])) continue;
            MakeButton(col, "Btn_" + Scenes[i], Labels[i], i == 0 ? Accent : Glass,
                   new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(300, 100), 40, ll, Scenes[i]);
            y -= 120f;
        }
        return true;
    }

    // ================= helpers =================

    static void MakeButton(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 pos, Vector2 size,
                       float fontSize, LevelLoader ll, string scene)
    {
        RectTransform rt = UI(name, parent);
        Rect(rt, anchor, new Vector2(0.5f, 0.5f), pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        SetSprite(img, color);
        UnityEngine.UI.Button b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        b.colors = cb;
        UnityEventTools.AddStringPersistentListener(b.onClick, ll.Load, scene);

        TextMeshProUGUI t = MakeText(rt, "Text (TMP)", label, fontSize, Vector2.zero, Vector2.zero);
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(b);
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 box)
    {
        RectTransform rt = UI(name, parent);
        Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, box);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        return t;
    }

    static void SetSprite(Image img, Color color)
    {
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.35f;
        img.color = color;
    }

    static RectTransform UI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Rect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    static TMP_FontAsset AnyFont()
    {
        foreach (TextMeshProUGUI t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            if (t.font != null) return t.font;
        return null;
    }

    static T Get<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void Remove(Transform parent, string name)
    {
        Transform t;
        while ((t = Find(parent, name)) != null) Object.DestroyImmediate(t.gameObject);
    }

    static Transform Find(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static Transform FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = Find(root.transform, name);
            if (t != null) return t;
        }
        return null;
    }

    static bool Exists(string scene) { return AssetDatabase.LoadAssetAtPath<SceneAsset>(S + scene + ".unity") != null; }

    static void Save()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }
}
