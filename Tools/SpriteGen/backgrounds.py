# -*- coding: utf-8 -*-
# =====================================================================
#  FONS DE LES PANTALLES FINALS (Resources/Backgrounds/<nom>.png, 960x540)
# =====================================================================
# La façana del restaurant, amb el traç dels anys 30 (contorn negre i colors plans):
#   GameOver  de nit: plou, el cartell diu TANCAT, els clients s'han quedat el local
#   Win       a trenc d'alba: surt el sol, el servei s'ha acabat i el cartell ja diu TANCAT
#   WinPorta  el mateix sense cartell (l'escena final anima el xef girant-lo)
#   CartellObert / CartellTancat  el cartell de la porta, per les dues cares (192x96)
#   python3 Tools/SpriteGen/backgrounds.py            -> escriu els PNG
#   python3 Tools/SpriteGen/backgrounds.py full.png   -> i una làmina per veure'ls
import os, sys, math, random
from PIL import Image, ImageDraw, ImageFont

W, H = 960, 540
INK = (24, 14, 14)


def font(size):
    for p in ('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf', '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',
              '/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf'):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def box(d, x0, y0, x1, y1, fill, w=4):
    d.rectangle([x0, y0, x1, y1], fill=fill, outline=INK, width=w)


def text_c(d, cx, cy, txt, size, fill, stroke=3):
    f = font(size)
    bb = d.textbbox((0, 0), txt, font=f, stroke_width=stroke)
    d.text((cx - (bb[2] - bb[0]) / 2 - bb[0], cy - (bb[3] - bb[1]) / 2 - bb[1]), txt, font=f, fill=fill, stroke_width=stroke, stroke_fill=INK)


def sky(im, top, bottom):
    d = ImageDraw.Draw(im)
    for y in range(H):
        k = y / H
        d.line([(0, y), (W, y)], fill=tuple(int(top[i] + (bottom[i] - top[i]) * k) for i in range(3)))


def facade(d, night, r):
    # la paret de maons
    wall = (150, 70, 56) if night else (196, 100, 84)
    mortar = (96, 44, 36) if night else (140, 66, 52)
    box(d, 120, 110, 840, 470, wall, 5)
    for j, y in enumerate(range(114, 466, 18)):
        d.line([(124, y), (836, y)], fill=mortar, width=2)
        off = 0 if j % 2 else 22
        for x in range(124 + off, 836, 44):
            d.line([(x, y), (x, min(y + 18, 466))], fill=mortar, width=2)
    # el tendal de ratlles
    stripes = ((200, 40, 40), (240, 232, 214)) if not night else ((130, 30, 34), (170, 160, 148))
    n = 12
    for i in range(n):
        x0 = 130 + i * (700 / n)
        d.polygon([(x0, 170), (x0 + 700 / n, 170), (x0 + 700 / n + 6, 214), (x0 + 6, 214)], fill=stripes[i % 2], outline=INK)
    for i in range(n):  # les puntes del tendal
        x0 = 136 + i * (700 / n)
        d.pieslice([x0, 198, x0 + 700 / n, 230], 0, 180, fill=stripes[i % 2], outline=INK, width=3)
    # el rètol
    box(d, 300, 116, 660, 166, (40, 30, 26), 5)
    text_c(d, 480, 141, 'ESTEM TANCATS', 30, (255, 214, 72) if not night else (255, 120, 90), 3)
    # les finestres
    lit = (255, 214, 120) if night else (190, 225, 240)
    for x0 in (170, 610):
        box(d, x0, 250, x0 + 180, 380, lit, 5)
        d.line([(x0 + 90, 250), (x0 + 90, 380)], fill=INK, width=5)
        d.line([(x0, 315), (x0 + 180, 315)], fill=INK, width=5)
        box(d, x0 - 8, 380, x0 + 188, 394, (110, 70, 50), 4)  # l'ampit
    # la porta
    box(d, 420, 240, 540, 470, (110, 64, 44), 5)
    box(d, 436, 256, 524, 340, lit, 4)
    d.ellipse([520, 360, 532, 372], fill=(220, 180, 70), outline=INK, width=3)
    # el terra
    box(d, -10, 470, W + 10, H + 10, (90, 84, 90) if night else (170, 160, 150), 5)
    for y in range(486, H, 22):
        for x in range(-20 + (y % 44), W, 44):
            d.rounded_rectangle([x, y, x + 38, y + 16], 6, outline=(70, 64, 70) if night else (140, 130, 120), width=2)


def silhouettes(d, r):
    """Els clients que s'han quedat el restaurant: ombres a les finestres."""
    for x0 in (170, 610):
        for k in range(3):
            cx = x0 + 30 + k * 60 + r.randint(-8, 8)
            d.ellipse([cx - 16, 300, cx + 16, 332], fill=(60, 34, 30))
            d.rectangle([cx - 24, 330, cx + 24, 380], fill=(60, 34, 30))
            if r.random() < 0.6:  # una forquilla enlaire
                d.line([(cx + 22, 334), (cx + 34, 296)], fill=(60, 34, 30), width=5)


