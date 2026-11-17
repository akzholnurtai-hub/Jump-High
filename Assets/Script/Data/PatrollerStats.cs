using UnityEngine;

[CreateAssetMenu(fileName = "PatrollerStats", menuName = "Jump High/Enemy/Patroller")]
public class PatrollerStats : EnemyStats
{
    [Header("Patroller")]
    [Tooltip("m/s")] public float patrolSpeed;
    [Tooltip("m/s (compare with the player's moveSpeed)")] public float chaseSpeed;
    [Tooltip("m, starts the wind-up")] public float attackRange;
    [Tooltip("m, the strike lands only if the player is still this close")] public float hitRange;
    [Tooltip("s of red warning before the hit")] public float windUp;
    [Tooltip("s standing still after the hit")] public float recovery;
    [Tooltip("s without seeing the player -> back to Patrol")] public float loseTime;
}
