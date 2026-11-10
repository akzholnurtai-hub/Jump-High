# Assignment 10 · Three Enemy Types — Decision Log

**Game:** Jump High (Unity 6.6, 3D, Android)
**Agent used:** Claude Code

1. **Enemy damage and the "Enemy" tag.** I asked Claude Code for three enemy FSMs (Patroller, Shooter, Charger). My `Movement.cs` restarts the level when the player touches anything tagged `Enemy`, so the new enemies are **not** tagged `Enemy`. They deal damage through `Health.TakeDamage(1)` instead, so the hit uses my heart system and the invincibility frames.

2. **Shots flew through walls.** The Shooter re-uses my `Fireball` prefab. Its collider is a trigger, so the shot passed through cover and the player could not hide. I added `ShotStopper.cs`, which destroys the shot when it touches the `Walls` layer. Sight raycasts also use an `obstacleMask` with only `Walls` ticked, so a ray never hits the enemy's own collider.

3. **Numbers for my player speed.** The lecture numbers assume a player at 4 m/s, but my player runs at 6 m/s. With chase speed 3 m/s the Patroller was never a threat, so I set it to **4 m/s** and the Shooter's shot speed to **7 m/s**. To keep the attacks fair, the Shooter's aim and the Charger's wind-up stop turning **0.15–0.2 s before the attack**, so stepping aside at the last moment works.

4. **Camera and aiming.** The agent first made a camera that swings behind the player. On the phone the player spun in circles, so I replaced it with a camera I turn myself: **mouse on PC, swipe on the phone**. Touches on the joystick and buttons are ignored. The player now **shoots where the camera looks**, not where the character faces. I also asked the agent to restyle the HUD, did not like it, and **reverted it to my original HUD**.

5. **APK build failed.** The first Android build hung on the JDK/SDK check and then failed because Gradle could not download its plugins (`Read timed out`). The project and the code were fine. I closed the stuck `java.exe` process and rebuilt the APK once the network was back.

---

## Tuning log (fill in after a playtest with a new player)

| Change (old → new) | Why | Tries (before → after) |
|---|---|---|
| Patroller chase speed 3 → 4 m/s | Player runs 6 m/s; Patroller never caught anyone | _ → _ |
| | | |
| | | |
