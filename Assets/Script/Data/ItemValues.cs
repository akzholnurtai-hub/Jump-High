using UnityEngine;

// Pickups, traps and projectiles that are not a player or an enemy.
[CreateAssetMenu(fileName = "ItemValues", menuName = "Jump High/Item Values")]
public class ItemValues : ScriptableObject
{
    [Header("Pickups")]
    [Tooltip("score per coin")] public int coinScore;
    [Tooltip("hearts restored by a Heart")] public int heartHeal;
    [Tooltip("s before a fallen Rock / Heart / Bomb disappears")] public float pickupLifetime;

    [Header("Traps")]
    [Tooltip("damage of spikes, moving blocks, spin bars")] public int trapDamage;
    [Tooltip("deg/s")] public float spinnerSpeed;
    public int lavaDamage;
    [Tooltip("m/s upward kick from lava")] public float lavaBounce;

    [Header("Falling things")]
    public int rockDamage;
    [Tooltip("damage if the bomb lands on the player")] public int bombDamage;
    [Tooltip("s from landing to explosion")] public float bombFuse;
    [Tooltip("m, 0 = no blast")] public float bombBlastRadius;
    public int bombBlastDamage;

    [Header("Fireball")]
    public int fireballDamage;
    [Tooltip("m/s if the shooter does not set it")] public float fireballSpeed;
    [Tooltip("s")] public float fireballLifetime;
}
