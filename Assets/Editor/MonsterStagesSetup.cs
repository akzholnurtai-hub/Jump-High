using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using TMPro;

// "Monsters > 1. Build 5 stages" turns Jump High into a monster fight through abandoned places.
//  - 3 monsters made from primitives on top of the Week 10 AI: Ghoul (melee, Patroller),
//    Spitter (spits from range, Shooter), Brute (charges, Charger). Prefabs: Assets/Prefabs/Monsters.
//  - 5 stages, easy -> hard: Dead Street, Rust Factory, Metro Tunnel, Ghost Hospital, Monster Lair (boss).
//    Scenes: Assets/Scenes/Stages, data: Assets/Data/Stages (enemies, time, falling debris).
//  - GameConfig and Build Settings now use only the new stages (old scenes stay in the project).
// The stages are kept simple on purpose: a corridor of plain walls, obstacles, monsters, an exit door.
// Safe to run again: the stage scenes are rebuilt, monster numbers set by hand are kept.
public static class MonsterStagesSetup
{
    const string MatDir = "Assets/Materials/Abandoned";
    const string MonsterDir = "Assets/Prefabs/Monsters";
    const string MonsterDataDir = "Assets/Data/Monsters";
    const string StageDataDir = "Assets/Data/Stages";
    const string StageSceneDir = "Assets/Scenes/Stages";
    const string Template = "Assets/Scenes/Level_Enemies.unity";
    const string BossTemplate = "Assets/Scenes/Level3_Boss.unity";
    const int GroundLayer = 6, WallsLayer = 8;
    const float Half = 7f;                      // m, half width of the walkable path

    static readonly string[] Names = { "Dead Street", "Rust Factory", "Metro Tunnel", "Ghost Hospital", "Monster Lair" };
    static readonly string[] KeepRoots = { "LevelRunner", "EventSystem", "Main Camera", "Directional Light", "Global Volume", "Canvas", "Spawner" };

    static Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static System.Random rng;
    static Transform map;
    static LevelData cur;
    static List<Vector3> coins = new List<Vector3>();
    static EnemyStats ghoul, spitter, brute;

    // =====================================================================
    [MenuItem("Monsters/1. Build 5 stages")]
    static void Menu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("Monsters", "Stop Play mode first.", "OK"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string log = Build();
        Debug.Log("[Monsters]\n" + log);
        EditorUtility.DisplayDialog("Monsters - done", log + "\nOpen MainMenu and press Play.", "OK");
    }

    public static void RunBatch()
    {
        string log = Build();
        Debug.Log("[Monsters]\n" + log);
        EditorApplication.Exit(log.Contains("! ") ? 1 : 0);
    }

