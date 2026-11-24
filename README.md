# Jump High

A 3D mobile action game made in Unity 6 for Game Development Fundamentals (Narxoz University).

Fight your way through abandoned places full of monsters. In every stage you must **kill all the monsters**,
then the exit door opens. On the way you jump over barriers, pits, spinning pipes and sliding blocks,
and avoid toxic pools, broken glass and falling debris. The last stage is a demon boss.

- **Engine:** Unity 6000.6.0f1 (URP), Input System 1.20.1, AI Navigation (NavMesh)
- **Platform:** Android (APK), landscape
- **Scenes:** MainMenu → Stage1 → Stage2 → Stage3 → Stage4 → Stage5 (boss) → Win
- **Author:** Akzhol Nurtai

## How to play

| Phone | Keyboard | Action |
|---|---|---|
| Joystick (bottom left) | WASD / arrows | Move |
| Swipe on the screen | Mouse | Turn the camera |
| FIRE | Right mouse button | Shoot (where the camera looks) |
| JUMP | Space | Jump |
| II (top right) | Esc | Pause |

HUD: hearts (top left), skull + monsters left, timer, pause button.
Coins give extra score. Hearts sometimes fall from the ceiling and heal you.

## Stages (easy → hard)

| # | Stage | Monsters | Obstacles | Time |
|---|---|---|---|---|
| 1 | Dead Street | 3 Ghouls | barriers, crates, 1 sliding block, 1 spinning pipe, toxic pool | 150 s |
| 2 | Rust Factory | 4 Ghouls, 2 Spitters | toxic pools, 2 spinning pipes, sliding block, raised floor, falling debris | 200 s |
| 3 | Metro Tunnel | 3 Ghouls, 2 Spitters, 1 Brute | pits (fall = death), plank bridge, sliding block, spinning pipe, darkness | 220 s |
| 4 | Ghost Hospital | 5 Ghouls, 3 Spitters, 2 Brutes | rooms with one door, glass spikes, toxic, 2 sliding blocks, spinning pipe | 260 s |
| 5 | Monster Lair | demon boss + 2 Ghouls | fireball fans, falling rocks | 300 s |

## Monsters

| Monster | AI (Week 10) | How it attacks | Warning before the attack |
|---|---|---|---|
| **Ghoul** | Patroller | walks, sees you, chases, hits up close | stops, turns red, wind-up sound |
| **Spitter** | Shooter | keeps its distance and spits | red aim line + wind-up sound |
| **Brute** | Charger | charges in a straight line | red lane on the floor + wind-up sound |
| **Demon boss** | Boss | fireball fans, rocks from the sky | growl 0.6 s before the fan, red mark under the rock |

A shot-dead monster falls over backwards, lies for a moment and sinks into the floor.

## Project map

| Folder | What is inside |
|---|---|
| `Assets/Script` | game code: `Movement`, `Health`, `Shooter`, `Boss`, `Enemies/`, `Level/`, `Save/`, `Audio/`, `World/` |
| `Assets/Data` | all balance numbers (ScriptableObjects): player, items, boss, `Monsters/`, `Stages/` |
| `Assets/Resources` | `GameConfig` (knows every data asset), `SoundLibrary` (every sound) |
| `Assets/Audio` | `MainMixer`, music, sound effects |
| `Assets/Scenes/Stages` | Stage1–5 (built by the tool below) |
| `Assets/Editor` | editor tools (menus below) |
| `Tools` | Python scripts that made my own sounds and the skull icon |
| `Docs` | GDD, stage screenshots |

How the scripts talk to each other: **`ScriptMap.md`**.

## Editor tools (Unity menu bar)

| Menu | What it does |
|---|---|
| **Monsters → 1. Build 5 stages** | builds the 5 stage scenes, monster prefabs, stage data, NavMesh, Build Settings |
| **Week12 → 1. Setup Sound Pass** / **2. Check** / **Open Audio Mixer** | sound setup, checklist, mixer window |
| **Week11 → Save file → …** | open / print / break / delete `save.json` |
| **Week10 → Build Android APK** | makes the APK |
| **RK2 → Build Mobile UI** | phone HUD and menus |

