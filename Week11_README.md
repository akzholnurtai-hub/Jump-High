# Assignment 11 · Data and Saves (Jump High)

## Не жасалды

| Не | Қайда |
|---|---|
| **Баланс сандары → дата ассеттер** (ScriptableObject) | `Assets/Data/` |
| Ойыншы: жылдамдық, секіру, HP, оқ, cooldown | `Data/PlayerStats.asset` |
| Монета, жүрек, тұзақ, лава, бомба, fireball | `Data/ItemValues.asset` |
| Босс: HP, үзіліс, веер, тас жаңбыры | `Data/BossStats.asset` |
| Жаулар: Spider-Bot / Jet-Mech / Rhino-Tank | `Data/Enemies/*.asset` |
| **4 деңгей = 4 LevelData** (уақыт, мақсат, ұпай, жаулар, құлайтын заттар) | `Data/Levels/Level_01..04.asset` |
| Бәрін байланыстыратын бір ассет | `Assets/Resources/GameConfig.asset` |
| Бір скрипт барлық деңгейді жүктейді | `Script/Level/LevelRunner.cs` |
| Level Select (құлыпталған / ашық, best score) | `Script/Level/LevelSelect.cs` |
| **JSON сақтау** (`persistentDataPath/save.json`) | `Script/Save/SaveSystem.cs`, `SaveData.cs`, `SaveHooks.cs` |
| Reset progress + растау терезесі | `MenuButtons.AskReset / ConfirmReset` |
| Сахнаны баптайтын құрал | `Assets/Editor/Week11Setup.cs` |

Ескі `MidtermSetup`, `RK2Maps`, `Week10Setup`, `Week10Levels`, `SpawnerRamp` `#if LEGACY_TOOLS` арқылы өшірілді (сандарды тікелей сахнаға жазатын). Қаласаңыз өзіңіз өшіріп тастаңыз.

## Іске қосу (бір рет)

1. Unity-ге оралыңыз → скрипттер компиляцияланады. Console-да **қызыл қате болмауы керек**.
2. Мәзір: **Week11 → 1. Setup Data + Saves**. Ол:
   - `Level_Enemies` ішіндегі 4 жауды prefab қылады (`Assets/Prefabs/Enemies`) және олардың орнын `Level_02.asset`-ке жазады, сахнадан өшіреді → енді жауларды `LevelRunner` деректен шығарады;
   - әр деңгейге `LevelRunner` объектісін қосады;
   - MainMenu-ге **Level Select** панелін, **Reset progress** батырмасын және **растау терезесін** қосады.
3. `MainMenu` ашып **Play**: PLAY → деңгей таңдау → 1-деңгей ғана ашық. Деңгейді өтсеңіз келесісі ашылады.
4. APK: **Week10 → Build Android APK** (бұрынғыдай).

> `Level_Enemies` сахнасында жаулар енді көрінбейді — `LevelRunner` объектісін таңдасаңыз, олардың орны қызыл шеңбермен (Gizmos) көрінеді.

## save.json қайда

- Windows (Editor): `C:\Users\<сіз>\AppData\LocalLow\Akzhol\Jump High\save.json` — немесе **Week11 → Save file → Open folder**
- Android: `/Android/data/com.jumphigh.game/files/save.json`

Мысал:
```json
{
    "version": 1,
    "unlocked": [1, 2],
    "records": [ { "level": 1, "bestScore": 1250, "bestTime": 41.3, "clears": 2 } ],
    "bestScore": 1250,
    "lastLevel": 2,
    "totalCoins": 17,
    "deaths": 3,
    "settings": { "musicVolume": 0.5, "musicOn": true, "fullscreen": true, "playerName": "Akzhol", "playerColor": 0 }
}
```

## Қашан сақталады

| Сәт | Код |
|---|---|
| Деңгей өтілді | `LevelRunner.Complete → SaveSystem.CompleteLevel → Save()` |
| Баптау өзгерді (дыбыс, музыка, аты, түсі, fullscreen) | `SaveSystem.SettingsChanged → RequestSave()` → 0.5 с кейін `SaveHooks` жазады |
| Қолданба фонға кетті | `SaveHooks.OnApplicationPause(true) → Save()` |
| Шығу / Reset / Pause → Menu | `Save()` |

