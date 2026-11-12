# -*- coding: utf-8 -*-
# =====================================================================
#  DECORACIÓ ANIMADA DE LES SALES i l'avís d'atac dels clients (Resources/Decor, 64x64)
# =====================================================================
# Una peça per sala, que el joc anima (RoomDecor.cs) amb partícules:
#   Gramola (Sala, notes musicals)   CartellBar (Barra, neó que parpelleja)   Olla (Cuina, vapor)
#   Testos (Terrassa, fulles)        Xemeneia (Teulada, fum)                  Barril (Celler, gotes de vi)
#   BlocGel (Cambra, boira i neu)    Focus (Plató, llum que escombra)
#   i les peces de les partícules: Nota, Fulla, Llum; i Atac (la forquilla i el ganivet dels clients)
#   Tomaquet i Esquitx: els tomàquets que llença el públic del Plató
#   python3 Tools/SpriteGen/decor.py            -> escriu els PNG
#   python3 Tools/SpriteGen/decor.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, WHITE, darker, lighter

WOOD = (150, 95, 55)
BRASS = (220, 180, 70)


def gramola():
    box = Part(WOOD, darker(WOOD), hi=lighter(WOOD, 0.3)).rect(14, 40, 50, 56)
    disc = Part((40, 36, 40), None).ellipse(32, 40, 15, 4)
    label = Part((200, 50, 50), None, outline=False).ellipse(32, 40, 4, 1.4)
    arm = Part(BRASS, None).line(44, 38, 40, 30)
    horn = Part(BRASS, darker(BRASS), hi=WHITE)
    for i in range(24):
        t = i / 23
        x = 40 - t * 14; y = 30 - t * 20
        horn.ellipse(x, y, 2 + t * 9, 2 + t * 9)
    inside = Part((120, 80, 30), None, outline=False).ellipse(26, 10, 7, 7)
    return [box, disc, label, arm, horn, inside]


def nota():
    n = Part((30, 20, 20), None)
    n.ellipse(24, 46, 7, 5.5).ellipse(46, 40, 7, 5.5)
    n.rect(29, 14, 31, 46).rect(51, 8, 53, 40)
    n.line(29, 14, 53, 8).line(29, 15, 53, 9).line(29, 16, 53, 10)
    return [n]


def cartell_bar():
    board = Part((40, 30, 36), None).rect(6, 18, 58, 46)
    neon = Part((255, 90, 140), None, outline=False)
    # B
    neon.rect(12, 24, 13, 40).line(13, 24, 19, 24).line(13, 32, 19, 32).line(13, 40, 19, 40).line(20, 25, 20, 31).line(20, 33, 20, 39)
    # A
    neon.line(24, 40, 29, 24).line(29, 24, 34, 40).line(26, 34, 32, 34)
    # R
    neon.rect(38, 24, 39, 40).line(39, 24, 45, 24).line(46, 25, 46, 31).line(39, 32, 45, 32).line(41, 33, 47, 40)
    glow = Part((255, 200, 220), None, outline=False)
    for (x, y) in ((12, 24), (29, 24), (38, 24), (19, 40), (46, 40)): glow.put(x, y)
    chains = Part((150, 150, 160), None, outline=False).line(14, 6, 14, 18).line(50, 6, 50, 18)
    return [chains, board, neon, glow]


def olla():
    stove = Part((60, 60, 66), (40, 40, 46)).rect(8, 46, 56, 60)
    fire = Part((250, 140, 40), None, outline=False)
    for x in (20, 32, 44): fire.ellipse(x, 46, 3, 2)
    pot = Part((170, 175, 185), (120, 125, 135), hi=WHITE)
    pot.rect(14, 22, 50, 44).ellipse(32, 44, 18, 4)
    lid = Part((150, 155, 165), (110, 115, 125)).ellipse(32, 22, 19, 5)
    knob = Part((60, 40, 30), None).ellipse(32, 17, 3, 2)
    handles = Part((60, 40, 30), None).rect(8, 26, 13, 29).rect(51, 26, 56, 29)
    return [stove, fire, handles, pot, lid, knob]


def testos():
    pot = Part((200, 100, 60), (160, 70, 40), hi=(230, 140, 100))
    pot.rect(18, 40, 46, 58).rect(15, 36, 49, 41)
    plant = Part((70, 150, 60), (40, 110, 40), hi=(130, 200, 110))
    for (x, y, rx, ry) in ((32, 24, 12, 12), (20, 30, 8, 7), (44, 30, 8, 7), (32, 12, 7, 7)):
        plant.ellipse(x, y, rx, ry)
    flowers = Part((240, 80, 100), None)
    for (x, y) in ((26, 18), (40, 22), (32, 28), (20, 28)): flowers.ellipse(x, y, 2, 2)
    return [plant, flowers, pot]


def fulla():
    l = Part((90, 170, 60), (50, 120, 40), hi=(160, 220, 120))
    for t in range(-12, 13):
        w = 7 * math.cos(t / 12 * math.pi / 2)
        l.rect(32 - w, 32 + t, 32 + w, 32 + t)
    vein = Part((50, 110, 40), None, outline=False).line(32, 20, 32, 44)
    return [l, vein]


