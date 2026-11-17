using UnityEngine;

[CreateAssetMenu(fileName = "ChargerStats", menuName = "Jump High/Enemy/Charger")]
public class ChargerStats : EnemyStats
{
    [Header("Charger")]
    [Tooltip("s in Idle before the next charge")] public float restTime;
    [Tooltip("s of warning")] public float windUp;
    [Tooltip("s, stops turning at the end of the wind-up")] public float aimLock;
    [Tooltip("m/s")] public float dashSpeed;
    [Tooltip("m")] public float dashDistance;
    [Tooltip("m")] public float hitRadius;
    [Tooltip("s, free hits for the player")] public float stunTime;
    [Tooltip("m, charges only when this close to its home")] public float homeRadius;
    [Tooltip("m/s when walking back home")] public float returnSpeed;
}
