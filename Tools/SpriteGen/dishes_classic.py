# -*- coding: utf-8 -*-
# =====================================================================
#  ICONES DELS PLATS DE SEMPRE (Resources/Dishes/<nom>.png, 64x64)
# =====================================================================
# Els 30 plats del principi, redibuixats amb el mateix traç que els plats nous (dishes.py):
# contorn negre d'un píxel, colors plans, ombra a baix a la dreta i la llum blanca.
#   python3 Tools/SpriteGen/dishes_classic.py            -> escriu els PNG
#   python3 Tools/SpriteGen/dishes_classic.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from characters import Part, composite, to_image, WHITE, darker, lighter
from items import shine
from dishes import plate, bowl


def blob(c, x, y, rx, ry, hi=True):
    p = Part(c, darker(c), hi=lighter(c, 0.45) if hi else None)
    p.ellipse(x, y, rx, ry)
    return p


def steam(xs, y0=30, n=10):
    s = Part((250, 250, 250), None, outline=False)
    for x0 in xs:
        for t in range(n):
            s.put(x0 + math.sin(t * 0.9) * 1.6, y0 - t * 1.6)
    return s


# ---------- PLATS AMB PLAT ----------

def botifarra():
    ps = plate()
    b = Part((150, 75, 60), (110, 50, 40), hi=(200, 120, 100))
    b.tube([(14, 40), (30, 30), (46, 36)], 4)
    marks = Part((80, 40, 30), None, outline=False)
    for x in (20, 28, 36): marks.line(x, 33, x + 3, 38)
    beans = Part((245, 240, 225), (210, 200, 180))
    for (x, y) in ((34, 44), (40, 43), (46, 42), (28, 45), (44, 46), (38, 47), (50, 44)):
        beans.ellipse(x, y, 2.4, 1.6)
    return ps + [beans, b, marks]


def bunyols():
    ps = plate()
    for (x, y) in ((24, 38), (38, 37), (31, 43), (44, 43), (20, 44)):
        b = blob((220, 160, 70), x, y, 6, 5.5)
        shine(b, x, y, 6, 5.5)
        ps.append(b)
    lemon = Part((250, 225, 60), (220, 190, 30)).ellipse(48, 32, 5, 3.5)
    return ps + [lemon]


def cafe():
    saucer = Part((248, 248, 244), (206, 206, 214)).ellipse(32, 48, 24, 7)
    cup = Part((250, 250, 248), (210, 210, 218), hi=WHITE)
    cup.ellipse(32, 36, 14, 12)
    cup.remove(lambda x, y: y < 30)
    cup.rect(18, 26, 46, 30)
    handle = Part((250, 250, 248), None).tube([(45, 32), (52, 35), (45, 40)], 1.6)
    coffee = Part((70, 40, 25), None, outline=False).ellipse(32, 28, 12, 3)
    crema = Part((180, 120, 70), None, outline=False).ellipse(30, 28, 6, 1.4)
    return [saucer, handle, cup, coffee, crema, steam((28, 36), 22, 9)]


def calamars():
    ps = plate()
    for (x, y) in ((22, 38), (34, 35), (44, 40), (30, 44)):
        r = Part((235, 195, 110), (200, 155, 70), hi=(250, 230, 170))
        r.ellipse(x, y, 7, 5)
        r.remove(lambda px, py, x=x, y=y: ((px + 0.5 - x) / 3.5) ** 2 + ((py + 0.5 - y) / 2.2) ** 2 < 1)
        ps.append(r)
    lemon = Part((250, 225, 60), (220, 190, 30)).ellipse(48, 32, 5, 3.5)
    return ps + [lemon]


def calcots():
    ps = plate()
    for k in range(4):
        y = 30 + k * 4
        burnt = Part((50, 45, 40), (30, 28, 25)).tube([(10, y + 4), (26, y), (40, y - 2)], 2.2)
        green = Part((90, 160, 60), (60, 120, 40)).tube([(40, y - 2), (48, y - 4), (56, y - 8)], 1.6)
        white = Part((240, 240, 225), None, outline=False).put(38, y - 2).put(39, y - 2)
        ps += [burnt, green, white]
    sauce = Part((210, 90, 50), (170, 60, 30), hi=(240, 140, 90)).ellipse(22, 46, 8, 3.5)
    return ps + [sauce]


