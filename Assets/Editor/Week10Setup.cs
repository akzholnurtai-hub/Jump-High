#if LEGACY_TOOLS
// Week 11: switched off - old week 10 level builder.
// It wrote balance numbers straight into scenes; those numbers now live in data assets (Assets/Data).
// Safe to delete. Kept only so the history is easy to see.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using TMPro;

// Week 10 · Assignment "Three Enemy Types".
// Menu: Week10 > Build Enemy Level
// Copies Level1 -> Level_Enemies, builds an arena (walls, lava, spinning bar, tower, hearts),
// places robot enemies (Spider-Bot x2, Jet-Mech, Rhino-Tank), bakes a NavMesh,
// gives the player a new gun (Energy Bolt, 3-way spread) in every level.
public static class Week10Setup
{
    const string S = "Assets/Scenes/";
    const string Scene = "Level_Enemies";
    const string MatDir = "Assets/Materials/Week10";

    static Transform root;
    static int wallsLayer;
    static Material wallMat, lineMat;

    [MenuItem("Week10/Build Enemy Level")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string path = S + Scene + ".unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (!EditorUtility.DisplayDialog("Week10", Scene + " already exists. Rebuild it from Level1?", "Rebuild", "Cancel"))
                return;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.DeleteAsset(S + Scene);
        }

        wallsLayer = EnsureLayer("Walls");
        MakeMaterials();
        LayerMask walls = 1 << wallsLayer;
        GameObject bolt = MakeEnergyBolt(walls);

        AssetDatabase.CopyAsset(S + "Level1.unity", path);
        EditorSceneManager.OpenScene(path);

        ClearOldLevel();
        root = new GameObject("EnemyArena").transform;
        BuildArena();
        PlacePlayerCoinsExit();

        GameObject fireball = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fireball.prefab");
        AudioClip warn = FindClip("bullet");

        MakeSpider("Spider-Bot", walls, warn, new[] {
            new Vector3(-11, 0, -2), new Vector3(-3, 0, -2), new Vector3(-3, 0, 6), new Vector3(-11, 0, 6) });
        MakeSpider("Spider-Bot 2", walls, warn, new[] {
            new Vector3(-12, 0, 11.5f), new Vector3(-2, 0, 11.5f) });
        MakeMech(walls, warn, fireball);
        MakeRhino(walls, warn);

        BakeNavMesh();
        UpgradeGunInOpenScene(bolt);
        SaveScene();

        WireBuildOrder(bolt);