def sign(d, cx, top, txt, col):
    d.line([(cx - 30, top - 16), (cx, top - 40), (cx + 30, top - 16)], fill=INK, width=3)
    box(d, cx - 48, top - 16, cx + 48, top + 22, (245, 235, 205), 4)
    text_c(d, cx, top + 3, txt, 20, col, 2)


def gameover():
    r = random.Random(7)
    im = Image.new('RGB', (W, H))
    sky(im, (14, 16, 40), (52, 44, 80))
    d = ImageDraw.Draw(im)
    for _ in range(40):  # estrelles
        x, y = r.randint(0, W), r.randint(0, 110)
        d.point((x, y), fill=(240, 240, 220))
    d.ellipse([60, 26, 130, 96], fill=(245, 240, 205), outline=INK, width=4)  # la lluna
    d.ellipse([82, 26, 146, 90], fill=(14, 16, 40))
    facade(d, True, r)
    silhouettes(d, r)
    sign(d, 480, 280, 'TANCAT', (200, 40, 40))
    # un fanal
    box(d, 880, 220, 892, 470, (40, 40, 48), 3)
    box(d, 862, 196, 910, 228, (255, 214, 120), 4)
    # el barret del xef abandonat a terra
    d.ellipse([200, 488, 268, 510], fill=(240, 240, 236), outline=INK, width=4)
    d.ellipse([210, 456, 258, 500], fill=(250, 250, 248), outline=INK, width=4)
    # la pluja
    for _ in range(260):
        x, y = r.randint(-40, W), r.randint(-20, H)
        d.line([(x, y), (x + 6, y + 18)], fill=(150, 170, 210), width=2)
    return im


def win(with_sign=True):
    r = random.Random(9)
    im = Image.new('RGB', (W, H))
    sky(im, (250, 170, 110), (255, 230, 170))
    d = ImageDraw.Draw(im)
    # el sol que surt darrere el restaurant, amb raigs
    for i in range(16):
        a = math.radians(i * 22.5)
        d.polygon([(480, 110), (480 + math.cos(a - 0.08) * 700, 110 + math.sin(a - 0.08) * 700),
                   (480 + math.cos(a + 0.08) * 700, 110 + math.sin(a + 0.08) * 700)], fill=(255, 205, 120))
    d.ellipse([400, 30, 560, 190], fill=(255, 214, 72), outline=INK, width=5)
    facade(d, False, r)
    if with_sign:
        sign(d, 480, 280, 'TANCAT', (200, 40, 40))
    # garlandes de banderetes
    cols = [(200, 40, 40), (255, 214, 72), (60, 140, 200), (60, 160, 80)]
    for (x0, x1, sag) in ((130, 480, 40), (480, 840, 40)):
        pts = [(x0 + (x1 - x0) * t / 20, 230 + math.sin(math.pi * t / 20) * sag) for t in range(21)]
        d.line(pts, fill=INK, width=3)
        for i in range(0, 20, 2):
            (ax, ay), (bx, by) = pts[i], pts[i + 1]
            d.polygon([(ax, ay), (bx, by), ((ax + bx) / 2, ay + 22)], fill=cols[(i // 2) % 4], outline=INK)
    # confeti
    for _ in range(140):
        x, y = r.randint(0, W), r.randint(0, H)
        c = cols[r.randint(0, 3)]
        a = r.uniform(0, math.pi)
        d.line([(x, y), (x + math.cos(a) * 8, y + math.sin(a) * 8)], fill=c, width=4)
    return im


def door_sign(txt, col):
    im = Image.new('RGBA', (192, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([4, 4, 188, 92], 10, fill=(245, 235, 205), outline=INK, width=6)
    d.rounded_rectangle([14, 14, 178, 82], 6, outline=col, width=3)
    text_c(d, 96, 50, txt, 40, col, 3)
    return im


BACKGROUNDS = {'GameOver': gameover, 'Win': win, 'WinPorta': lambda: win(False),
               'CartellObert': lambda: door_sign('OBERT', (40, 140, 60)), 'CartellTancat': lambda: door_sign('TANCAT', (200, 40, 40))}


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Backgrounds'))
    os.makedirs(out, exist_ok=True)
    ims = []
    for n, fn in BACKGROUNDS.items():
        im = fn()
        im.save(os.path.join(out, n + '.png'))
        ims.append(im)
    print('%d fons' % len(ims))
    if len(sys.argv) > 1:
        sh = Image.new('RGB', (W, H * len(ims) + 10 * (len(ims) - 1)), (0, 0, 0))
        for i, im in enumerate(ims):
            sh.paste(im, (0, i * (H + 10)), im if im.mode == 'RGBA' else None)
        sh.save(sys.argv[1])


if __name__ == '__main__':
    main()
