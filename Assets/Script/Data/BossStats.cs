using UnityEngine;

[CreateAssetMenu(fileName = "BossStats", menuName = "Jump High/Boss Stats")]
public class BossStats : ScriptableObject
{
    public int maxHp;
    [Tooltip("score for the kill")] public int killScore;

    [Header("Rhythm")]
    [Tooltip("s before the first attack")] public float startDelay;
    [Tooltip("s between attacks")] public float pause;
    [Tooltip("phase 2 starts at this part of max HP (0.5 = half)")] public float phase2At;
    [Tooltip("pause is multiplied by this in phase 2")] public float phase2PauseScale;

    [Header("Fan attack")]
    public int fanCount;
    public int fanCountPhase2;
    [Tooltip("degrees between fireballs")] public float fanAngleStep;
    [Tooltip("s after the fan")] public float fanRecover;
    [Tooltip("m/s")] public float fireballSpeed;

    [Header("Rock rain")]
    [Tooltip("s the red mark is shown before the rock falls")] public float rainWarnTime;
    [Tooltip("m above the ground")] public float rockDropHeight;
}
