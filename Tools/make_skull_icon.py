# Draws the HUD monster icon (a skull) for Jump High. Own work, CC0.
#   py Tools/make_skull_icon.py   ->  Assets/Textures/SkullIcon.png (256 x 256, transparent)
import os
from PIL import Image, ImageDraw

S = 256
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
bone, dark, edge = (236, 230, 214, 255), (40, 12, 12, 255), (120, 20, 20, 255)

d.ellipse((38, 22, 218, 182), fill=bone, outline=edge, width=8)          # head
d.rounded_rectangle((78, 140, 178, 222), radius=18, fill=bone, outline=edge, width=8)   # jaw
d.ellipse((70, 82, 120, 136), fill=dark)                                  # eyes
d.ellipse((136, 82, 186, 136), fill=dark)
d.ellipse((84, 98, 102, 116), fill=(255, 60, 30, 255))                     # glowing pupils
d.ellipse((154, 98, 172, 116), fill=(255, 60, 30, 255))
d.polygon([(128, 140), (114, 164), (142, 164)], fill=dark)                 # nose
for x in (103, 128, 153):                                                  # teeth gaps
    d.line((x, 180, x, 216), fill=edge, width=6)

out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Textures", "SkullIcon.png")
img.save(out)
print("wrote", os.path.normpath(out))
