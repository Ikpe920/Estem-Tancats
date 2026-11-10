# -*- coding: utf-8 -*-
# =====================================================================
#  PROJECTILS I CORS (mateix estil que els personatges)
# =====================================================================
# Contorn negre d'un píxel, colors plans, una ombra i la "llum" blanca en forma de falca
# típica dels dibuixos dels anys 30.
#
#   python3 Tools/SpriteGen/items.py            -> escriu els PNG a Assets/Sprites/
#   python3 Tools/SpriteGen/items.py full.png   -> a més, una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, INK, WHITE, darker, lighter


def shine(part, cx, cy, rx, ry, a0=110, a1=160, inner=0.45, outer=0.85):
    """La falca blanca: els píxels de la peça dins d'un tros d'anell, a dalt a l'esquerra."""
    for (x, y), c in list(part.px.items()):
        dx = (x + 0.5 - cx) / rx; dy = (y + 0.5 - cy) / ry
        d = math.hypot(dx, dy)
        ang = math.degrees(math.atan2(-dy, dx)) % 360
        if inner < d < outer and a0 < ang < a1:
            part.px[(x, y)] = WHITE


def banana():
    b = Part((250, 210, 60), (210, 160, 30))
    for i in range(41):
        t = i / 40
        a = math.radians(200 + t * 140)
        x = 32 + math.cos(a) * 24; y = 22 - math.sin(a) * 18
        b.ellipse(x, y, 4.2 - abs(t - 0.5) * 4, 4.2 - abs(t - 0.5) * 4)
    tips = Part((110, 70, 30), None)
    tips.ellipse(8.5, 27, 1.6, 1.6).ellipse(55.5, 27, 1.6, 1.6)
    hl = Part((255, 245, 180), None, outline=False)
    for i in range(12, 30):
        a = math.radians(200 + i / 40 * 140)
        hl.put(32 + math.cos(a) * 24, 22 - math.sin(a) * 18 - 2)
    return [b, hl, tips]


def chicken_leg():
    bone = Part((246, 240, 226), (210, 200, 182))
    bone.tube([(40, 40), (46, 48), (52, 55)], 2.4)
    bone.ellipse(52, 59, 3, 3).ellipse(57, 55, 3, 3)
    meat = Part((200, 120, 55), (150, 82, 36), hi=(236, 170, 100))
    meat.ellipse(27, 25, 18, 16)
    meat.ellipse(38, 37, 9, 8)
    shine(meat, 27, 25, 18, 16)
    spots = Part((160, 92, 40), None, outline=False)
    for (x, y) in ((20, 30), (30, 20), (34, 30), (24, 22)):
        spots.put(x, y)
    return [bone, meat, spots]


def enemy_plate():
    plate = Part((248, 248, 244), (210, 210, 214))
    plate.ellipse(32, 32, 27, 27)
    rim = Part((214, 220, 232), None, outline=False)
    rim.ellipse(32, 32, 19, 19)
    rim.remove(lambda x, y: (x + 0.5 - 32) ** 2 + (y + 0.5 - 32) ** 2 < 17.5 ** 2)
    blue = Part((70, 110, 200), None, outline=False)  # la sanefa blava
    blue.ellipse(32, 32, 24, 24)
    blue.remove(lambda x, y: (x + 0.5 - 32) ** 2 + (y + 0.5 - 32) ** 2 < 22.8 ** 2)
    food = Part((200, 60, 40), (150, 36, 24), hi=(240, 120, 90))
    food.ellipse(30, 33, 10, 7)
    peas = Part((90, 180, 70), None)
    peas.ellipse(42, 40, 2.6, 2.6).ellipse(37, 44, 2.4, 2.4).ellipse(44, 45, 2.2, 2.2)
    shine(plate, 32, 32, 27, 27, inner=0.75, outer=0.95)
    return [plate, blue, rim, food, peas]


def pan():
    handle = Part((70, 50, 40), (50, 34, 28))
    handle.rect(44, 29, 62, 34)
    handle.put(59, 31, INK).put(59, 32, INK)
    body = Part((60, 60, 70), (36, 36, 44))
    body.ellipse(24, 32, 22, 22)
    inner = Part((90, 90, 104), None, outline=False)
    inner.ellipse(24, 32, 17, 17)
    shine(body, 24, 32, 22, 22, inner=0.82, outer=0.98)
    shine(inner, 24, 32, 17, 17, a0=115, a1=150, inner=0.3, outer=0.7)
    return [handle, body, inner]


