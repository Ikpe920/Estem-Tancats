# -*- coding: utf-8 -*-
# =====================================================================
#  ICONES DELS PUNTS FEBLES (Resources/Weak/<nom>.png, 64x64): foc, gel, explosió i crític
# =====================================================================
#   python3 Tools/SpriteGen/weak_icons.py
import os, math
from PIL import Image, ImageDraw

S = 64
K = 4  # es dibuixa a 4x i es redueix (vores netes)
INK = (26, 18, 20, 255)


def canvas():
    im = Image.new('RGBA', (S * K, S * K), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def done(im):
    return im.resize((S, S), Image.LANCZOS)


def poly(d, pts, fill, w=5):
    pts = [(x * K, y * K) for x, y in pts]
    d.polygon(pts, fill=fill)
    d.line(pts + [pts[0]], fill=INK, width=w * K // 2, joint='curve')


def fire():
    im, d = canvas()
    outer = [(32, 4), (40, 18), (50, 24), (52, 40), (46, 54), (32, 60), (18, 54), (12, 40), (16, 28), (22, 32), (24, 18)]
    poly(d, outer, (236, 96, 36, 255))
    inner = [(32, 22), (38, 34), (42, 44), (38, 54), (32, 56), (26, 54), (22, 44), (27, 38)]
    poly(d, inner, (252, 206, 72, 255), 3)
    return done(im)


def ice():
    im, d = canvas()
    c = 32 * K
    for i in range(6):
        a = math.radians(i * 60 + 90)
        x, y = c + math.cos(a) * 26 * K, c - math.sin(a) * 26 * K
        d.line([c, c, x, y], fill=INK, width=9 * K)
        for t in (0.55,):
            bx, by = c + math.cos(a) * 26 * K * t, c - math.sin(a) * 26 * K * t
            for s in (-1, 1):
                b = a + s * math.radians(40)
                d.line([bx, by, bx + math.cos(b) * 9 * K, by - math.sin(b) * 9 * K], fill=INK, width=7 * K)
    for i in range(6):
        a = math.radians(i * 60 + 90)
        x, y = c + math.cos(a) * 25 * K, c - math.sin(a) * 25 * K
        d.line([c, c, x, y], fill=(170, 225, 255, 255), width=4 * K)
        bx, by = c + math.cos(a) * 26 * K * 0.55, c - math.sin(a) * 26 * K * 0.55
        for s in (-1, 1):
            b = a + s * math.radians(40)
            d.line([bx, by, bx + math.cos(b) * 8 * K, by - math.sin(b) * 8 * K], fill=(170, 225, 255, 255), width=3 * K)
    d.ellipse([c - 6 * K, c - 6 * K, c + 6 * K, c + 6 * K], fill=(235, 248, 255, 255), outline=INK, width=2 * K)
    return done(im)


def explosion():
    im, d = canvas()
    pts = []
    for i in range(20):
        r = 29 if i % 2 == 0 else 17
        a = math.radians(i * 18 - 90)
        pts.append((32 + math.cos(a) * r, 33 + math.sin(a) * r))
    poly(d, pts, (226, 62, 44, 255))
    pts2 = []
    for i in range(16):
        r = 17 if i % 2 == 0 else 10
        a = math.radians(i * 22.5 - 80)
        pts2.append((32 + math.cos(a) * r, 33 + math.sin(a) * r))
    poly(d, pts2, (252, 196, 64, 255), 3)
    return done(im)


def crit():
    im, d = canvas()
    pts = []
    for i in range(10):
        r = 29 if i % 2 == 0 else 12
        a = math.radians(i * 36 - 90)
        pts.append((32 + math.cos(a) * r, 34 + math.sin(a) * r))
    poly(d, pts, (250, 210, 60, 255))
    d.line([25 * K, 26 * K, 29 * K, 30 * K], fill=(255, 248, 210, 255), width=3 * K)
    return done(im)


ICONS = {'Fire': fire, 'Ice': ice, 'Explosion': explosion, 'Crit': crit}

if __name__ == '__main__':
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Weak'))
    os.makedirs(out, exist_ok=True)
    sheet = Image.new('RGBA', (S * 4 + 24, S), (60, 40, 44, 255))
    for i, (n, fn) in enumerate(ICONS.items()):
        im = fn()
        im.save(os.path.join(out, n + '.png'))
        sheet.alpha_composite(im, (i * (S + 8), 0))
    if os.environ.get('SHEET'):  # SHEET=fitxer.png per veure-les totes
        sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(os.environ['SHEET'])
    print('fet')
