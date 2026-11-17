using UnityEngine;

[CreateAssetMenu(fileName = "ShooterStats", menuName = "Jump High/Enemy/Shooter")]
public class ShooterStats : EnemyStats
{
    [Header("Shooter")]
    [Tooltip("m, distance it tries to keep")] public float preferredDistance;
    [Tooltip("m, +/- around preferredDistance where it stands still")] public float distanceSlack;
    [Tooltip("m/s")] public float moveSpeed;
    [Tooltip("m/s when walking back home")] public float returnSpeed;
    [Tooltip("m, never walks further than this from home")] public float leash;
    [Tooltip("s after seeing the player before it starts to aim")] public float reactionTime;
    [Tooltip("s of laser before the shot")] public float aimTime;
    [Tooltip("s, last part of the aim does not turn (dodge works)")] public float aimLock;
    [Tooltip("m/s")] public float shotSpeed;
    [Tooltip("s after a shot")] public float cooldown;
    [Tooltip("s unseen -> Idle")] public float loseTime;
}
