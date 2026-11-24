using System.Collections;
using UnityEngine;

// The exit door of a monster stage. It stays shut (red lamp) while monsters are alive,
// and slides up (green lamp) when LevelRunner says the exit is open.
// The real check is in Movement (the Exit trigger behind the door), this is what the player sees.
public class ExitGate : MonoBehaviour
{
    public Transform door;
    public Renderer lamp;
    public Material lampOpen;
    public float openHeight = 4.5f;      // m the door moves up
    public float openTime = 1.2f;        // s

    bool open;

    void Start()
    {
        LevelRunner.MonstersChanged += Check;
        Check();
    }

    void OnDestroy()
    {
        LevelRunner.MonstersChanged -= Check;
    }

    void Check()
    {
        if (open || LevelRunner.Current == null || !LevelRunner.Current.ExitOpen) return;
        open = true;
        if (lamp != null && lampOpen != null) lamp.sharedMaterial = lampOpen;
        if (door != null) StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        Vector3 from = door.localPosition;
        Vector3 to = from + Vector3.up * openHeight;
        for (float t = 0f; t < openTime; t += Time.deltaTime)
        {
            door.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / openTime));
            yield return null;
        }
        door.localPosition = to;
    }
}
