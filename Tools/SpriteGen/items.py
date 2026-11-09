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


ITEMS = {
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
        im.save(os.path.join(assets, path + '.png'))
        rows.append((path.split('/')[-1], [im]))
    print('%d objectes' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=4, cols=7)


if __name__ == '__main__':
    main()
