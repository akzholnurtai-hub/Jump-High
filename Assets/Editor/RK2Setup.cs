using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.OnScreen;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class RK2Setup
{
    const string S = "Assets/Scenes/";
    const string GameName = "JUMP HIGH";

    static readonly Color Panel = new Color(0.07f, 0.09f, 0.16f, 0.78f);
    static readonly Color Accent = new Color(1f, 0.55f, 0.1f, 1f);
    static readonly Color FireRed = new Color(0.85f, 0.2f, 0.15f, 0.85f);
    static readonly Color Glass = new Color(1f, 1f, 1f, 0.22f);
    static readonly Color Ink = Color.white;

    static TMP_FontAsset font;

    static RK2Setup()
    {
        if (EditorPrefs.GetBool("RK2_MobileUI_v1")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorPrefs.SetBool("RK2_MobileUI_v1", true);
            Build();
        };
    }

    [MenuItem("RK2/Build Mobile UI")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        VersionControlSettings.mode = "Visible Meta Files";
        EditorSettings.serializationMode = SerializationMode.ForceText;
        PlayerSettings.productName = "Jump High";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        BuildLevel("Level1");
        BuildLevel("Level2");
        BuildLevel("Level3_Boss");
        BuildMenu("MainMenu", false);
        BuildMenu("Win", true);

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(S + "MainMenu.unity");
        EditorUtility.DisplayDialog("RK2", "Mobile UI is ready.\nCheck it in Window > General > Device Simulator.", "OK");
    }

    static void BuildLevel(string scene)
    {
        EditorSceneManager.OpenScene(S + scene + ".unity");
        Movement mv = Object.FindAnyObjectByType<Movement>();
        HUD hud = Object.FindAnyObjectByType<HUD>();
        Canvas canvas = hud.GetComponent<Canvas>().rootCanvas;
        font = mv.coinText.font;

        SetScaler(canvas);
        RectTransform safe = SafeAreaOf(canvas.transform);
        foreach (Transform t in Children(canvas.transform))
            if (t != safe && t.name != "PausePanel") t.SetParent(safe, false);

        for (int i = 0; i < hud.hearts.Length; i++)
            Place(hud.hearts[i], new Vector2(0, 1), new Vector2(40 + i * 115, -40), new Vector2(100, 100));

        bool boss = scene == "Level3_Boss";
        Transform icon = Find("CarrotIcon");
        RectTransform coinBox = Box(safe, "CoinBox", new Vector2(-170, -40), new Vector2(300, 100));
        if (icon != null)
        {
            icon.SetParent(coinBox, false);
            Place(icon, new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(70, 70));
        }
        mv.coinText.transform.SetParent(coinBox, false);
        BoxText(mv.coinText, icon != null ? 100 : 20, "0 / 0");

        RectTransform timeBox = Box(safe, "TimeBox", new Vector2(170, -40), new Vector2(300, 100));
        mv.timerText.transform.SetParent(timeBox, false);
        BoxText(mv.timerText, 20, "Time: 60");
        mv.timerText.alignment = TextAlignmentOptions.Center;

        coinBox.gameObject.SetActive(!boss);
        timeBox.gameObject.SetActive(!boss);
        mv.coinText.gameObject.SetActive(true);
        mv.timerText.gameObject.SetActive(true);
        if (icon != null) icon.gameObject.SetActive(true);

        Transform bossHp = Find("BossHP");
        if (bossHp != null)
        {
            bossHp.SetParent(safe, false);
            Place(bossHp, new Vector2(0.5f, 1), new Vector2(0, -95), new Vector2(600, 40));
        }

        Place(mv.winText, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1300, 160));
        StyleText(mv.winText, 60, new Color(1f, 0.93f, 0.55f));
        mv.winText.text = "";

        Hide("CountText");
        Hide("MessageTex");

        Transform stickBG = Find("StickBG");
        Place(stickBG, new Vector2(0, 0), new Vector2(70, 60), new Vector2(300, 300));
        SetImage(stickBG, Circle(), Glass);
        Transform knob = Find("Knob");
        Place(knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
        knob.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        SetImage(knob, Circle(), Accent);
        OnScreenStick stick = stickBG.GetComponentInChildren<OnScreenStick>(true);
        if (stick != null)
        {
            stick.movementRange = 85f;
            EditorUtility.SetDirty(stick);
        }

        Transform jump = Find("Btn_Jump");
        Place(jump, new Vector2(1, 0), new Vector2(-70, 70), new Vector2(230, 230));
        StyleButton(jump, Circle(), new Color(Accent.r, Accent.g, Accent.b, 0.88f), "JUMP", 46);

        Transform fire = Find("Btn_Fire");
        Place(fire, new Vector2(1, 0), new Vector2(-330, 170), new Vector2(180, 180));
        StyleButton(fire, Circle(), FireRed, "FIRE", 40);

        PauseMenu pm = canvas.GetComponent<PauseMenu>();
        if (pm == null) pm = canvas.gameObject.AddComponent<PauseMenu>();

        Transform pauseBtn = Find("Menu_Btn");
        if (pauseBtn == null) pauseBtn = Find("Btn_Pause");
        pauseBtn.name = "Btn_Pause";
        Place(pauseBtn, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(140, 140));
        StyleButton(pauseBtn, Rounded(), Panel, "II", 64);
        SetClick(pauseBtn.GetComponent<Button>(), pm.Pause);

        pm.pausePanel = BuildPausePanel(canvas.transform, pm);
        EditorUtility.SetDirty(pm);

        SaveScene();
    }

    static GameObject BuildPausePanel(Transform canvas, PauseMenu pm)
    {
        Transform old = canvas.Find("PausePanel");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        RectTransform panel = NewUI("PausePanel", canvas);
        Stretch(panel);
        Image dim = panel.gameObject.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.6f);

        RectTransform safe = NewUI("SafeArea", panel);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeArea>();

        RectTransform card = NewUI("Card", safe);
        Place(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 760));
        card.pivot = new Vector2(0.5f, 0.5f);
        SetImage(card, Rounded(), Panel);

        TextMeshProUGUI title = NewText("Title", card, "PAUSED", 96);
        Place(title, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(640, 130));

        NewButton("Btn_Resume", card, "Resume", Accent, new Vector2(0, 110), pm.Resume);
        NewButton("Btn_Restart", card, "Restart", Glass, new Vector2(0, -40), pm.Restart);
        NewButton("Btn_Menu", card, "Menu", Glass, new Vector2(0, -190), pm.GoToMenu);

        panel.SetAsLastSibling();
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    static void BuildMenu(string scene, bool win)
    {
        EditorSceneManager.OpenScene(S + scene + ".unity");
        Transform main = Find("MainPanel");
        Canvas canvas = main.GetComponentInParent<Canvas>().rootCanvas;
        font = Find("Btn_Start").GetComponentInChildren<TextMeshProUGUI>(true).font;

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace) SetScaler(c);

        Transform bg = canvas.transform.Find("Background");
        if (bg == null)
        {
            RectTransform r = NewUI("Background", canvas.transform);
            r.gameObject.AddComponent<Image>();
            bg = r;
        }
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = win ? new Color(0.1f, 0.07f, 0.02f, 0.75f) : new Color(0.04f, 0.06f, 0.14f, 0.7f);
        bg.GetComponent<Image>().raycastTarget = false;
        bg.SetAsFirstSibling();

        RectTransform safe = SafeAreaOf(canvas.transform);
        foreach (Transform t in Children(canvas.transform))
            if (t != safe && t != bg) t.SetParent(safe, false);

        foreach (string p in new[] { "MainPanel", "SettingsPanel", "HelpPanel" })
        {
            Transform t = Find(p);
            if (t == null) continue;
            Stretch(t.GetComponent<RectTransform>());
            Image img = t.GetComponent<Image>();
            if (img != null) img.color = new Color(0, 0, 0, p == "MainPanel" ? 0f : 0.55f);
        }

        Transform titleT = Find("Title");
        TextMeshProUGUI title = titleT != null ? titleT.GetComponent<TextMeshProUGUI>() : NewText("Title", main, "", 120);
        title.transform.SetParent(main, false);
        Place(title, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1300, 170));
        StyleText(title, win ? 130 : 120, win ? new Color(1f, 0.85f, 0.3f) : Accent);
        title.text = win ? "VICTORY!" : GameName;

        Transform field = Find("Field_Name");
        if (field != null)
        {
            field.gameObject.SetActive(!win);
            Place(field, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(560, 100));
            field.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            TMP_InputField input = field.GetComponent<TMP_InputField>();
            if (input != null)
            {
                if (input.textComponent != null) input.textComponent.fontSize = 42;
                TextMeshProUGUI ph = input.placeholder as TextMeshProUGUI;
                if (ph != null) { ph.fontSize = 42; ph.text = "Your name..."; }
            }
        }

        if (win)
        {
            MenuButton("Btn_Start", "Menu", Accent, 60);
            MenuButton("Btn_Quit", "Quit", Glass, -90);
        }
        else
        {
            MenuButton("Btn_Start", "PLAY", Accent, 110);
            MenuButton("Btn_Help", "How to play", Glass, -35);
            MenuButton("Btn_Settings", "Settings", Glass, -180);
            MenuButton("Btn_Quit", "Quit", Glass, -325);
        }

        BuildSettings();
        BuildHelp();
        SaveScene();
    }

    static void MenuButton(string name, string label, Color color, float y)
    {
        Transform t = Find(name);
        if (t == null) return;
        Place(t, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(520, 120));
        t.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        StyleButton(t, Rounded(), color, label, 52);
    }

    static void BuildSettings()
    {
        Transform panel = Find("SettingsPanel");
        if (panel == null) return;

        float[] ys = { 260, 170, 90, 0, -80 };
        string[] order = { "Dropdown_Color", "Toggle_Fullscreen", "Toggle_Music", "Text (TMP)", "Slider" };
        for (int i = 0; i < order.Length; i++)
        {
            Transform t = FindChild(panel, order[i]);
            if (t == null) continue;
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, ys[i]);
            rt.localScale = Vector3.one * (order[i] == "Text (TMP)" ? 1f : 2.5f);
            if (order[i] == "Text (TMP)")
            {
                TextMeshProUGUI txt = t.GetComponent<TextMeshProUGUI>();
                rt.sizeDelta = new Vector2(600, 70);
                StyleText(txt, 44, Ink);
                txt.text = "Music volume";
            }
        }

        Transform back = FindChild(panel, "Btn_Back");
        if (back != null)
        {
            Place(back, new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(400, 120));
            back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            StyleButton(back, Rounded(), Glass, "Back", 52);
        }
    }

    static void BuildHelp()
    {
        Transform panel = Find("HelpPanel");
        if (panel == null) return;

        Transform scroll = FindChild(panel, "Scroll View");
        if (scroll != null)
        {
            Place(scroll, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1300, 640));
            scroll.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            SetImage(scroll, Rounded(), Panel);

            Transform content = FindChild(scroll, "Content");
            TextMeshProUGUI txt = content.GetComponentInChildren<TextMeshProUGUI>(true);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(crt.sizeDelta.x, 760);

            RectTransform trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -30);
            trt.sizeDelta = new Vector2(-80, 720);
            StyleText(txt, 40, Ink);
            txt.alignment = TextAlignmentOptions.TopLeft;
            txt.textWrappingMode = TextWrappingModes.Normal;
            txt.text =
                "<b><color=#FF8C1A>GOAL</color></b>  Collect every coin. Then walk into the door to open the next level.\n\n" +
                "<b>Level 1</b>  learn to run and jump, avoid falling bombs.\n" +
                "<b>Level 2</b>  enemies patrol the platforms.\n" +
                "<b>Boss</b>  shoot the boss, dodge fireballs and red circles.\n\n" +
                "<b><color=#FF8C1A>CONTROLS</color></b>  Joystick: move.  JUMP: jump.  FIRE: shoot.  II: pause.\n\n" +
                "You have 3 hearts and 60 seconds per level. Pick up hearts to heal.";
        }

        Transform back = FindChild(panel, "back");
        if (back != null)
        {
            Place(back, new Vector2(0.5f, 0.5f), new Vector2(0, -360), new Vector2(400, 120));
            back.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            StyleButton(back, Rounded(), Glass, "Back", 52);
        }
    }

    static void SetScaler(Canvas canvas)
    {
        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) cs = canvas.gameObject.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight = 1f;
        EditorUtility.SetDirty(cs);
    }

    static RectTransform SafeAreaOf(Transform canvas)
    {
        Transform t = canvas.Find("SafeArea");
        RectTransform rt = t != null ? t.GetComponent<RectTransform>() : NewUI("SafeArea", canvas);
        Stretch(rt);
        if (rt.GetComponent<SafeArea>() == null) rt.gameObject.AddComponent<SafeArea>();
        return rt;
    }

    static RectTransform Place(Component c, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = c.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static RectTransform Box(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        Transform t = Find(name);
        RectTransform rt = t != null ? t.GetComponent<RectTransform>() : NewUI(name, parent);
        rt.SetParent(parent, false);
        Place(rt, new Vector2(0.5f, 1), pos, size);
        SetImage(rt, Rounded(), Panel);
        return rt;
    }

    static void BoxText(TextMeshProUGUI t, float left, string sample)
    {
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, 0);
        rt.offsetMax = new Vector2(-20, 0);
        rt.localScale = Vector3.one;
        StyleText(t, 48, Ink);
        t.alignment = TextAlignmentOptions.Left;
        t.text = sample;
    }

    static void StyleText(TextMeshProUGUI t, float size, Color color)
    {
        if (font != null) t.font = font;
        t.enableAutoSizing = false;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        EditorUtility.SetDirty(t);
    }

    static void StyleButton(Transform btn, Sprite sprite, Color color, string label, float size)
    {
        SetImage(btn, sprite, color);

        Button b = btn.GetComponent<Button>();
        if (b != null)
        {
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cb.selectedColor = Color.white;
            b.colors = cb;
            EditorUtility.SetDirty(b);
        }

        TextMeshProUGUI t = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (t == null) t = NewText("Text (TMP)", btn, label, size);
        Stretch(t.GetComponent<RectTransform>());
        StyleText(t, size, Ink);
        t.text = label;
    }

    static void SetImage(Component c, Sprite sprite, Color color)
    {
        Image img = c.GetComponent<Image>();
        if (img == null) img = c.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite == Rounded() ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 0.35f;
        img.color = color;
        EditorUtility.SetDirty(img);
    }

    static Sprite Rounded()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    static Sprite Circle()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size)
    {
        TextMeshProUGUI t = NewUI(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(t, size, Ink);
        t.text = text;
        return t;
    }

    static void NewButton(string name, Transform parent, string label, Color color, Vector2 pos, UnityAction action)
    {
        RectTransform rt = NewUI(name, parent);
        Place(rt, new Vector2(0.5f, 0.5f), pos, new Vector2(520, 120));
        rt.gameObject.AddComponent<Image>();
        Button b = rt.gameObject.AddComponent<Button>();
        StyleButton(rt, Rounded(), color, label, 52);
        SetClick(b, action);
    }

    static void SetClick(Button b, UnityAction action)
    {
        while (b.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(b.onClick, 0);
        UnityEventTools.AddPersistentListener(b.onClick, action);
        EditorUtility.SetDirty(b);
    }

    static Transform[] Children(Transform t)
    {
        Transform[] list = new Transform[t.childCount];
        for (int i = 0; i < t.childCount; i++) list[i] = t.GetChild(i);
        return list;
    }

    static Transform Find(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = FindChild(root.transform, name);
            if (t != null) return t;
        }
        return null;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void Hide(string name)
    {
        Transform t = Find(name);
        if (t != null) t.gameObject.SetActive(false);
    }

    static void SaveScene()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }
}