## Видео (2–3 мин, ағылшынша, өз дауысыңыз)

1. **Live balance** — Play басыңыз, `Data/PlayerStats.asset` → `moveSpeed` 6 → 12. Ойыншы бірден жылдам жүгіреді, код өзгермейді. Немесе `Enemies/SpiderBot` → `chaseSpeed`, `Levels/Level_01` → `timeLimit`.
   *(Play кезінде ассетке енгізген өзгеріс сақталып қалады — видеодан кейін қайтарыңыз.)*
2. **Phone test** — 1-деңгейді өтіңіз (2-деңгей ашылады), Settings-те дыбысты өзгертіңіз → қолданбаны **толық жабыңыз** (recent apps-тан сырып тастаңыз) → қайта ашыңыз → деңгей ашық, best score, дыбыс орнында.
3. **Broken file test** — Editor-да: **Week11 → Save file → Break it** (немесе save.json-ды Блокнотпен ашып ортасын өшіріңіз) → Play → ойын құламайды, Console-да `[Save] save.json is broken ... Starting with defaults.` жазуы шығады, бұзылған файлдың көшірмесі `save.broken.json`.
4. **Script map** — төмендегі сызбаны түсіндіріңіз.

## Script map

```
GameConfig (Resources) ──┬── PlayerStats ──► Movement, Health, Shooter, Bullet
                         ├── ItemValues  ──► Hazard/Rock/Heart/Bomb/Fireball, Lava, Spinner, coin score
                         ├── BossStats   ──► Boss
                         └── levels[] ──► LevelData ──► LevelRunner (one per level scene)
                                              │            ├─ spawns EnemySpawn → EnemyStats.prefab → EnemyBase.Init(stats, route)
                                              │            ├─ Movement reads timeLimit / coinsRequired, Health reads killHeight
                                              │            ├─ Spawner reads hazards, Patrol reads moverSpeedScale
                                              │            └─ Complete(): score → SaveSystem.CompleteLevel → next LevelData
EnemyStats (Patroller/Shooter/Charger) ──► PatrollerAI / ShooterAI / ChargerAI, EnemyHealth

SaveSystem (static) ── save.json (JsonUtility) ◄── SaveHooks (OnApplicationPause / Quit)
      ▲        ▲
LevelSelect   MenuButtons / ProfileSettings (settings, Reset + confirm)
```

Неге осылай: сандар бір жерде (дизайнер кодқа тимейді), бір `LevelRunner` кез келген `LevelData`-ны жүктейді, сақтау бір статикалық класта — оны кез келген скрипт шақырады, ал `SaveHooks` фонға кеткенде жазады.

## Canvas-қа тексеру

- [ ] Скрипттерде баланс сандары жоқ (тек кішкентай техникалық `const`: жер тексеру радиусы, gizmo өлшемі, т.б.)
- [ ] 3+ деңгей LevelData-дан жүктеледі (бізде 4)
- [ ] Прогресс JSON файлында, `persistentDataPath`
- [ ] Файл жоқ / бұзылған → defaults, құламайды
- [ ] Деңгейден кейін, баптау өзгергенде, pause-та сақталады
- [ ] Reset progress + растау терезесі
- [ ] `DECISIONS.md` — өз жолдарыңызды қосыңыз (телефон тесті, live balance)
- [ ] Inspector-дағы бір дата ассеттің скриншоты + save.json мәтіні
- [ ] APK + видео (MP4 файл)
- [ ] Осы аптаның коммиттері GitHub-та

## Қорғауға дайындалу

- Неге ScriptableObject, неге MonoBehaviour-дағы `public float` емес?
- `GameConfig.Get()` ассетті қалай табады? (`Resources.Load`)
- `LevelRunner` неге `[DefaultExecutionOrder(-100)]`? (Movement/Spawner деңгейді Awake-тен кейін оқиды)
- `EnemyHealth` HP-ны неге `Start`-та алады, `Awake`-та емес? (`Init` арасында шақырылады)
- Неге `JsonUtility`, неге `BinaryFormatter` емес? Неге `Dictionary` жоқ?
- Бұзылған файлда не болады? (`try/catch` → `save.broken.json` → defaults → `Sanitize`)
- Неге `OnApplicationPause(true)`-да сақтаймыз?
- Неге алдымен `save.tmp`-ға жазамыз?
