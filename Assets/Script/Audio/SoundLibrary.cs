using System;
using UnityEngine;
using UnityEngine.Audio;

// Every sound event of the game.
public enum Sfx { Jump, Hurt, Pickup, Heal, Shoot, EnemyWindUp, BossWindUp, EnemyHit, EnemyDeath, UIClick, Win, Lose }

// All clips, volumes and audio numbers live here, not in code (same idea as GameConfig).
// The asset is Assets/Resources/SoundLibrary.asset, so AudioManager finds it with Resources.Load.
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Jump High/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Mixer: Master > Music > Soundtrack, Master > SFX; snapshots Normal / Paused")]
    public AudioMixer mixer;

    [Header("Music")]
    public AudioClip menuMusic;                                  // MainMenu and Win
    public AudioClip levelMusic;                                 // every level in GameConfig
    [Tooltip("s, old track fades out while the new one fades in")] public float crossfadeTime = 1f;
    [Tooltip("s, Normal <-> Paused snapshot")] public float pauseFadeTime = 0.3f;

    [Header("3D sounds (enemies, boss)")]
    [Range(0f, 1f), Tooltip("0 = 2D, 1 = full 3D. A bit of 2D so a wind-up off screen is still heard")]
    public float spatialBlend = 0.7f;
    [Tooltip("m, full volume inside this distance")] public float minDistance = 6f;
    [Tooltip("m, quietest at this distance")] public float maxDistance = 40f;

    [Header("Sound events")]
    public SoundEvent jump = new SoundEvent();
    public SoundEvent hurt = new SoundEvent();
    public SoundEvent pickup = new SoundEvent();
    public SoundEvent heal = new SoundEvent();
    public SoundEvent shoot = new SoundEvent();
    public SoundEvent enemyWindUp = new SoundEvent();
    public SoundEvent bossWindUp = new SoundEvent();
    public SoundEvent enemyHit = new SoundEvent();
    public SoundEvent enemyDeath = new SoundEvent();
    public SoundEvent uiClick = new SoundEvent();
    public SoundEvent win = new SoundEvent();
    public SoundEvent lose = new SoundEvent();

    public SoundEvent Get(Sfx s)
    {
        switch (s)
        {
            case Sfx.Jump: return jump;
            case Sfx.Hurt: return hurt;
            case Sfx.Pickup: return pickup;
            case Sfx.Heal: return heal;
            case Sfx.Shoot: return shoot;
            case Sfx.EnemyWindUp: return enemyWindUp;
            case Sfx.BossWindUp: return bossWindUp;
            case Sfx.EnemyHit: return enemyHit;
            case Sfx.EnemyDeath: return enemyDeath;
            case Sfx.UIClick: return uiClick;
            case Sfx.Win: return win;
            case Sfx.Lose: return lose;
        }
        return null;
    }
}

// One sound event: 1-3 alternative clips + a random pitch, so a sound heard 5 times in a row
// does not sound like a robot.
[Serializable]
public class SoundEvent
{
    public AudioClip[] clips = new AudioClip[0];
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("random pitch between these two (1 = normal)")] public float minPitch = 0.92f;
    public float maxPitch = 1.08f;

    [NonSerialized] int last = -1;

    // a random clip, but not the same one twice in a row
    public AudioClip Pick()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        int i;
        if (last < 0 || last >= clips.Length) i = UnityEngine.Random.Range(0, clips.Length);
        else
        {
            i = UnityEngine.Random.Range(0, clips.Length - 1);   // one of the others...
            if (i >= last) i++;                                  // ...by skipping the last one
        }
        last = i;
        return clips[i];
    }

    public float RandomPitch() => UnityEngine.Random.Range(minPitch, maxPitch);
}
