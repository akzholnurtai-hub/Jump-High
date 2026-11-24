# Script Map — Jump High

My game has four parts: **data**, **game**, **save**, **sound**. Data says the numbers, game uses them, save remembers progress, sound makes every action heard.

## 1. Data (no numbers in code)

```
GameConfig  (Assets/Resources)
 ├── PlayerStats   speed, jump, HP, gun
 ├── ItemValues    coins, traps, fireballs, bombs
 ├── BossStats     boss HP, attacks, phase 2
 ├── EnemyStats    SpiderBot / JetMech / RhinoTank
 ├── LevelData x4  scene, enemies, time, goal, next level
 └── default settings for the save file
```

Any script gets the numbers with `GameConfig.Get()`.
If I change a number in the Inspector, the game changes. I do not touch the code.

## 2. Game

```
MainMenu
 ├── MenuButtons  ── Play / Reset (with confirm) / volume
 └── LevelSelect  ── one button per LevelData, locked or open (asks SaveSystem)
        │
        ▼  LevelRunner.Load(level)
Level scene
 └── LevelRunner  ── reads LevelData, spawns enemies, counts score
       ├── Movement (player) ── moves, jumps, coins, timer ── Shooter (gun) ── Bullet
       ├── Health   (player) ── hearts, damage, death ── HUD
       ├── EnemyBase ── PatrollerAI / ShooterAI / ChargerAI ── hit player's Health
       ├── Boss      ── Fireball + Rock ── hit player's Health
       └── Hazard    ── traps, lava, bombs, fireballs
```

- **LevelRunner** is the same script in every level. It does not know numbers, it takes them from LevelData.
- When the player reaches the exit or kills the boss, `LevelRunner.Complete()` adds the bonus, saves and loads the next level.
- Enemies and the boss do not kill the player directly. They call `Health.TakeDamage()`, so all damage goes through one place.

## 3. Save

```
SaveSystem ── save.json in Application.persistentDataPath
 ├── SaveData   unlocked levels, best scores, coins, deaths, settings
 └── SaveHooks  saves on pause / quit, and 0.5 s after a settings change
```

- **Load:** at game start. No file or broken file → default values, no crash.
- **Save:** after a level, after Reset, when settings change, and on `OnApplicationPause(true)`.
- I use **JsonUtility**, not BinaryFormatter. Lists instead of Dictionary, because JsonUtility cannot save a Dictionary.

## 4. Sound (Week 12)

```
SoundLibrary (Assets/Resources)  mixer, menu + level music, 12 sound events (clips, volume, pitch range)
        │
AudioManager  (made before the first scene, DontDestroyOnLoad = one for the whole game)
 ├── Music A / Music B ── crossfade 1 s ── mixer group Music > Soundtrack
 ├── UI source        ── clicks, also in the pause menu ── SFX
 └── 16 SFX sources   ── 2D (player) or 3D at a point (enemies, boss) ── SFX

Movement   ── Jump, Pickup, Lose          Health     ── Hurt, Heal, Lose
Shooter    ── Shoot                       EnemyBase  ── EnemyWindUp (start of every wind-up)
EnemyHealth── EnemyHit, EnemyDeath        Boss       ── BossWindUp, EnemyWindUp (rock mark), hit, death
LevelRunner── Win                         ClickSound ── UIClick (on every button)
PauseMenu  ── AudioManager.SetPaused  -> Paused snapshot (Soundtrack -14 dB + lowpass)
VolumeSlider ── SaveSystem.Settings -> AudioManager.ApplyVolumes -> mixer MusicVol / SFXVol (dB)
```

- Scripts never hold clips: they say **what** happened (`AudioManager.Play(Sfx.Jump)`), the SoundLibrary says **how it sounds**.
- **Why one AudioManager:** it lives between scenes, so the music does not restart on a level load, and a lose sound is not cut by the scene reload.
- **Why dB:** the ear hears loudness in a log scale. Slider 0.5 = -6 dB, 0.1 = -20 dB, 0 = -80 dB (off).
- **Wind-up first:** the sound plays at the START of the wind-up, so the player hears the attack before it lands.

## Who talks to who (short)

| Script | Talks to | Why |
|---|---|---|
| LevelSelect | SaveSystem, LevelRunner | show locked levels, start a level |
| LevelRunner | LevelData, SaveSystem, Movement, HUD | build the level, save the result |
| Movement | LevelRunner, SaveSystem | score for coins, finish the level, count deaths |
| Enemies / Boss / Hazard | Health | hurt the player |
| Health | HUD, SaveSystem | show hearts, count deaths |
| MenuButtons / ProfileSettings | SaveSystem | settings and reset |
| VolumeSlider | SaveSystem, AudioManager | save the volume, send it to the mixer in dB |
| Movement / Health / enemies / Boss / LevelRunner | AudioManager | play a sound event |
| PauseMenu | AudioManager | Paused snapshot |
| AudioManager | SoundLibrary, AudioMixer, GameConfig | clips, groups, which scene is a level |
