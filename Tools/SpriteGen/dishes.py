# -*- coding: utf-8 -*-
# =====================================================================
#  ICONES DELS PLATS NOUS (Resources/Dishes/<nom>.png, 64x64)
# =====================================================================
# Mateix traç que els personatges: contorn negre d'un píxel, colors plans i la llum blanca.
#   python3 Tools/SpriteGen/dishes.py            -> escriu els PNG
#   python3 Tools/SpriteGen/dishes.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, INK, WHITE, darker, lighter
from items import shine


def plate(cy=44, rx=26, ry=11):
    p = Part((248, 248, 244), (206, 206, 214))
    p.ellipse(32, cy, rx, ry)
    rim = Part((220, 226, 238), None, outline=False)
    rim.ellipse(32, cy, rx - 5, ry - 3)
    rim.remove(lambda x, y: ((x + 0.5 - 32) / (rx - 7)) ** 2 + ((y + 0.5 - cy) / (ry - 4)) ** 2 < 1)
    return [p, rim]


def bowl(c=(240, 240, 236), cy=42):
    b = Part(c, darker(c, 0.82))
    b.ellipse(32, cy, 22, 14)
    b.remove(lambda x, y: y < cy - 2)
    b.ellipse(32, cy - 2, 22, 6)
    return b


def padro():
    ps = plate()
    for i, (x, y, a) in enumerate(((20, 38, -0.4), (28, 34, 0.2), (36, 38, -0.2), (44, 34, 0.5), (32, 42, 0.0))):
        pep = Part((70, 150, 50), (40, 100, 30), hi=(150, 210, 110))
        for t in range(9):
            pep.ellipse(x + math.cos(a) * t * 0.9, y + math.sin(a) * t * 0.9, 2.6 - t * 0.12, 2.6 - t * 0.12)
        pep.put(x - math.cos(a) * 1.5, y - math.sin(a) * 1.5 - 2, (110, 80, 40))
        ps.append(pep)
    salt = Part(WHITE, None, outline=False)
    for (x, y) in ((24, 33), (35, 36), (41, 40), (29, 40)): salt.put(x, y)
    return ps + [salt]


def mel_mato():
    ps = plate()
    m = Part((250, 250, 245), (220, 220, 214), hi=WHITE)
    m.ellipse(32, 36, 14, 9)
    m.rect(18, 36, 46, 41)
    honey = Part((240, 170, 40), None, hi=(255, 220, 120))
    honey.tube([(22, 32), (32, 29), (42, 33)], 1.4)
    honey.tube([(30, 31), (31, 36), (29, 41)], 1.1)
    shine(m, 32, 36, 14, 9, inner=0.4, outer=0.8)
    return ps + [m, honey]


def carquinyolis():
    ps = plate()
    for (x, y, a) in ((22, 37, -0.3), (34, 34, 0.25), (42, 40, -0.15), (28, 42, 0.1)):
        c = Part((220, 170, 90), (180, 125, 60), hi=(245, 210, 140))
        dx, dy = math.cos(a), math.sin(a)
        for t in range(-5, 6):
            c.ellipse(x + dx * t, y + dy * t, 2.6, 2.6)
        alm = Part((250, 235, 200), None, outline=False)
        alm.put(x - dx * 2, y - dy * 2).put(x + dx * 2, y + dy * 2)
        ps += [c, alm]
    return ps


def vermut():
    glass = Part((230, 240, 245), None)
    glass.ellipse(32, 22, 13, 12)
    glass.remove(lambda x, y: y < 18)
    glass.rect(19, 14, 45, 18)
    drink = Part((170, 30, 40), (120, 20, 30), outline=False)
    drink.ellipse(32, 22, 11, 10)
    drink.remove(lambda x, y: y < 19)
    stem = Part((230, 240, 245), None).rect(31, 33, 33, 52).ellipse(32, 54, 9, 2.5)
    olive = Part((100, 140, 50), None, hi=(170, 210, 110)).ellipse(36, 22, 3, 3)
    stick = Part((180, 140, 80), None).line(30, 12, 39, 26)
    orange = Part((250, 150, 40), (220, 110, 20)).ellipse(22, 16, 4, 4)
    return [stem, glass, drink, stick, olive, orange]