def coca_recapte():
    ps = plate()
    c = Part((230, 180, 100), (190, 140, 70))
    c.rect(10, 32, 54, 42)
    pepper = Part((200, 40, 40), None, outline=False)
    eggplant = Part((90, 40, 100), None, outline=False)
    fish = Part((150, 160, 170), None, outline=False)
    for i, x in enumerate(range(13, 52, 6)):
        (pepper if i % 3 == 0 else eggplant if i % 3 == 1 else fish).rect(x, 34, x + 3, 40)
    return ps + [c, pepper, eggplant, fish]


def croquetes():
    ps = plate()
    for (x, y, a) in ((22, 38, 0.3), (34, 35, -0.2), (44, 41, 0.1), (30, 43, -0.4)):
        c = Part((210, 140, 60), (170, 100, 40), hi=(245, 190, 110))
        for t in range(-4, 5):
            c.ellipse(x + math.cos(a) * t, y + math.sin(a) * t, 3.6, 3.6)
        crumbs = Part((240, 190, 110), None, outline=False)
        for t in (-3, 0, 3): crumbs.put(x + math.cos(a) * t, y + math.sin(a) * t - 2)
        ps += [c, crumbs]
    return ps


def fideua():
    ps = plate()
    f = Part((240, 180, 70), (205, 140, 40), hi=(255, 220, 130))
    f.ellipse(32, 38, 20, 8)
    noodles = Part((210, 130, 40), None, outline=False)
    for i in range(10):
        x = 16 + i * 3.4
        noodles.line(x, 34 + (i % 3), x + 2, 40 - (i % 2))
    chili = Part((220, 30, 30), (160, 20, 20)).tube([(40, 28), (46, 26), (50, 30)], 1.8)
    stem = Part((60, 130, 40), None, outline=False).put(39, 28).put(38, 27)
    prawn = Part((240, 120, 80), (200, 80, 50)).tube([(20, 34), (16, 30), (22, 28)], 2)
    return ps + [f, noodles, prawn, chili, stem]


def macarrons():
    ps = plate()
    for (x, y, a) in ((20, 38, 0.4), (28, 34, -0.3), (36, 38, 0.2), (44, 35, -0.5), (26, 43, 0.1), (38, 44, -0.2), (46, 41, 0.6)):
        m = Part((240, 160, 70), (200, 110, 40))
        dx, dy = math.cos(a) * 3, math.sin(a) * 3
        m.ellipse(x - dx, y - dy, 2.6, 2.6).ellipse(x, y, 2.6, 2.6).ellipse(x + dx, y + dy, 2.6, 2.6)
        hole = Part((150, 50, 30), None, outline=False).put(x + dx, y + dy)
        ps += [m, hole]
    sauce = Part((210, 50, 40), None, outline=False)
    for (x, y) in ((24, 40), (32, 41), (41, 40), (30, 36)): sauce.ellipse(x, y, 1.6, 1)
    cheese = Part((255, 245, 200), None, outline=False)
    for (x, y) in ((25, 33), (35, 32), (42, 37), (30, 46)): cheese.put(x, y)
    return ps + [sauce, cheese]


def mandonguilles():
    ps = plate()
    sauce = Part((180, 70, 40), None, outline=False).ellipse(32, 40, 19, 7)
    ps.append(sauce)
    for (x, y) in ((24, 37), (38, 36), (31, 42), (43, 42)):
        m = blob((140, 80, 50), x, y, 5.5, 5)
        shine(m, x, y, 5.5, 5)
        ps.append(m)
    peas = Part((110, 180, 60), None)
    for (x, y) in ((20, 42), (48, 38), (33, 34)): peas.ellipse(x, y, 1.3, 1.3)
    return ps + [peas]


def olives():
    ps = plate()
    for (x, y) in ((22, 38), (34, 35), (44, 39), (28, 43), (40, 44)):
        o = blob((110, 150, 50), x, y, 5, 4)
        shine(o, x, y, 5, 4)
        stuff = Part((230, 80, 50), None, outline=False).ellipse(x + 2, y - 1, 1.4, 1.2)
        ps += [o, stuff]
    stick = Part((200, 160, 100), None).line(30, 20, 36, 34)
    return ps + [stick]


