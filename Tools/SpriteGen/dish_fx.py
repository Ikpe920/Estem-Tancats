# -*- coding: utf-8 -*-
# =====================================================================
#  ELS PLATS DINS DE LA PARTIDA (Resources/DishFx i Resources/Projectiles, 64x64)
# =====================================================================
# El que es veu quan un plat fa el seu efecte, amb el traç de la resta del joc:
#   Fuet_0..2   el cop de fuet (3 fotogrames: s'enrotlla, s'estira i peta)
#   Bomba       la bomba de la Barceloneta, amb la metxa encesa
#   Pastis      el pastís de nata que cau del cel
#   Nata        la taca de nata que deixa a terra
#   Sopa        el toll de sopa de galets
#   Bombolla    una bombolla de cava
#   Projectiles/Ganivet   els ganivets del pernil (cap a la dreta)
#   Projectiles/Panellet  els panellets que llences en fer el dash
#   python3 Tools/SpriteGen/dish_fx.py            -> escriu els PNG
#   python3 Tools/SpriteGen/dish_fx.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, WHITE, darker, lighter
from items import shine

FUET = (160, 70, 60)
FUET_S = (120, 45, 40)
FUET_HI = (210, 120, 110)


def bez(p0, p1, p2, t):
    a = (1 - t) ** 2; b = 2 * (1 - t) * t; c = t * t
    return a * p0[0] + b * p1[0] + c * p2[0], a * p0[1] + b * p1[1] + c * p2[1]


def fuet_frame(f):
    """El fuet com un fuet de debò: el mànec a l'esquerra i la tralla cap a la dreta."""
    whip = Part(FUET, FUET_S, hi=FUET_HI)
    skin = Part((240, 235, 225), None, outline=False)
    if f == 0:   # enrotllat, agafant embranzida
        pts = [(6, 34), (20, 14), (34, 30)]
        tail = [(34, 30), (40, 42), (30, 44)]
    elif f == 1:  # s'estira fent una ona
        pts = [(6, 34), (22, 22), (36, 32)]
        tail = [(36, 32), (48, 42), (58, 30)]
    else:        # recte del tot: peta
        pts = [(6, 34), (24, 30), (40, 32)]
        tail = [(40, 32), (50, 33), (60, 31)]
    for i in range(41):
        t = i / 40
        x, y = bez(*pts, t)
        whip.ellipse(x, y, 3.4 - t * 0.8, 3.4 - t * 0.8)
    for i in range(41):
        t = i / 40
        x, y = bez(*tail, t)
        whip.ellipse(x, y, 2.6 - t * 1.6, 2.6 - t * 1.6)
    for i in range(0, 40, 4):  # la pell blanca del fuet
        x, y = bez(*pts, i / 40)
        skin.put(x + 1, y - 1)
    handle = Part((70, 50, 40), (45, 32, 25)).rect(2, 31, 9, 37)
    ps = [whip, skin, handle]
    if f == 2:  # l'esclat a la punta
        crack = Part((255, 240, 160), None)
        cx, cy = 58, 31
        for a in range(0, 360, 45):
            r = 7 if a % 90 == 0 else 4
            crack.line(cx, cy, cx + math.cos(math.radians(a)) * r, cy + math.sin(math.radians(a)) * r)
        ps.append(crack)
    return ps


def bomba():
    b = Part((190, 130, 70), (150, 95, 50))
    b.ellipse(32, 38, 17, 16)
    shine(b, 32, 38, 17, 16)
    sauce = Part((210, 50, 35), (160, 35, 25), outline=False)
    sauce.ellipse(30, 27, 11, 5)
    for (x, y) in ((22, 30), (36, 31), (28, 32)): sauce.ellipse(x, y, 2, 2.4)
    aioli = Part((252, 248, 225), None, outline=False)
    aioli.ellipse(34, 25, 4, 2)
    fuse = Part((90, 70, 50), None).tube([(36, 22), (42, 12), (48, 8)], 1)
    spark = Part((255, 210, 60), None)
    for a in range(0, 360, 45):
        r = 5 if a % 90 == 0 else 3
        spark.line(49, 7, 49 + math.cos(math.radians(a)) * r, 7 + math.sin(math.radians(a)) * r)
    core = Part(WHITE, None, outline=False).put(49, 7)
    return [b, sauce, aioli, fuse, spark, core]