def allioli():
    ps = []
    b = Part((200, 120, 70), (150, 85, 45), hi=(230, 160, 110))  # morter de terrissa
    b.ellipse(32, 40, 20, 13)
    b.remove(lambda x, y: y < 36)
    b.ellipse(32, 36, 20, 6)
    sauce = Part((252, 248, 225), None, hi=WHITE, outline=False)
    sauce.ellipse(32, 36, 16, 4.5)
    garlic = Part((245, 240, 230), (215, 205, 190))
    garlic.ellipse(16, 24, 5, 6).put(16, 17, (180, 170, 140))
    pestle = Part((170, 110, 60), (130, 80, 40)).tube([(40, 20), (44, 12), (48, 6)], 2.4)
    return [pestle, b, sauce, garlic]


def tomaquet_penjar():
    ps = []
    string = Part((200, 180, 140), None).line(32, 4, 32, 22)
    ps.append(string)
    for (x, y) in ((24, 26), (40, 26), (32, 34), (22, 42), (42, 42), (32, 50)):
        t = Part((220, 50, 40), (170, 30, 25))
        t.ellipse(x, y, 7, 6.5)
        shine(t, x, y, 7, 6.5)
        leaf = Part((60, 140, 50), None, outline=False).put(x, y - 6).put(x - 1, y - 6).put(x + 1, y - 6)
        ps += [t, leaf]
    return ps


def cireres():
    stems = Part((90, 120, 40), None)
    stems.tube([(22, 40), (28, 20), (36, 10)], 0.8)
    stems.tube([(42, 42), (40, 22), (36, 10)], 0.8)
    leaf = Part((70, 150, 60), (40, 110, 40)).ellipse(42, 12, 6, 3)
    ps = [stems, leaf]
    for (x, y) in ((21, 44), (43, 46)):
        c = Part((200, 20, 40), (140, 10, 25))
        c.ellipse(x, y, 9, 8.5)
        shine(c, x, y, 9, 8.5)
        ps.append(c)
    return ps


def pernil():
    ps = plate()
    for k in range(3):
        h = Part((200, 70, 80), (160, 45, 55))
        h.ellipse(24 + k * 8, 37 - k * 2, 9, 5)
        fat = Part((250, 225, 225), None, outline=False)
        for t in range(-6, 7): fat.put(24 + k * 8 + t, 35 - k * 2 + (t * t) * 0.04)
        ps += [h, fat]
    knife = Part((214, 222, 232), None, hi=WHITE)
    knife.line(10, 20, 30, 28).line(10, 21, 30, 29)
    handle = Part((50, 35, 30), None).tube([(30, 28.5), (36, 31), (41, 33)], 1.6)
    return ps + [knife, handle]


def sopa():
    b = bowl((235, 225, 210))
    broth = Part((240, 200, 110), None, outline=False).ellipse(32, 40, 18, 4.5)
    galets = Part((250, 235, 180), (210, 185, 120))
    for (x, y) in ((24, 40), (32, 39), (40, 41), (28, 42), (36, 42)):
        galets.ellipse(x, y, 2.4, 1.6)
    steam = Part((250, 250, 250), None, outline=False)
    for x0 in (24, 32, 40):
        for t in range(10):
            steam.put(x0 + math.sin(t * 0.9) * 1.6, 32 - t * 1.6)
    return [b, broth, galets, steam]


def pastis_nata():
    ps = plate(cy=50, rx=24, ry=8)
    cake = Part((245, 225, 190), (210, 180, 140))
    cake.rect(16, 30, 48, 48)
    cream = Part((255, 252, 245), (225, 222, 215), hi=WHITE)
    cream.ellipse(32, 30, 16, 5)
    for x in range(18, 48, 6): cream.ellipse(x, 34, 2.5, 3)
    layer = Part((200, 80, 90), None, outline=False).rect(17, 40, 47, 41)
    cherry = Part((220, 30, 40), None, hi=WHITE).ellipse(32, 24, 3.4, 3.4)
    return ps + [cake, layer, cream, cherry]


def cava():
    b = Part((40, 80, 50), (25, 55, 35), hi=(110, 160, 120))
    b.rect(26, 26, 38, 58)
    b.ellipse(32, 26, 6, 5)
    b.rect(30, 12, 34, 24)
    foil = Part((230, 190, 70), (190, 150, 40)).rect(29, 8, 35, 16)
    label = Part((245, 235, 205), None, outline=False).rect(27, 38, 37, 47)
    label.rect(29, 41, 35, 41, (180, 40, 40))
    bubbles = Part((250, 250, 220), None)
    for (x, y, r) in ((24, 8, 2), (40, 5, 1.6), (36, 1.5, 1.2), (20, 2, 1.3)):
        bubbles.ellipse(x, y, r, r)
    return [b, foil, label, bubbles]