def pa_tomaquet():
    ps = plate()
    bread = Part((225, 170, 90), (180, 125, 60))
    bread.rect(14, 30, 50, 44)
    crumb = Part((240, 210, 150), None, outline=False).rect(17, 32, 47, 41)
    tom = Part((220, 60, 50), None, outline=False)
    for (x, y) in ((20, 34), (27, 36), (34, 33), (40, 37), (45, 34), (24, 39), (38, 40)):
        tom.ellipse(x, y, 2.2, 1.4)
    oil = Part((250, 230, 120), None, outline=False).put(30, 35).put(42, 39).put(22, 37)
    return ps + [bread, crumb, tom, oil]


def paella():
    pan = Part((70, 70, 75), (45, 45, 50)).ellipse(32, 38, 26, 13)
    handles = Part((70, 70, 75), None).rect(2, 36, 7, 39).rect(57, 36, 62, 39)
    rice = Part((245, 190, 60), (215, 155, 40), outline=False).ellipse(32, 37, 22, 10)
    ps = [handles, pan, rice]
    for (x, y) in ((20, 34), (40, 32), (32, 40)):
        pr = Part((240, 110, 70), (200, 70, 40)).tube([(x - 4, y), (x, y - 3), (x + 4, y - 1)], 1.8)
        ps.append(pr)
    mussels = Part((40, 40, 60), None)
    for (x, y) in ((26, 42), (44, 39), (30, 31)): mussels.ellipse(x, y, 3, 1.8)
    peas = Part((100, 170, 60), None, outline=False)
    for (x, y) in ((22, 39), (36, 35), (42, 43), (28, 35)): peas.put(x, y)
    lemon = Part((250, 225, 60), (220, 190, 30)).ellipse(48, 30, 5, 3.5)
    pepper = Part((210, 40, 40), None, outline=False).rect(34, 43, 39, 44)
    return ps + [mussels, peas, pepper, lemon]


def panellets():
    ps = plate()
    for (x, y) in ((22, 37), (36, 35), (29, 42), (43, 42)):
        p = blob((240, 210, 150), x, y, 5.5, 5)
        pine = Part((250, 240, 200), (200, 180, 120), outline=False)
        for a in range(0, 360, 60):
            pine.put(x + math.cos(math.radians(a)) * 3, y + math.sin(math.radians(a)) * 2.6)
        ps += [p, pine]
    return ps


def pollastre_curri():
    b = bowl((240, 240, 236))
    curry = Part((230, 160, 40), None, outline=False).ellipse(32, 40, 18, 4.5)
    meat = Part((245, 210, 160), (210, 170, 120))
    for (x, y) in ((24, 39), (32, 38), (40, 40)): meat.ellipse(x, y, 3.4, 2.4)
    herbs = Part((70, 150, 60), None, outline=False).put(28, 37).put(36, 37).put(42, 38)
    return [b, curry, meat, herbs, steam((26, 38), 30, 8)]


def romesco():
    b = bowl((200, 120, 70), cy=44)
    sauce = Part((210, 80, 40), None, hi=(240, 130, 80), outline=False).ellipse(32, 42, 18, 4.5)
    almond = Part((220, 170, 110), (180, 130, 80))
    almond.ellipse(14, 20, 4, 6).ellipse(24, 16, 4, 6)
    pepper = Part((200, 40, 40), (150, 25, 25), hi=(240, 110, 100)).ellipse(46, 22, 8, 9)
    stem = Part((60, 120, 40), None).rect(45, 11, 47, 14)
    return [almond, pepper, stem, b, sauce]


def brava():
    ps = plate()
    for (x, y) in ((22, 37), (32, 34), (42, 37), (27, 43), (38, 43)):
        p = Part((240, 200, 110), (205, 160, 70), hi=(255, 235, 170))
        p.rect(x - 4, y - 3, x + 4, y + 3)
        ps.append(p)
    sauce = Part((220, 60, 30), None, outline=False)
    for (x, y) in ((22, 35), (32, 32), (42, 35), (27, 41), (38, 41)): sauce.ellipse(x, y, 3, 1.4)
    aioli = Part((252, 248, 225), None, outline=False)
    for (x, y) in ((24, 34), (40, 34), (29, 40)): aioli.put(x, y)
    return ps + [sauce, aioli]


