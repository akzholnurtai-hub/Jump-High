# Assignment 12 · Sound Pass (Jump High)

## Не жасалды

| Талап | Қалай / қайда |
|---|---|
| **AudioMixer**: Master, Music, SFX | `Assets/Audio/MainMixer.mixer`: Master ▸ Music ▸ Soundtrack, Master ▸ SFX |
| Әр AudioSource өз тобына | музыка → Soundtrack (Music ішінде), қалғаны → SFX. Сахналар мен prefab-тар `Week12` құралымен бапталды |
| **8+ дыбыс оқиғасы** (12) | jump, hurt (hit), pickup, heal, shoot, **enemy wind-up**, boss wind-up, enemy hit, death, UI click, win, lose |
| Шабуыл алдында естіледі | `EnemyBase.PlayWarn()` wind-up **басында**: Charger (WindUp), Shooter (Aim), Patroller (Attack). Босс: 0.6 с бұрын ырылдайды, тас түсер жерде қызыл белгімен бірге дыбыс |
| **2 музыка** + crossfade | мәзір: `menu_theme.wav`, деңгей: tatamusic. 1 с crossfade (sin/cos). Деңгей қайта жүктелсе музыка **басынан басталмайды** |
| **Music / SFX слайдерлері** dB-мен | `VolumeSlider.cs`, `Log10(v) * 20`, жазуда "Music  -6 dB" көрінеді. `save.json`-ға сақталады (өткен аптадағы SaveSystem) |
| **Paused snapshot** | `PauseMenu` → `AudioManager.SetPaused(true)`: Soundtrack −14 dB + lowpass 700 Hz. Ойын дыбыстары тоқтайды, музыка күңгірт ойнай береді |
| **Вариация** | pitch 0.92–1.08 + 2 клип (jump, hurt, coin, wind-up, enemy hit); бір клип қатарынан екі рет шықпайды |
| Дереккөз + лицензия | `README.md` → "Sound sources and licenses" |
| DECISIONS.md | "Week 12 · Sound Pass" бөлімі |

Жаңа скрипттер: `Assets/Script/Audio/` → `AudioManager`, `SoundLibrary`, `VolumeSlider`, `ClickSound`.
Барлық клиптер мен дыбыс деңгейлері: `Assets/Resources/SoundLibrary.asset` (кодта клип жоқ).
Жаңа дыбыстарды жасаған скрипт: `Tools/make_sounds.py` (өз жұмысым, CC0).

## Іске қосу

`Week12 → 1. Setup Sound Pass` **бір рет іске қосылып қойған** (сахналар сақталған). Қайта іске қоссаңыз да зиян жоқ.
Тексеру: **Week12 → 2. Check** — бәрі `OK` болуы керек.

1. Unity-де жобаны ашыңыз, Console-да қызыл қате болмауы керек.
2. `MainMenu` → Play. **Settings** ашыңыз: Music және SFX слайдерлері.
   > ⚠️ Сіздің save.json-да `musicVolume = 0` тұр, сондықтан бастапқыда музыка **естілмейді** — Music слайдерін көтеріңіз.
3. APK: бұрынғыдай **Week10 → Build Android APK**.

## Canvas-қа скриншот: AudioMixer

**Week12 → Open Audio Mixer** (немесе Window → Audio → Audio Mixer, `Assets/Audio/MainMixer`).
Сол жақта Snapshots (Normal, Paused) және Groups (Master, Music, Soundtrack, SFX) көрінетіндей түсіріңіз.
Paused snapshot-ты таңдасаңыз, Soundtrack −14 dB болады — соны да көрсетуге болады.

## Видео (2–3 мин, ағылшынша) — `Week12_VideoScript.md`

## Тапсырмас бұрын

- [ ] Өзгерістерді **commit** жасаңыз (осы аптаның коммиттері керек) және push.
- [ ] DECISIONS.md-ге өзіңіз байқағанды қосыңыз (телефонда тексергенде не дұрыс емес болды, не өзгерттіңіз).
- [ ] APK, AudioMixer скриншоты, видео (MP4 файл).
