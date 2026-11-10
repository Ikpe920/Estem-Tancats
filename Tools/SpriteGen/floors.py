# -*- coding: utf-8 -*-
# =====================================================================
#  TERRES DE LES SALES NOVES (Resources/Actes/<nom>.png, 256x256, es repeteixen sense tall)
# =====================================================================
#   python3 Tools/SpriteGen/floors.py            -> Celler, Cambra i Plato
#   python3 Tools/SpriteGen/floors.py full.png   -> i una làmina per veure'ls
import os, sys, random
from PIL import Image, ImageDraw

N = 256


def jitter(c, r, k=10):
    j = r.randint(-k, k)  # el mateix canvi als tres canals: més clar o més fosc, sense canviar de color
    return tuple(max(0, min(255, v + j)) for v in c)


def celler():
    """Lloses de pedra fosca, irregulars, amb taques de vi."""
    r = random.Random(5)
    im = Image.new('RGB', (N, N), (40, 34, 32))
    d = ImageDraw.Draw(im)
    rows = [0, 40, 84, 124, 168, 212, 256]
    for i in range(len(rows) - 1):
        y0, y1 = rows[i], rows[i + 1]
        x = -r.randint(0, 40) if i % 2 else 0
        while x < N:
            w = r.randint(44, 76)
            base = jitter((92, 82, 76), r, 12)
            d.rectangle([x + 2, y0 + 2, x + w - 2, y1 - 2], fill=base)
            d.line([x + 3, y0 + 3, x + w - 3, y0 + 3], fill=jitter((112, 102, 94), r, 4))
            for _ in range(6):  # gra de la pedra
                px, py = r.randint(x + 4, x + w - 4), r.randint(y0 + 4, y1 - 4)
                d.point((px % N, py), fill=jitter((70, 62, 58), r, 6))
            x += w
    for _ in range(5):  # taques de vi
        cx, cy, rr = r.randint(0, N), r.randint(0, N), r.randint(6, 13)
        for dx in (-N, 0, N):
            for dy in (-N, 0, N):
                d.ellipse([cx + dx - rr, cy + dy - rr * 0.7, cx + dx + rr, cy + dy + rr * 0.7], fill=(92, 30, 44))
    return im


def cambra():
    """Planxa d'acer amb relleu de diamants i gebre a les vores."""
    r = random.Random(7)
    im = Image.new('RGB', (N, N), (150, 165, 178))
    d = ImageDraw.Draw(im)
    for y in range(0, N, 16):
        for x in range(0, N, 16):
            ox = 8 if (y // 16) % 2 else 0
            cx, cy = (x + ox) % N, y + 8
            for dx in (-N, 0, N):
                d.line([cx + dx - 4, cy + 3, cx + dx + 4, cy - 3], fill=(196, 210, 222), width=2)
                d.line([cx + dx - 3, cy + 4, cx + dx + 5, cy - 2], fill=(110, 124, 138))
    for x in range(0, N, 64):  # juntes de les planxes
        d.line([x, 0, x, N], fill=(96, 108, 120), width=2)
    for y in range(0, N, 128):
        d.line([0, y, N, y], fill=(96, 108, 120), width=2)
    for _ in range(8):  # gebre
        cx, cy, rr = r.randint(0, N), r.randint(0, N), r.randint(6, 14)
        for dx in (-N, 0, N):
            for dy in (-N, 0, N):
                d.ellipse([cx + dx - rr, cy + dy - rr * 0.6, cx + dx + rr, cy + dy + rr * 0.6], fill=(222, 236, 246))
                d.ellipse([cx + dx - rr * 0.5, cy + dy - rr * 0.3, cx + dx + rr * 0.5, cy + dy + rr * 0.3], fill=(244, 250, 255))
    return im


def plato():
    """Plató de televisió: escaquer blanc i negre, cinta adhesiva per marcar i cables."""
    r = random.Random(9)
    im = Image.new('RGB', (N, N), (30, 30, 34))
    d = ImageDraw.Draw(im)
    s = 32
    for y in range(0, N, s):
        for x in range(0, N, s):
            if ((x // s) + (y // s)) % 2 == 0:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=(222, 218, 210))
                d.line([x, y, x + s - 1, y], fill=(240, 236, 228))
            else:
                d.line([x, y, x + s - 1, y], fill=(48, 48, 54))
    for (x, y) in ((40, 60), (180, 150), (110, 220)):  # creus de cinta groga
        d.line([x - 8, y - 8, x + 8, y + 8], fill=(240, 200, 40), width=3)
        d.line([x - 8, y + 8, x + 8, y - 8], fill=(240, 200, 40), width=3)
    pts = [(0, 100), (60, 110), (120, 96), (190, 120), (256, 100)]  # un cable
    d.line(pts, fill=(20, 20, 24), width=3)
    return im


FLOORS = {'Celler': celler, 'Cambra': cambra, 'Plato': plato}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Actes'))
    ims = []
    for name, fn in FLOORS.items():
        im = fn()
        im.save(os.path.join(out, name + '.png'))
        ims.append(im)
    print('%d terres' % len(ims))
    if len(sys.argv) > 1:
        sheet = Image.new('RGB', (len(ims) * (N * 2 + 8), N * 2))
        for i, im in enumerate(ims):
            tile = Image.new('RGB', (N * 2, N * 2))
            for dx in (0, N):
                for dy in (0, N):
                    tile.paste(im, (dx, dy))
            sheet.paste(tile, (i * (N * 2 + 8), 0))
        sheet.save(sys.argv[1])


if __name__ == '__main__':
    main()
