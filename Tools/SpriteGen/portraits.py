# -*- coding: utf-8 -*-
# =====================================================================
#  RETRATS GRANS DELS XEFS (Resources/Portraits/<xef>[_V1|_V2].png, 256x256)
# =====================================================================
# Un medalló de cartell dels anys 30: raigs de sol del color del xef, anella daurada amb contorn
# negre, i el xef (el sprite de characters.py) ben gran al mig. Un per cada vestit.
#   python3 Tools/SpriteGen/portraits.py            -> escriu els PNG
#   python3 Tools/SpriteGen/portraits.py full.png   -> i una làmina per veure'ls
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw
from characters import CHARACTERS, OUTFITS, CHEF_NAMES, character, to_image

S = 256
INK = (24, 14, 14, 255)
GOLD = (236, 190, 70, 255)
GOLD_D = (170, 120, 40, 255)

# el color de fons de cada xef (el mateix to que la seva carta)
TONE = {'Chef': (235, 225, 205), 'Carnisser': (210, 90, 80), 'Pastisser': (240, 150, 185), 'Sushiman': (90, 110, 200), 'Pizzer': (90, 170, 100)}


def medallion(name, ch):
    tone = TONE.get(name, (200, 200, 200))
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    mask = Image.new('L', (S, S), 0)
    ImageDraw.Draw(mask).ellipse([14, 14, S - 14, S - 14], fill=255)
    bg = Image.new('RGBA', (S, S), tone + (255,))
    d = ImageDraw.Draw(bg)
    light = tuple(min(255, int(v * 1.15) + 20) for v in tone) + (255,)
    c = S / 2
    for i in range(18):  # els raigs de sol
        a0 = math.radians(i * 20)
        a1 = math.radians(i * 20 + 10)
        d.polygon([(c, c), (c + math.cos(a0) * 200, c + math.sin(a0) * 200), (c + math.cos(a1) * 200, c + math.sin(a1) * 200)], fill=light)
    # el xef, ben gran (3x), i una ombra a terra
    d.ellipse([c - 60, 206, c + 60, 228], fill=tuple(int(v * 0.7) for v in tone) + (255,))
    sprite = to_image(character(ch, 'front', 0)).resize((192, 192), Image.NEAREST)
    bg.alpha_composite(sprite, (int(c - 96), 32))
    im.paste(bg, (0, 0), mask)
    # l'anella daurada amb contorn negre
    dd = ImageDraw.Draw(im)
    dd.ellipse([6, 6, S - 6, S - 6], outline=INK, width=6)
    dd.ellipse([11, 11, S - 11, S - 11], outline=GOLD, width=7)
    dd.ellipse([17, 17, S - 17, S - 17], outline=GOLD_D, width=2)
    for i in range(12):  # els reblons de l'anella
        a = math.radians(i * 30)
        x, y = c + math.cos(a) * (c - 14), c + math.sin(a) * (c - 14)
        dd.ellipse([x - 4, y - 4, x + 4, y + 4], fill=(255, 235, 150, 255), outline=INK, width=2)
    return im


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Portraits'))
    os.makedirs(out, exist_ok=True)
    ims = []
    for n in CHEF_NAMES:
        for suffix, over in [('', {})] + sorted(OUTFITS.get(n, {}).items()):
            im = medallion(n, dict(CHARACTERS[n], **over))
            im.save(os.path.join(out, n + suffix + '.png'))
            ims.append(im)
    print('%d retrats' % len(ims))
    if len(sys.argv) > 1:
        cols = 3
        rows = (len(ims) + cols - 1) // cols
        sh = Image.new('RGBA', (cols * (S + 10) + 10, rows * (S + 10) + 10), (58, 40, 38, 255))
        for i, im in enumerate(ims):
            sh.alpha_composite(im, (10 + (i % cols) * (S + 10), 10 + (i // cols) * (S + 10)))
        sh.resize((sh.width // 2, sh.height // 2)).save(sys.argv[1])


if __name__ == '__main__':
    main()
