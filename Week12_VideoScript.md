# Video script: Assignment 12 (≈ 2 min)

Game sound on, your voice, English, MP4 file.
Ойын дыбысы қосулы, өз дауысыңыз, ағылшынша, MP4 файл.

---

**1. Intro** ▶ Main Menu
**EN:** Hi, this is my game Jump High. This week I added sound: a mixer, 12 sound events, 2 music tracks and saved volume.
**KZ:** Сәлем, бұл менің Jump High ойыным. Осы аптада дыбыс қостым: микшер, 12 дыбыс, 2 музыка және сақталатын дыбыс деңгейі.

**2. Mixer** ▶ Audio Mixer терезесі, Normal → Paused басу
**EN:** My mixer has Master, Music and SFX. Every sound goes through it. The Paused snapshot makes the music quieter and muffled.
**KZ:** Микшерімде Master, Music және SFX бар. Әр дыбыс солар арқылы өтеді. Paused снапшоты музыканы ақырын әрі күңгірт қылады.

**3. Sounds** ▶ батырма, секіру, монета, ату, монстр құлайды, монстр ұрады, шығу есігі
**EN:** Click, jump, coin, shoot, hit, the monster dies. A monster hits me. I reach the exit: win. Repeated sounds change pitch a little.
**KZ:** Басу, секіру, монета, ату, соққы, монстр өледі. Монстр мені ұрады. Шығуға жеттім: жеңіс. Қайталанатын дыбыстардың биіктігі сәл өзгереді.

**4. Ears test** ▶ камераны бұрып, Brute-тың дыбысын естіп, қашу
**EN:** I can't see the monster, but I hear its wind-up sound, so I jump away before it hits me.
**KZ:** Монстрды көрмеймін, бірақ wind-up дыбысын естимін, сондықтан ол тимей тұрып қашып үлгеремін.

**5. Crossfade + pause** ▶ Menu → Play, сосын II → Resume
**EN:** Menu and level music change smoothly. Pause: the music gets quiet and muffled.
**KZ:** Мәзір мен деңгей музыкасы жұмсақ ауысады. Пауза: музыка ақырын әрі күңгірт болады.

**6. Settings test (phone)** ▶ слайдерлер → қолданбаны толық жабу → қайта ашу
**EN:** I change the volume, close the app and open it again. The volume is the same.
**KZ:** Дыбыс деңгейін өзгертемін, қолданбаны жабамын, қайта ашамын. Дыбыс деңгейі сол күйі.

**7. Script map** ▶ `ScriptMap_Week12.png` суретін ашып, айтқан жеріңізді тышқанмен көрсетіп отырыңыз
**EN:** This is my script map. At the top are my game scripts: Movement, Health, Shooter, the monsters, LevelRunner and the buttons. They only tell the AudioManager what happened, for example Play Jump.
**KZ:** Бұл менің скрипт картам. Жоғарыда ойын скрипттері: Movement, Health, Shooter, монстрлар, LevelRunner және батырмалар. Олар AudioManager-ге тек не болғанын айтады, мысалы Play Jump.

**EN:** The AudioManager is in the middle. There is only one for the whole game, so the music does not restart on a new level. It takes the sound from the SoundLibrary and plays it through the mixer.
**KZ:** Ортада AudioManager. Ол бүкіл ойынға біреу ғана, сондықтан жаңа деңгейде музыка басынан басталмайды. Ол дыбысты SoundLibrary-ден алып, микшер арқылы ойнатады.

**EN:** At the bottom: the volume slider saves the value in my save file, and the AudioManager sends it to the mixer in decibels. The pause button calls SetPaused, which turns on the Paused snapshot.
**KZ:** Төменде: дыбыс слайдері мәнді сақтау файлына жазады, ал AudioManager оны микшерге децибелмен жібереді. Пауза батырмасы SetPaused-ты шақырады, ол Paused снапшотын қосады.

**8. End** ▶ README → дыбыс лицензиялары
**EN:** Sound sources are in the README. Thank you!
**KZ:** Дыбыстардың дереккөзі README-де. Рақмет!
