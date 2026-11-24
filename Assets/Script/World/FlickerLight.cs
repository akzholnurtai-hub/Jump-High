using UnityEngine;

// A broken lamp in an abandoned building: mostly on, sometimes it blinks out for a moment.
[RequireComponent(typeof(Light))]
public class FlickerLight : MonoBehaviour
{
    [Range(0f, 1f)] public float blinkChance = 0.12f;   // chance of a short blackout at each change

    Light lamp;
    float baseIntensity;
    float next;

    void Awake()
    {
        lamp = GetComponent<Light>();
        baseIntensity = lamp.intensity;
    }

    void Update()
    {
        if (Time.time < next) return;
        bool blink = Random.value < blinkChance;
        lamp.intensity = blink ? baseIntensity * 0.08f : baseIntensity * Random.Range(0.8f, 1.1f);
        next = Time.time + (blink ? Random.Range(0.04f, 0.15f) : Random.Range(0.1f, 0.6f));
    }
}
