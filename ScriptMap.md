# Script Map — Jump High

My game has three parts: **data**, **game**, **save**. Data says the numbers, game uses them, save remembers progress.

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

## Who talks to who (short)

| Script | Talks to | Why |
|---|---|---|
| LevelSelect | SaveSystem, LevelRunner | show locked levels, start a level |
| LevelRunner | LevelData, SaveSystem, Movement, HUD | build the level, save the result |
| Movement | LevelRunner, SaveSystem | score for coins, finish the level, count deaths |
| Enemies / Boss / Hazard | Health | hurt the player |
| Health | HUD, SaveSystem | show hearts, count deaths |
| MenuButtons / ProfileSettings | SaveSystem | settings and reset |
