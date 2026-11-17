using UnityEngine;

// Numbers every enemy type has. PatrollerStats / ShooterStats / ChargerStats add their own.
// One asset = one enemy type. The asset also knows the prefab, so a LevelData only says
// "this type at this point" and LevelRunner can spawn it.
public abstract class EnemyStats : ScriptableObject
{
    [Header("Type")]
    public string displayName;
    [Tooltip("filled by Week11 > Setup")] public GameObject prefab;

    [Header("Body")]
    public int maxHp;
    [Tooltip("hearts taken from the player per hit")] public int damage;
    [Tooltip("score for a kill")] public int killScore;

    [Header("Sight")]
    [Tooltip("m")] public float viewRange;
    [Tooltip("degrees, whole cone (360 = sees all around)")] public float fov;
    [Tooltip("s between sight checks")] public float sightInterval;

    [Header("Turn")]
    [Tooltip("deg/s while walking")] public float turnSpeed;
    [Tooltip("deg/s while aiming / winding up")] public float aimTurnSpeed;
}