        EditorSceneManager.OpenScene(path);
        EditorUtility.DisplayDialog("Week10",
            "Level_Enemies is ready.\n\n" +
            "Enemies: 2x Spider-Bot, Jet-Mech, Rhino-Tank.\n" +
            "Map: lava, spinning bar, tower coin, hearts.\n" +
            "Gun: Energy Bolt, 3-way spread (all levels).\n\nPress Play!", "OK");
    }

    // ================= level =================

    static void ClearOldLevel()
    {
        Transform old = Find("NewMap");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        foreach (Transform t in All())
        {
            if (t == null) continue;
            GameObject go = t.gameObject;
            if (go.CompareTag("Player") || t.GetComponentInParent<Movement>() != null) continue;
            if (go.CompareTag("Enemy") || go.GetComponent<Patrol>() != null || go.GetComponent<Hazard>() != null)
                go.SetActive(false);
        }
        foreach (Transform t in All())
        {
            if (t == null) continue;
            foreach (Spawner s in t.GetComponents<Spawner>()) Object.DestroyImmediate(s);
            foreach (SpawnerRamp s in t.GetComponents<SpawnerRamp>()) Object.DestroyImmediate(s);
        }
    }

    static void BuildArena()
    {
        // outer walls (Plane is 30 x 30 m at y = 0)
        Wall("Border N", new Vector3(0, 0, 14.5f), new Vector3(30, 2.5f, 1));
        Wall("Border S", new Vector3(0, 0, -14.5f), new Vector3(30, 2.5f, 1));
        Wall("Border E", new Vector3(14.5f, 0, 0), new Vector3(1, 2.5f, 30));
        Wall("Border W", new Vector3(-14.5f, 0, 0), new Vector3(1, 2.5f, 30));

        // start area (south-west): lava river to jump over, tower with a coin on top
        Lava("Lava River", new Vector3(-8, 0, -4.5f), new Vector3(12, 2.2f));
        Tower("Coin Tower", new Vector3(-12.5f, 0, -8), new Vector3(2, 1.4f, 2));
        Tower("Step Box", new Vector3(-9.5f, 0, -8), new Vector3(1.5f, 0.7f, 1.5f));

        // west: Spider-Bot walks around this block -> cover
        Wall("Patrol Block", new Vector3(-7, 0, 2), new Vector3(3, 2.5f, 3));
        Wall("Middle Divider", new Vector3(0, 0, 1), new Vector3(0.8f, 2.5f, 8));

        // south-east: Jet-Mech, cover walls + spinning bar you must jump over
        Wall("Mech Cover 1", new Vector3(2, 0, -8), new Vector3(0.8f, 2.5f, 4));
        Wall("Mech Cover 2", new Vector3(7, 0, -4), new Vector3(4, 2.5f, 0.8f));
        Wall("Mech Cover 3", new Vector3(5, 0, -11.5f), new Vector3(2, 2.5f, 0.8f));
        SpinBar("Spin Bar", new Vector3(8, 0, -8), 5f, 100f);

        // east passage: lava pool between the Mech zone and the Rhino zone
        Lava("Lava Pool", new Vector3(12, 0, 0), new Vector3(4.2f, 2.4f));

        // north-east: Rhino-Tank arena in front of the exit
        Wall("Rhino Wall S", new Vector3(6, 0, 4), new Vector3(8, 2.5f, 0.8f));
        Wall("Rhino Pillar 1", new Vector3(3, 0, 9), new Vector3(1.2f, 2.5f, 1.2f));
        Wall("Rhino Pillar 2", new Vector3(10.5f, 0, 6.5f), new Vector3(1.2f, 2.5f, 1.2f));

        // hearts (heal) after the hard parts
        Heart(new Vector3(-1.5f, 0, -12));
        Heart(new Vector3(12, 0, 2.6f));
    }

    static void PlacePlayerCoinsExit()
    {
        Movement m = Object.FindAnyObjectByType<Movement>();
        if (m != null)
        {
            m.transform.position = new Vector3(-11, 1, -11);
            m.transform.rotation = Quaternion.identity;
            m.timeLimit = 150f;
            m.nextScene = "Level2";
            EditorUtility.SetDirty(m);
        }

        Vector3[] spots =
        {
            new Vector3(-12.5f, 1.4f, -8),                         // on the tower
            new Vector3(-3, 0, -2), new Vector3(-11, 0, 6),       // Spider zone
            new Vector3(11, 0, -11), new Vector3(4, 0, -8),       // Mech zone
            new Vector3(9, 0, 8), new Vector3(1.5f, 0, 12),       // Rhino zone
        };
        List<Transform> coins = new List<Transform>();
        foreach (Transform t in All())
            if (t.CompareTag("Coin")) coins.Add(t);
        coins.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        for (int i = 0; i < coins.Count; i++)
        {
            coins[i].gameObject.SetActive(i < spots.Length);
            if (i < spots.Length) coins[i].position = spots[i] + Vector3.up;
        }

        MoveGroup(new[] { "Platform_A (2)", "DoorR", "Exit" }, new Vector3(9, 0.58f, 12), -90f);
    }

    // ================= enemies =================

    // Patroller -> Spider-Bot: black body, purple stripe, red eyes, 8 legs
    static void MakeSpider(string name, LayerMask walls, AudioClip warn, Vector3[] route)
    {
        GameObject e = Root(name, route[0], 0.65f, 4, out TMP_Text label);
        Transform m = Pivot(e.transform, "Model", Vector3.zero, Vector3.zero);

        Material shell = Mat("Spider_Shell", new Color(0.13f, 0.13f, 0.16f));
        Material accent = Mat("Spider_Accent", new Color(0.55f, 0.12f, 0.8f));
        Material eye = Glow("Spider_Eye", new Color(1f, 0.1f, 0.08f));

        Part(PrimitiveType.Sphere, m, "Abdomen", new Vector3(0, 0.95f, -0.45f), new Vector3(1f, 0.8f, 1.2f), shell);
        Part(PrimitiveType.Sphere, m, "Stripe", new Vector3(0, 1.2f, -0.45f), new Vector3(0.35f, 0.45f, 1.05f), accent);
        Part(PrimitiveType.Sphere, m, "Head", new Vector3(0, 0.85f, 0.35f), new Vector3(0.7f, 0.55f, 0.7f), shell);
        Part(PrimitiveType.Sphere, m, "Eye L", new Vector3(-0.13f, 0.95f, 0.66f), Vector3.one * 0.14f, eye);
        Part(PrimitiveType.Sphere, m, "Eye R", new Vector3(0.13f, 0.95f, 0.66f), Vector3.one * 0.14f, eye);
        Part(PrimitiveType.Sphere, m, "Eye L2", new Vector3(-0.26f, 0.9f, 0.58f), Vector3.one * 0.08f, eye);
        Part(PrimitiveType.Sphere, m, "Eye R2", new Vector3(0.26f, 0.9f, 0.58f), Vector3.one * 0.08f, eye);

        List<Transform> legs = new List<Transform>();
        for (int k = 0; k < 4; k++)
            for (int side = -1; side <= 1; side += 2)
            {
                float yaw = side * (90f + (k - 1.5f) * 30f);
                Transform leg = Pivot(m, "Leg " + (side < 0 ? "L" : "R") + k, new Vector3(side * 0.3f, 0.85f, 0.45f - k * 0.3f), new Vector3(0, yaw, 0));
                Part(PrimitiveType.Cube, leg, "Upper", new Vector3(0, 0.175f, 0.3f), new Vector3(0.09f, 0.09f, 0.7f), shell, new Vector3(-30, 0, 0));
                Part(PrimitiveType.Cube, leg, "Lower", new Vector3(0, -0.25f, 0.8f), new Vector3(0.07f, 0.07f, 1.26f), accent, new Vector3(71.6f, 0, 0));
                legs.Add(leg);
            }

        EnemyAnimator anim = m.gameObject.AddComponent<EnemyAnimator>();
        anim.style = EnemyAnimator.Style.Spider;
        anim.legs = legs.ToArray();

        Transform pts = new GameObject(name + " Points").transform;
        pts.SetParent(root, false);
        Transform[] points = new Transform[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            points[i] = new GameObject("Point " + i).transform;
            points[i].SetParent(pts, false);
            points[i].position = route[i];
        }

        NavMeshAgent agent = e.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2f;
        agent.baseOffset = 0f;

        PatrollerAI ai = e.AddComponent<PatrollerAI>();
        SerializedObject so = new SerializedObject(ai);
        Common(so, walls, m, label, warn, 6f, 100f);
        SerializedProperty arr = so.FindProperty("points");
        arr.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Shooter -> Jet-Mech: steel armor, teal visor + core, arm cannon, hovering on jet flames
    static void MakeMech(LayerMask walls, AudioClip warn, GameObject fireball)
    {
        GameObject e = Root("Jet-Mech", new Vector3(11.5f, 0, -9), 0.5f, 5, out TMP_Text label);
        e.transform.rotation = Quaternion.Euler(0, -90, 0);
        Transform m = Pivot(e.transform, "Model", new Vector3(0, 0.3f, 0), Vector3.zero);

        Material steel = Mat("Mech_Steel", new Color(0.38f, 0.42f, 0.48f));
        Material dark = Mat("Mech_Dark", new Color(0.14f, 0.15f, 0.2f));
        Material glow = Glow("Mech_Glow", new Color(0.1f, 0.9f, 1f));
        Material fire = Glow("Mech_Flame", new Color(1f, 0.55f, 0.1f));

        Part(PrimitiveType.Cube, m, "Leg L", new Vector3(-0.2f, 0.45f, 0), new Vector3(0.26f, 0.9f, 0.3f), dark);
        Part(PrimitiveType.Cube, m, "Leg R", new Vector3(0.2f, 0.45f, 0), new Vector3(0.26f, 0.9f, 0.3f), dark);
        Part(PrimitiveType.Cube, m, "Pelvis", new Vector3(0, 0.95f, 0), new Vector3(0.6f, 0.2f, 0.35f), steel);
        Part(PrimitiveType.Cube, m, "Torso", new Vector3(0, 1.35f, 0), new Vector3(0.8f, 0.65f, 0.45f), steel);
        Part(PrimitiveType.Cylinder, m, "Core", new Vector3(0, 1.4f, 0.23f), new Vector3(0.22f, 0.02f, 0.22f), glow, new Vector3(90, 0, 0));
        Part(PrimitiveType.Sphere, m, "Shoulder L", new Vector3(-0.5f, 1.6f, 0), Vector3.one * 0.35f, dark);
        Part(PrimitiveType.Sphere, m, "Shoulder R", new Vector3(0.5f, 1.6f, 0), Vector3.one * 0.35f, dark);
        Part(PrimitiveType.Cube, m, "Arm L", new Vector3(-0.55f, 1.25f, 0.05f), new Vector3(0.18f, 0.6f, 0.2f), steel);
        Part(PrimitiveType.Cube, m, "Arm Cannon", new Vector3(0.55f, 1.4f, 0.35f), new Vector3(0.22f, 0.22f, 0.75f), dark);
        Part(PrimitiveType.Cylinder, m, "Cannon Glow", new Vector3(0.55f, 1.4f, 0.74f), new Vector3(0.14f, 0.03f, 0.14f), glow, new Vector3(90, 0, 0));
        Part(PrimitiveType.Cube, m, "Head", new Vector3(0, 1.9f, 0), new Vector3(0.38f, 0.38f, 0.4f), steel);
        Part(PrimitiveType.Cube, m, "Visor", new Vector3(0, 1.92f, 0.2f), new Vector3(0.3f, 0.08f, 0.03f), glow);
        Part(PrimitiveType.Cube, m, "Jetpack", new Vector3(0, 1.4f, -0.32f), new Vector3(0.5f, 0.6f, 0.2f), dark);
        Transform f1 = Part(PrimitiveType.Cylinder, m, "Flame L", new Vector3(-0.15f, 0.95f, -0.32f), new Vector3(0.12f, 0.2f, 0.12f), fire);
        Transform f2 = Part(PrimitiveType.Cylinder, m, "Flame R", new Vector3(0.15f, 0.95f, -0.32f), new Vector3(0.12f, 0.2f, 0.12f), fire);

        EnemyAnimator anim = m.gameObject.AddComponent<EnemyAnimator>();
        anim.style = EnemyAnimator.Style.Hover;
        anim.legs = new Transform[0];
        anim.flames = new[] { f1, f2 };

        Transform fp = new GameObject("FirePoint").transform;
        fp.SetParent(e.transform, false);
        fp.localPosition = new Vector3(0.55f, 1.7f, 1f);

        LineRenderer laser = Line(e.transform, "Laser", 0.06f);

        ShooterAI ai = e.AddComponent<ShooterAI>();
        SerializedObject so = new SerializedObject(ai);
        Common(so, walls, m, label, warn, 9f, 360f);
        so.FindProperty("shotPrefab").objectReferenceValue = fireball;
        so.FindProperty("firePoint").objectReferenceValue = fp;
        so.FindProperty("laser").objectReferenceValue = laser;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Charger -> Rhino-Tank: armored body, big horn, glowing eyes, 4 stomping legs
    static void MakeRhino(LayerMask walls, AudioClip warn)
    {
        GameObject e = Root("Rhino-Tank", new Vector3(7, 0, 9), 0.7f, 6, out TMP_Text label);
        e.transform.rotation = Quaternion.Euler(0, -90, 0);
        Transform m = Pivot(e.transform, "Model", Vector3.zero, Vector3.zero);

        Material armor = Mat("Rhino_Armor", new Color(0.36f, 0.42f, 0.28f));
        Material plate = Mat("Rhino_Plate", new Color(0.22f, 0.25f, 0.2f));
        Material horn = Mat("Rhino_Horn", new Color(0.9f, 0.85f, 0.72f));
        Material eye = Glow("Rhino_Eye", new Color(1f, 0.5f, 0.05f));

        Part(PrimitiveType.Cube, m, "Body", new Vector3(0, 1f, -0.1f), new Vector3(1.2f, 0.85f, 1.7f), armor);
        Part(PrimitiveType.Cube, m, "Back Plate", new Vector3(0, 1.5f, -0.2f), new Vector3(0.9f, 0.2f, 1.3f), plate);
        Part(PrimitiveType.Cube, m, "Head", new Vector3(0, 0.95f, 0.95f), new Vector3(0.8f, 0.6f, 0.6f), armor);
        Part(PrimitiveType.Capsule, m, "Horn", new Vector3(0, 1.2f, 1.35f), new Vector3(0.18f, 0.3f, 0.18f), horn, new Vector3(55, 0, 0));
        Part(PrimitiveType.Capsule, m, "Horn Small", new Vector3(0, 1.38f, 1.08f), new Vector3(0.12f, 0.16f, 0.12f), horn, new Vector3(30, 0, 0));
        Part(PrimitiveType.Sphere, m, "Eye L", new Vector3(-0.28f, 1.05f, 1.24f), Vector3.one * 0.12f, eye);
        Part(PrimitiveType.Sphere, m, "Eye R", new Vector3(0.28f, 1.05f, 1.24f), Vector3.one * 0.12f, eye);

        // order FL, FR, BR, BL: diagonal legs move together
        Vector3[] at = { new Vector3(-0.45f, 0.6f, 0.55f), new Vector3(0.45f, 0.6f, 0.55f),
                         new Vector3(0.45f, 0.6f, -0.65f), new Vector3(-0.45f, 0.6f, -0.65f) };
        Transform[] legs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            legs[i] = Pivot(m, "Leg " + i, at[i], Vector3.zero);
            Part(PrimitiveType.Cube, legs[i], "Leg Mesh", new Vector3(0, -0.3f, 0), new Vector3(0.32f, 0.6f, 0.36f), plate);
        }

        EnemyAnimator anim = m.gameObject.AddComponent<EnemyAnimator>();
        anim.style = EnemyAnimator.Style.Quadruped;
        anim.legs = legs;

        LineRenderer lane = Line(e.transform, "Dash Lane", 1f);
        lane.alignment = LineAlignment.TransformZ;                      // lie flat on the floor
        lane.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        ChargerAI ai = e.AddComponent<ChargerAI>();
        SerializedObject so = new SerializedObject(ai);
        Common(so, walls, m, label, warn, 6.5f, 360f);
        so.FindProperty("lane").objectReferenceValue = lane;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Common(SerializedObject so, LayerMask walls, Transform model, TMP_Text label, AudioClip warn, float range, float fov)
    {
        so.FindProperty("obstacleMask").intValue = walls.value;
        so.FindProperty("model").objectReferenceValue = model;
        so.FindProperty("stateLabel").objectReferenceValue = label;
        so.FindProperty("warnClip").objectReferenceValue = warn;
        so.FindProperty("viewRange").floatValue = range;
        so.FindProperty("fov").floatValue = fov;
    }

    // root at the feet: capsule collider + kinematic rigidbody + health + 3D label
    static GameObject Root(string name, Vector3 pos, float radius, int hp, out TMP_Text label)
    {
        GameObject e = new GameObject(name);
        e.transform.SetParent(root, false);
        e.transform.position = pos;

        CapsuleCollider col = e.AddComponent<CapsuleCollider>();
        col.center = Vector3.up;
        col.height = 2f;
        col.radius = radius;
        Rigidbody rb = e.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        EnemyHealth h = e.AddComponent<EnemyHealth>();
        SerializedObject hs = new SerializedObject(h);
        hs.FindProperty("maxHp").intValue = hp;
        hs.ApplyModifiedPropertiesWithoutUndo();

        GameObject lb = new GameObject("StateLabel");
        lb.transform.SetParent(e.transform, false);
        lb.transform.localPosition = new Vector3(0, 2.8f, 0);
        TextMeshPro tmp = lb.AddComponent<TextMeshPro>();
        tmp.text = name;
        tmp.fontSize = 4;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(5, 1);
        label = tmp;
        return e;
    }

    static Transform Pivot(Transform parent, string name, Vector3 pos, Vector3 euler)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localEulerAngles = euler;
        return t;
    }

    static Transform Part(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        Renderer r = g.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return g.transform;
    }

    static LineRenderer Line(Transform parent, string name, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.sharedMaterial = lineMat;
        lr.startColor = new Color(1f, 0.2f, 0.15f, 0.9f);
        lr.endColor = new Color(1f, 0.2f, 0.15f, 0.4f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.enabled = false;
        return lr;
    }

    // ================= map pieces =================

    static void Wall(string name, Vector3 floorPos, Vector3 size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = floorPos + Vector3.up * size.y / 2f;
        go.transform.localScale = size;
        go.layer = wallsLayer;
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    // a block you can stand on (Ground tag + layer, so jumping works)
    static void Tower(string name, Vector3 floorPos, Vector3 size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = floorPos + Vector3.up * size.y / 2f;
        go.transform.localScale = size;
        go.tag = "Ground";
        int g = LayerMask.NameToLayer("Ground");
        if (g >= 0) go.layer = g;
        go.isStatic = true;
        go.GetComponent<Renderer>().sharedMaterial = Mat("Tower", new Color(0.55f, 0.48f, 0.38f));
    }

    static void Lava(string name, Vector3 center, Vector2 size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = center + Vector3.up * 0.03f;
        go.transform.localScale = new Vector3(size.x, 0.06f, size.y);
        go.GetComponent<Renderer>().sharedMaterial = Glow("Lava", new Color(1f, 0.32f, 0.05f));

        BoxCollider bc = go.GetComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = new Vector3(0, 8f, 0);           // trigger is 1 m tall (scale y is 0.06)
        bc.size = new Vector3(1, 16f, 1);
        go.AddComponent<Lava>();
    }

    static void SpinBar(string name, Vector3 floorPos, float length, float speed)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(root, false);
        pivot.transform.position = floorPos;
        Rigidbody rb = pivot.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        Spinner sp = pivot.AddComponent<Spinner>();
        sp.degPerSec = speed;
        Hazard hz = pivot.AddComponent<Hazard>();
        hz.damage = 1;
        hz.destroyOnHit = false;
        hz.lifeTime = 0f;

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.SetParent(pivot.transform, false);
        post.transform.localPosition = new Vector3(0, 0.4f, 0);
        post.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        post.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "Bar";
        bar.transform.SetParent(pivot.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.45f, 0);
        bar.transform.localScale = new Vector3(length, 0.3f, 0.3f);
        bar.GetComponent<Renderer>().sharedMaterial = Glow("SpinBar", new Color(1f, 0.15f, 0.2f));
    }

    static void Heart(Vector3 floorPos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Heart.prefab");
        if (prefab == null) return;
        GameObject h = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        h.transform.SetParent(root, true);
        h.transform.position = floorPos + Vector3.up;
    }

    // ================= player gun =================

    static GameObject MakeEnergyBolt(LayerMask walls)
    {
        const string path = "Assets/Prefabs/EnergyBolt.prefab";

        GameObject go = new GameObject("EnergyBolt");
        SphereCollider c = go.AddComponent<SphereCollider>();
        c.isTrigger = true;
        c.radius = 0.25f;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        Bullet b = go.AddComponent<Bullet>();
        b.speed = 20f;
        b.lifeTime = 1.5f;
        ShotStopper stop = go.AddComponent<ShotStopper>();
        stop.walls = walls;

        Color cyan = new Color(0.3f, 0.95f, 1f);
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        core.name = "Core";
        Object.DestroyImmediate(core.GetComponent<Collider>());
        core.transform.SetParent(go.transform, false);
        core.transform.localEulerAngles = new Vector3(90, 0, 0);
        core.transform.localScale = new Vector3(0.16f, 0.32f, 0.16f);
        core.GetComponent<Renderer>().sharedMaterial = Glow("EnergyBolt", cyan);

        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.15f;
        tr.startWidth = 0.22f;
        tr.endWidth = 0f;
        tr.sharedMaterial = lineMat;
        tr.startColor = cyan;
        tr.endColor = new Color(cyan.r, cyan.g, cyan.b, 0f);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void UpgradeGunInOpenScene(GameObject bolt)
    {
        foreach (Transform t in All())
        {
            Shooter s = t.GetComponent<Shooter>();
            if (s == null) continue;
            s.bulletPrefab = bolt;
            s.pattern = Shooter.Pattern.Spread;
            s.spreadCount = 3;
            s.spreadAngle = 12f;
            s.cooldown = 0.35f;
            EditorUtility.SetDirty(s);
        }
    }

    // ================= navmesh =================

    static void BakeNavMesh()
    {
        Transform plane = Find("Plane");
        GameObject host = new GameObject("NavMesh");
        host.transform.SetParent(root, false);
        NavMeshSurface surf = host.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.All;
        surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        int ground = LayerMask.NameToLayer("Ground");
        surf.layerMask = (1 << wallsLayer) | (ground >= 0 ? 1 << ground : 0) | (plane != null ? 1 << plane.gameObject.layer : 0);

        try
        {
            surf.BuildNavMesh();
            if (!AssetDatabase.IsValidFolder(S + Scene)) AssetDatabase.CreateFolder("Assets/Scenes", Scene);
            AssetDatabase.CreateAsset(surf.navMeshData, S + Scene + "/NavMesh.asset");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("NavMesh was not baked automatically (" + ex.Message +
                             "). Select EnemyArena/NavMesh and click Bake. The Spider-Bots also work without it.");
        }
    }

    // ================= helpers =================

    static void WireBuildOrder(GameObject bolt)
    {
        string path = S + Scene + ".unity";
        List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == path);
        int at = list.FindIndex(s => s.path == S + "Level1.unity");
        list.Insert(at >= 0 ? at + 1 : list.Count, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();

        foreach (string lvl in new[] { "Level1", "Level2", "Level3_Boss" })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(S + lvl + ".unity") == null) continue;
            EditorSceneManager.OpenScene(S + lvl + ".unity");
            if (lvl == "Level1")
            {
                Movement m = Object.FindAnyObjectByType<Movement>();
                if (m != null) { m.nextScene = Scene; EditorUtility.SetDirty(m); }
            }
            UpgradeGunInOpenScene(bolt);
            SaveScene();
        }
    }

    static int EnsureLayer(string name)
    {
        int l = LayerMask.NameToLayer(name);
        if (l >= 0) return l;
        SerializedObject tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tm.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = name;
                tm.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        Debug.LogError("No free layer for " + name);
        return 0;
    }

    static void MakeMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/Materials", "Week10");
        wallMat = Mat("Wall", new Color(0.42f, 0.4f, 0.38f));

        lineMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "/WarningLine.mat");
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(lineMat, MatDir + "/WarningLine.mat");
        }
    }

    static Material Mat(string name, Color c)
    {
        string p = MatDir + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { color = c };
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Material Glow(string name, Color c)
    {
        Material m = Mat(name, c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * 2.5f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    static AudioClip FindClip(string part)
    {
        foreach (string g in AssetDatabase.FindAssets("t:AudioClip " + part))
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g));
        return null;
    }

    static void MoveGroup(string[] names, Vector3 target, float yaw)
    {
        Transform main = Find(names[0]);
        if (main == null) { Debug.LogWarning("Exit group not found, move the gate by hand."); return; }
        Transform holder = main.parent != null && main.parent.name.EndsWith("_Holder") ? main.parent : null;
        if (holder == null)
        {
            holder = new GameObject(main.name + "_Holder").transform;
            holder.SetParent(main.parent, false);
            main.SetParent(holder, false);
        }
        holder.localRotation = Quaternion.Euler(0, yaw, 0);
        holder.localScale = Vector3.one;
        foreach (string n in names)
        {
            Transform t = Find(n);
            if (t != null && t.parent != holder) t.SetParent(holder, false);
        }
        Vector3 local = holder.parent == null ? target : holder.parent.InverseTransformPoint(target);
        holder.localPosition = local - Quaternion.Euler(0, yaw, 0) * main.localPosition;
    }

    static List<Transform> All()
    {
        List<Transform> list = new List<Transform>();
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            list.AddRange(go.GetComponentsInChildren<Transform>(true));
        return list;
    }

    static Transform Find(string name)
    {
        foreach (Transform t in All())
            if (t != null && t.name == name) return t;
        return null;
    }

    static void SaveScene()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }
}
#endif
