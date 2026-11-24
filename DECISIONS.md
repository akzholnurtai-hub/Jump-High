

## Week 10 · Three Enemy Types

2026-11-05 **Enemy damage and the "Enemy" tag.** I asked Claude Code for three enemy FSMs (Patroller, Shooter, Charger). My `Movement.cs` restarts the level when the player touches anything tagged `Enemy`, so the new enemies are **not** tagged `Enemy`. They deal damage through `Health.TakeDamage(1)` instead, so the hit uses my heart system and the invincibility frames.
2026-11-06 **Shots flew through walls.** The Shooter re-uses my `Fireball` prefab. Its collider is a trigger, so the shot passed through cover and the player could not hide. I added `ShotStopper.cs`, which destroys the shot when it touches the `Walls` layer. Sight raycasts also use an `obstacleMask` with only `Walls` ticked, so a ray never hits the enemy's own collider.
2026-11-07 **Numbers for my player speed.** The lecture numbers assume a player at 4 m/s, but my player runs at 6 m/s. With chase speed 3 m/s the Patroller was never a threat, so I set it to **4 m/s** and the Shooter's shot speed to **7 m/s**. To keep the attacks fair, the Shooter's aim and the Charger's wind-up stop turning **0.15–0.2 s before the attack**, so stepping aside at the last moment works.
2026-11-09 **Camera and aiming.** The agent first made a camera that swings behind the player. On the phone the player spun in circles, so I replaced it with a camera I turn myself: **mouse on PC, swipe on the phone**. Touches on the joystick and buttons are ignored. The player now **shoots where the camera looks**, not where the character faces. I also asked the agent to restyle the HUD, did not like it, and **reverted it to my original HUD**.
2026-11-11 **APK build failed.** The first Android build hung on the JDK/SDK check and then failed because Gradle could not download its plugins (`Read timed out`). The project and the code were fine. I closed the stuck `java.exe` process and rebuilt the APK once the network was back.

### Tuning log (after a playtest with a new player)

| Change (old → new) | Why | Tries (before → after) |
|---|---|---|
| Patroller (Spider-Bot) chase speed 3 → 4 m/s | Player runs 6 m/s; Patroller never caught anyone | _ → _ |
| Shooter (Jet-Mech) shot speed → 7 m/s | Lecture value was tuned for a 4 m/s player; at 6 m/s the shot was too easy to outrun | _ → _ |
| Shooter aim lock 0 → 0.2 s | The shot always followed the player, dodging at the last moment was impossible | _ → _ |
| Charger (Rhino-Tank) wind-up aim lock 0 → 0.15 s | Same as the Shooter: the dash turned with the player until the last frame | _ → _ |
| Boss level timer 999 → 300 s | With the time bonus a 999 s timer gave a huge score | _ → _ |

## Week 11 · Data and Saves

2026-11-11 Asked Claude Code to move every balance number into ScriptableObjects. It copied the numbers from my current scenes (player 6 m/s, jump 7, enemy stats from Level_Enemies), so the game plays the same as before. Assets: `Assets/Data` + `Assets/Resources/GameConfig.asset`.
2026-11-12 The old editor tools (MidtermSetup, RK2Maps, Week10Setup, Week10Levels) wrote numbers straight into scene fields that no longer exist, so the project would not compile. They are switched off with `#if LEGACY_TOOLS`. The agent was not allowed to delete files, so I can delete them myself.
2026-11-12 Save: JsonUtility + try/catch + defaults, never BinaryFormatter. JsonUtility cannot write a Dictionary, so unlocked levels are a `List<int>` and best scores a `List<LevelRecord>`.
2026-11-14 Broken save.json: a copy is kept as `save.broken.json`, the game starts with defaults and writes a clean file. The file is written to `save.tmp` first and then moved, so closing the app in the middle of a write does not leave half a file.
2026-11-14 The volume slider would have written the file on every frame of a drag. Changed to `RequestSave()`: `SaveHooks` writes it 0.5 s after the last change. Level end and Reset still save at once.
2026-11-15 The enemies of Level_Enemies became prefabs plus spawn points in `Level_02.asset`, and `LevelRunner` spawns them. Patrol waypoints are kept as positions because a prefab cannot point at scene objects.
2026-11-17 The boss level had a 999 s timer. With a time bonus that would give a huge score, so `Level_04` has 300 s.
2026-11-17 Name, music and fullscreen moved from PlayerPrefs to save.json. A name saved in an older build is lost once.
2026-11-17 Boss fireballs flew through the pillars and hit me. The Week 10 `ShotStopper` was only added to the Shooter's shots, not the Boss's. Moved the wall check into `Fireball.cs` itself: it is destroyed on any solid object (not the player, the shooter, hazards or the floor below it), and it looks ahead every physics step so a fast ball cannot skip a thin wall.

## Week 12 · Sound Pass

2026-11-24 Asked Claude Code for the sound layer: one `AudioManager` with DontDestroyOnLoad, an AudioMixer (Master > Music > Soundtrack, Master > SFX) and a `SoundLibrary` asset with every clip and volume. Every level used to have its own "Music" AudioSource, so the music started again on every level load and death. The setup tool removed them; now the same track keeps playing and menu <-> level is a 1 s crossfade.
2026-11-24 `AudioSource.PlayClipAtPoint` (enemy hit, boss hit, bomb, shot, wind-up) makes a temporary source with **no mixer group**, so these sounds would ignore the SFX slider. All of them go through `AudioManager.PlayAt` / `PlayClipAt` now, which use pooled sources in the SFX group.
2026-11-24 My pause menu set `AudioListener.pause = true`, which stops the music too, so a Paused snapshot would never be heard. The music and UI-click sources have `ignoreListenerPause = true`: the game sounds stop, the music keeps playing quieter and muffled (Paused snapshot: Soundtrack -14 dB, lowpass 700 Hz). The mixer runs on unscaled time, otherwise the snapshot does not move while `timeScale = 0`.
2026-11-24 The pause snapshot and the Music slider cannot use the same volume: a parameter the slider sets with `SetFloat` is no longer controlled by snapshots. So the slider is on the Music group and the snapshot is on its child group Soundtrack.
2026-11-24 Old save files (Week 11) have no `sfxVolume`, and JsonUtility reads a missing number as 0, which would mute every sound. Added `audioVersion` to the settings: a file with version 0 gets the default SFX volume (0.8) once.
2026-11-24 Volume is `Log10(slider) * 20` dB, 0 = -80 dB (silent, Log10(0) is minus infinity). Repeated sounds get pitch 0.92-1.08 and 2 clips where I had them (jump, hit, coin, wind-up, enemy hit); win and lose jingles keep pitch 1.
2026-11-24 The boss fan attack had no warning at all. Added `fanWarnTime` (0.6 s) in BossStats: the boss growls first, the fireballs come 0.6 s later. The rock rain plays the wind-up sound together with the red mark.
2026-11-24 With "Enter Play Mode Options" on, Unity does not call `sceneLoaded` for the first scene, so the menu started without music in the editor. `AudioManager.Start` now sets up the first scene too.