def xemeneia():
    c = Part((170, 80, 64), (120, 55, 44), hi=(210, 120, 100))
    c.rect(18, 22, 46, 60)
    top = Part((90, 85, 90), (60, 56, 60)).rect(14, 16, 50, 23)
    mortar = Part((120, 55, 44), None, outline=False)
    for y in range(28, 60, 7):
        mortar.rect(19, y, 45, y)
        off = 0 if (y // 7) % 2 else 6
        for x in range(19 + off, 45, 12): mortar.rect(x, y, x, y + 6)
    hole = Part((30, 26, 30), None, outline=False).rect(20, 17, 44, 19)
    return [c, mortar, top, hole]


def barril():
    b = Part((140, 85, 50), (100, 60, 35), hi=(185, 130, 85))
    b.ellipse(32, 32, 22, 26)
    b.remove(lambda x, y: x < 13 or x > 51)
    hoops = Part((80, 80, 90), None, outline=False)
    for y in (14, 26, 38, 50):
        hoops.rect(12, y, 52, y + 1)
    tap = Part(BRASS, None).rect(29, 40, 35, 44).rect(31, 44, 33, 50)
    drop = Part((150, 20, 50), None).ellipse(32, 55, 2, 2.6)
    return [b, hoops, tap, drop]


def bloc_gel():
    b = Part((180, 225, 250), (120, 180, 220), hi=WHITE)
    b.rect(10, 22, 54, 56)
    top = Part((215, 240, 255), None).rect(10, 14, 54, 22)
    cracks = Part((110, 170, 210), None, outline=False)
    cracks.line(20, 30, 28, 40).line(28, 40, 24, 50).line(40, 26, 46, 36)
    fish = Part((240, 150, 80), (200, 110, 50))
    fish.ellipse(32, 40, 8, 4).line(40, 40, 44, 36).line(40, 40, 44, 44)
    return [b, top, fish, cracks]


def focus():
    stand = Part((50, 50, 56), None).line(32, 40, 22, 60).line(32, 40, 42, 60).line(32, 40, 32, 60)
    lamp = Part((60, 60, 70), (40, 40, 48), hi=(130, 130, 140))
    lamp.rect(18, 16, 46, 40)
    lens = Part((255, 240, 170), None, hi=WHITE).ellipse(32, 28, 9, 9)
    barn = Part((80, 80, 90), None).rect(14, 12, 50, 16)
    return [stand, lamp, barn, lens]


def llum():
    """Un con de llum (es tenyeix i es fa transparent des del codi)."""
    l = Part((255, 250, 220), None, outline=False)
    for y in range(0, 64):
        w = 4 + y * 0.45
        l.rect(32 - w, y, 32 + w, y)
    return [l]


def atac():
    """La forquilla i el ganivet creuats: el client està a punt d'atacar."""
    fork = Part((214, 222, 232), (160, 170, 185), hi=WHITE)
    fork.line(14, 50, 40, 20).line(15, 50, 41, 20)
    for k in range(3):
        fork.line(38 + k * 3, 22 - k * 3, 44 + k * 3, 14 - k * 3)
    knife = Part((214, 222, 232), (160, 170, 185), hi=WHITE)
    for t in range(0, 26):
        knife.ellipse(50 - t, 50 - t * 1.15, 2.6 - t * 0.04, 2.6 - t * 0.04)
    handle = Part((90, 60, 40), None).line(48, 50, 54, 56).line(49, 50, 55, 56)
    lines = Part((255, 214, 72), None, outline=False)
    for (x0, y0, x1, y1) in ((6, 30, 12, 32), (8, 20, 13, 24), (52, 8, 56, 4), (58, 18, 62, 16)):
        lines.line(x0, y0, x1, y1)
    return [fork, knife, handle, lines]


def tomaquet():
    t = Part((220, 50, 40), (170, 30, 25), hi=(250, 130, 110))
    t.ellipse(32, 36, 18, 16)
    leaf = Part((60, 140, 50), (40, 100, 30))
    for a in range(0, 360, 72):
        leaf.ellipse(32 + math.cos(math.radians(a)) * 5, 21 + math.sin(math.radians(a)) * 3, 3, 2)
    stem = Part((60, 110, 40), None).rect(31, 14, 33, 20)
    shine = Part(WHITE, None, outline=False).ellipse(24, 30, 3, 4)
    return [t, leaf, stem, shine]


def esquitx():
    s = Part((210, 40, 35), (160, 25, 20))
    s.ellipse(32, 34, 14, 11)
    for a in range(0, 360, 45):
        r = 18 + (a % 90) / 10
        x = 32 + math.cos(math.radians(a)) * r; y = 34 + math.sin(math.radians(a)) * r * 0.8
        s.ellipse(x, y, 3.4, 3)
        s.line(32, 34, x, y)
    seeds = Part((250, 220, 120), None, outline=False)
    for (x, y) in ((28, 32), (36, 36), (32, 30), (26, 38)): seeds.put(x, y)
    return [s, seeds]


SPRITES = {
    'Gramola': gramola, 'Nota': nota, 'CartellBar': cartell_bar, 'Olla': olla, 'Testos': testos, 'Fulla': fulla,
    'Xemeneia': xemeneia, 'Barril': barril, 'BlocGel': bloc_gel, 'Focus': focus, 'Llum': llum, 'Atac': atac,
    'Tomaquet': tomaquet, 'Esquitx': esquitx,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Decor'))
    os.makedirs(out, exist_ok=True)
    rows = []
    for name, fn in SPRITES.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(out, name + '.png'))
        rows.append((name, [im]))
    print('%d sprites' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=3, cols=6)


if __name__ == '__main__':
    main()
