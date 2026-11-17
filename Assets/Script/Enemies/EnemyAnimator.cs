using UnityEngine;

// Procedural animation for the robot models (no animation files needed).
// Put it on the "Model" child. Speed is measured from how fast the enemy (parent) moves.
public class EnemyAnimator : MonoBehaviour
{
    public enum Style { Spider, Hover, Quadruped }

    public Style style = Style.Spider;
    public Transform[] legs;            // pivots that swing
    public Transform[] flames;          // jet flames that flicker (Hover)
    public float hoverBob = 0.08f;      // m

    Quaternion[] baseRot;
    Vector3[] flameScale;
    Vector3 baseLocal;
    Vector3 lastPos;
    float phase;

    void Awake()
    {
        baseLocal = transform.localPosition;
        lastPos = Root().position;

        baseRot = new Quaternion[legs != null ? legs.Length : 0];
        for (int i = 0; i < baseRot.Length; i++) baseRot[i] = legs[i].localRotation;

        flameScale = new Vector3[flames != null ? flames.Length : 0];
        for (int i = 0; i < flameScale.Length; i++) flameScale[i] = flames[i].localScale;
    }

    Transform Root() { return transform.parent != null ? transform.parent : transform; }

    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 p = Root().position;
        float speed = (p - lastPos).magnitude / dt;   // m/s
        lastPos = p;

        float amount = Mathf.Clamp01(speed / 3f);
        phase += dt * (4f + speed * 2.5f);

        for (int i = 0; i < baseRot.Length; i++)
        {
            Quaternion swing;
            if (style == Style.Spider)
            {
                float s = Mathf.Sin(phase + i * Mathf.PI * 0.5f);
                swing = Quaternion.Euler(s * 18f * amount, s * 22f * amount, 0f);
            }
            else
            {
                float s = Mathf.Sin(phase + (i % 2 == 0 ? 0f : Mathf.PI));
                swing = Quaternion.Euler(s * 35f * amount, 0f, 0f);
            }
            legs[i].localRotation = baseRot[i] * swing;
        }

        if (style == Style.Hover)
        {
            transform.localPosition = baseLocal + Vector3.up * Mathf.Sin(Time.time * 3f) * hoverBob;
            for (int i = 0; i < flameScale.Length; i++)
                flames[i].localScale = new Vector3(flameScale[i].x, flameScale[i].y * Random.Range(0.7f, 1.3f), flameScale[i].z);
        }
        else
        {
            transform.localPosition = baseLocal + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * 0.06f * amount;
        }
    }
}
