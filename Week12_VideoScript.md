# Video script — Assignment 12 · Sound Pass (2–3 min)

Record with **game sound on** and your voice. English. A file (MP4), not a link.

## 1. Intro (~10 s)
"Hi, this is Jump High. This week I added a full sound layer: a mixer, twelve sound events, two music tracks and saved volume settings."
> Көрсету: Main Menu, мәзір музыкасы естіліп тұр.

## 2. The mixer (~20 s)
"This is my AudioMixer. Master has two groups: Music and SFX. Music has a child group, Soundtrack, which the Paused snapshot changes. The sliders control Music and SFX, the pause controls Soundtrack, so they never fight."
> Көрсету: Window → Audio → Audio Mixer, Normal / Paused snapshot-тарды ауыстырып көрсету.

## 3. The events (~40 s)
"Every action has a sound: button click... jump... coin... I get hit... heart... shooting... enemy hit... enemy death. Jump, hit and coin have two clips and a random pitch, so they don't sound the same every time."
> Көрсету: мәзірде батырма басу, Level 1-де секіру, монета, тұзаққа тию, ату. Level 2-де жауды өлтіру.

## 4. Ears test (~25 s)
"Now the ears test. The Rhino-Tank is behind me, I can't see it... I hear the wind-up... I step aside, and the dash misses. The boss growls before every fireball fan."
> Көрсету: камераны жаудан бұрып тұрып, wind-up дыбысын естіп, қашу. Сосын босс.

## 5. Crossfade and pause (~20 s)
"Menu music and level music crossfade in one second, no click. When I die, the level reloads but the music doesn't restart. Pause: the game sounds stop and the music gets quiet and muffled."
> Көрсету: Play → деңгей; өліп қайта бастау; II батырмасы → пауза → Resume.

## 6. Settings test on the phone (~25 s)
"Settings: the sliders show decibels: half the slider is minus 6 dB. I set SFX low and music high... now I close the app completely... and open it again. The volume is the same."
> Көрсету: телефонда Settings, слайдерлер, қолданбаны толық жабу (recent apps → swipe), қайта ашу.

## 7. Script map (~30 s)
"Scripts never hold sound clips. They only say what happened, like AudioManager.Play(Sfx.Jump). The SoundLibrary asset says how it sounds. AudioManager is one object that lives between scenes, so the music doesn't restart and a death sound isn't cut. The volume slider saves into my save file, and AudioManager sends it to the mixer in decibels: Log10 of the value times 20."
> Көрсету: `ScriptMap.md` → "4. Sound" бөлімі, `SoundLibrary.asset` Inspector-да.
