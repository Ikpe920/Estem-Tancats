# -*- coding: utf-8 -*-
# =====================================================================
#  GEL I EXPLOSIONS DE DIBUIX ANIMAT (Resources/Fx/...)
# =====================================================================
# El mateix estil que el foc (fire.py): contorn negre gruixut i colors plans.
#   IceSpike_0..3  vidres de gel que surten de terra (4 fotogrames, es reprodueixen un cop)
#   IceShard       una estella de gel (per a les esquerdes que surten disparades)
#   Snow           un floc de neu
#   Boom_0..5      l'explosió (6 fotogrames): flaix, bola de foc, foc i fum, fum que s'esvaeix
#   Bum            el rètol "BUM!" de còmic
#   python3 Tools/SpriteGen/ice_boom.py            -> genera els sprites
#   python3 Tools/SpriteGen/ice_boom.py full.png   -> i una làmina per veure'ls
import os, sys, math, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont

S = 64
K = 4
INK = (24, 14, 14, 255)
ICE_D = (90, 160, 220, 255)
ICE = (150, 210, 245, 255)
ICE_L = (215, 242, 255, 255)
WHITE = (255, 255, 255, 255)
RED = (214, 52, 34, 255)
ORANGE = (246, 128, 36, 255)
YELLOW = (255, 214, 72, 255)
CORE = (255, 248, 200, 255)
SMOKE = (120, 110, 106, 255)
SMOKE_L = (170, 160, 154, 255)


def new(size=S):
    return Image.new('RGBA', (size * K, size * K), (0, 0, 0, 0))


def down(im, size=S):
    return im.resize((size, size), Image.LANCZOS)


def mask_of(polys, size=S, ellipses=()):
    m = Image.new('L', (size * K, size * K), 0)
    d = ImageDraw.Draw(m)
    for p in polys:
        d.polygon([(x * K, y * K) for x, y in p], fill=255)
    for (x, y, rx, ry) in ellipses:
        d.ellipse([(x - rx) * K, (y - ry) * K, (x + rx) * K, (y + ry) * K], fill=255)
    return m


def paint(im, m, col):
    im.paste(Image.new('RGBA', im.size, col), (0, 0), m)


def outline(im, m, w=11):
    paint(im, m.filter(ImageFilter.MaxFilter(w)), INK)


# ---------- GEL ----------

def crystal(cx, base, h, w, lean):
    """Un vidre de gel: un prisma amb punta, una mica inclinat."""
    tipx = cx + lean
    return [(cx - w, base), (cx - w * 0.9 + lean * 0.6, base - h * 0.65), (tipx, base - h),
            (cx + w * 0.9 + lean * 0.6, base - h * 0.65), (cx + w, base)]


SPIKES = [(32, 56, 44, 7, 0), (20, 56, 30, 6, -6), (44, 56, 32, 6, 7), (12, 56, 16, 4, -6), (52, 56, 18, 4, 6)]


def ice_spike(f):
    grow = (0.35, 0.65, 1.0, 1.0)[f]
    im = new()
    polys = [crystal(cx, b, h * grow, w * (0.7 + 0.3 * grow), l * grow) for (cx, b, h, w, l) in SPIKES]
    m = mask_of(polys)
    outline(im, m)
    paint(im, m, ICE)
    # la cara il·luminada de cada vidre (la meitat esquerra) i la línia del mig
    d = ImageDraw.Draw(im)
    for (cx, b, h, w, l) in SPIKES:
        hh, ww, ll = h * grow, w * (0.7 + 0.3 * grow), l * grow
        left = [(cx - ww, b), (cx - ww * 0.9 + ll * 0.6, b - hh * 0.65), (cx + ll, b - hh), (cx + ll * 0.3, b)]
        d.polygon([(x * K, y * K) for x, y in left], fill=ICE_L)
        d.line([((cx + ll * 0.3) * K, b * K), ((cx + ll) * K, (b - hh) * K)], fill=ICE_D, width=K * 2)
    if f == 3:  # l'últim: s'esquerda
        r = random.Random(4)
        for _ in range(5):
            x = r.uniform(18, 46); y = r.uniform(30, 50)
            d.line([(x * K, y * K), ((x + r.uniform(-4, 4)) * K, (y + r.uniform(3, 7)) * K)], fill=INK, width=K * 2)
    # la neu a terra
    snow = mask_of([], ellipses=[(32, 57, 26, 4)])
    paint(im, snow, WHITE)
    return down(im)


def ice_shard():
    im = new()
    p = [(4, 32), (30, 22), (60, 32), (30, 42)]
    m = mask_of([p])
    outline(im, m, 9)
    paint(im, m, ICE)
    d = ImageDraw.Draw(im)
    d.polygon([(x * K, y * K) for x, y in [(4, 32), (30, 22), (60, 32)]], fill=ICE_L)
    return down(im)


def snow():
    im = new()
    m = Image.new('L', im.size, 0)
    d = ImageDraw.Draw(m)
    c = 32 * K
    for i in range(6):
        a = math.radians(i * 60 + 90)
        x, y = c + math.cos(a) * 26 * K, c - math.sin(a) * 26 * K
        d.line([c, c, x, y], fill=255, width=7 * K)
        bx, by = c + math.cos(a) * 15 * K, c - math.sin(a) * 15 * K
        for s in (-1, 1):
            b = a + s * math.radians(45)
            d.line([bx, by, bx + math.cos(b) * 9 * K, by - math.sin(b) * 9 * K], fill=255, width=5 * K)
    d.ellipse([c - 8 * K, c - 8 * K, c + 8 * K, c + 8 * K], fill=255)
    outline(im, m, 9)
    paint(im, m, ICE_L)
    ImageDraw.Draw(im).ellipse([c - 4 * K, c - 4 * K, c + 4 * K, c + 4 * K], fill=WHITE)
    return down(im)


