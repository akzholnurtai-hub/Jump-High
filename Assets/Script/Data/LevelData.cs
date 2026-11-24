using System;
using System.Collections.Generic;
using UnityEngine;

public enum LevelGoal { CollectCoins, DefeatBoss, KillAllMonsters }   // new values only at the end (saved as numbers)

// One asset = one level. LevelRunner reads it when the scene starts:
// time limit, goal, score rules, which enemies to spawn and where, falling hazards.
[CreateAssetMenu(fileName = "Level_01", menuName = "Jump High/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level")]
    [Tooltip("1, 2, 3... used in the save file")] public int levelNumber;
    public string displayName;
    [Tooltip("scene with the map")] public string sceneName;
    [Tooltip("empty = after this level comes the Win screen")] public LevelData nextLevel;

    [Header("Goal")]
    public LevelGoal goal;
    [Tooltip("coins that open the gate, 0 = every coin in the scene")] public int coinsRequired;
    [Tooltip("s")] public float timeLimit;

    [Header("Score")]
    public int clearBonus;
    [Tooltip("score per second left on the timer")] public int timeBonusPerSecond;

    [Header("World")]
    [Tooltip("falling below this Y = death")] public float killHeight;
    [Tooltip("1 = moving platforms / blocks at their own speed")] public float moverSpeedScale;

    [Header("Enemies (spawned by LevelRunner)")]
    public List<EnemySpawn> enemies = new List<EnemySpawn>();

    [Header("Falling hazards (Spawner)")]
    public HazardWave hazards = new HazardWave();
}

[Serializable]
public class EnemySpawn
{
    public EnemyStats type;
    public Vector3 position;
    [Tooltip("degrees")] public float yaw;
    [Tooltip("Patroller waypoints, world positions")] public List<Vector3> route = new List<Vector3>();
}

[Serializable]
public class HazardWave
{
    public bool enabled;
    [Tooltip("s before the first drop")] public float firstDelay;
    [Tooltip("s between drops")] public float interval;
    [Tooltip("s, every this often the interval gets shorter (0 = never)")] public float rampEvery;
    [Tooltip("interval is multiplied by this at each ramp")] public float rampFactor;
    [Tooltip("s, the interval never gets shorter than this")] public float minInterval;
    [Tooltip("m, half size of the drop zone (x, z) around the Spawner")] public Vector2 area;
    [Tooltip("m, world Y where things appear")] public float height;
}
