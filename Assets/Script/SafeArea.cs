using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform panel;
    Rect lastArea;
    Vector2Int lastScreen;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
            Apply();
    }

    void Apply()
    {
        lastArea = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = lastArea.position;
        Vector2 max = lastArea.position + lastArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        panel.anchorMin = min;
        panel.anchorMax = max;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
    }
}
