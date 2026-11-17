# Video script — Assignment 11 (2–3 min)

## 1. Intro (~10 s)
"Hi, this is my game Jump High. This week I moved all balance numbers into data assets, built levels from data, and added a JSON save."
> Көрсету: ойынның Main Menu экраны.

## 2. Data assets (~25 s)
"All numbers live in ScriptableObjects in the Data folder. GameConfig holds all of them: player stats, boss stats, enemy stats, item values and four LevelData assets. Scripts only read them with GameConfig.Get()."
> Көрсету: Project терезесінде `Assets/Data` папкасы, сосын `PlayerStats` және `Level_02` asset-ін Inspector-да ашу.

## 3. Live balance (~25 s)
"Now I change one number with no code. Spider-Bot chase speed is 4. I press Play... I set it to 8... and it chases me much faster."
> Көрсету: Play басу → `Data/Enemies/SpiderBot` → `chaseSpeed` 4 → 8 → жаудың жылдам қуғанын көрсету. Соңында қайта 4 қою.

## 4. Levels from data (~15 s)
"Each level is a LevelData asset: scene, enemies, time and next level. The same LevelRunner script loads any of them. Level select shows locked and unlocked levels from the save."
> Көрсету: Main Menu-дағы Level Select (жабық/ашық батырмалар), бір деңгейге кіру.

## 5. Phone restart test (~30 s)
"Now on the phone. I finish level 1, level 2 is unlocked. I change the music volume. Now I close the app completely... and open it again. Level 2 is still unlocked and the volume is the same."
> Көрсету: телефонның экран жазбасы. Деңгейді өту → дыбысты өзгерту → recent apps-тан қолданбаны толық жабу → қайта ашу → Level Select пен дыбыс сақталған.

## 6. Broken file test (~30 s)
"The save is a JSON file in persistentDataPath. Here it is. Now I break it by hand and save. I start the game... no crash, it starts with default values, and the broken file is kept as save.broken.json."
> Көрсету: Notepad-та `save.json` → ішін бұзу (`asdf` жазу) → сақтау → ойынды қосу → тек Level 1 ашық → папкада `save.broken.json` пайда болғаны.
> Ескерту: тестке дейін `save.json`-ның көшірмесін алып қойыңыз.

## 7. Script map (~30 s)
"This is my script map. MenuButtons and LevelSelect start a level through LevelRunner. GameConfig gives the numbers. LevelRunner runs the player, enemies and the boss. All damage goes to Health through TakeDamage, and Health updates the HUD. When a level ends, SaveSystem writes save.json. SaveHooks also saves when the app is paused, because a phone can kill the app in the background."
> Көрсету: `ScriptMap_Week11.png` суреті, сөйлеп тұрып тінтуірмен әр қорапты көрсету.

## 8. End (~10 s)
"I use JsonUtility, not BinaryFormatter. My decisions with the AI agent are in DECISIONS.md. Thank you."
> Көрсету: GitHub-тағы `DECISIONS.md` файлы.