def escalivada():
    ps = plate()
    for (x, y, c) in ((22, 38, (200, 40, 40)), (34, 36, (90, 40, 110)), (44, 40, (230, 140, 40)), (28, 42, (200, 50, 40))):
        v = Part(c, darker(c), hi=lighter(c, 0.4))
        for t in range(-6, 7): v.ellipse(x + t, y + math.sin(t * 0.5) * 0.8, 2.6, 2.4)
        ps.append(v)
    grill = Part((40, 30, 30), None, outline=False)
    for x in (22, 30, 38): grill.line(x, 33, x + 6, 44)
    return ps + [grill]


def bombes():
    ps = plate()
    for (x, y) in ((24, 36), (40, 36), (32, 41)):
        b = Part((190, 130, 70), (150, 95, 50))
        b.ellipse(x, y, 7, 6.5)
        shine(b, x, y, 7, 6.5)
        sauce = Part((210, 60, 40), None, outline=False).ellipse(x - 1, y - 4, 3.4, 1.8)
        aioli = Part((252, 248, 225), None, outline=False).ellipse(x + 2, y - 5, 2, 1.2)
        ps += [b, sauce, aioli]
    return ps


def fuet():
    f = Part((160, 70, 60), (120, 45, 40), hi=(210, 120, 110))
    f.tube([(8, 50), (30, 30), (56, 14)], 4.4)
    skin = Part((240, 235, 225), None, outline=False)
    for i in range(0, 40, 3):
        t = i / 40.0
        x = (1 - t) ** 2 * 8 + 2 * (1 - t) * t * 30 + t * t * 56
        y = (1 - t) ** 2 * 50 + 2 * (1 - t) * t * 30 + t * t * 14
        skin.put(x + 1, y - 1)
    slices = []
    for (x, y) in ((44, 46), (52, 52)):
        s = Part((170, 80, 70), None)
        s.ellipse(x, y, 5, 5)
        dots = Part((250, 230, 230), None, outline=False).put(x - 1, y).put(x + 2, y - 1).put(x, y + 2)
        slices += [s, dots]
    return [f, skin] + slices


def truita():
    ps = plate()
    t = Part((245, 200, 80), (210, 160, 50), hi=(255, 230, 140))
    t.ellipse(32, 36, 20, 9)
    t.rect(12, 36, 52, 41)
    t.ellipse(32, 41, 20, 4)
    cut = Part((250, 235, 180), None, outline=False)
    cut.rect(33, 32, 37, 44)
    for (x, y) in ((34, 35), (36, 39), (34, 42)): cut.put(x, y, (230, 190, 90))
    shine(t, 32, 36, 20, 9, inner=0.5, outer=0.85)
    return ps + [t, cut]


def truita_projectile():
    t = Part((245, 200, 80), (205, 155, 45), hi=(255, 230, 140))
    t.ellipse(32, 32, 24, 24)
    brown = Part((200, 140, 50), None, outline=False)
    for (x, y) in ((24, 26), (38, 22), (40, 38), (26, 40), (33, 33)):
        brown.ellipse(x, y, 2.5, 2)
    shine(t, 32, 32, 24, 24, inner=0.8, outer=0.97)
    return [t, brown]


DISHES = {
    'pebrots_de_padro': padro,
    'mel_i_mato': mel_mato,
    'carquinyolis': carquinyolis,
    'vermut': vermut,
    'allioli': allioli,
    'tomaquet_de_penjar': tomaquet_penjar,
    'cireres': cireres,
    'pernil_tallat_a_ganivet': pernil,
    'sopa_de_galets': sopa,
    'pastis_de_nata': pastis_nata,
    'cava': cava,
    'escalivada': escalivada,
    'bombes_de_la_barceloneta': bombes,
    'fuet': fuet,
    'truita_de_patates': truita,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    assets = os.path.normpath(os.path.join(here, '..', '..', 'Assets'))
    rows = []
    for name, fn in DISHES.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(assets, 'Resources', 'Dishes', name + '.png'))
        rows.append((name, [im]))
    im = to_image(composite(truita_projectile()))
    im.save(os.path.join(assets, 'Resources', 'Projectiles', 'Truita.png'))
    rows.append(('Truita (projectil)', [im]))
    print('%d icones' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=3, cols=8)


if __name__ == '__main__':
    main()