def pastis():
    plate = Part((248, 248, 244), (206, 206, 214)).ellipse(32, 50, 26, 8)
    tin = Part((210, 160, 90), (170, 120, 60))
    tin.ellipse(32, 44, 22, 8)
    tin.rect(10, 36, 54, 44)
    cream = Part((255, 252, 245), (225, 222, 215), hi=WHITE)
    cream.ellipse(32, 30, 22, 10)
    cream.rect(10, 30, 54, 37)
    for x in range(12, 54, 7): cream.ellipse(x, 38, 3.4, 3)
    for (x, y) in ((20, 22), (32, 18), (44, 22)): cream.ellipse(x, y, 5, 4)
    cherry = Part((220, 30, 40), None, hi=WHITE).ellipse(32, 13, 4, 4)
    stem = Part((60, 110, 40), None).line(33, 9, 37, 4)
    return [plate, tin, cream, cherry, stem]


def nata():
    s = Part((255, 252, 245), (225, 222, 215), hi=WHITE)
    s.ellipse(32, 34, 18, 13)
    for a in range(0, 360, 40):
        r = 22 + (a % 80) / 10
        x = 32 + math.cos(math.radians(a)) * r
        y = 34 + math.sin(math.radians(a)) * r * 0.75
        s.ellipse(x, y, 4, 3.4)
        s.line(32, 34, x, y)
    for (x, y, r) in ((8, 14, 2.4), (56, 12, 2), (54, 56, 2.6), (10, 56, 1.8)):
        s.ellipse(x, y, r, r)
    cherry = Part((220, 30, 40), None, hi=WHITE).ellipse(36, 30, 3.4, 3.4)
    return [s, cherry]


def sopa():
    p = Part((240, 196, 110), (210, 160, 80), hi=(255, 230, 160))
    p.ellipse(32, 32, 28, 22)
    for a in range(0, 360, 60):
        p.ellipse(32 + math.cos(math.radians(a)) * 24, 32 + math.sin(math.radians(a)) * 18, 6, 5)
    galets = Part((250, 235, 180), (210, 185, 120))
    for (x, y) in ((22, 28), (36, 24), (42, 36), (28, 40), (34, 33), (16, 36), (46, 28)):
        galets.ellipse(x, y, 3, 2)
        galets.put(x, y, (200, 160, 90))
    fat = Part((255, 240, 180), None, outline=False)
    for (x, y) in ((26, 22), (40, 42), (20, 44), (48, 22)): fat.ellipse(x, y, 1.4, 1)
    return [p, galets, fat]


def bombolla():
    b = Part((230, 245, 255), None)
    b.ellipse(32, 32, 22, 22)
    b.remove(lambda x, y: ((x + 0.5 - 32) / 19) ** 2 + ((y + 0.5 - 32) / 19) ** 2 < 1)
    shine_ = Part(WHITE, None, outline=False)
    for i in range(10):
        a = math.radians(120 + i * 6)
        shine_.ellipse(32 + math.cos(a) * 14, 32 - math.sin(a) * 14, 1.6, 1.6)
    dot = Part(WHITE, None, outline=False).ellipse(40, 22, 2, 2)
    return [b, shine_, dot]


def ganivet():
    blade = Part((214, 222, 232), (160, 170, 185), hi=WHITE)
    for x in range(26, 60):
        t = (x - 26) / 34
        top = 26 + t * t * 5
        blade.rect(x, top, x, 36)
    edge = Part((245, 248, 255), None, outline=False).rect(28, 35, 54, 35)
    handle = Part((60, 40, 32), (40, 26, 20)).rect(6, 28, 26, 38)
    rivets = Part((220, 200, 120), None, outline=False).put(11, 33).put(19, 33)
    return [blade, edge, handle, rivets]


def panellet():
    p = Part((240, 210, 150), (205, 170, 110))
    p.ellipse(32, 34, 20, 18)
    pine = Part((250, 240, 200), (210, 190, 140))
    for ring, (r, n) in enumerate(((13, 9), (6, 5))):
        for k in range(n):
            a = math.radians(k * 360 / n + ring * 20)
            pine.ellipse(32 + math.cos(a) * r, 33 + math.sin(a) * r * 0.9, 2.6, 1.8)
    pine.ellipse(32, 33, 2.6, 1.8)
    return [p, pine]


FX = {
    'DishFx/Fuet_0': lambda: fuet_frame(0),
    'DishFx/Fuet_1': lambda: fuet_frame(1),
    'DishFx/Fuet_2': lambda: fuet_frame(2),
    'DishFx/Bomba': bomba,
    'DishFx/Pastis': pastis,
    'DishFx/Nata': nata,
    'DishFx/Sopa': sopa,
    'DishFx/Bombolla': bombolla,
    'Projectiles/Ganivet': ganivet,
    'Projectiles/Panellet': panellet,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    res = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources'))
    os.makedirs(os.path.join(res, 'DishFx'), exist_ok=True)
    rows = []
    for name, fn in FX.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(res, name + '.png'))
        rows.append((name, [im]))
    print('%d sprites' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=3, cols=5)


if __name__ == '__main__':
    main()
