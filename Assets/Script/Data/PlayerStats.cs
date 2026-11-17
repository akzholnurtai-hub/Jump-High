using UnityEngine;

// Every balance number of the player. Scripts read it while the game runs,
// so a change in the Inspector works at once, even in Play mode.
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Jump High/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Move")]
    [Tooltip("m/s")] public float moveSpeed;
    [Tooltip("m/s upward at the start of a jump")] public float jumpForce;
    [Tooltip("how fast the body turns to the move direction")] public float turnSpeed;

    [Header("Health")]
    public int maxHp;
    [Tooltip("s of blinking after a hit, no damage")] public float invincibleTime;
    [Tooltip("s between two blinks")] public float blinkInterval;

    [Header("Gun")]
    public Shooter.Pattern firePattern;
    [Tooltip("s between two shots")] public float fireCooldown;
    [Tooltip("m/s")] public float bulletSpeed;
    [Tooltip("s before the bullet disappears")] public float bulletLifetime;
    [Tooltip("bullets in the Spread fan")] public int spreadCount;
    [Tooltip("degrees between Spread bullets")] public float spreadAngle;
    [Tooltip("bullets in one Burst")] public int burstCount;
    [Tooltip("s between Burst bullets")] public float burstDelay;
}
