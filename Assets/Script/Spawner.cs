using System.Collections;
using UnityEngine;

// Drops random hazards (rocks, bombs, hearts) around itself.
// When, how often and how wide: LevelData.hazards of the current level.
public class Spawner : MonoBehaviour
{
    public Hazard[] prefabs;

    IEnumerator Start()
    {
        LevelData level = LevelRunner.CurrentLevel;
        if (level == null || !level.hazards.enabled || prefabs == null || prefabs.Length == 0) yield break;

        HazardWave w = level.hazards;
        float interval = w.interval;
        float nextRamp = w.rampEvery > 0f ? Time.time + w.firstDelay + w.rampEvery : Mathf.Infinity;

        yield return new WaitForSeconds(w.firstDelay);

        while (true)
        {
            Hazard pick = prefabs[Random.Range(0, prefabs.Length)];
            Vector3 pos = new Vector3(
                transform.position.x + Random.Range(-w.area.x, w.area.x),
                w.height,
                transform.position.z + Random.Range(-w.area.y, w.area.y));
            Instantiate(pick, pos, Quaternion.identity);

            if (Time.time >= nextRamp)                       // the rain gets faster over time
            {
                interval = Mathf.Max(w.minInterval, interval * w.rampFactor);
                nextRamp = Time.time + w.rampEvery;
            }
            yield return new WaitForSeconds(interval);
        }
    }
}