# ---------- EXPLOSIONS ----------

def star_poly(cx, cy, r_out, r_in, n, rot=0.0, wobble=0.0, seed=0):
    r = random.Random(seed)
    pts = []
    for i in range(n * 2):
        rr = (r_out if i % 2 == 0 else r_in) * (1 + r.uniform(-wobble, wobble))
        a = math.radians(i * 180.0 / n + rot)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    return pts


def puffs(cx, cy, r, n, seed, spread=1.0):
    rnd = random.Random(seed)
    out = []
    for i in range(n):
        a = i / n * 2 * math.pi + rnd.uniform(-0.3, 0.3)
        d = r * spread * rnd.uniform(0.5, 0.9)
        out.append((cx + math.cos(a) * d, cy + math.sin(a) * d, r * rnd.uniform(0.45, 0.65), r * rnd.uniform(0.45, 0.65)))
    out.append((cx, cy, r * 0.7, r * 0.7))
    return out


def boom(f):
    im = new()
    d = ImageDraw.Draw(im)
    if f == 0:  # el flaix: una estrella blanca i groga
        m = mask_of([star_poly(32, 32, 24, 10, 8, 0, 0.15, 1)])
        outline(im, m)
        paint(im, m, YELLOW)
        paint(im, mask_of([star_poly(32, 32, 14, 6, 8, 22, 0.1, 2)]), CORE)
    elif f in (1, 2):  # la bola de foc punxeguda
        r = 26 if f == 1 else 29
        m = mask_of([star_poly(32, 32, r, r * 0.62, 11, f * 9, 0.18, 3 + f)])
        outline(im, m)
        paint(im, m, RED)
        paint(im, mask_of([star_poly(32, 32, r * 0.75, r * 0.48, 9, f * 13, 0.18, 5 + f)]), ORANGE)
        paint(im, mask_of([], ellipses=[(32, 32, r * 0.38, r * 0.38)]), YELLOW)
        paint(im, mask_of([], ellipses=[(30, 30, r * 0.18, r * 0.18)]), CORE)
    elif f == 3:  # foc i fum que s'arrodoneix
        m = mask_of([], ellipses=puffs(32, 32, 22, 7, 8))
        outline(im, m)
        paint(im, m, SMOKE)
        paint(im, mask_of([], ellipses=puffs(32, 34, 15, 5, 9, 0.8)), ORANGE)
        paint(im, mask_of([], ellipses=[(32, 35, 7, 6)]), YELLOW)
    else:  # fum que s'esvaeix
        r = 22 if f == 4 else 18
        sp = 1.1 if f == 4 else 1.45
        m = mask_of([], ellipses=puffs(32, 32, r, 7, 10 + f, sp))
        outline(im, m)
        paint(im, m, SMOKE)
        hl = mask_of([], ellipses=[(x - rx * 0.25, y - ry * 0.25, rx * 0.5, ry * 0.5) for (x, y, rx, ry) in puffs(32, 32, r, 7, 10 + f, sp)])
        paint(im, hl, SMOKE_L)
        if f == 4:
            paint(im, mask_of([], ellipses=[(32, 34, 5, 4)]), ORANGE)
    return down(im)


def font(size):
    for p in ('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', '/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf',
              '/usr/share/fonts/truetype/freefont/FreeSansBold.ttf'):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def bum():
    """El rètol "BUM!" de còmic: globus punxegut groc amb les lletres vermelles."""
    W = 128
    im = Image.new('RGBA', (W * K, W * K), (0, 0, 0, 0))
    m = Image.new('L', im.size, 0)
    ImageDraw.Draw(m).polygon([(x * K, y * K) for x, y in star_poly(64, 64, 60, 40, 12, 7, 0.12, 21)], fill=255)
    outline(im, m, 13)
    paint(im, m, YELLOW)
    d = ImageDraw.Draw(im)
    f = font(30 * K)
    txt = 'BUM!'
    bb = d.textbbox((0, 0), txt, font=f, stroke_width=4 * K)
    tw, th = bb[2] - bb[0], bb[3] - bb[1]
    d.text(((W * K - tw) / 2 - bb[0], (W * K - th) / 2 - bb[1]), txt, font=f, fill=RED, stroke_width=4 * K, stroke_fill=INK)
    return im.resize((W, W), Image.LANCZOS)


def build():
    out = {}
    for f in range(4):
        out['IceSpike_%d' % f] = ice_spike(f)
    out['IceShard'] = ice_shard()
    out['Snow'] = snow()
    for f in range(6):
        out['Boom_%d' % f] = boom(f)
    out['Bum'] = bum()
    return out


def sheet(ims, path):
    Z = 3
    names = list(ims.keys())
    cols = 6
    rows = (len(names) + cols - 1) // cols
    cell = S * Z + 20
    sh = Image.new('RGBA', (cols * cell + 20, rows * cell + 20), (58, 40, 38, 255))
    for i, n in enumerate(names):
        im = ims[n]
        im = im.resize((S * Z, S * Z), Image.NEAREST if im.width == S else Image.LANCZOS)
        sh.alpha_composite(im, (20 + (i % cols) * cell, 20 + (i // cols) * cell))
    sh.save(path)


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'Resources', 'Fx'))
    os.makedirs(out, exist_ok=True)
    ims = build()
    for n, im in ims.items():
        im.save(os.path.join(out, n + '.png'))
    print('%d sprites de gel i explosions' % len(ims))
    if len(sys.argv) > 1:
        sheet(ims, sys.argv[1])


if __name__ == '__main__':
    main()
