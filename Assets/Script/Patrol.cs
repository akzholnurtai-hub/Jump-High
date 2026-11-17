using UnityEngine;

// Moving platform / moving block between two points.
// "speed" is part of the map layout (each mover has its own);
// LevelData.moverSpeedScale makes all movers of a level faster or slower.
public class Patrol : MonoBehaviour
{
    const float ArriveDistance = 0.1f;   // m

    public Transform pointA;
    public Transform pointB;
    public float speed;                  // m/s for this mover, set in the scene

    Transform target;

    void Start()
    {
        target = pointB;
    }

    void Update()
    {
        LevelData level = LevelRunner.CurrentLevel;
        float scale = level != null ? level.moverSpeedScale : 1f;

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * scale * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < ArriveDistance)
        {
            if (target == pointA) target = pointB;
            else target = pointA;
        }
    }
}