    static string Build()
    {
        StringBuilder log = new StringBuilder();
        foreach (string d in new[] { MatDir, MonsterDir, MonsterDataDir, StageDataDir, StageSceneDir }) EnsureFolder(d);

        MakeMaterials();
        SkullSprite();
        GameObject gP = MakeMonster("Ghoul", "Assets/Prefabs/Enemies/SpiderBot.prefab", BuildGhoul);
        GameObject sP = MakeMonster("Spitter", "Assets/Prefabs/Enemies/JetMech.prefab", BuildSpitter);
        GameObject bP = MakeMonster("Brute", "Assets/Prefabs/Enemies/RhinoTank.prefab", BuildBrute);
        ghoul = MakeStats<PatrollerStats>("Ghoul", "Assets/Data/Enemies/SpiderBot.asset", gP, s =>
        { s.maxHp = 3; s.chaseSpeed = 4.6f; s.viewRange = 9f; s.killScore = 100; });
        spitter = MakeStats<ShooterStats>("Spitter", "Assets/Data/Enemies/JetMech.asset", sP, s =>
        { s.maxHp = 4; s.viewRange = 13f; s.killScore = 150; });
        brute = MakeStats<ChargerStats>("Brute", "Assets/Data/Enemies/RhinoTank.asset", bP, s =>
        { s.maxHp = 6; s.viewRange = 11f; s.killScore = 250; });
        log.AppendLine("Monsters: Ghoul, Spitter, Brute (" + MonsterDir + ")");

        LevelData[] data = new LevelData[5];
        for (int i = 0; i < 5; i++)
            data[i] = MakeStageData(i + 1, i == 4 ? "Assets/Data/Levels/Level_04.asset" : "Assets/Data/Levels/Level_02.asset");
        for (int i = 0; i < 5; i++) { data[i].nextLevel = i < 4 ? data[i + 1] : null; EditorUtility.SetDirty(data[i]); }
        AssetDatabase.SaveAssets();          // a later import (NavMesh asset) reloads unsaved assets from disk

        System.Action[] builders = { Stage1, Stage2, Stage3, Stage4 };
        for (int i = 0; i < 4; i++) log.AppendLine(BuildStage(i + 1, builders[i]));
        log.AppendLine(BuildBossStage());
        for (int i = 0; i < 5; i++) data[i] = Data(i + 1);

        GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
        for (int i = 0; i < 5; i++) data[i] = Data(i + 1);
        cfg.levels = new List<LevelData>(data);
        EditorUtility.SetDirty(cfg);

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + cfg.menuScene + ".unity", true));
        for (int i = 1; i <= 5; i++) scenes.Add(new EditorBuildSettingsScene(StageSceneDir + "/Stage" + i + ".unity", true));
        scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + cfg.winScene + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        log.AppendLine("GameConfig + Build Settings: MainMenu, Stage1-5, Win");

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/" + cfg.menuScene + ".unity");
        return log.ToString();
    }

    // =====================================================================
    // materials (URP Lit)
    static void MakeMaterials()
    {
        Mat("Asphalt", new Color(0.17f, 0.17f, 0.18f));
        Mat("Concrete", new Color(0.46f, 0.45f, 0.42f));
        Mat("ConcreteDark", new Color(0.27f, 0.27f, 0.26f));
        Mat("Brick", new Color(0.42f, 0.22f, 0.17f));
        Mat("Rust", new Color(0.42f, 0.24f, 0.12f), 0.25f, 0.4f);
        Mat("Metal", new Color(0.32f, 0.34f, 0.36f), 0.45f, 0.7f);
        Mat("Wood", new Color(0.38f, 0.27f, 0.16f));
        Mat("Stripe", new Color(0.8f, 0.62f, 0.08f));
        Mat("Tile", new Color(0.62f, 0.65f, 0.6f), 0.35f);
        Mat("TileDirty", new Color(0.45f, 0.5f, 0.44f), 0.3f);
        Mat("Toxic", new Color(0.25f, 0.85f, 0.15f), 0.9f, 0f, new Color(0.2f, 0.9f, 0.1f) * 1.4f);
        Mat("LampRed", new Color(1f, 0.1f, 0.05f), 0.5f, 0f, new Color(1f, 0.1f, 0.05f) * 3f);
        Mat("LampGreen", new Color(0.1f, 1f, 0.2f), 0.5f, 0f, new Color(0.1f, 1f, 0.2f) * 3f);
        Mat("ExitPad", new Color(0.1f, 0.6f, 0.2f), 0.5f, 0f, new Color(0.1f, 0.8f, 0.2f) * 1.5f);
        Mat("LampBulb", new Color(1f, 0.9f, 0.7f), 0.5f, 0f, new Color(1f, 0.85f, 0.6f) * 2.5f);
        // monsters
        Mat("GhoulSkin", new Color(0.36f, 0.43f, 0.31f), 0.35f);
        Mat("SpitterSkin", new Color(0.42f, 0.24f, 0.45f), 0.5f);
        Mat("BruteSkin", new Color(0.36f, 0.17f, 0.13f), 0.3f);
        Mat("BossSkin", new Color(0.3f, 0.06f, 0.08f), 0.4f);
        Mat("Bone", new Color(0.86f, 0.82f, 0.7f));
        Mat("Mouth", new Color(0.15f, 0.02f, 0.02f), 0.7f);
        Mat("EyeYellow", new Color(1f, 0.85f, 0.1f), 0.8f, 0f, new Color(1f, 0.8f, 0.05f) * 3f);
        Mat("EyeRed", new Color(1f, 0.1f, 0.05f), 0.8f, 0f, new Color(1f, 0.08f, 0.02f) * 3f);
    }

    static Material Mat(string name, Color c, float smooth = 0.12f, float metal = 0f, Color? emission = null)
    {
        string path = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.color = c;
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        mats[name] = m;
        return m;
    }

    static Material M(string name) => mats[name];

    // =====================================================================
    // monsters: a copy of the robot prefab (AI, collider, health stay), new body from primitives
    static GameObject MakeMonster(string name, string robotPath, System.Func<GameObject, Transform, Transform[]> body)
    {
        string path = MonsterDir + "/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) AssetDatabase.CopyAsset(robotPath, path);

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        root.name = name;
        Transform label = root.transform.Find("StateLabel");
        if (label != null && label.TryGetComponent(out TMP_Text text)) text.text = name;
        if (label != null) label.gameObject.SetActive(false);   // debug state text: hidden, it only confused players
        Transform model = root.transform.Find("Model");
        if (model == null) { model = new GameObject("Model").transform; model.SetParent(root.transform, false); }
        for (int i = model.childCount - 1; i >= 0; i--) Object.DestroyImmediate(model.GetChild(i).gameObject);
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = Vector3.one;

        Transform[] legs = body(root, model);
        EnemyAnimator anim = model.GetComponent<EnemyAnimator>();
        if (anim == null) anim = model.gameObject.AddComponent<EnemyAnimator>();
        anim.legs = legs;
        anim.flames = new Transform[0];

        EnemyBase ai = root.GetComponent<EnemyBase>();
        SerializedObject so = new SerializedObject(ai);
        so.FindProperty("model").objectReferenceValue = model;
        so.FindProperty("body").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static Transform[] BuildGhoul(GameObject root, Transform m)
    {
        m.GetComponent<EnemyAnimator>().style = EnemyAnimator.Style.Quadruped;
        Part(m, PrimitiveType.Capsule, "Torso", new Vector3(0, 1.18f, 0), new Vector3(0.62f, 0.5f, 0.48f), "GhoulSkin", new Vector3(18, 0, 0));
        for (int k = 0; k < 3; k++)
            Part(m, PrimitiveType.Cube, "Spine " + k, new Vector3(0, 1.3f + 0.13f * k, -0.24f + 0.02f * k), Vector3.one * 0.12f, "Bone", new Vector3(45, 0, 45));
        Part(m, PrimitiveType.Sphere, "Head", new Vector3(0, 1.72f, 0.28f), new Vector3(0.46f, 0.42f, 0.5f), "GhoulSkin");
        Part(m, PrimitiveType.Cube, "Jaw", new Vector3(0, 1.53f, 0.42f), new Vector3(0.34f, 0.1f, 0.3f), "Mouth", new Vector3(15, 0, 0));
        Part(m, PrimitiveType.Cube, "Tooth L", new Vector3(-0.08f, 1.6f, 0.53f), new Vector3(0.05f, 0.09f, 0.05f), "Bone");
        Part(m, PrimitiveType.Cube, "Tooth R", new Vector3(0.08f, 1.6f, 0.53f), new Vector3(0.05f, 0.09f, 0.05f), "Bone");
        Part(m, PrimitiveType.Sphere, "Eye L", new Vector3(-0.11f, 1.77f, 0.5f), Vector3.one * 0.1f, "EyeYellow");
        Part(m, PrimitiveType.Sphere, "Eye R", new Vector3(0.11f, 1.77f, 0.5f), Vector3.one * 0.1f, "EyeYellow");

        Transform[] arms = new Transform[2], legs = new Transform[2];
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? -1f : 1f;
            Transform arm = Pivot(m, (s == 0 ? "Arm L" : "Arm R"), new Vector3(0.4f * x, 1.42f, 0.12f));
            Part(arm, PrimitiveType.Capsule, "Upper", new Vector3(0, -0.3f, 0.05f), new Vector3(0.17f, 0.32f, 0.17f), "GhoulSkin");
            Part(arm, PrimitiveType.Capsule, "Fore", new Vector3(0, -0.78f, 0.14f), new Vector3(0.14f, 0.28f, 0.14f), "GhoulSkin", new Vector3(-15, 0, 0));
            Part(arm, PrimitiveType.Cube, "Claw", new Vector3(0, -1.08f, 0.2f), new Vector3(0.16f, 0.16f, 0.06f), "Bone", new Vector3(20, 0, 0));
            arms[s] = arm;

            Transform leg = Pivot(m, (s == 0 ? "Leg L" : "Leg R"), new Vector3(0.18f * x, 0.78f, 0));
            Part(leg, PrimitiveType.Capsule, "Thigh", new Vector3(0, -0.2f, 0.05f), new Vector3(0.22f, 0.22f, 0.22f), "GhoulSkin");
            Part(leg, PrimitiveType.Capsule, "Shin", new Vector3(0, -0.56f, -0.02f), new Vector3(0.18f, 0.22f, 0.18f), "GhoulSkin");
            Part(leg, PrimitiveType.Cube, "Foot", new Vector3(0, -0.74f, 0.08f), new Vector3(0.2f, 0.08f, 0.32f), "Bone");
            legs[s] = leg;
        }
        Body(root, new Vector3(0, 0.95f, 0), 0.42f, 1.95f, 1);
        if (root.TryGetComponent(out NavMeshAgent agent)) { agent.radius = 0.42f; agent.height = 1.95f; }
        return new[] { legs[0], legs[1], arms[1], arms[0] };     // left leg swings with the right arm
    }

    static Transform[] BuildSpitter(GameObject root, Transform m)
    {
        EnemyAnimator a = m.GetComponent<EnemyAnimator>();
        a.style = EnemyAnimator.Style.Hover;
        a.hoverBob = 0.12f;
        Part(m, PrimitiveType.Sphere, "Sac", new Vector3(0, 1.55f, 0), new Vector3(1f, 0.85f, 1f), "SpitterSkin");
        Part(m, PrimitiveType.Sphere, "Bump 1", new Vector3(-0.3f, 1.85f, -0.25f), Vector3.one * 0.35f, "SpitterSkin");
        Part(m, PrimitiveType.Sphere, "Bump 2", new Vector3(0.3f, 1.85f, -0.25f), Vector3.one * 0.32f, "SpitterSkin");
        Part(m, PrimitiveType.Sphere, "Bump 3", new Vector3(0, 1.97f, -0.1f), Vector3.one * 0.3f, "SpitterSkin");
        Part(m, PrimitiveType.Sphere, "Glow Belly", new Vector3(0, 1.4f, 0.37f), new Vector3(0.55f, 0.45f, 0.3f), "Toxic");
        Part(m, PrimitiveType.Cylinder, "Mouth", new Vector3(0, 1.62f, 0.55f), new Vector3(0.26f, 0.18f, 0.26f), "Mouth", new Vector3(90, 0, 0));
        Part(m, PrimitiveType.Cylinder, "Lip", new Vector3(0, 1.62f, 0.66f), new Vector3(0.3f, 0.03f, 0.3f), "Mouth", new Vector3(90, 0, 0));
        Part(m, PrimitiveType.Sphere, "Eye 1", new Vector3(-0.22f, 1.88f, 0.38f), Vector3.one * 0.12f, "EyeRed");
        Part(m, PrimitiveType.Sphere, "Eye 2", new Vector3(0.22f, 1.88f, 0.38f), Vector3.one * 0.12f, "EyeRed");
        Part(m, PrimitiveType.Sphere, "Eye 3", new Vector3(0, 1.98f, 0.42f), Vector3.one * 0.12f, "EyeRed");

        Transform[] t = new Transform[4];
        Vector3[] at = { new Vector3(-0.28f, 1.2f, 0.25f), new Vector3(0.28f, 1.2f, 0.25f), new Vector3(0.28f, 1.2f, -0.25f), new Vector3(-0.28f, 1.2f, -0.25f) };
        for (int i = 0; i < 4; i++)
        {
            t[i] = Pivot(m, "Tentacle " + i, at[i]);
            Part(t[i], PrimitiveType.Capsule, "Seg 1", new Vector3(0, -0.3f, 0), new Vector3(0.13f, 0.3f, 0.13f), "SpitterSkin");
            Part(t[i], PrimitiveType.Capsule, "Seg 2", new Vector3(0, -0.72f, 0.05f), new Vector3(0.09f, 0.22f, 0.09f), "SpitterSkin");
            Part(t[i], PrimitiveType.Sphere, "Tip", new Vector3(0, -0.92f, 0.08f), Vector3.one * 0.1f, "Toxic");
        }
        Transform fire = root.transform.Find("FirePoint");
        if (fire != null) fire.localPosition = new Vector3(0, 1.62f, 0.85f);
        Body(root, new Vector3(0, 1.1f, 0), 0.55f, 2.2f, 1);          // from the floor: low bullets hit too
        return t;
    }

    static Transform[] BuildBrute(GameObject root, Transform m)
    {
        m.GetComponent<EnemyAnimator>().style = EnemyAnimator.Style.Quadruped;
        Part(m, PrimitiveType.Capsule, "Body", new Vector3(0, 1.15f, -0.05f), new Vector3(1.05f, 0.95f, 1.05f), "BruteSkin", new Vector3(90, 0, 0));
        Part(m, PrimitiveType.Sphere, "Hump", new Vector3(0, 1.6f, 0.25f), new Vector3(0.95f, 0.7f, 0.9f), "BruteSkin");
        for (int k = 0; k < 4; k++)
            Part(m, PrimitiveType.Cube, "Spike " + k, new Vector3(0, 1.92f - 0.08f * k, 0.4f - 0.32f * k), new Vector3(0.14f, 0.42f, 0.14f), "Bone", new Vector3(-25, 0, 0));
        Part(m, PrimitiveType.Sphere, "Head", new Vector3(0, 1.25f, 1.0f), new Vector3(0.7f, 0.6f, 0.7f), "BruteSkin");
        Part(m, PrimitiveType.Cube, "Jaw", new Vector3(0, 1.0f, 1.18f), new Vector3(0.5f, 0.16f, 0.4f), "Mouth");
        Part(m, PrimitiveType.Cube, "Tusk L", new Vector3(-0.25f, 1.45f, 1.25f), new Vector3(0.1f, 0.1f, 0.55f), "Bone", new Vector3(-35, -15, 0));
        Part(m, PrimitiveType.Cube, "Tusk R", new Vector3(0.25f, 1.45f, 1.25f), new Vector3(0.1f, 0.1f, 0.55f), "Bone", new Vector3(-35, 15, 0));
        Part(m, PrimitiveType.Sphere, "Eye L", new Vector3(-0.17f, 1.35f, 1.3f), Vector3.one * 0.1f, "EyeRed");
        Part(m, PrimitiveType.Sphere, "Eye R", new Vector3(0.17f, 1.35f, 1.3f), Vector3.one * 0.1f, "EyeRed");

        Vector3[] at = { new Vector3(-0.42f, 0.85f, 0.55f), new Vector3(0.42f, 0.85f, 0.55f), new Vector3(0.42f, 0.85f, -0.6f), new Vector3(-0.42f, 0.85f, -0.6f) };
        Transform[] legs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            legs[i] = Pivot(m, "Leg " + i, at[i]);
            Part(legs[i], PrimitiveType.Capsule, "Leg", new Vector3(0, -0.32f, 0), new Vector3(0.32f, 0.38f, 0.32f), "BruteSkin");
            Part(legs[i], PrimitiveType.Cube, "Hoof", new Vector3(0, -0.78f, 0.05f), new Vector3(0.34f, 0.12f, 0.36f), "Bone");
        }
        Body(root, new Vector3(0, 0.95f, 0.25f), 0.9f, 1.9f, 1);      // from the floor: low bullets hit too
        return legs;
    }

    static void Body(GameObject root, Vector3 center, float radius, float height, int direction)
    {
        CapsuleCollider c = root.GetComponent<CapsuleCollider>();
        if (c == null) return;
        c.center = center; c.radius = radius; c.height = height; c.direction = direction;
    }

    static T MakeStats<T>(string name, string fromPath, GameObject prefab, System.Action<T> firstTime) where T : EnemyStats
    {
        string path = MonsterDataDir + "/" + name + ".asset";
        bool isNew = AssetDatabase.LoadAssetAtPath<T>(path) == null;
        if (isNew) AssetDatabase.CopyAsset(fromPath, path);
        T s = AssetDatabase.LoadAssetAtPath<T>(path);
        s.displayName = name;
        s.prefab = prefab;
        if (isNew) firstTime(s);                     // later runs keep the numbers changed in the Inspector
        EditorUtility.SetDirty(s);
        return s;
    }

    static LevelData MakeStageData(int n, string fromPath)
    {
        string path = StageDataDir + "/Stage_0" + n + ".asset";
        if (AssetDatabase.LoadAssetAtPath<LevelData>(path) == null) AssetDatabase.CopyAsset(fromPath, path);
        LevelData d = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        d.levelNumber = n;
        d.displayName = Names[n - 1];
        d.sceneName = "Stage" + n;
        d.goal = n == 5 ? LevelGoal.DefeatBoss : LevelGoal.KillAllMonsters;
        d.coinsRequired = 0;
        d.clearBonus = 500 + 250 * n;
        d.timeBonusPerSecond = 10;
        d.killHeight = -10f;
        d.moverSpeedScale = 1f;
        d.enemies = new List<EnemySpawn>();
        return d;
    }

    // =====================================================================
    // stages
    static LevelData Data(int n) => AssetDatabase.LoadAssetAtPath<LevelData>(StageDataDir + "/Stage_0" + n + ".asset");

    // asset imports can unload objects held in memory: always take fresh ones by path
    static void ReloadMonsters()
    {
        ghoul = AssetDatabase.LoadAssetAtPath<EnemyStats>(MonsterDataDir + "/Ghoul.asset");
        spitter = AssetDatabase.LoadAssetAtPath<EnemyStats>(MonsterDataDir + "/Spitter.asset");
        brute = AssetDatabase.LoadAssetAtPath<EnemyStats>(MonsterDataDir + "/Brute.asset");
    }

    static string BuildStage(int n, System.Action layout)
    {
        string path = StageSceneDir + "/Stage" + n + ".unity";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(Template, path);
        Scene scene = EditorSceneManager.OpenScene(path);      // unloads unused assets, so load the data after it
        ReloadMonsters();
        LevelData data = Data(n);

        GameObject player = null, coin = null;
        foreach (GameObject r in scene.GetRootGameObjects())
        {
            if (r.CompareTag("Player")) { player = r; continue; }
            if (System.Array.IndexOf(KeepRoots, r.name) >= 0) continue;
            if (coin == null && r.CompareTag("Coin")) { coin = r; continue; }
            Object.DestroyImmediate(r);
        }
        if (player == null) return "! Stage" + n + ": no Player in " + Template;

        cur = data;
        rng = new System.Random(1000 + n);
        coins.Clear();
        map = new GameObject("Map").transform;
        layout();

        player.transform.SetPositionAndRotation(new Vector3(0, 1f, 1.5f), Quaternion.identity);
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null) cam.transform.SetPositionAndRotation(new Vector3(0, 5f, -5.5f), Quaternion.Euler(20, 0, 0));
        SetRunner(data);

        if (coin != null)
        {
            foreach (Vector3 p in coins)
            {
                GameObject c = Object.Instantiate(coin, p, coin.transform.rotation);
                c.name = "Coin";
            }
            Object.DestroyImmediate(coin);
        }

        GameObject sp = GameObject.Find("Spawner");
        if (sp == null) sp = new GameObject("Spawner");
        Spawner spawner = sp.GetComponent<Spawner>();
        if (spawner == null) spawner = sp.AddComponent<Spawner>();
        spawner.follow = player.transform;
        spawner.prefabs = new[] { Haz("Rock"), Haz("BombHazard"), Haz("Heart") };

        string result = "Stage" + n + " " + data.displayName + ": " + data.enemies.Count + " monsters, " + coins.Count + " coins, " + data.timeLimit + " s";
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssetIfDirty(data);
        Hud();
        Bake(map, StageSceneDir + "/Stage" + n + "_NavMesh.asset");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return result;
    }

    // ---------------------------------------------------------------- HUD
    const string SkullPath = "Assets/Textures/SkullIcon.png";      // made by Tools/make_skull_icon.py

    static void SkullSprite()
    {
        TextureImporter imp = AssetImporter.GetAtPath(SkullPath) as TextureImporter;
        if (imp == null || imp.textureType == TextureImporterType.Sprite) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
    }

    // Top of the screen: the coin box becomes the monster box (skull + monsters left),
    // and the big message goes to the centre (it was anchored to the left).
    static void Hud()
    {
        foreach (UnityEngine.UI.Image img in Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include))
            if (img.name == "CarrotIcon" || img.name == "MonsterIcon")
            {
                img.name = "MonsterIcon";
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkullPath);
                img.preserveAspect = true;
                EditorUtility.SetDirty(img);
            }
        foreach (TextMeshProUGUI t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
            if (t.name == "MessageTex")
            {
                RectTransform rt = t.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0, -170);
                rt.sizeDelta = new Vector2(1400, 220);
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.fontSize = 56;
                t.text = "";
                t.gameObject.SetActive(true);                     // it was switched off, so stage messages never showed
                HUD hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
                if (hud != null) { hud.messageText = t; EditorUtility.SetDirty(hud); }
                EditorUtility.SetDirty(t);
            }
    }

    static Hazard Haz(string prefab) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab").GetComponent<Hazard>();

    static void SetRunner(LevelData data)
    {
        LevelRunner runner = Object.FindAnyObjectByType<LevelRunner>();
        if (runner == null) runner = new GameObject("LevelRunner").AddComponent<LevelRunner>();
        SerializedObject so = new SerializedObject(runner);
        so.FindProperty("level").objectReferenceValue = data;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Bake(Transform root, string dataPath)
    {
        NavMeshSurface surf = root.gameObject.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.Children;
        surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surf.layerMask = (1 << GroundLayer) | (1 << WallsLayer);
        surf.BuildNavMesh();
        AssetDatabase.DeleteAsset(dataPath);
        if (surf.navMeshData != null) AssetDatabase.CreateAsset(surf.navMeshData, dataPath);
        EditorUtility.SetDirty(surf);
    }

    static void Hazards(bool on, float firstDelay = 0, float interval = 0, float rampEvery = 0, float minInterval = 0)
    {
        HazardWave w = cur.hazards;
        w.enabled = on;
        w.firstDelay = firstDelay;
        w.interval = interval;
        w.rampEvery = rampEvery;
        w.rampFactor = 0.85f;
        w.minInterval = minInterval;
        w.area = new Vector2(4f, 5f);
        w.height = 14f;
    }

    // ---------------------------------------------------------------- 1. Dead Street (easy)
    // Every stage is the same idea: a corridor with low walls, obstacles in the way,
    // monsters to kill and the exit door at the end. Stages differ only in numbers and colours.
    static void Stage1()
    {
        const float L = 100f;
        cur.timeLimit = 150f;
        Hazards(false);
        Corridor(L, "Asphalt", "Brick");
        Atmosphere(new Color(0.36f, 0.33f, 0.3f), 0.018f, new Color(1f, 0.78f, 0.55f), 0.9f);

        Crate(-4.5f, 0, 12, 1.2f); Crate(4, 0, 16, 1.2f);
        Barrier(-4.6f, 22, 4.4f); Barrier(0, 22, 4.4f); Barrier(4.6f, 22, 4.4f);      // first jump
        Coin(0, 1, 8); Coin(0, 2.2f, 22); Coin(4, 2.1f, 16);
        Ghoul(0, 31, -4, 28, 4, 35);
        SlideBlock(42, 2.5f);
        Crate(4, 0, 51, 1.2f); Crate(4, 1.2f, 51, 1.2f); Crate(5.2f, 0, 51, 1.2f); Coin(4, 3.1f, 51);
        Ghoul(1, 57, -3, 54, 4, 61);
        SpinBar(0, 66, 12);
        Toxic(-3, 74, 3, 4);
        Ghoul(-2, 82, -4, 78, 3, 86);
        Coin(0, 1, 40); Coin(0, 1, 62); Coin(0, 1, 90);
        Lamp(0, 5, 30); Lamp(0, 5, 60); Lamp(0, 5, 88);
        Gate(L - 4);
    }

    // ---------------------------------------------------------------- 2. Rust Factory
    static void Stage2()
    {
        const float L = 115f;
        cur.timeLimit = 200f;
        Hazards(true, 15f, 4f, 30f, 2.5f);
        Corridor(L, "ConcreteDark", "Rust");
        Atmosphere(new Color(0.3f, 0.25f, 0.2f), 0.022f, new Color(1f, 0.65f, 0.4f), 0.65f);

        Crate(-4, 0, 9, 1.2f); Crate(-4, 1.2f, 9, 1.2f); Coin(-4, 3.1f, 9);
        Toxic(-3, 20, 4, 5); Toxic(3.5f, 27, 3, 4);
        Ghoul(0, 24, -4, 22, 2, 30);
        SpinBar(0, 38, 12); Coin(0, 2.3f, 38);
        Box("Machine", new Vector3(-4.5f, 1.25f, 46), new Vector3(3, 2.5f, 3), "Metal", WallsLayer);
        Box("Machine", new Vector3(4.5f, 1.25f, 50), new Vector3(3, 2.5f, 3), "Metal", WallsLayer);
        Ghoul(-3, 55, -4, 53, 2, 59);
        SlideBlock(60, 3f);
        Spitter(4.5f, 64);
        Crate(-3, 0, 67, 1.2f); Crate(3, 0, 67, 1.2f);                                // steps up
        Box("Raised Floor", new Vector3(0, 1.2f, 75), new Vector3(Half * 2, 2.4f, 10), "Metal", GroundLayer);
        Coin(0, 3.4f, 72); Coin(0, 3.4f, 78);
        Ghoul(0, 76, -4, 74, 4, 78);
        Toxic(0, 86, 6, 3);
        SpinBar(0, 91, 12);
        Spitter(-4.5f, 96);
        Ghoul(3, 101, 0, 99, 4, 105);
        Coin(0, 1, 100);
        Lamp(0, 6, 20); Lamp(0, 6, 50); Lamp(0, 6, 80); Lamp(0, 6, 105);
        Gate(L - 4);
    }

    // ---------------------------------------------------------------- 3. Metro Tunnel
    static void Stage3()
    {
        const float L = 125f;
        cur.timeLimit = 220f;
        Hazards(true, 10f, 3.5f, 25f, 2.2f);
        Corridor(L, null, "TileDirty");                                             // floor with pits below
        Atmosphere(new Color(0.07f, 0.09f, 0.09f), 0.035f, new Color(0.6f, 0.7f, 0.8f), 0.25f);

        // pits: fall in = dead (LevelData.killHeight)
        Floor(-5, 28, "Concrete"); Floor(32, 58, "Concrete"); Floor(63, 90, "Concrete"); Floor(94, L + 6, "Concrete");
        Stripes(27.85f); Stripes(32.15f); Stripes(57.85f); Stripes(63.15f); Stripes(89.85f); Stripes(94.15f);
        Box("Plank", new Vector3(1, -0.1f, 60.5f), new Vector3(0.9f, 0.2f, 6), "Wood", GroundLayer);
        Coin(0, 2.2f, 30); Coin(1, 1.2f, 60.5f); Coin(0, 2.2f, 92);

        Ghoul(0, 16, -3, 13, 3, 20);
        Ghoul(-2, 45, -4, 41, 2, 50);
        Crate(4.5f, 0, 49, 1.2f); Spitter(4.5f, 53);
        SlideBlock(70, 3f);
        Ghoul(3, 78, 0, 74, 5, 82);
        SpinBar(0, 100, 12);
        Crate(-4.5f, 0, 103, 1.2f); Spitter(-4.5f, 107);
        Brute(1, 116);
        Lamp(0, 5, 15); Lamp(0, 5, 45); Lamp(0, 5, 75); Lamp(0, 5, 108);
        Gate(L - 4);
    }

    // ---------------------------------------------------------------- 4. Ghost Hospital (hard)
    static void Stage4()
    {
        const float L = 135f;
        cur.timeLimit = 260f;
        Hazards(true, 8f, 3f, 20f, 1.8f);
        Corridor(L, "Tile", "TileDirty");
        Atmosphere(new Color(0.19f, 0.24f, 0.2f), 0.03f, new Color(0.75f, 0.95f, 0.8f), 0.35f);

        float[] doors = { -4, 4, -4, 4, 0, -4 };                                   // one door per wall, left / right
        for (int i = 0; i < doors.Length; i++) Partition(18 * (i + 1), doors[i]);
        for (int i = 0; i < 7; i++) Lamp(0, 4.5f, 9 + 18 * i);

        Ghoul(0, 12, -3, 9, 3, 15); Coin(4, 1, 4);                                  // room 1
        Spikes(2, 27, 6, 2); Ghoul(-3, 26, -5, 22, -1, 31); Spitter(4.5f, 33);       // room 2
        Toxic(0, 45, 8, 6); Brute(0, 51); Coin(-5.5f, 1, 45); Coin(5.5f, 1, 45);     // room 3
        SpinBar(0, 63, 10); Ghoul(3, 59, 1, 57, 5, 61); Ghoul(-3, 68, -5, 66, 0, 70); // room 4
        Spikes(0, 78, 4, 2); Spitter(-4.5f, 86); Spitter(4.5f, 81);                  // room 5
        SlideBlock(96, 3.5f); Brute(0, 105); Coin(0, 1, 100);                        // room 6
        SlideBlock(116, 4f); Ghoul(0, 122, -3, 119, 3, 125); Coin(3, 1, 112);        // last corridor
        Gate(L - 4);
    }

    // ---------------------------------------------------------------- 5. Monster Lair (boss)
    static string BuildBossStage()
    {
        string path = StageSceneDir + "/Stage5.unity";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(BossTemplate, path);
        Scene scene = EditorSceneManager.OpenScene(path);
        ReloadMonsters();
        LevelData data = Data(5);
        cur = data;
        rng = new System.Random(1005);
        map = new GameObject("Map").transform;
        data.timeLimit = 300f;

        Bounds area = new Bounds(Vector3.zero, new Vector3(40, 1, 40));
        foreach (GameObject r in scene.GetRootGameObjects())
        {
            if (r.name == "Plane") { Recolor(r, "Asphalt"); area = r.GetComponent<Renderer>().bounds; }
            else if (r.name.StartsWith("Island")) Recolor(r, "Concrete");
            else if (r.name.StartsWith("Pillar")) Recolor(r, "Rust");
            else if (r.name == "Boss") Demon(r.transform);
        }

        // four low walls around the arena
        float ex = Mathf.Min(area.extents.x, 24f), ez = Mathf.Min(area.extents.z, 24f);
        Vector3 c = area.center;
        Box("Wall", new Vector3(c.x, 1.5f, c.z + ez), new Vector3(ex * 2, 3, 1), "Brick", WallsLayer);
        Box("Wall", new Vector3(c.x, 1.5f, c.z - ez), new Vector3(ex * 2, 3, 1), "Brick", WallsLayer);
        Box("Wall", new Vector3(c.x + ex, 1.5f, c.z), new Vector3(1, 3, ez * 2), "Brick", WallsLayer);
        Box("Wall", new Vector3(c.x - ex, 1.5f, c.z), new Vector3(1, 3, ez * 2), "Brick", WallsLayer);
        Lamp(c.x - 10, 6, c.z + 8); Lamp(c.x + 10, 6, c.z + 8);
        Atmosphere(new Color(0.2f, 0.05f, 0.05f), 0.025f, new Color(1f, 0.4f, 0.3f), 0.55f);

        Ghoul(-6, -2, -8, -4, -4, 0);
        Ghoul(6, -2, 4, -4, 8, 0);
        Hud();
        SetRunner(data);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssetIfDirty(data);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "Stage5 " + data.displayName + ": boss + " + data.enemies.Count + " ghouls";
    }

    // The boss was the human hero model: hide it and give the boss a monster body (3 m demon).
    // The Boss script, its collider, FirePoint and attacks stay the same.
    static void Demon(Transform boss)
    {
        Transform human = boss.Find("mixamorig:Meshes");
        if (human != null) human.gameObject.SetActive(false);
        Transform old = boss.Find("Demon");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        Transform d = new GameObject("Demon").transform;
        d.SetParent(boss, false);
        d.localScale = Vector3.one / Mathf.Max(0.01f, boss.lossyScale.x);   // parts below are in metres
        for (int s = -1; s <= 1; s += 2)
        {
            Part(d, PrimitiveType.Capsule, "Leg", new Vector3(0.45f * s, 0.6f, 0), new Vector3(0.5f, 0.62f, 0.5f), "BossSkin");
            Part(d, PrimitiveType.Cube, "Hoof", new Vector3(0.45f * s, 0.08f, 0.12f), new Vector3(0.5f, 0.16f, 0.6f), "Bone");
            Part(d, PrimitiveType.Sphere, "Shoulder", new Vector3(0.85f * s, 2.6f, 0), Vector3.one * 0.62f, "BossSkin");
            Part(d, PrimitiveType.Capsule, "Arm", new Vector3(1.05f * s, 1.95f, 0.1f), new Vector3(0.4f, 0.65f, 0.4f), "BossSkin", new Vector3(0, 0, 8 * s));
            Part(d, PrimitiveType.Cube, "Claw", new Vector3(1.12f * s, 1.25f, 0.28f), new Vector3(0.3f, 0.32f, 0.12f), "Bone", new Vector3(25, 0, 0));
            Part(d, PrimitiveType.Cube, "Horn", new Vector3(0.32f * s, 3.45f, 0.05f), new Vector3(0.16f, 0.16f, 0.7f), "Bone", new Vector3(-55, 20 * s, 0));
            Part(d, PrimitiveType.Cube, "Horn Tip", new Vector3(0.42f * s, 3.85f, -0.2f), new Vector3(0.1f, 0.1f, 0.5f), "Bone", new Vector3(-80, 30 * s, 0));
            Part(d, PrimitiveType.Sphere, "Eye", new Vector3(0.18f * s, 3.12f, 0.5f), Vector3.one * 0.15f, "EyeRed");
        }
        Part(d, PrimitiveType.Sphere, "Hips", new Vector3(0, 1.25f, 0), new Vector3(1.1f, 0.6f, 0.8f), "BossSkin");
        Part(d, PrimitiveType.Capsule, "Torso", new Vector3(0, 2.05f, 0), new Vector3(1.4f, 0.85f, 1f), "BossSkin");
        Part(d, PrimitiveType.Cube, "Chest Plate", new Vector3(0, 2.3f, 0.45f), new Vector3(0.9f, 0.6f, 0.15f), "Bone", new Vector3(-10, 0, 0));
        Part(d, PrimitiveType.Sphere, "Head", new Vector3(0, 3.05f, 0.15f), new Vector3(0.8f, 0.75f, 0.8f), "BossSkin");
        Part(d, PrimitiveType.Cube, "Mouth", new Vector3(0, 2.88f, 0.5f), new Vector3(0.42f, 0.12f, 0.1f), "LampRed");
        for (int k = 0; k < 4; k++)
            Part(d, PrimitiveType.Cube, "Back Spike", new Vector3(0, 2.8f - 0.35f * k, -0.5f), new Vector3(0.14f, 0.45f, 0.14f), "Bone", new Vector3(-50, 0, 0));
    }

    static void Recolor(GameObject go, string mat)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = M(mat);
    }

    // =====================================================================
    // enemies and coins (written into the LevelData, LevelRunner spawns them)
    static void Ghoul(float x, float z, float ax, float az, float bx, float bz)
    {
        EnemySpawn s = new EnemySpawn { type = ghoul, position = new Vector3(x, 0, z), yaw = 180f };
        s.route.Add(new Vector3(ax, 0, az));
        s.route.Add(new Vector3(bx, 0, bz));
        cur.enemies.Add(s);
    }

    static void Spitter(float x, float z) => cur.enemies.Add(new EnemySpawn { type = spitter, position = new Vector3(x, 0, z), yaw = 180f });
    static void Brute(float x, float z) => cur.enemies.Add(new EnemySpawn { type = brute, position = new Vector3(x, 0, z), yaw = 180f });
    static void Coin(float x, float y, float z) => coins.Add(new Vector3(x, y, z));

    // =====================================================================
    // world pieces
    static GameObject Box(string name, Vector3 pos, Vector3 size, string mat, int layer, Quaternion? rot = null, bool collider = true, Transform parent = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent != null ? parent : map, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = size;
        go.layer = layer;
        if (layer == GroundLayer || layer == WallsLayer) go.tag = "Ground";
        go.GetComponent<Renderer>().sharedMaterial = M(mat);
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 size, string mat, int layer, Quaternion? rot = null, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(map, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = size;
        go.layer = layer;
        if (layer == GroundLayer || layer == WallsLayer) go.tag = "Ground";
        go.GetComponent<Renderer>().sharedMaterial = M(mat);
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    static void Floor(float z0, float z1, string mat)
    {
        Box("Floor", new Vector3(0, -0.5f, (z0 + z1) / 2), new Vector3(Half * 2 + 2, 1, z1 - z0), mat, GroundLayer);
    }

    // The whole stage: floor (or none, then the stage adds it), two side walls and two end walls.
    // The walls go 6 m below the floor, so a pit looks deep.
    static void Corridor(float length, string floorMat, string wallMat)
    {
        const float H = 5f, Below = 6f;
        float z0 = -5f, z1 = length + 6f;
        if (floorMat != null) Floor(z0, z1, floorMat);
        for (int s = -1; s <= 1; s += 2)
            Box("Side Wall", new Vector3(s * (Half + 1), (H - Below) / 2, (z0 + z1) / 2), new Vector3(1, H + Below, z1 - z0), wallMat, WallsLayer);
        Box("Back Wall", new Vector3(0, H / 2, z0 - 0.5f), new Vector3(Half * 2 + 3, H, 1), wallMat, WallsLayer);
        Box("End Wall", new Vector3(0, H / 2, z1 + 0.5f), new Vector3(Half * 2 + 3, H, 1), wallMat, WallsLayer);
    }

    // a cross wall with one door (doorX = centre of the gap)
    static void Partition(float z, float doorX)
    {
        const float H = 5f, Door = 3f, DoorH = 3.2f;
        float left = doorX - Door / 2, right = doorX + Door / 2;
        float wl = left - (-Half - 0.5f), wr = (Half + 0.5f) - right;
        if (wl > 0.05f) Box("Partition", new Vector3(-Half - 0.5f + wl / 2, H / 2, z), new Vector3(wl, H, 0.5f), "Tile", WallsLayer);
        if (wr > 0.05f) Box("Partition", new Vector3(right + wr / 2, H / 2, z), new Vector3(wr, H, 0.5f), "Tile", WallsLayer);
        Box("Lintel", new Vector3(doorX, DoorH + (H - DoorH) / 2, z), new Vector3(Door, H - DoorH, 0.5f), "Tile", WallsLayer);
    }

    static void Crate(float x, float y, float z, float size)
    {
        Box("Crate", new Vector3(x, y + size / 2, z), Vector3.one * size, "Wood", GroundLayer, Quaternion.Euler(0, R(-12, 12), 0));
    }

    // concrete barrier, 0.9 m: jump over it
    static void Barrier(float x, float z, float len)
    {
        Box("Barrier", new Vector3(x, 0.45f, z), new Vector3(len, 0.9f, 0.5f), "Concrete", GroundLayer);
    }

    static void Stripes(float z)
    {
        Box("Edge Stripe", new Vector3(0, 0.01f, z), new Vector3(Half * 2, 0.03f, 0.3f), "Stripe", 0, null, false);
    }

    static void Lamp(float x, float y, float z)
    {
        Box("Lamp", new Vector3(x, y, z), new Vector3(0.5f, 0.15f, 0.5f), "LampBulb", 0, null, false);
        GameObject l = new GameObject("Light");
        l.transform.SetParent(map, false);
        l.transform.localPosition = new Vector3(x, y - 0.4f, z);
        Light light = l.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.65f);
        light.range = 14f;
        light.intensity = 2.2f;
        light.shadows = LightShadows.None;
        l.AddComponent<FlickerLight>();
    }

    static void Toxic(float x, float z, float sx, float sz)
    {
        GameObject pool = Box("Toxic Pool", new Vector3(x, 0.02f, z), new Vector3(sx, 0.04f, sz), "Toxic", 0, null, false);
        BoxCollider c = pool.AddComponent<BoxCollider>();
        c.isTrigger = true;
        c.center = new Vector3(0, 12f, 0);            // local: the pool is 0.04 thick, so 12 x 0.04 = 0.5 m up
        c.size = new Vector3(1, 25f, 1);              // 1 m tall trigger
        pool.AddComponent<ToxicZone>();
    }

    // broken glass: a Hazard that hurts when you walk in
    static void Spikes(float x, float z, float sx, float sz)
    {
        GameObject root = new GameObject("Spikes");
        root.transform.SetParent(map, false);
        root.transform.localPosition = new Vector3(x, 0, z);
        BoxCollider c = root.AddComponent<BoxCollider>();
        c.isTrigger = true;
        c.center = new Vector3(0, 0.4f, 0);
        c.size = new Vector3(sx, 0.8f, sz);
        root.AddComponent<Hazard>().destroyOnHit = false;
        int n = Mathf.RoundToInt(sx * sz * 3);
        for (int i = 0; i < n; i++)
            Box("Shard", new Vector3(R(-sx / 2, sx / 2), 0.2f, R(-sz / 2, sz / 2)), new Vector3(0.08f, R(0.3f, 0.6f), 0.25f), "Metal", 0,
                Quaternion.Euler(R(-25, 25), R(0, 180), R(-25, 25)), false, root.transform);
    }

    // rotating low pipe: jump over it (Spinner + Hazard, like the old spin bars)
    static void SpinBar(float x, float z, float len)
    {
        Prim(PrimitiveType.Cylinder, "Spin Hub", new Vector3(x, 0.3f, z), new Vector3(0.8f, 0.3f, 0.8f), "Metal", GroundLayer);
        GameObject bar = Prim(PrimitiveType.Cylinder, "Spin Bar", new Vector3(x, 0.55f, z), new Vector3(0.35f, len / 2, 0.35f), "Stripe", 0, Quaternion.Euler(0, 0, 90));
        bar.AddComponent<Rigidbody>().isKinematic = true;
        bar.AddComponent<Spinner>();
        bar.AddComponent<Hazard>().destroyOnHit = false;
    }

    // low block sliding across the path: jump over it or wait for the gap (Patrol + Hazard)
    static void SlideBlock(float z, float speed)
    {
        GameObject block = Box("Slide Block", new Vector3(-Half + 1.2f, 0.45f, z), new Vector3(1.6f, 0.9f, 1.2f), "Stripe", 0);
        GameObjectUtility.SetStaticEditorFlags(block, (StaticEditorFlags)0);   // it moves
        block.AddComponent<Rigidbody>().isKinematic = true;
        block.AddComponent<Hazard>().destroyOnHit = false;
        Patrol p = block.AddComponent<Patrol>();
        p.pointA = Point("Slide A", new Vector3(-Half + 1.2f, 0.45f, z));
        p.pointB = Point("Slide B", new Vector3(Half - 1.2f, 0.45f, z));
        p.speed = speed;
    }

    static Transform Point(string name, Vector3 pos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(map, false);
        t.localPosition = pos;
        return t;
    }

    // exit: a metal door that slides up when every monster is dead, the Exit trigger behind it
    static void Gate(float z)
    {
        GameObject g = new GameObject("Exit Gate");
        g.transform.SetParent(map, false);
        g.transform.localPosition = new Vector3(0, 0, z);
        Transform t = g.transform;
        Box("Post", new Vector3(-2.2f, 2.75f, 0), new Vector3(1, 5.5f, 1), "ConcreteDark", WallsLayer, null, true, t);
        Box("Post", new Vector3(2.2f, 2.75f, 0), new Vector3(1, 5.5f, 1), "ConcreteDark", WallsLayer, null, true, t);
        Box("Lintel", new Vector3(0, 5.8f, 0), new Vector3(5.4f, 0.6f, 1), "ConcreteDark", WallsLayer, null, true, t);
        float side = Half + 1f - 2.7f;
        Box("Gate Wall", new Vector3(-(2.7f + side / 2), 3, 0), new Vector3(side, 6, 1), "ConcreteDark", WallsLayer, null, true, t);
        Box("Gate Wall", new Vector3(2.7f + side / 2, 3, 0), new Vector3(side, 6, 1), "ConcreteDark", WallsLayer, null, true, t);
        GameObject door = Box("Door", new Vector3(0, 2.5f, 0), new Vector3(3.4f, 5f, 0.4f), "Rust", WallsLayer, null, true, t);
        GameObjectUtility.SetStaticEditorFlags(door, (StaticEditorFlags)0);   // it moves, so it is not static
        GameObject lamp = Box("Gate Lamp", new Vector3(0, 6.35f, -0.4f), new Vector3(0.8f, 0.35f, 0.2f), "LampRed", 0, null, false, t);
        GameObject pad = Box("Exit Pad", new Vector3(0, 0.02f, 2.2f), new Vector3(3, 0.04f, 2), "ExitPad", 0, null, false, t);

        GameObject exit = new GameObject("Exit");
        exit.tag = "Exit";
        exit.transform.SetParent(t, false);
        exit.transform.localPosition = new Vector3(0, 1.5f, 2.2f);
        BoxCollider bc = exit.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(3.2f, 3f, 2f);

        ExitGate eg = g.AddComponent<ExitGate>();
        eg.door = door.transform;
        eg.lamp = lamp.GetComponent<Renderer>();
        eg.lampOpen = M("LampGreen");
        pad.name = "Exit Pad";
    }

    // fog, ambient light, sun, camera background
    static void Atmosphere(Color fog, float density, Color sun, float sunIntensity)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = density;
        RenderSettings.fogColor = fog;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = fog * 1.4f + new Color(0.08f, 0.08f, 0.08f);
        RenderSettings.skybox = null;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;     // no blue sky reflected in dark ruins

        GameObject sunGo = GameObject.Find("Directional Light");
        if (sunGo != null && sunGo.TryGetComponent(out Light l))
        {
            l.color = sun;
            l.intensity = sunIntensity;
            sunGo.transform.rotation = Quaternion.Euler(40, -35, 0);
        }
        GameObject camGo = GameObject.Find("Main Camera");
        if (camGo != null && camGo.TryGetComponent(out Camera cam))
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fog;
        }
    }

    // =====================================================================
    static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, string mat, Vector3 euler = default)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = M(mat);
        return go.transform;
    }

    static Transform Pivot(Transform parent, string name, Vector3 pos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
