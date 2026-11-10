using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Menu: Week10 > Restore old HUD + new mouse camera (all levels)
// Puts the hearts, carrot/coin box and timer back to the original RK2 look
// (removes HeartBox, CoinBar, DamageFlash, HUDFx, outlines) and sets up the mouse/swipe camera.
public static class Week10HUD
{
    const string S = "Assets/Scenes/";
    static readonly string[] Levels = { "Level1", "Level_Enemies", "Level2", "Level3_Boss" };
    static readonly Color Panel = new Color(0.07f, 0.09f, 0.16f, 0.78f);   // the RK2 panel color

    [MenuItem("Week10/Restore old HUD + new mouse camera (all levels)")]
    static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int done = 0;
        foreach (string lvl in Levels)
        {
            string path = S + lvl + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue;
            EditorSceneManager.OpenScene(path);
            RestoreHUD();
            SetupCamera();
            Scene sc = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            done++;
        }
        EditorUtility.DisplayDialog("Week10",
            "Old HUD is back and the mouse/swipe camera is on in " + done + " levels.\n\n" +
            "PC: move the mouse to look (click the Game view first). Esc = pause.\n" +
            "Phone: swipe the screen to look.", "OK");
    }

    static void RestoreHUD()
    {
        HUD hud = Object.FindAnyObjectByType<HUD>();
        Movement mv = Object.FindAnyObjectByType<Movement>();
        if (hud == null) return;
        Canvas c = hud.GetComponentInParent<Canvas>();
        Transform canvas = c != null ? c.rootCanvas.transform : hud.transform;

        // things added by the restyle
        foreach (string n in new[] { "HeartBox", "DamageFlash", "CoinBar" })
        {
            Transform t;
            while ((t = Find(canvas, n)) != null) Object.DestroyImmediate(t.gameObject);
        }
        foreach (HUDFx fx in canvas.GetComponentsInChildren<HUDFx>(true)) Object.DestroyImmediate(fx);

        // hearts: back to the top-left row
        for (int i = 0; i < hud.hearts.Length; i++)
        {
            Image h = hud.hearts[i];
            if (h == null) continue;
            RemoveEffects(h.gameObject);
            h.preserveAspect = false;
            Place(h.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40 + i * 115, -40), new Vector2(100, 100));
            EditorUtility.SetDirty(h);
        }

        // carrot / coin box and timer box
        foreach (string n in new[] { "CoinBox", "TimeBox" })
        {
            Transform box = Find(canvas, n);
            if (box == null) continue;
            RemoveEffects(box.gameObject);
            Image img = box.GetComponent<Image>();
            if (img != null)
            {
                img.color = Panel;
                img.pixelsPerUnitMultiplier = 0.35f;
                EditorUtility.SetDirty(img);
            }
            box.localScale = Vector3.one;
        }

        Transform icon = Find(canvas, "CarrotIcon");
        if (icon != null)
            Place(icon.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(70, 70));

        if (mv != null && mv.coinText != null)
        {
            RemoveEffects(mv.coinText.gameObject);
            mv.coinText.color = Color.white;
            mv.coinText.rectTransform.offsetMin = new Vector2(icon != null ? 100 : 20, 0);
            mv.coinText.rectTransform.offsetMax = new Vector2(-20, 0);
            EditorUtility.SetDirty(mv.coinText);
        }
        if (mv != null && mv.timerText != null)
        {
            RemoveEffects(mv.timerText.gameObject);
            mv.timerText.color = Color.white;
            EditorUtility.SetDirty(mv.timerText);
        }
    }

    static void SetupCamera()
    {
        CameraFollow cf = Object.FindAnyObjectByType<CameraFollow>();
        if (cf == null) return;
        cf.offset = new Vector3(0f, 4f, -7f);
        cf.mouseSensitivity = 0.12f;
        cf.touchSensitivity = 0.2f;
        cf.lockCursor = true;
        int walls = LayerMask.NameToLayer("Walls");
        cf.obstacleMask = walls >= 0 ? (1 << walls) : 0;
        EditorUtility.SetDirty(cf);
    }

    // Outline derives from Shadow, so this removes both
    static void RemoveEffects(GameObject go)
    {
        foreach (Shadow s in go.GetComponents<Shadow>()) Object.DestroyImmediate(s);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    static Transform Find(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
