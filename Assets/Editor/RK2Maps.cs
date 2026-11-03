using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class RK2Maps
{
    const string S = "Assets/Scenes/";

    static Material mat;
    static int layer;
    static Transform root;
    static GameObject padSource;
    static GameObject wallSource;

    static RK2Maps()
    {
        if (EditorPrefs.GetBool("RK2_Maps_v15")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorPrefs.SetBool("RK2_Maps_v15", true);
            Remake();
        };
    }

    [MenuItem("RK2/Remake Level Maps")]
    static void Remake()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Level1();
        Level2();
        Boss();
        EditorSceneManager.OpenScene(S + "Level1.unity");
        EditorUtility.DisplayDialog("RK2", "Level maps are updated.", "OK");
    }

    static void Level1()
    {
        Open("Level1", "Platform_B", "52b6b38fcdb757b42b3fe21f388787de");
        Hide("Platform_A", "Platform_A (1)", "Platform_B", "Platform_B (1)", "Platform_C",
             "Ramp", "Ramp (1)", "Dominoes", "Cube", "Enemy (2)", "Enemy (4)", "Enemy (5)", "Enemy (6)", "Enemy (8)");

        Block("Step 1", 0, -6, 0.8f, 5, 4);
        Block("Step 2", -3, -1, 1.8f, 4, 4);
        Block("Bridge 1", 2.5f, -1, 1.8f, 7, 2.6f);
        Block("Step 3", 8, -1, 1.8f, 4, 5);
        Block("Step 4", 8, 4, 2.8f, 4, 3);
        Block("Step 5", 4, 6.5f, 3.8f, 4, 4);
        Block("Bridge 2", -2.5f, 6.5f, 3.8f, 9, 2.6f);
        Block("Step 6", -9, 6.5f, 3.8f, 4, 4);
        Block("Step 7", -9, 11.5f, 4.8f, 4, 4);
        Block("Step 8", -5, 12, 5.8f, 3, 3);
        Block("Gate Step", -2.2f, 11.5f, 6.8f, 2.2f, 3);
        MoveGroup(new[] { "Platform_A (2)", "DoorR", "Exit" }, new Vector3(2f, 7.38f, 11.5f), -90f);

        Pad("Pad 1", new Vector3(0.5f, 1.8f, -1.6f));
        Pad("Pad 2", new Vector3(2.5f, 1.8f, -0.4f));
        Pad("Pad 3", new Vector3(4.5f, 1.8f, -1.6f));
        Pad("Pad 4", new Vector3(9.3f, 1.8f, -2.8f));
        PatrolPad("Pad 5", new Vector3(6.6f, 2.8f, 4.9f), new Vector3(9.4f, 2.8f, 4.9f), 1.2f);
        Bobber("Bobber 1", new Vector3(3, 3.8f, 5.4f));
        Bobber("Bobber 2", new Vector3(5, 3.8f, 7.6f));
        PatrolPad("Pad 8", new Vector3(-6, 3.8f, 7.1f), new Vector3(-2, 3.8f, 7.1f), 1.2f);
        PatrolPad("Pad 9", new Vector3(1, 3.8f, 5.9f), new Vector3(-3, 3.8f, 5.9f), 1.2f);
        Wall("Wall 1", new Vector3(-7.4f, 3.8f, 6.5f), 4f, 90f);
        PatrolPad("Pad 10", new Vector3(-7.8f, 4.8f, 10), new Vector3(-7.8f, 4.8f, 13), 1.2f);

        SpinBar("Spin Bar 1", new Vector3(-5, 5.8f, 12), 2.6f);

        Coins(new Vector3[]
        {
            new Vector3(0, 0.8f, -6), new Vector3(-3, 1.8f, -1), new Vector3(8, 1.8f, -0.5f),
            new Vector3(8, 2.8f, 3.3f), new Vector3(-9, 3.8f, 6.5f), new Vector3(-10, 4.8f, 12),
            new Vector3(-2.2f, 6.8f, 11.5f),
        });

        Save();
    }

    static void Level2()
    {
        Open("Level2", "Platform_B", "f50c9e36bcc01354ea52c567ff253054");
        Hide("Platform_A", "Platform_A (1)", "Platform_B", "Platform_B (1)", "Platform_C", "Platform_C (2)",
             "Ramp", "Ramp (1)", "Dominoes", "Cube", "Enemy (2)", "Enemy (4)", "Enemy (5)", "Enemy (6)", "Enemy (8)",
             "Step (1)", "Step (2)", "Step (3)", "Step (4)");

        Block("Island 1", 0, -7, 0.8f, 5, 4);
        Block("Island 2", 0, -2.5f, 1.8f, 4, 3);
        Block("Bridge A", -4.5f, -2.5f, 1.8f, 5, 2.6f);
        Block("Island 3", -9, -2.5f, 1.8f, 4, 5);
        Block("Island 4", -9, 2, 2.8f, 4, 3);
        Block("Island 5", -9, 6, 3.8f, 4, 3);
        Block("Bridge B Left", -4.75f, 6, 3.8f, 4.5f, 2.6f);
        Block("Bridge B Right", 3.75f, 6, 3.8f, 4.5f, 2.6f);
        Block("Island 6", 8, 6, 3.8f, 4, 4);
        Block("Island 7", 8, 10.5f, 4.8f, 4, 3);
        Block("Bridge C", 4.5f, 10.5f, 4.8f, 3, 2.6f);
        MoveGroup(new[] { "Platform_A (2)", "DoorR", "Exit" }, new Vector3(0, 5.38f, 11.5f), -90f);

        PatrolPad("Pad 1", new Vector3(-2.5f, 1.8f, -3.1f), new Vector3(-6.5f, 1.8f, -3.1f), 1.4f);
        Pad("Pad 2", new Vector3(-4.5f, 1.8f, -1.9f));
        Wall("Wall 1", new Vector3(-9, 1.8f, -0.6f), 4f, 0f);
        Bobber("Bobber 1", new Vector3(-10.2f, 3.8f, 6.8f));
        Bobber("Bobber 2", new Vector3(-8, 3.8f, 5.2f));
        SpinBar("Spin Bar 1", new Vector3(-9, 2.8f, 2), 2.6f);
        PatrolPad("Pad 3", new Vector3(-6.5f, 3.8f, 5.4f), new Vector3(-3, 3.8f, 5.4f), 1.4f);
        MovingPlatform("Moving Platform", new Vector3(-1.5f, 3.8f, 6), new Vector3(0.5f, 3.8f, 6), 2f, 1f);
        Bobber("Bobber 3", new Vector3(3.5f, 3.8f, 6.6f));
        PatrolPad("Pad 4", new Vector3(5.5f, 3.8f, 5.4f), new Vector3(2, 3.8f, 5.4f), 1.6f);
        Wall("Wall 2", new Vector3(6.4f, 4.8f, 10.5f), 3f, 90f);

        Coins(new Vector3[]
        {
            new Vector3(-1.2f, 0.8f, -7), new Vector3(1.2f, 0.8f, -7),
            new Vector3(0, 1.8f, -2.5f),
            new Vector3(-9.8f, 1.8f, -3.8f), new Vector3(-8.2f, 1.8f, -4.2f),
            new Vector3(7.2f, 3.8f, 6.8f), new Vector3(8.8f, 3.8f, 5.2f),
            new Vector3(9, 4.8f, 11),
        });

        Spawner sp = Object.FindAnyObjectByType<Spawner>();
        if (sp != null) sp.interval = 2f;

        Save();
    }

    static void Boss()
    {
        Open("Level3_Boss", "Island (1)", "0a47857fa1585564eb094b458f06c59a");

        SetBlock("Island (1)", -9, -6, 1.0f, 3.5f, 3.5f);
        SetBlock("Island (2)", 9, -6, 1.0f, 3.5f, 3.5f);
        SetBlock("Island (3)", -11, 1, 2.0f, 3.5f, 3.5f);
        SetBlock("Island (4)", 11, 1, 2.0f, 3.5f, 3.5f);
        SetPillar("Pillar (1)", -5, -3);
        SetPillar("Pillar (2)", 5, -3);

        GameObject p3 = Object.Instantiate(Find("Pillar (1)").gameObject, root);
        p3.name = "Pillar (3)";
        p3.transform.position = new Vector3(0, 1.5f, -7);

        Save();
    }

    static Transform Pad(string name, Vector3 surface)
    {
        GameObject go = Object.Instantiate(padSource, root);
        go.name = name;
        go.SetActive(true);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(1f, 0.3f, 1f);
        go.transform.position = surface + Vector3.up * 0.15f;

        Patrol old = go.GetComponent<Patrol>();
        if (old != null) Object.DestroyImmediate(old);
        return go.transform;
    }

    static void PatrolPad(string name, Vector3 a, Vector3 b, float speed)
    {
        Transform t = Pad(name, a);
        Patrol p = t.gameObject.AddComponent<Patrol>();
        p.pointA = Point(name + "_A", a + Vector3.up * 0.15f);
        p.pointB = Point(name + "_B", b + Vector3.up * 0.15f);
        p.speed = speed;
    }

    static void Bobber(string name, Vector3 surface)
    {
        Animated(name, padSource, surface + Vector3.up * 0.15f, 0f, new Vector3(1f, 0.3f, 1f), "Assets/Animations/Enemy (1).controller");
    }

    static void SpinBar(string name, Vector3 surface, float length)
    {
        Animated(name, padSource, surface + Vector3.up * 0.15f, 0f, new Vector3(length, 0.3f, 0.4f), "Assets/Animations/Enemy (2).controller");
    }

    static void MovingPlatform(string name, Vector3 a, Vector3 b, float length, float speed)
    {
        GameObject go = Block(name, a.x, a.z, a.y, length, 2.6f);
        Patrol p = go.AddComponent<Patrol>();
        p.pointA = Point(name + "_A", go.transform.position);
        p.pointB = Point(name + "_B", new Vector3(b.x, go.transform.position.y, b.z));
        p.speed = speed;
    }

    static void Wall(string name, Vector3 surface, float width, float yaw)
    {
        GameObject w = Animated(name, wallSource, surface + Vector3.up * 0.5f, yaw, new Vector3(width, 1f, 0.34f), null);
        w.tag = "Untagged";
        if (w.GetComponent<Hazard>() == null)
        {
            Hazard h = w.AddComponent<Hazard>();
            h.damage = 1;
            h.destroyOnHit = false;
            h.lifeTime = 0f;
        }
    }

    static GameObject Animated(string name, GameObject source, Vector3 low, float yaw, Vector3 scale, string controller)
    {
        Transform holder = new GameObject(name + "_Holder").transform;
        holder.SetParent(root, false);
        holder.rotation = Quaternion.Euler(0, yaw, 0);

        GameObject go = Object.Instantiate(source, holder);
        go.name = name;
        go.SetActive(true);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        Patrol old = go.GetComponent<Patrol>();
        if (old != null) Object.DestroyImmediate(old);
        if (controller != null)
        {
            Animator an = go.GetComponent<Animator>();
            if (an == null) an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controller);
        }

        AnimatorController ctrl = (AnimatorController)go.GetComponent<Animator>().runtimeAnimatorController;
        AnimationClip clip = (AnimationClip)ctrl.layers[0].stateMachine.defaultState.motion;
        clip.SampleAnimation(go, 0f);
        Vector3 basePos = go.transform.localPosition;
        holder.position = low - holder.rotation * basePos;
        return go;
    }

    static Transform Point(string name, Vector3 pos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(root, false);
        t.position = pos;
        return t;
    }

    static void Open(string scene, string matFrom, string planeMatGuid)
    {
        EditorSceneManager.OpenScene(S + scene + ".unity");
        Transform src = Find(matFrom);
        mat = src.GetComponent<Renderer>().sharedMaterial;
        layer = src.gameObject.layer;
        Transform enemy = Find("Enemy (4)");
        padSource = enemy != null ? enemy.gameObject : null;
        Transform wall = Find("Cube");
        wallSource = wall != null ? wall.gameObject : null;

        root = Find("NewMap");
        if (root != null) Object.DestroyImmediate(root.gameObject);
        root = new GameObject("NewMap").transform;

        Material plane = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(planeMatGuid));
        Transform p = Find("Plane");
        if (p != null && plane != null) p.GetComponent<Renderer>().sharedMaterial = plane;
    }

    static GameObject Block(string name, float x, float z, float top, float sx, float sz)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root, false);
        go.tag = "Ground";
        go.layer = layer;
        go.transform.position = new Vector3(x, top - 0.2f, z);
        go.transform.localScale = new Vector3(sx, 0.4f, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void SetBlock(string name, float x, float z, float top, float sx, float sz)
    {
        Transform t = Find(name);
        if (t == null) return;
        t.gameObject.SetActive(true);
        t.rotation = Quaternion.identity;
        t.position = new Vector3(x, top - 0.15f, z);
        t.localScale = new Vector3(sx, 0.3f, sz);
    }

    static void SetPillar(string name, float x, float z)
    {
        Transform t = Find(name);
        if (t == null) return;
        t.gameObject.SetActive(true);
        t.position = new Vector3(x, t.localScale.y / 2f, z);
    }

    static void Coins(Vector3[] spots)
    {
        List<Transform> coins = new List<Transform>();
        foreach (Transform t in All())
            if (t.CompareTag("Coin")) coins.Add(t);
        coins.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        for (int i = 0; i < coins.Count; i++)
        {
            coins[i].gameObject.SetActive(i < spots.Length);
            if (i < spots.Length) coins[i].position = spots[i] + Vector3.up * 1.0f;
        }
    }

    static void MoveGroup(string[] names, Vector3 target, float yaw)
    {
        Transform main = Find(names[0]);
        Quaternion r = Quaternion.Euler(0, yaw, 0);
        Transform holder = Holder(main, yaw);
        foreach (string n in names)
        {
            Transform t = Find(n);
            if (t != null && t.parent != holder) t.SetParent(holder, false);
        }
        Vector3 local = holder.parent == null ? target : holder.parent.InverseTransformPoint(target);
        holder.localPosition = local - r * main.localPosition;
    }

    static Transform Holder(Transform t, float yaw)
    {
        Transform holder = t.parent != null && t.parent.name.EndsWith("_Holder") ? t.parent : null;
        if (holder == null)
        {
            holder = new GameObject(t.name + "_Holder").transform;
            holder.SetParent(t.parent, false);
            t.SetParent(holder, false);
        }
        holder.localRotation = Quaternion.Euler(0, yaw, 0);
        holder.localScale = Vector3.one;
        return holder;
    }

    static void Hide(params string[] names)
    {
        foreach (string n in names)
        {
            Transform t = Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
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
            if (t.name == name) return t;
        return null;
    }

    static void Save()
    {
        Scene sc = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
    }
}