def sausage():
    s = Part((190, 80, 70), (140, 50, 44), hi=(236, 140, 120))
    s.tube([(9, 34), (32, 30), (55, 34)], 7)
    hl = Part((250, 190, 170), None, outline=False)
    for x in range(14, 30): hl.put(x, 27 + (x - 14) * 0.08)
    for x in range(34, 40): hl.put(x, 27.5)
    marks = Part((150, 56, 48), None, outline=False)
    for x in (22, 32, 42): marks.rect(x, 34, x + 2, 34)
    return [s, hl, marks]


def heart(full=True):
    c = (230, 40, 56) if full else (64, 64, 84)
    h = Part(c, darker(c, 0.75), hi=None)
    h.ellipse(19, 21, 15, 15).ellipse(45, 21, 15, 15)
    for y in range(24, 58):
        w = 28 * (1 - (y - 24) / 34) ** 0.9
        h.rect(32 - w, y, 32 + w, y)
    # la llum: una falca blanca a dalt a l'esquerra, i un puntet
    sh = Part(WHITE if full else (120, 120, 150), None, outline=False)
    for (x, y) in list(h.px):
        dx = (x + 0.5 - 19) / 15; dy = (y + 0.5 - 21) / 15
        d = math.hypot(dx, dy); ang = math.degrees(math.atan2(-dy, dx)) % 360
        if 0.4 < d < 0.75 and 105 < ang < 165: sh.put(x, y)
    sh.ellipse(11.5, 31, 1.3, 1.3)
    return [h, sh]


def pizza():
    base = Part((236, 186, 100), (196, 140, 70), hi=(250, 220, 150))
    base.ellipse(32, 32, 26, 26)
    sauce = Part((205, 45, 35), None, outline=False)
    sauce.ellipse(32, 32, 21, 21)
    cheese = Part((250, 225, 120), None, outline=False)
    for (x, y, r) in ((24, 26, 5), (39, 24, 4), (36, 38, 6), (22, 39, 4), (32, 31, 3)):
        cheese.ellipse(x, y, r, r * 0.8)
    top = Part((150, 30, 30), (110, 20, 20))
    for (x, y) in ((28, 20), (43, 31), (25, 33), (34, 44), (41, 42), (19, 27)):
        top.ellipse(x, y, 2.6, 2.6)
    leaf = Part((60, 150, 60), None, outline=False)
    for (x, y) in ((33, 25), (29, 41), (44, 37)):
        leaf.rect(x, y, x + 1, y + 1)
    # els talls de la pizza
    cuts = Part((150, 100, 50), None, outline=False)
    for a in (0, 60, 120):
        for t in range(-20, 21):
            ra = math.radians(a)
            cuts.put(32 + math.cos(ra) * t, 32 + math.sin(ra) * t)
    shine(base, 32, 32, 26, 26, inner=0.85, outer=0.98)
    return [base, sauce, cheese, cuts, top, leaf]


def handbag():
    strap = Part((110, 55, 35), None).tube([(20, 26), (32, 8), (44, 26)], 1.4)
    bag = Part((160, 60, 90), (120, 40, 64), hi=(215, 120, 155))
    bag.rect(14, 24, 50, 52)
    bag.ellipse(32, 52, 18, 4)
    clasp = Part((240, 200, 60), None).rect(29, 28, 35, 32)
    shine(bag, 32, 38, 18, 14, inner=0.5, outer=0.9)
    return [strap, bag, clasp]


def star():
    st = Part((250, 200, 50), (210, 150, 20), hi=(255, 240, 150))
    pts = []
    for i in range(10):
        r = 26 if i % 2 == 0 else 11
        a = math.radians(-90 + i * 36)
        pts.append((32 + math.cos(a) * r, 33 + math.sin(a) * r))
    # omplir l'estrella
    for y in range(64):
        for x in range(64):
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if ((yi > y + 0.5) != (yj > y + 0.5)) and (x + 0.5 < (xj - xi) * (y + 0.5 - yi) / (yj - yi + 1e-9) + xi):
                    inside = not inside
                j = i
            if inside: st.put(x, y)
    eyes = Part(INK, None, outline=False).rect(27, 28, 28, 33).rect(36, 28, 37, 33)
    return [st, eyes]


