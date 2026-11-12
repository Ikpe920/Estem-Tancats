# -*- coding: utf-8 -*-
# =====================================================================
#  LES AMPOLLES QUE EXPLOTEN (la Barra) — Resources/Hazards, 64x64
# =====================================================================
# Amb el traç de la resta del joc (contorn negre, colors plans, llum blanca):
#   Ampolla_0..3  l'ampolla de licor de contraban que bull (4 fotogrames: el tap salta,
#                 les bombolles pugen i l'etiqueta de la calavera tremola)
#   Vidre         una estella de vidre verd (surten disparades quan peta)
#   python3 Tools/SpriteGen/bottles.py            -> escriu els PNG
#   python3 Tools/SpriteGen/bottles.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, WHITE

GLASS = (60, 150, 80)
GLASS_S = (35, 100, 55)
GLASS_HI = (140, 210, 150)
LIQ = (170, 220, 90)


def bottle(f):
    hop = (0, 1, 3, 1)[f]          # el tap salta una mica
    lean = (0, 1, 0, -1)[f]        # l'ampolla es gronxa
    b = Part(GLASS, GLASS_S, hi=GLASS_HI)
    b.ellipse(32 + lean, 46, 13, 14)
    b.rect(19 + lean, 34, 45 + lean, 50)
    b.ellipse(32 + lean, 34, 13, 6)
    b.rect(28 + lean * 0.6, 14, 36 + lean * 0.6, 32)
    liquid = Part(LIQ, None, outline=False)
    liquid.ellipse(32 + lean, 48, 10, 10)
    liquid.rect(22 + lean, 40, 42 + lean, 48)
    liquid.remove(lambda x, y: y < 40 + (f % 2))
    label = Part((245, 235, 205), (210, 195, 160))
    label.rect(22 + lean, 38, 42 + lean, 50)
    skull = Part((40, 30, 28), None, outline=False)
    sx = 32 + lean
    skull.ellipse(sx, 42, 4, 3.4)
    skull.rect(sx - 2, 45, sx + 2, 46)
    eyes = Part((245, 235, 205), None, outline=False).put(sx - 2, 42).put(sx + 1, 42)
    xxx = Part((180, 40, 40), None, outline=False)
    for k in range(3):
        x = sx - 6 + k * 5
        xxx.line(x - 1, 48, x + 1, 50).line(x + 1, 48, x - 1, 50)
    cork = Part((190, 140, 90), (150, 100, 60))
    cork.rect(27 + lean * 0.6, 8 - hop, 37 + lean * 0.6, 14 - hop)
    ps = [b, liquid, label, skull, eyes, xxx, cork]
    # les bombolles que surten pel coll
    bub = Part((230, 250, 210), None)
    for k, (dx, y, r) in enumerate(((-9, 12, 1.8), (9, 9, 1.5), (-12, 5, 1.4), (12, 3, 1.8))):
        if (k + f) % 4 == 3:
            continue
        bub.ellipse(32 + dx + lean, y + 2 - f, r, r)
    ps.append(bub)
    # la llum de l'ampolla
    shine = Part(WHITE, None, outline=False)
    for y in range(36, 52, 2):
        shine.put(23 + lean, y)
    shine.rect(30 + lean * 0.6, 16, 30 + lean * 0.6, 26)
    ps.append(shine)
    return ps


def glass():
    s = Part(GLASS_HI, GLASS, hi=WHITE)
    for y in range(18, 46):
        t = (y - 18) / 28
        w = 10 * math.sin(t * math.pi) + 2
        s.rect(32 - w * 0.6 + t * 6, y, 32 + w * 0.4 + t * 6, y)
    return [s]


SPRITES = {
    'Ampolla_0': lambda: bottle(0),
    'Ampolla_1': lambda: bottle(1),
    'Ampolla_2': lambda: bottle(2),
    'Ampolla_3': lambda: bottle(3),
    'Vidre': glass,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Hazards'))
    os.makedirs(out, exist_ok=True)
    rows = []
    for name, fn in SPRITES.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(out, name + '.png'))
        rows.append((name, [im]))
    print('%d sprites' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=4, cols=5)


if __name__ == '__main__':
    main()
