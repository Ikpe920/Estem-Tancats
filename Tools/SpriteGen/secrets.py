# -*- coding: utf-8 -*-
# =====================================================================
#  ELS SECRETS: el Fantasma del Restaurant (cap secret) i les receptes perdudes
# =====================================================================
#   Bosses/Fantasma.png, Walk/Fantasma_1.png   el fantasma amb barret de xef i el cullerot
#   BossShots/PlatFantasma.png                  el plat fantasmagòric que llença
#   DishFx/Recepta.png                          el pergamí d'una recepta perduda
#   python3 Tools/SpriteGen/secrets.py            -> escriu els PNG
#   python3 Tools/SpriteGen/secrets.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, WHITE

SHEET = (236, 240, 252)
SHEET_S = (180, 190, 220)
SHEET_HI = WHITE


def ghost():
    body = Part(SHEET, SHEET_S, hi=SHEET_HI)
    body.ellipse(32, 30, 17, 18)
    body.rect(15, 30, 49, 50)
    # la vora de baix, ondulada
    for k in range(5):
        body.ellipse(18 + k * 7, 51, 3.8, 4)
    body.remove(lambda x, y: y > 50 and ((x - 15) % 7) in (6,) and y > 52)
    hat = Part((255, 255, 255), (205, 205, 215))
    hat.ellipse(32, 9, 11, 6).ellipse(24, 11, 5, 4).ellipse(40, 11, 5, 4)
    hat.rect(23, 11, 41, 17)
    band = Part((200, 205, 220), None, outline=False).rect(23, 15, 41, 16)
    eyes = Part((30, 20, 40), None)
    eyes.ellipse(26, 29, 3.4, 4.5).ellipse(38, 29, 3.4, 4.5)
    glint = Part((140, 230, 200), None, outline=False).put(27, 28).put(39, 28)
    mouth = Part((30, 20, 40), None)
    mouth.ellipse(32, 39, 5, 3.6)
    tongue = Part((150, 220, 190), None, outline=False).ellipse(32, 41, 2.6, 1.2)
    arm = Part(SHEET, SHEET_S).tube([(47, 34), (54, 30), (57, 22)], 2.6)
    ladle = Part((150, 160, 175), None, hi=WHITE)
    ladle.line(57, 22, 59, 8)
    ladle.ellipse(59, 7, 4, 3)
    ectoplasm = Part((150, 230, 200), None, outline=False)
    for (x, y) in ((12, 44), (52, 46), (10, 36)):
        ectoplasm.ellipse(x, y, 1.6, 1.6)
    return [arm, body, hat, band, eyes, glint, mouth, tongue, ladle, ectoplasm]


def ghost_plate():
    p = Part((210, 245, 230), (150, 210, 190), hi=WHITE)
    p.ellipse(32, 32, 22, 22)
    rim = Part((170, 230, 210), None, outline=False)
    rim.ellipse(32, 32, 15, 15)
    rim.remove(lambda x, y: ((x + 0.5 - 32) / 13) ** 2 + ((y + 0.5 - 32) / 13) ** 2 < 1)
    face = Part((40, 80, 70), None, outline=False)
    face.ellipse(27, 30, 2, 2.6).ellipse(37, 30, 2, 2.6)
    face.ellipse(32, 38, 3, 2)
    return [p, rim, face]


def recipe():
    paper = Part((245, 230, 190), (215, 190, 140), hi=(255, 248, 225))
    paper.rect(14, 12, 50, 52)
    roll = Part((230, 205, 155), (190, 160, 110))
    roll.ellipse(32, 12, 20, 4).ellipse(32, 52, 20, 4)
    lines = Part((150, 110, 70), None, outline=False)
    for y in (20, 25, 30, 35, 40):
        lines.rect(19, y, 19 + (y * 13) % 22 + 6, y)
    seal = Part((190, 40, 40), (140, 25, 25), hi=(240, 110, 100)).ellipse(42, 44, 5, 5)
    q = Part((255, 214, 72), None)
    for (x, y) in ((8, 6), (56, 8)):
        q.put(x, y).put(x - 1, y).put(x + 1, y).put(x, y - 1).put(x, y + 1)
    return [paper, roll, lines, seal, q]


SPRITES = {
    'Bosses/Fantasma': ghost,
    'Walk/Fantasma_1': ghost,
    'BossShots/PlatFantasma': ghost_plate,
    'DishFx/Recepta': recipe,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    res = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources'))
    rows = []
    for name, fn in SPRITES.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(res, name + '.png'))
        rows.append((name, [im]))
    print('%d sprites' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=4, cols=4)


if __name__ == '__main__':
    main()