def bill():
    b = Part((120, 190, 120), (80, 150, 85), hi=(180, 230, 180))
    b.rect(6, 18, 58, 46)
    b.rect(10, 22, 54, 42, (150, 210, 150))
    b.ellipse(32, 32, 8, 8, (90, 160, 95))
    b.rect(30, 26, 34, 38, (150, 210, 150))
    for (x, y) in ((13, 25), (51, 39)): b.ellipse(x, y, 2.4, 2.4, (90, 160, 95))
    return [b]


def beer():
    b = Part((150, 100, 40), (110, 70, 25), hi=(220, 170, 90))
    b.rect(25, 24, 39, 58)
    b.ellipse(32, 24, 7, 5)
    b.rect(29, 8, 35, 22)
    cap = Part((220, 200, 60), None).rect(28, 5, 36, 9)
    label = Part((245, 235, 205), None, outline=False).rect(26, 36, 38, 46)
    label.rect(28, 40, 36, 41, (200, 40, 40))
    return [b, cap, label]


def pan_shot():
    handle = Part((70, 50, 40), (50, 34, 28)).rect(42, 29, 62, 35)
    body = Part((60, 60, 70), (36, 36, 44))
    body.ellipse(24, 32, 20, 20)
    fire = Part((250, 140, 40), None, outline=False)
    fire.ellipse(24, 32, 14, 14)
    fire.ellipse(24, 32, 8, 8, (255, 220, 90))
    shine(body, 24, 32, 20, 20, inner=0.82, outer=0.98)
    return [handle, body, fire]


def cork():
    c = Part((200, 150, 90), (160, 110, 60), hi=(235, 195, 140))
    c.rect(20, 18, 44, 46)
    c.ellipse(32, 18, 12, 4, (220, 175, 115))
    for (x, y) in ((25, 26), (37, 30), (29, 38), (39, 41), (24, 43)): c.put(x, y, (150, 100, 55))
    return [c]


def iceball():
    cone = Part((220, 160, 80), (180, 120, 50))
    for i in range(14):
        cone.rect(22 + i * 0.7, 36 + i, 42 - i * 0.7, 36 + i)
    ball = Part((170, 225, 245), (120, 185, 220), hi=WHITE)
    ball.ellipse(32, 28, 16, 14)
    shine(ball, 32, 28, 16, 14)
    flakes = Part(WHITE, None, outline=False)
    for (x, y) in ((26, 24), (38, 30), (31, 34)): flakes.put(x, y)
    return [cone, ball, flakes]


def paper():
    p = Part((248, 246, 238), (210, 205, 195))
    p.rect(14, 8, 50, 56)
    lines = Part((150, 150, 160), None, outline=False)
    for y in (16, 22, 28, 34): lines.rect(18, y, 44, y)
    stamp = Part((210, 40, 40), None, outline=False)
    stamp.ellipse(36, 44, 9, 7)
    stamp.ellipse(36, 44, 6, 4, (248, 246, 238))
    return [p, lines, stamp]


ITEMS = {
    'Resources/BossShots/Star': star,
    'Resources/BossShots/Bill': bill,
    'Resources/BossShots/Beer': beer,
    'Resources/BossShots/Pan': pan_shot,
    'Resources/BossShots/Cork': cork,
    'Resources/BossShots/IceBall': iceball,
    'Resources/BossShots/Paper': paper,
    'Resources/Projectiles/Pizza': pizza,
    'Resources/Projectiles/Bolso': handbag,
    'Sprites/Projectiles/Banana': banana,
    'Sprites/Projectiles/ChickenLeg': chicken_leg,
    'Sprites/Projectiles/EnemyPlate': enemy_plate,
    'Sprites/Projectiles/Pan': pan,
    'Sprites/Projectiles/Sausage': sausage,
    'Sprites/UI/Heart': lambda: heart(True),
    'Sprites/UI/HeartEmpty': lambda: heart(False),
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    assets = os.path.normpath(os.path.join(here, '..', '..', 'Assets'))
    rows = []
    for path, fn in ITEMS.items():
        im = to_image(composite(fn()))
        os.makedirs(os.path.dirname(os.path.join(assets, path)), exist_ok=True)
        im.save(os.path.join(assets, path + '.png'))
        rows.append((path.split('/')[-1], [im]))
    print('%d objectes' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=4, cols=7)


if __name__ == '__main__':
    main()