def xili():
    b = bowl((150, 70, 40))
    chili = Part((170, 50, 30), None, outline=False).ellipse(32, 40, 18, 4.5)
    beans = Part((110, 40, 30), None, outline=False)
    for (x, y) in ((24, 40), (30, 39), (36, 41), (41, 39)): beans.ellipse(x, y, 1.6, 1.1)
    cheese = Part((250, 210, 70), None, outline=False)
    for (x, y) in ((27, 38), (34, 38), (39, 41)): cheese.line(x, y, x + 2, y - 1)
    pepper = Part((220, 30, 30), (160, 20, 20)).tube([(42, 22), (48, 18), (52, 24)], 2)
    return [b, chili, beans, cheese, pepper, steam((26, 34), 30, 7)]


# ---------- BOLS I SOPES ----------

def escudella():
    b = bowl((235, 225, 210))
    broth = Part((235, 190, 110), None, outline=False).ellipse(32, 40, 18, 4.5)
    pilota = Part((150, 90, 60), (110, 60, 40)).ellipse(26, 39, 5, 3)
    galets = Part((250, 235, 180), (210, 185, 120))
    for (x, y) in ((36, 39), (42, 41), (33, 42)): galets.ellipse(x, y, 2.4, 1.6)
    carrot = Part((240, 130, 40), None, outline=False).rect(38, 37, 41, 38)
    return [b, broth, pilota, galets, carrot, steam((24, 32, 40))]


def gaspatxo():
    b = bowl((245, 245, 240))
    soup = Part((230, 80, 50), None, hi=(250, 140, 100), outline=False).ellipse(32, 40, 18, 4.5)
    cucumber = Part((120, 180, 70), (80, 130, 40))
    for (x, y) in ((26, 40), (34, 39), (40, 41)): cucumber.rect(x, y - 1, x + 2, y + 1)
    oil = Part((250, 220, 90), None, outline=False).put(30, 41).put(37, 40)
    ice = Part((220, 240, 255), (170, 210, 235)).rect(44, 18, 50, 24).rect(14, 20, 19, 25)
    return [b, soup, cucumber, oil, ice]


def fondue():
    pot = Part((200, 60, 40), (150, 40, 25), hi=(240, 120, 90))
    pot.ellipse(32, 42, 18, 12)
    pot.remove(lambda x, y: y < 36)
    pot.ellipse(32, 36, 18, 5)
    cheese = Part((250, 210, 80), None, hi=(255, 240, 160), outline=False).ellipse(32, 36, 15, 3.5)
    stand = Part((60, 55, 55), None).rect(18, 52, 46, 55)
    flame = Part((250, 140, 40), None).ellipse(32, 57, 4, 2.5)
    fork = Part((200, 205, 215), None).line(42, 34, 52, 12)
    bread = Part((220, 170, 90), (180, 130, 60)).rect(49, 8, 55, 13)
    drip = Part((250, 210, 80), None, outline=False).line(44, 32, 44, 36)
    return [stand, flame, pot, cheese, fork, bread, drip]


def sorbet():
    glass = Part((230, 240, 245), None)
    glass.ellipse(32, 36, 15, 10)
    glass.remove(lambda x, y: y < 34)
    stem = Part((230, 240, 245), None).rect(31, 44, 33, 54).ellipse(32, 56, 9, 2.5)
    ps = [stem, glass]
    for (x, y) in ((24, 30), (38, 30), (31, 23)):
        s = Part((250, 240, 140), (225, 210, 90), hi=WHITE)
        s.ellipse(x, y, 8, 7)
        ps.append(s)
    leaf = Part((80, 160, 70), (50, 120, 40)).ellipse(42, 16, 5, 2.5)
    lemon = Part((250, 225, 60), (220, 190, 30)).ellipse(20, 18, 4, 4)
    return ps + [leaf, lemon]