The old levels (Level1, Level2, Level3_Boss, Level_Enemies) are still in the project but not in the build.

## Save file

`save.json` in `Application.persistentDataPath` (JsonUtility, never BinaryFormatter): unlocked stages, best scores,
coins, deaths and settings (name, music on/off, **Music and SFX volume**). A broken file is kept as
`save.broken.json` and the game starts with defaults.

## Sound (Assignment 12: Sound Pass)

- **AudioMixer** `Assets/Audio/MainMixer`: **Master** → **Music** → Soundtrack, **Master** → **SFX**. Every AudioSource is routed.
- **Snapshots:** Normal and **Paused** (Soundtrack −14 dB + lowpass 700 Hz): pause makes the music quiet and muffled.
- **12 sound events:** jump, hurt, pickup (coin), heal, shoot, enemy wind-up, boss wind-up, enemy hit, death, UI click, win, lose.
- **Wind-up first:** every monster attack plays its wind-up sound at the start of the wind-up, so you hear it before it lands.
- **2 music tracks:** menu and level, 1 s crossfade; the music does not restart when a stage reloads.
- **Settings:** Music and SFX sliders in decibels (`Log10(value) × 20`), saved in `save.json`.
- **Variation:** random pitch 0.92–1.08 and 2 clips for jump, hurt, coin, wind-up, enemy hit.

Code: `Assets/Script/Audio/` (`AudioManager`, `SoundLibrary`, `VolumeSlider`, `ClickSound`).

### Sound sources and licenses

| File (`Assets/Audio/...`) | Used for | Source | License |
|---|---|---|---|
| `Music/tatamusic-game-game-music-599001.mp3` | level music | Pixabay, "Game Music" by tatamusic | Pixabay Content License |
| `Music/menu_theme.wav` | menu + win music | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/freesound_community-072782_bullet-39717.mp3` | player shot | Pixabay, freesound_community | Pixabay Content License |
| `SFX/freesound_community-084373_heal-36672.mp3` | heal | Pixabay, freesound_community | Pixabay Content License |
| `SFX/freesound_community-breaking-small-bone-finger-88085.mp3` | player hurt (variant 1) | Pixabay, freesound_community | Pixabay Content License |
| `SFX/lost-money-on-the-game-account.mp3` | coin pickup (variant 1) | Pixabay | Pixabay Content License |
| `SFX/jump.wav`, `SFX/win.wav` | jump (variant 1), win | own, generated by script | own work, CC0 |
| `SFX/jump_2.wav`, `coin_2.wav`, `hurt_2.wav` | second variants | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/enemy_windup_1.wav`, `enemy_windup_2.wav` | monster wind-up | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/boss_windup.wav` | boss wind-up | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/enemy_hit_1.wav`, `enemy_hit_2.wav` | monster / boss hit | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/enemy_death.wav` | monster / boss death | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/ui_click.wav` | every button | own, `Tools/make_sounds.py` | own work, CC0 |
| `SFX/lose.wav` | death / time up | own, `Tools/make_sounds.py` | own work, CC0 |

My own sounds are made only from sine / square / triangle waves and noise; `py Tools/make_sounds.py` makes them
again (same result every time). Pixabay Content License: free to use in games, no attribution needed,
the sound itself may not be sold on its own.

## Other assets

| Asset | Source | License |
|---|---|---|
| Hero model, Idle / Run / Jump animations | Mixamo (Adobe) | free to use in games |
| Monster and stage models | made from Unity primitives by `MonsterStagesSetup.cs` | own work |
| Skull HUD icon (`Assets/Textures/SkullIcon.png`) | own, `Tools/make_skull_icon.py` | own work, CC0 |
| Font LiberationSans | TextMesh Pro | SIL Open Font License |

## AI use

AI tools (Claude Code) helped with this project. What I asked, what the agent got wrong and what I changed
is in **`DECISIONS.md`** (weeks 10–12) and `AI_LOG.txt` (weeks 8–9).
