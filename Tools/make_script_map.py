# Draws ScriptMap_Week12.png in the same simple style as ScriptMap_Week11.png.
#   py Tools/make_script_map.py
import math
import os
from PIL import Image, ImageDraw, ImageFont

W, H = 1200, 780
img = Image.new("RGB", (W, H), "white")
d = ImageDraw.Draw(img)
F = r"C:\Windows\Fonts"
title = ImageFont.truetype(os.path.join(F, "arialbd.ttf"), 24)
font = ImageFont.truetype(os.path.join(F, "arial.ttf"), 16)
small = ImageFont.truetype(os.path.join(F, "arial.ttf"), 13)

YELLOW, BLUE, GREEN, RED, PURPLE, WHITE = (255, 242, 204), (218, 232, 252), (213, 232, 212), (248, 206, 204), (225, 213, 231), (255, 255, 255)
LINE, GREY = (0, 0, 0), (70, 70, 70)


def box(x, y, w, h, fill, text):
    d.rectangle((x, y, x + w, y + h), fill=fill, outline=LINE, width=1)
    d.multiline_text((x + w / 2, y + h / 2), text, font=font, fill=LINE, anchor="mm", align="center")
    return x, y, w, h


def top(b): return (b[0] + b[2] / 2, b[1])
def bottom(b): return (b[0] + b[2] / 2, b[1] + b[3])
def left(b): return (b[0], b[1] + b[3] / 2)
def right(b): return (b[0] + b[2], b[1] + b[3] / 2)


def arrow(p1, p2, text="", dx=0, dy=-10):
    d.line((p1, p2), fill=LINE, width=1)
    a = math.atan2(p2[1] - p1[1], p2[0] - p1[0])
    d.polygon([p2, (p2[0] - 12 * math.cos(a - 0.35), p2[1] - 12 * math.sin(a - 0.35)),
               (p2[0] - 12 * math.cos(a + 0.35), p2[1] - 12 * math.sin(a + 0.35))], fill=LINE)
    if text:
        label((p1[0] + p2[0]) / 2 + dx, (p1[1] + p2[1]) / 2 + dy, text)


def label(x, y, text):
    l, t, r, b = d.textbbox((x, y), text, font=small, anchor="mm")
    d.rectangle((l - 4, t - 2, r + 4, b + 2), fill="white")
    d.text((x, y), text, font=small, fill=GREY, anchor="mm")


d.text((20, 20), "Script Map - Jump High (Week 12: Sound)", font=title, fill=LINE)

# game scripts: they only say WHAT happened
names = ["Movement", "Health", "Shooter", "EnemyBase", "Boss", "LevelRunner", "ClickSound"]
sounds = ["jump, coin, lose", "hurt, heal, lose", "shoot", "wind-up, hit, death", "wind-up, death", "win", "button click"]
game = []
x = 45
for n, s in zip(names, sounds):
    b = box(x, 80, 145, 45, YELLOW, n)
    d.text((x + 72, 140), s, font=small, fill=GREY, anchor="mm")
    game.append(b)
    x += 160

am = box(470, 275, 260, 70, (255, 229, 204), "AudioManager\n(one for the whole game)")
for b in game:
    arrow(bottom(b)[0:1] + (bottom(b)[1] + 25,), top(am))
label(600, 215, "Play(Sfx.Jump)")

# left: sounds data
lib = box(40, 280, 200, 60, BLUE, "SoundLibrary")
arrow(left(am), right(lib), "reads clips")
box(40, 360, 95, 40, BLUE, "clips")
box(145, 360, 95, 40, BLUE, "volume, pitch")
box(40, 410, 200, 40, BLUE, "menu + level music")

# right: mixer
mix = box(960, 280, 200, 60, PURPLE, "AudioMixer")
arrow(right(am), left(mix), "volume in dB")
box(960, 360, 95, 40, PURPLE, "Music")
box(1065, 360, 95, 40, PURPLE, "SFX")
box(960, 410, 200, 40, PURPLE, "Soundtrack (in Music)")
box(960, 460, 95, 40, PURPLE, "Normal")
box(1065, 460, 95, 40, PURPLE, "Paused")
d.text((1060, 520), "snapshots", font=small, fill=GREY, anchor="mm")

# bottom: pause and settings
pause = box(330, 560, 160, 50, RED, "PauseMenu")
arrow(top(pause), (540, 340), "SetPaused()", -55, 0)
slider = box(560, 560, 160, 50, GREEN, "VolumeSlider")
save = box(790, 560, 160, 50, GREEN, "SaveSystem")
arrow(right(slider), left(save))
arrow(top(save), (680, 340), "ApplyVolumes()", 60, 0)
js = box(790, 670, 160, 45, GREEN, "save.json")
arrow(bottom(save), top(js))
d.text((640, 636), "Music / SFX slider", font=small, fill=GREY, anchor="mm")

# notes, like Week 11
notes = ["music: crossfade 1 s, no restart on a new level",
         "pause: game sounds stop, music quieter + lowpass",
         "slider 0..1 -> Log10(value) x 20 = dB",
         "same sound twice: random pitch 0.92-1.08"]
for i, n in enumerate(notes):
    d.text((40, 655 + i * 22), n, font=small, fill=GREY)

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "ScriptMap_Week12.png")
img.save(out)
print("wrote", os.path.normpath(out))