def crema_catalana():
    dish = Part((210, 120, 70), (160, 85, 45), hi=(240, 160, 110))
    dish.ellipse(32, 40, 24, 12)
    cream = Part((250, 220, 120), None, outline=False).ellipse(32, 38, 19, 8)
    sugar = Part((200, 120, 40), None, hi=(240, 170, 80), outline=False).ellipse(32, 37, 17, 6.5)
    crack = Part((140, 70, 20), None, outline=False)
    crack.line(22, 36, 30, 39).line(30, 39, 36, 35).line(36, 35, 42, 38)
    spoon = Part((210, 215, 225), None, hi=WHITE).ellipse(48, 22, 4, 3).line(46, 25, 38, 34)
    cinnamon = Part((150, 80, 40), None).rect(10, 18, 22, 20)
    return [dish, cream, sugar, crack, cinnamon, spoon]


def torrons():
    ps = []
    t1 = Part((240, 220, 170), (210, 185, 130), hi=(255, 245, 210))
    t1.rect(10, 30, 36, 46)
    alm = Part((200, 150, 90), None, outline=False)
    for (x, y) in ((14, 34), (22, 38), (30, 33), (18, 42), (28, 43)): alm.ellipse(x, y, 2, 1.4)
    t2 = Part((200, 150, 80), (160, 110, 50), hi=(230, 190, 120))
    t2.rect(28, 20, 54, 34)
    paper = Part((245, 240, 230), None).rect(26, 46, 56, 50)
    return [paper, t1, alm, t2]


def xurros():
    cup = Part((245, 245, 240), (205, 205, 212), hi=WHITE)
    cup.ellipse(22, 42, 12, 10)
    cup.remove(lambda x, y: y < 36)
    cup.rect(10, 32, 34, 36)
    choco = Part((90, 50, 30), None, outline=False).ellipse(22, 33, 10, 2.5)
    ps = [cup, choco]
    for k, (x0, y0) in enumerate(((30, 50), (36, 48), (42, 46))):
        x = Part((220, 150, 60), (180, 110, 40), hi=(245, 200, 120))
        x.tube([(x0, y0), (x0 + 8, y0 - 18), (x0 + 12, y0 - 34)], 2.4)
        ridge = Part((180, 110, 40), None, outline=False)
        for t in range(0, 30, 4): ridge.put(x0 + 1 + t * 0.35, y0 - 2 - t)
        ps += [x, ridge]
    sugar = Part((255, 255, 255), None, outline=False)
    for (x, y) in ((34, 30), (41, 26), (46, 20), (38, 38)): sugar.put(x, y)
    return ps + [sugar]


# ---------- AMPOLLES I ALTRES ----------

def oli():
    b = Part((170, 190, 60), (120, 140, 30), hi=(220, 235, 130))
    b.rect(24, 26, 40, 58)
    b.ellipse(32, 26, 8, 6)
    b.rect(29, 10, 35, 22)
    cap = Part((40, 40, 45), None).rect(28, 6, 36, 11)
    label = Part((245, 235, 200), None, outline=False).rect(26, 38, 38, 50)
    olive = Part((90, 120, 40), None, outline=False).ellipse(32, 44, 3, 2.4)
    leaf = Part((60, 110, 40), None, outline=False).line(34, 42, 37, 40)
    drop = Part((240, 210, 60), (200, 170, 30)).ellipse(48, 46, 3, 4)
    return [b, cap, label, olive, leaf, drop]


def ratafia():
    b = Part((90, 40, 30), (60, 25, 20), hi=(160, 90, 70))
    b.ellipse(32, 44, 13, 14)
    b.rect(28, 14, 36, 32)
    cork = Part((190, 140, 90), (150, 100, 60)).rect(28, 8, 36, 14)
    label = Part((245, 225, 170), None, outline=False).rect(23, 42, 41, 50)
    label.rect(26, 45, 38, 45, (160, 40, 40))
    glass = Part((230, 240, 245), None).rect(46, 44, 54, 56)
    shot = Part((120, 50, 30), None, outline=False).rect(47, 49, 53, 55)
    return [b, cork, label, glass, shot]


