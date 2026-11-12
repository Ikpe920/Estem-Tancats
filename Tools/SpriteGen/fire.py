# -*- coding: utf-8 -*-
# =====================================================================
#  EL FOC DE DIBUIX ANIMAT (Resources/Fx/...): flama en 6 fotogrames, espurna, fum i cremat
# =====================================================================
#   python3 Tools/SpriteGen/fire.py             -> genera els sprites
#   python3 Tools/SpriteGen/fire.py full.png    -> i una làmina per veure'ls
# Estil anys 30: contorn negre gruixut, colors plans (vermell, taronja, groc i un cor clar),
# i les llengües de foc que ballen d'un fotograma a l'altre (com una animació feta a mà).
import os, sys, math, random
from PIL import Image, ImageDraw, ImageFilter

S = 64           # mida final
K = 4            # es dibuixa a 4x i es redueix
FRAMES = 6
INK = (24, 14, 14, 255)
RED = (214, 52, 34, 255)
ORANGE = (246, 128, 36, 255)
YELLOW = (255, 214, 72, 255)
CORE = (255, 248, 200, 255)


def tongue(cx, base, h, r, sway, bend=1.0):
    """Una llengua de foc: bombolla a baix i punta que es corba. Torna els punts del contorn."""
    pts = []
    n = 28
    for i in range(n + 1):  # costat esquerre, de baix a dalt
        s = i / n
        w = r * (1 - s) ** 0.9 * min(1.0, (s + 0.35) / 0.55)
        c = cx + sway * s ** 2 * bend
        pts.append((c - w, base - s * h))
    for i in range(n, -1, -1):  # costat dret, de dalt a baix
        s = i / n
        w = r * (1 - s) ** 0.9 * min(1.0, (s + 0.35) / 0.55)
        c = cx + sway * s ** 2 * bend
        pts.append((c + w, base - s * h))
    # el cul arrodonit
    for i in range(1, 12):
        a = math.pi * i / 12
        pts.append((cx + r * 0.95 * math.cos(a), base + r * 0.55 * math.sin(a)))
    return [(x * K, y * K) for x, y in pts]


def layer(shapes, color):
    im = Image.new('L', (S * K, S * K), 0)
    d = ImageDraw.Draw(im)
    for p in shapes:
        d.polygon(p, fill=255)
    return im, color


def flame(f):
    """El fotograma f (0..FRAMES-1) de la flama."""
    ph = f / FRAMES * 2 * math.pi
    base = 54
    # tres llengües: la del mig, alta, i dues de petites que ballen a contratemps
    def tongues(scale):
        sw = 7 * scale
        return [
            tongue(32 + 1.5 * math.sin(ph), base, 46 * scale * (0.92 + 0.08 * math.sin(ph * 2)), 15 * scale, sw * math.sin(ph)),
            tongue(22 + 1.0 * math.sin(ph + 2), base + 1, 28 * scale * (0.85 + 0.15 * math.sin(ph + 1.7)), 9 * scale, -sw * 0.9 * math.sin(ph + 2.1)),
            tongue(42 + 1.0 * math.sin(ph + 4), base + 1, 32 * scale * (0.85 + 0.15 * math.sin(ph + 3.4)), 9.5 * scale, sw * 0.9 * math.sin(ph + 4.2)),
        ]
    outer = tongues(1.0)
    mid = [tongue(32 + 1.5 * math.sin(ph), base, 36 * (0.9 + 0.1 * math.sin(ph * 2 + 1)), 11, 5 * math.sin(ph + 0.6)),
           tongue(24, base, 18 * (0.85 + 0.15 * math.sin(ph + 1.7)), 6, -4 * math.sin(ph + 2.1)),
           tongue(40, base, 21 * (0.85 + 0.15 * math.sin(ph + 3.4)), 6.5, 4 * math.sin(ph + 4.2))]
    inner = [tongue(32 + math.sin(ph), base, 24 * (0.88 + 0.12 * math.sin(ph * 2 + 2)), 7.5, 3 * math.sin(ph + 1.1))]
    core = [tongue(32 + math.sin(ph), base, 12, 4.5, 1.5 * math.sin(ph + 1.5))]

    out = Image.new('RGBA', (S * K, S * K), (0, 0, 0, 0))
    m, _ = layer(outer, None)
    ink = m.filter(ImageFilter.MaxFilter(13))  # el contorn negre
    out.paste(Image.new('RGBA', out.size, INK), (0, 0), ink)
    for shapes, col in ((outer, RED), (mid, ORANGE), (inner, YELLOW), (core, CORE)):
        mm, _ = layer(shapes, col)
        out.paste(Image.new('RGBA', out.size, col), (0, 0), mm)
    # una lluentor blanca a la llengua del mig
    d = ImageDraw.Draw(out)
    hx, hy = (29 + math.sin(ph)) * K, 34 * K
    d.ellipse([hx - 1.6 * K, hy - 3 * K, hx + 1.6 * K, hy + 3 * K], fill=(255, 255, 240, 255))
    return out.resize((S, S), Image.LANCZOS)


