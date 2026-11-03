using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class MidtermSetup
{
    const string S = "Assets/Scenes/";

    [MenuItem("Midterm/Setup")]
    static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        AudioClip jump = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/jump.wav");
        AudioClip win = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/win.wav");
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>(S + "tatamusic-game-game-music-599001.mp3");

        EditorSceneManager.OpenScene(S + "Level1.unity");
        Movement p = Object.FindAnyObjectByType<Movement>();
        p.gameObject.tag = "Player";
        p.nextScene = "Level2";
        p.jumpSound = jump;
        if (p.audioSource == null) p.audioSource = p.GetComponent<AudioSource>();
        AddMusic(music);
        SaveScene();

        Copy("Level1", "Level2");
        Copy("Level1", "Level3_Boss");
        Copy("MainMenu", "Win");

        SetupLevel2();
        SetupBoss();
        SetupWin(win);

        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(S + "MainMenu.unity", true),
            new EditorBuildSettingsScene(S + "Level1.unity", true),
            new EditorBuildSettingsScene(S + "Level2.unity", true),
            new EditorBuildSettingsScene(S + "Level3_Boss.unity", true),
            new EditorBuildSettingsScene(S + "Win.unity", true),
        };
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        EditorSceneManager.OpenScene(S + "MainMenu.unity");
        EditorUtility.DisplayDialog("Midterm", "Дайын! MainMenu-дан Play басып тексеріңіз.", "OK");
    }

    static void SetupLevel2()
    {
        EditorSceneManager.OpenScene(S + "Level2.unity");
        Object.FindAnyObjectByType<Movement>().nextScene = "Level3_Boss";

        foreach (Hazard h in Object.FindObjectsByType<Hazard>())
        {
            if (!h.name.StartsWith("Enemy") || h.name == "Enemy (6)") continue;

            Patrol pt = h.gameObject.AddComponent<Patrol>();
            pt.pointA = MakePoint(h.name + "_A", h.transform.position + Vector3.left * 2.5f);
            pt.pointB = MakePoint(h.name + "_B", h.transform.position + Vector3.right * 2.5f);
            pt.speed = 2f;
        }

        Spawner sp = Object.FindAnyObjectByType<Spawner>();
        if (sp != null) sp.interval *= 0.6f;

        SaveScene();
    }

    static void SetupBoss()
    {
        EditorSceneManager.OpenScene(S + "Level3_Boss.unity");
        Movement p = Object.FindAnyObjectByType<Movement>();
        p.nextScene = "Win";
        p.timeLimit = 999f;

        foreach (GameObject go in AllObjects())
        {
            if (go == null) continue;
            bool remove = go.CompareTag("Coin") || go.CompareTag("Exit") || go.CompareTag("Enemy")
                || go.name.StartsWith("Enemy") || go.name.StartsWith("Door")
                || go.GetComponent<Spawner>() != null || go.GetComponent<SurvivalTimer>() != null;
            if (remove) Object.DestroyImmediate(go);
        }

        Transform plane = Find("Plane");
        float groundY = plane != null ? plane.position.y : 0f;
        float shotY = p.GetComponent<Collider>().bounds.center.y;

        GameObject fireball = MakeFireball();
        GameObject warning = MakeWarning();

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Hero.fbx");
        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(model);
        PrefabUtility.UnpackPrefabInstance(boss, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        boss.name = "Boss";
        boss.transform.position = new Vector3(p.transform.position.x, groundY, p.transform.position.z + 18f);
        boss.transform.localScale = Vector3.one * 2.5f;
        Vector3 toPlayer = p.transform.position - boss.transform.position;
        toPlayer.y = 0;
        boss.transform.rotation = Quaternion.LookRotation(toPlayer);

        Material red = MakeMat("Assets/Prefabs/Boss_Mat.mat", new Color(0.75f, 0.12f, 0.1f), false);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == null || !mats[i].name.Contains("Eyes")) mats[i] = red;
            r.sharedMaterials = mats;
        }

        Bounds b = new Bounds(boss.transform.position, Vector3.zero);
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        float s = boss.transform.lossyScale.y;
        float height = b.size.y > 0.1f ? b.size.y / s : 2f;
        CapsuleCollider col = boss.AddComponent<CapsuleCollider>();
        col.height = height;
        col.radius = height * 0.2f;
        col.center = new Vector3(0, height / 2f, 0);

        Animator anim = boss.GetComponent<Animator>();
        if (anim == null) anim = boss.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Models/Animations/PlayerAnimator.controller");
        anim.applyRootMotion = false;

        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(boss.transform, false);
        fp.transform.position = new Vector3(boss.transform.position.x, shotY, boss.transform.position.z) + boss.transform.forward * 1.5f;

        Boss bs = boss.AddComponent<Boss>();
        bs.player = p.transform;
        bs.firePoint = fp.transform;
        bs.fireballPrefab = fireball;
        bs.warningPrefab = warning;
        bs.rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rock.prefab");
        bs.groundY = groundY + 0.02f;
        bs.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/freesound_community-breaking-small-bone-finger-88085.mp3");
        bs.anim = anim;
        bs.hpBar = MakeHpBar();

        Hazard hz = boss.AddComponent<Hazard>();
        hz.damage = 1;
        hz.destroyOnHit = false;
        hz.lifeTime = 0f;

        SaveScene();
    }

    static void SetupWin(AudioClip win)
    {
        EditorSceneManager.OpenScene(S + "Win.unity");

        Transform start = Find("Btn_Start");
        Find("Btn_Settings").gameObject.SetActive(false);
        Find("Btn_Help").gameObject.SetActive(false);

        Canvas canvas = start.GetComponentInParent<Canvas>().rootCanvas;
        WinScreen ws = canvas.gameObject.AddComponent<WinScreen>();

        Button btn = start.GetComponent<Button>();
        while (btn.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(btn.onClick, 0);
        UnityEventTools.AddPersistentListener(btn.onClick, ws.GoToMenu);

        TextMeshProUGUI label = start.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = "В меню";

        GameObject title = Object.Instantiate(label.gameObject, start.parent);
        title.name = "Title";
        TextMeshProUGUI t = title.GetComponent<TextMeshProUGUI>();
        t.text = "ПОБЕДА!";
        t.enableAutoSizing = false;
        t.fontSize = 110;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform rt = title.GetComponent<RectTransform>();
        RectTransform srt = start.GetComponent<RectTransform>();
        rt.anchorMin = srt.anchorMin;
        rt.anchorMax = srt.anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 160);
        rt.anchoredPosition = srt.anchoredPosition + new Vector2(0, 200);

        GameObject snd = new GameObject("WinSound");
        AudioSource a = snd.AddComponent<AudioSource>();
        a.clip = win;
        a.playOnAwake = true;

        SaveScene();
    }

    static MidtermSetup()
    {
        if (EditorPrefs.GetBool("MidtermRemake_v2")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorPrefs.SetBool("MidtermRemake_v2", true);
            Remake();
        };
    }

    [MenuItem("Midterm/Remake Level2 and Boss")]
    static void Remake()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        RemakeLevel2();
        RemakeBoss();
        EditorSceneManager.OpenScene(S + "Level2.unity");
        EditorUtility.DisplayDialog("Midterm", "Level2 мен Boss картасы өзгерді.", "OK");
    }

    static void RemakeLevel2()
    {
        EditorSceneManager.OpenScene(S + "Level2.unity");
        if (FindRoot("Map") != null) return;

        Transform pc = FindRoot("Platform_C");
        if (pc != null)
        {
            Vector3 pos = pc.position;
            Vector3 sc = pc.localScale;
            float left = pos.x - sc.x / 2f;
            float right = pos.x + sc.x / 2f;
            float gapL = -0.5f, gapR = 2.5f;

            GameObject second = Object.Instantiate(pc.gameObject);
            second.name = "Platform_C (2)";
            pc.localScale = new Vector3(gapL - left, sc.y, sc.z);
            pc.position = new Vector3((left + gapL) / 2f, pos.y, pos.z);
            second.transform.localScale = new Vector3(right - gapR, sc.y, sc.z);
            second.transform.position = new Vector3((gapR + right) / 2f, pos.y, pos.z);
        }

        GameObject map = new GameObject("Map");
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go == map || go.name == "Plane") continue;
            bool isMap = go.CompareTag("Ground") || go.CompareTag("Coin") || go.CompareTag("Exit")
                || go.CompareTag("Enemy") || go.name.StartsWith("Enemy") || go.name.StartsWith("Door")
                || go.name.StartsWith("Dominoes")
                || go.name.EndsWith("_A") || go.name.EndsWith("_B");
            if (isMap) go.transform.SetParent(map.transform, true);
        }
        map.transform.localScale = new Vector3(-1f, 1f, 1f);

        Transform pb = FindChild(map.transform, "Platform_B");
        Material islMat = TintedCopy(pb.GetComponent<Renderer>().sharedMaterial, new Color(1f, 0.8f, 0.6f), "Level2");
        GameObject coin = GameObject.FindGameObjectWithTag("Coin");
        Vector3[] steps =
        {
            new Vector3(0f, 0.9f, -6f), new Vector3(0f, 1.9f, -2f),
            new Vector3(0f, 2.9f, 2f),  new Vector3(0f, 3.6f, 6f),
        };
        for (int i = 0; i < steps.Length; i++)
        {
            MakeBlock("Step (" + (i + 1) + ")", steps[i], new Vector3(2f, 0.3f, 2f), islMat, pb.gameObject.layer);
            if (coin != null && i < 3)
                Object.Instantiate(coin, steps[i] + Vector3.up * 1.2f, coin.transform.rotation).name = "Coin Step " + (i + 1);
        }

        Tint(map, new Color(1f, 0.8f, 0.6f), "Level2");
        Tint(FindRoot("Plane").gameObject, new Color(1f, 0.8f, 0.6f), "Level2");
        SetSky("Sky_Level2", new Color(1f, 0.55f, 0.35f), new Color(0.35f, 0.25f, 0.2f), new Color(0.95f, 0.65f, 0.45f), 20f, 60f);
        SetSun(new Color(1f, 0.82f, 0.62f));
        SaveScene();
    }

    static void RemakeBoss()
    {
        EditorSceneManager.OpenScene(S + "Level3_Boss.unity");
        if (FindRoot("Island (1)") != null) return;

        Transform pb = FindRoot("Platform_B");
        Color dark = new Color(0.55f, 0.45f, 0.5f);
        Material mat = TintedCopy(pb.GetComponent<Renderer>().sharedMaterial, dark, "Boss");
        int layer = pb.gameObject.layer;

        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == "Plane") continue;
            if (go.CompareTag("Ground") || go.name.StartsWith("Dominoes"))
                Object.DestroyImmediate(go);
        }

        Vector3[] islands =
        {
            new Vector3(-6f, 1.2f, -3f), new Vector3(6f, 1.2f, -3f),
            new Vector3(-9f, 2.2f, 3f),  new Vector3(9f, 2.2f, 3f),
        };
        for (int i = 0; i < islands.Length; i++)
            MakeBlock("Island (" + (i + 1) + ")", islands[i], new Vector3(3f, 0.3f, 3f), mat, layer);

        MakeBlock("Pillar (1)", new Vector3(-4f, 1.5f, -5f), new Vector3(1.5f, 3f, 1.5f), mat, layer);
        MakeBlock("Pillar (2)", new Vector3(4f, 1.5f, -5f), new Vector3(1.5f, 3f, 1.5f), mat, layer);

        Movement p = Object.FindAnyObjectByType<Movement>();
        if (p.coinText != null) p.coinText.gameObject.SetActive(false);
        if (p.timerText != null) p.timerText.gameObject.SetActive(false);
        Transform icon = Find("CarrotIcon");
        if (icon != null) icon.gameObject.SetActive(false);

        Tint(FindRoot("Plane").gameObject, dark, "Boss");
        SetSky("Sky_Boss", new Color(0.5f, 0.1f, 0.15f), new Color(0.15f, 0.05f, 0.05f), new Color(0.35f, 0.08f, 0.1f), 15f, 45f);
        SetSun(new Color(1f, 0.6f, 0.5f));
        SaveScene();
    }

    static void SetSun(Color c)
    {
        foreach (Light l in Object.FindObjectsByType<Light>())
            if (l.type == LightType.Directional) l.color = c;
    }

    static void MakeBlock(string name, Vector3 pos, Vector3 size, Material mat, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.tag = "Ground";
        go.layer = layer;
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material TintedCopy(Material src, Color tint, string folder)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/" + folder)) AssetDatabase.CreateFolder("Assets/Materials", folder);

        string path = "Assets/Materials/" + folder + "/" + src.name + ".mat";
        if (AssetDatabase.GetAssetPath(src) == path) return src;

        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(src);
            if (src.HasProperty("_BaseColor") || src.HasProperty("_Color"))
                m.color = src.color * tint;
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    static void Tint(GameObject root, Color tint, string folder)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r.CompareTag("Coin") || r.GetComponentInParent<Hazard>() != null) continue;
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null) mats[i] = TintedCopy(mats[i], tint, folder);
            r.sharedMaterials = mats;
        }
    }

    static void SetSky(string name, Color sky, Color ground, Color fog, float fogStart, float fogEnd)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_SkyTint", sky);
        m.SetColor("_GroundColor", ground);
        m.SetFloat("_AtmosphereThickness", 1.4f);
        EditorUtility.SetDirty(m);

        RenderSettings.skybox = m;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
        if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static Transform FindRoot(string name)
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (go.name == name) return go.transform;
        return null;
    }

    static void Copy(string from, string to)
    {
        AssetDatabase.DeleteAsset(S + to + ".unity");
        AssetDatabase.CopyAsset(S + from + ".unity", S + to + ".unity");
    }

    static void SaveScene()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }

    static void AddMusic(AudioClip music)
    {
        if (music == null || Find("Music") != null) return;
        GameObject go = new GameObject("Music");
        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = music;
        a.loop = true;
        a.playOnAwake = true;
        a.volume = 0.25f;
    }

    static List<GameObject> AllObjects()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                list.Add(t.gameObject);
        return list;
    }

    static Transform Find(string name)
    {
        foreach (GameObject go in AllObjects())
            if (go.name == name) return go.transform;
        return null;
    }

    static Transform MakePoint(string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        return go.transform;
    }

    static Material MakeMat(string path, Color c, bool glow)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (glow)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject MakeFireball()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Fireball";
        go.transform.localScale = Vector3.one * 0.6f;
        go.GetComponent<SphereCollider>().isTrigger = true;
        go.GetComponent<Renderer>().sharedMaterial = MakeMat("Assets/Prefabs/Fireball_Mat.mat", new Color(1f, 0.38f, 0f), true);
        go.AddComponent<Rigidbody>().useGravity = false;
        Fireball f = go.AddComponent<Fireball>();
        f.lifeTime = 4f;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Fireball.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject MakeWarning()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Warning";
        go.transform.localScale = new Vector3(2f, 0.02f, 2f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = MakeMat("Assets/Prefabs/Warning_Mat.mat", new Color(0.9f, 0.1f, 0.1f), true);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Warning.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Slider MakeHpBar()
    {
        Canvas canvas = Object.FindAnyObjectByType<HUD>().GetComponent<Canvas>();

        DefaultControls.Resources res = new DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        GameObject go = DefaultControls.CreateSlider(res);
        go.name = "BossHP";
        go.transform.SetParent(canvas.transform, false);

        Slider s = go.GetComponent<Slider>();
        s.interactable = false;
        s.handleRect = null;
        Object.DestroyImmediate(go.transform.Find("Handle Slide Area").gameObject);
        go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = new Color(0.85f, 0.15f, 0.1f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(600, 30);
        rt.anchoredPosition = new Vector2(0, -80);

        GameObject label = new GameObject("BossLabel", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        TextMeshProUGUI t = label.AddComponent<TextMeshProUGUI>();
        TextMeshProUGUI any = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
        if (any != null) t.font = any.font;
        t.text = "BOSS";
        t.fontSize = 36;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1);
        lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0.5f, 0);
        lr.sizeDelta = new Vector2(0, 45);
        lr.anchoredPosition = new Vector2(0, 4);

        return s;
    }
}