def safra():
    jar = Part((230, 240, 245), None, hi=WHITE)
    jar.rect(18, 26, 46, 54)
    lid = Part((200, 60, 50), (150, 40, 30)).rect(16, 20, 48, 26)
    threads = Part((220, 60, 30), None, outline=False)
    for (x, y) in ((24, 36), (30, 42), (36, 34), (40, 46), (26, 48), (34, 50)):
        threads.line(x, y, x + 3, y - 4)
    flower = Part((150, 90, 190), (110, 60, 150))
    for a in range(0, 360, 72):
        flower.ellipse(50 + math.cos(math.radians(a)) * 4, 12 + math.sin(math.radians(a)) * 4, 3, 3)
    center = Part((240, 120, 30), None, outline=False).ellipse(50, 12, 1.6, 1.6)
    return [jar, threads, lid, flower, center]


def menu_dia():
    board = Part((50, 60, 55), (35, 42, 38))
    board.rect(12, 8, 52, 50)
    frame = Part((150, 100, 60), (110, 70, 40))
    frame.rect(10, 6, 54, 8).rect(10, 50, 54, 52).rect(10, 6, 12, 52).rect(52, 6, 54, 52)
    chalk = Part((235, 235, 225), None, outline=False)
    chalk.rect(18, 14, 46, 15)
    for y in (22, 28, 34, 40):
        chalk.rect(16, y, 16, y).rect(20, y, 20 + (y * 7) % 22, y)
    legs = Part((150, 100, 60), None).line(16, 52, 12, 60).line(48, 52, 52, 60)
    return [legs, board, frame, chalk]


def menu_degustacio():
    ps = plate(cy=46, rx=27, ry=11)
    cloche = Part((215, 220, 230), (165, 170, 185), hi=WHITE)
    cloche.ellipse(32, 40, 20, 16)
    cloche.remove(lambda x, y: y > 40)
    knob = Part((215, 220, 230), (165, 170, 185)).ellipse(32, 22, 3, 2.5)
    rim = Part((190, 195, 205), None).rect(12, 40, 52, 42)
    stars = Part((250, 210, 60), None)
    for (x, y) in ((10, 14), (54, 16), (48, 6)):
        stars.put(x, y).put(x - 1, y).put(x + 1, y).put(x, y - 1).put(x, y + 1)
    return ps + [cloche, rim, knob, stars]


DISHES = {
    'botifarra_amb_mongetes': botifarra,
    'bunyols_de_bacalla': bunyols,
    'cafe_sol': cafe,
    'calamars_a_la_romana': calamars,
    'calcots_amb_salvitxada': calcots,
    'coca_de_recapte': coca_recapte,
    'crema_catalana': crema_catalana,
    'croquetes_cruixents': croquetes,
    'escudella_i_carn_d_olla': escudella,
    'fideua_picant': fideua,
    'fondue_de_formatge': fondue,
    'gaspatxo_fresquet': gaspatxo,
    'macarrons_amb_tomaquet': macarrons,
    'mandonguilles': mandonguilles,
    'menu_de_degustacio': menu_degustacio,
    'menu_del_dia': menu_dia,
    'oli_d_oliva_verge': oli,
    'olives_farcides': olives,
    'pa_amb_tomaquet': pa_tomaquet,
    'paella_mixta': paella,
    'panellets': panellets,
    'pollastre_al_curri': pollastre_curri,
    'ratafia': ratafia,
    'romesco': romesco,
    'safra': safra,
    'salsa_brava': brava,
    'sorbet_de_llimona': sorbet,
    'torrons_de_xixona': torrons,
    'xili_amb_carn': xili,
    'xurros_amb_xocolata': xurros,
}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    assets = os.path.normpath(os.path.join(here, '..', '..', 'Assets'))
    rows = []
    for name, fn in DISHES.items():
        im = to_image(composite(fn()))
        im.save(os.path.join(assets, 'Resources', 'Dishes', name + '.png'))
        rows.append((name, [im]))
    print('%d icones' % len(rows))
    if len(sys.argv) > 1:
        from characters import make_sheet
        make_sheet(rows, sys.argv[1], scale=3, cols=8)


if __name__ == '__main__':
    main()