def star():
    """Espurna: una estrella de dibuix animat."""
    im = Image.new('RGBA', (S * K, S * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pts = []
    for i in range(8):
        r = 28 if i % 2 == 0 else 10
        a = math.radians(i * 45 - 90)
        pts.append(((32 + math.cos(a) * r) * K, (32 + math.sin(a) * r) * K))
    d.polygon(pts, fill=YELLOW)
    d.line(pts + [pts[0]], fill=INK, width=5 * K, joint='curve')
    d.ellipse([28 * K, 28 * K, 36 * K, 36 * K], fill=CORE)
    return im.resize((S, S), Image.LANCZOS)


def smoke():
    """Fum: un núvol arrodonit amb contorn (es tenyeix gris des del codi)."""
    im = Image.new('RGBA', (S * K, S * K), (0, 0, 0, 0))
    m = Image.new('L', im.size, 0)
    d = ImageDraw.Draw(m)
    for (x, y, r) in ((24, 36, 13), (38, 34, 14), (31, 24, 12), (44, 42, 9), (19, 44, 8)):
        d.ellipse([(x - r) * K, (y - r) * K, (x + r) * K, (y + r) * K], fill=255)
    ink = m.filter(ImageFilter.MaxFilter(11))
    im.paste(Image.new('RGBA', im.size, INK), (0, 0), ink)
    im.paste(Image.new('RGBA', im.size, (236, 232, 226, 255)), (0, 0), m)
    d2 = ImageDraw.Draw(im)
    for (x, y, r) in ((27, 21, 4), (36, 30, 3)):  # lluentors
        d2.ellipse([(x - r) * K, (y - r) * K, (x + r) * K, (y + r) * K], fill=(255, 255, 255, 255))
    return im.resize((S, S), Image.LANCZOS)


def scorch():
    """Marca de cremat a terra: taca de carbó irregular amb brases."""
    r = random.Random(3)
    im = Image.new('RGBA', (S * K, S * K), (0, 0, 0, 0))
    m = Image.new('L', im.size, 0)
    d = ImageDraw.Draw(m)
    pts = []
    for i in range(18):
        a = i / 18 * 2 * math.pi
        rr = r.uniform(22, 30)
        pts.append(((32 + math.cos(a) * rr) * K, (32 + math.sin(a) * rr * 0.8) * K))
    d.polygon(pts, fill=200)
    m = m.filter(ImageFilter.GaussianBlur(5 * K))
    im.paste(Image.new('RGBA', im.size, (30, 18, 14, 255)), (0, 0), m)
    d2 = ImageDraw.Draw(im)
    for _ in range(7):
        x, y = r.uniform(18, 46), r.uniform(22, 42)
        d2.ellipse([(x - 1.6) * K, (y - 1.6) * K, (x + 1.6) * K, (y + 1.6) * K], fill=(255, 120, 40, 230))
    return im.resize((S, S), Image.LANCZOS)


def build():
    out = {}
    for f in range(FRAMES):
        out['Flame_%d' % f] = flame(f)
    out['Ember'] = star()
    out['Smoke'] = smoke()
    out['Scorch'] = scorch()
    return out


def sheet(ims, path):
    """Làmina: els fotogrames grans, i una escena d'exemple a sobre de terra de la sala."""
    Z = 3
    W = S * Z * FRAMES + 20 * (FRAMES + 1)
    sh = Image.new('RGBA', (W, S * Z * 2 + 80), (58, 40, 38, 255))
    for f in range(FRAMES):
        sh.alpha_composite(ims['Flame_%d' % f].resize((S * Z, S * Z), Image.NEAREST), (20 + f * (S * Z + 20), 20))
    y = S * Z + 50
    x = 20
    for n in ('Ember', 'Smoke', 'Scorch'):
        sh.alpha_composite(ims[n].resize((S * 2, S * 2), Image.NEAREST), (x, y + 20))
        x += S * 2 + 20
    # una zona de foc: cremat a terra i flames de mides i fotogrames diferents
    floor = Image.new('RGBA', (W - x - 20, S * Z), (150, 120, 90, 255))
    fd = ImageDraw.Draw(floor)
    for i in range(0, floor.width, 48):
        fd.line([i, 0, i, floor.height], fill=(120, 94, 70, 255), width=3)
    sc = ims['Scorch'].resize((300, 220), Image.LANCZOS)
    floor.alpha_composite(sc, (floor.width // 2 - 150, floor.height // 2 - 90))
    rnd = random.Random(1)
    for i in range(9):
        sz = rnd.randint(70, 130)
        fl = ims['Flame_%d' % rnd.randint(0, FRAMES - 1)].resize((sz, sz), Image.LANCZOS)
        floor.alpha_composite(fl, (floor.width // 2 - 160 + rnd.randint(0, 260), floor.height // 2 - sz + rnd.randint(10, 60)))
    for i in range(3):
        st = ims['Ember'].resize((28, 28), Image.LANCZOS)
        floor.alpha_composite(st, (floor.width // 2 - 80 + i * 70, 20 + i * 12))
    sm = ims['Smoke'].resize((90, 90), Image.LANCZOS)
    tint = Image.new('RGBA', sm.size, (120, 112, 108, 0))
    floor.alpha_composite(sm, (floor.width // 2 + 70, 0))
    sh.alpha_composite(floor, (x, y))
    sh.save(path)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Fx'))
    os.makedirs(out, exist_ok=True)
    ims = build()
    for n, im in ims.items():
        im.save(os.path.join(out, n + '.png'))
    print('%d sprites de foc' % len(ims))
    if len(sys.argv) > 1:
        sheet(ims, sys.argv[1])


if __name__ == '__main__':
    main()
