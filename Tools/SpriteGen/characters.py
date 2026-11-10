# -*- coding: utf-8 -*-
# =====================================================================
#  GENERADOR DELS PERSONATGES (estil dibuixos animats dels anys 30)
# =====================================================================
# Dibuixa tots els xefs, clients i caps a 64x64 amb braços i cames "de goma",
# guants blancs, sabates grosses, cap rodó i ulls de "tros de pastís".
#
#   python3 Tools/SpriteGen/characters.py            -> escriu els PNG a Assets/
#   python3 Tools/SpriteGen/characters.py full.png   -> a més, una làmina amb tots
#
# Cada personatge és un diccionari (mira CHARACTERS a baix). Els noms dels fitxers són els
# mateixos que abans, així els .meta i el codi del joc no canvien.
# Necessita Pillow (pip install pillow).
import math, os, sys
from PIL import Image

N = 64
INK = (28, 22, 24)
GLOVE = (250, 248, 242)
GLOVE_S = (205, 200, 190)
WHITE = (255, 255, 255)


class Part:
    """Una peça del dibuix: uns píxels d'un color, amb el seu contorn negre."""

    def __init__(s, color, shade=None, outline=True, hi=None):
        s.px = {}
        s.color = color
        s.shade = shade
        s.outline = outline
        s.hi = hi

    def put(s, x, y, c=None):
        x = int(round(x)); y = int(round(y))
        if 0 <= x < N and 0 <= y < N:
            s.px[(x, y)] = c if c is not None else s.color
        return s

    def ellipse(s, cx, cy, rx, ry, c=None):
        rx = max(rx, 0.5); ry = max(ry, 0.5)
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                    s.put(x, y, c)
        return s

    def rect(s, x0, y0, x1, y1, c=None):
        for y in range(int(round(y0)), int(round(y1)) + 1):
            for x in range(int(round(x0)), int(round(x1)) + 1):
                s.put(x, y, c)
        return s

    def tube(s, pts, r, c=None):
        """Tub de goma: corba de Bézier quadràtica amb gruix."""
        (x0, y0), (x1, y1), (x2, y2) = pts
        for i in range(81):
            t = i / 80; a = (1 - t) ** 2; b = 2 * (1 - t) * t; d = t * t
            s.ellipse(a * x0 + b * x1 + d * x2, a * y0 + b * y1 + d * y2, r, r, c)
        return s

    def line(s, x0, y0, x1, y1, c=None):
        n = int(max(abs(x1 - x0), abs(y1 - y0)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            s.put(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, c)
        return s

    def remove(s, cond):
        for k in [k for k in s.px if cond(*k)]:
            del s.px[k]
        return s

    def keep(s, cond):
        return s.remove(lambda x, y: not cond(x, y))

    def shift(s, dx, dy):
        s.px = {(x + dx, y + dy): c for (x, y), c in s.px.items() if 0 <= x + dx < N and 0 <= y + dy < N}
        return s

    def mirror(s):
        s.px = {(N - 1 - x, y): c for (x, y), c in s.px.items()}
        return s


def composite(parts):
    img = [[None] * N for _ in range(N)]
    for p in parts:
        if p.outline:
            for (x, y) in list(p.px):
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    q = (x + dx, y + dy)
                    if q not in p.px and 0 <= q[0] < N and 0 <= q[1] < N:
                        img[q[1]][q[0]] = INK
        for (x, y), c in p.px.items():
            if p.shade and c == p.color:  # ombra a la dreta i a baix de la peça
                if (x + 1, y) not in p.px or (x + 2, y) not in p.px or (x, y + 1) not in p.px or (x, y + 2) not in p.px:
                    if not ((x - 1, y) not in p.px or (x, y - 1) not in p.px):
                        c = p.shade
            if p.hi and c == p.color and ((x - 1, y) not in p.px or (x, y - 1) not in p.px) \
                    and (x + 2, y) in p.px and (x, y + 2) in p.px:
                c = p.hi
            img[y][x] = c
    return img


def darker(c, k=0.75):
    return tuple(int(v * k) for v in c)


def lighter(c, k=0.35):
    return tuple(int(v + (255 - v) * k) for v in c)


# =====================================================================
#  OBJECTES A LA MÀ  (hx,hy = on és el guant; d = cap a on mira la mà: +1 dreta, -1 esquerra)
# =====================================================================

def item_parts(kind, hx, hy, d):
    ps = []
    if kind == 'cleaver':  # destral de carnisser
        ps.append(Part((120, 80, 50)).rect(hx - 0.5, hy - 6, hx + 0.5, hy))
        b = Part((196, 204, 214), (140, 150, 162), hi=(240, 244, 250))
        b.rect(min(hx, hx + d * 6), hy - 12, max(hx, hx + d * 6), hy - 6)
        b.put(hx + d * 5, hy - 11, INK)
        ps.append(b)
    elif kind == 'whisk':  # varetes
        ps.append(Part((170, 170, 180)).rect(hx - 0.5, hy - 5, hx + 0.5, hy))
        w = Part((214, 218, 226), None)
        w.ellipse(hx, hy - 10, 3, 5)
        w.keep(lambda x, y: (x - hx) % 2 == 0 or y <= hy - 14)
        ps.append(w)
    elif kind == 'knife':  # ganivet de sushi, llarg
        ps.append(Part((40, 30, 30)).rect(hx - 0.5, hy - 4, hx + 0.5, hy))
        ps.append(Part((214, 222, 232), None, hi=WHITE).rect(hx - 0.5, hy - 16, hx + 0.5, hy - 5))
    elif kind == 'tray':  # safata amb un got
        g = Part((150, 210, 240), None, hi=WHITE)
        g.rect(hx - 1, hy - 7, hx + 1, hy - 3)
        ps.append(g)
        ps.append(Part((200, 206, 214), (150, 156, 166), hi=WHITE).ellipse(hx, hy - 2, 5.5, 1.2))
    elif kind == 'card':  # carta del menú
        c = Part((240, 236, 222), (200, 192, 170))
        c.rect(hx - 3, hy - 9, hx + 3, hy - 1)
        c.rect(hx - 2, hy - 7, hx + 2, hy - 7, (60, 110, 200))
        c.rect(hx - 2, hy - 5, hx + 1, hy - 5, (60, 110, 200))
        c.rect(hx - 2, hy - 3, hx + 2, hy - 3, (60, 110, 200))
        ps.append(c)
    elif kind == 'drumstick':  # cuixa de pollastre
        ps.append(Part((246, 240, 226)).rect(hx - 0.5, hy - 6, hx + 0.5, hy).ellipse(hx - 1, hy - 1, 1, 1).ellipse(hx + 1, hy - 1, 1, 1))
        ps.append(Part((190, 110, 50), (140, 76, 34), hi=(230, 160, 90)).ellipse(hx, hy - 10, 3.2, 4))
    elif kind == 'bottle':  # ampolla de vi
        b = Part((60, 140, 70), (36, 96, 46), hi=(140, 210, 140))
        b.rect(hx - 2, hy - 10, hx + 2, hy)
        b.rect(hx - 0.5, hy - 14, hx + 0.5, hy - 10)
        b.rect(hx - 2, hy - 7, hx + 2, hy - 5, (240, 230, 200))
        ps.append(b)
    elif kind == 'mug':  # gerra de cervesa
        m = Part((240, 190, 60), (200, 140, 30), hi=(255, 230, 140))
        m.rect(hx - 2.5, hy - 7, hx + 2.5, hy)
        m.rect(hx - 2.5, hy - 8, hx + 2.5, hy - 7, WHITE)
        ps.append(m)
    elif kind == 'wine':  # copa
        ps.append(Part((220, 230, 236)).rect(hx, hy - 4, hx, hy))
        ps.append(Part((250, 230, 150), None, hi=WHITE).ellipse(hx, hy - 7, 2.5, 3).rect(hx - 2, hy - 10, hx + 2, hy - 8, (230, 240, 245)))
    elif kind == 'pen':  # bolígraf
        ps.append(Part((40, 60, 140)).rect(hx - 0.5, hy - 9, hx + 0.5, hy).put(hx, hy - 10, (230, 200, 60)))
    elif kind == 'clipboard':  # carpeta de la inspectora
        c = Part((150, 100, 60), (110, 70, 40))
        c.rect(hx - 5, hy - 12, hx + 4, hy)
        c.rect(hx - 4, hy - 10, hx + 3, hy - 1, (246, 244, 236))
        for i, yy in enumerate((hy - 8, hy - 6, hy - 4)):
            c.rect(hx - 3, yy, hx + 1 - i, yy, (210, 50, 50) if i == 1 else (120, 120, 130))
        c.rect(hx - 1.5, hy - 13, hx + 0.5, hy - 12, (200, 200, 210))
        ps.append(c)
    elif kind == 'pizza':  # pizza sencera, de pla
        pz = Part((236, 186, 100), (200, 140, 70))
        pz.ellipse(hx, hy - 6, 6, 5)
        sauce = Part((210, 50, 40), None, outline=False).ellipse(hx, hy - 6, 4.6, 3.8)
        for (x, y, c) in ((-2, -7, (250, 230, 120)), (2, -5, (250, 230, 120)), (1, -8, (60, 140, 60)), (-2, -4, (170, 40, 40)), (2.5, -7.5, (170, 40, 40))):
            sauce.put(hx + x, hy + y, c)
        ps += [pz, sauce]
    elif kind == 'lollipop':  # piruleta
        ps.append(Part((240, 240, 240)).rect(hx - 0.5, hy - 6, hx + 0.5, hy))
        l = Part((240, 80, 160), None, hi=WHITE)
        l.ellipse(hx, hy - 9, 3.2, 3.2)
        for a in range(0, 360, 60):
            l.put(hx + math.cos(math.radians(a)) * 1.8, hy - 9 + math.sin(math.radians(a)) * 1.8, (250, 220, 90))
        ps.append(l)
    elif kind == 'camera':  # càmera de fotos amb flaix
        c = Part((50, 50, 60), (30, 30, 36), hi=(110, 110, 130))
        c.rect(hx - 4, hy - 6, hx + 4, hy - 1)
        c.ellipse(hx, hy - 3.5, 2, 2, (140, 180, 230))
        c.rect(hx - 3, hy - 8, hx - 1, hy - 7, (250, 250, 200))
        ps.append(c)
    elif kind == 'phone':  # mòbil al pal de selfies
        ps.append(Part((60, 60, 70)).line(hx, hy, hx + d * 4, hy - 12))
        ph = Part((30, 30, 40), None)
        ph.rect(hx + d * 4 - 2, hy - 17, hx + d * 4 + 2, hy - 11)
        ph.rect(hx + d * 4 - 1, hy - 16, hx + d * 4 + 1, hy - 12, (120, 200, 250))
        ps.append(ph)
    elif kind == 'handbag':  # bolso de la iaia
        ps.append(Part((120, 60, 40)).tube([(hx - 3, hy - 5), (hx, hy - 10), (hx + 3, hy - 5)], 0.5))
        b = Part((160, 60, 90), (120, 40, 60), hi=(210, 120, 150))
        b.rect(hx - 4, hy - 6, hx + 4, hy + 1)
        b.rect(hx - 0.5, hy - 5, hx + 0.5, hy - 4, (240, 200, 60))
        ps.append(b)
    elif kind == 'mic':  # micròfon de la tele
        ps.append(Part((40, 40, 46)).rect(hx - 0.7, hy - 6, hx + 0.7, hy))
        m = Part((170, 170, 180), (120, 120, 130), hi=WHITE)
        m.ellipse(hx, hy - 8, 2.4, 2.6)
        ps.append(m)
        ps.append(Part((220, 40, 40), None).rect(hx - 1.5, hy - 5, hx + 1.5, hy - 4))
    elif kind == 'foodbox':  # caixa de repartiment
        b = Part((240, 170, 40), (200, 130, 20), hi=(255, 220, 120))
        b.rect(hx - 5, hy - 9, hx + 5, hy)
        b.rect(hx - 3, hy - 6, hx + 3, hy - 4, (60, 160, 90))
        ps.append(b)
    return ps


# =====================================================================
#  BARRETS I CABELLS
# =====================================================================

def hat_parts(ch, cx, hy, r, view, d):
    """view: 'front', 'back', 'side' (mirant a +x). d: +1 o -1, cap a on mira (per a les inclinacions)."""
    k = ch.get('hat')
    top = hy - r * 0.93
    ps = []
    sx = 1 if view == 'side' else 0  # en perfil, el barret una mica enrere
    if k == 'toque':
        hc = ch.get('hat_c', (255, 255, 250)); hs = ch.get('hat_s', (214, 210, 226))
        tilt = ch.get('tilt', 3) * (1 if view != 'back' else -1)
        bw = r * 0.72
        h = Part(hc, hs)
        h.rect(cx - bw + tilt * 0.3 - sx, top - 4, cx + bw + tilt * 0.3 - sx, top + 1)
        pr = 5
        for (ox, oy) in ((-0.55, 0.0), (0.55, 0.0), (0.0, -0.55)):
            h.ellipse(cx + bw * ox + tilt * (0.8 - oy) - sx, top - 4 - pr * 0.7 + pr * oy * 1.2, pr, pr * 0.95)
        ps.append(h)
        band = Part(ch.get('band', hs), None, outline=False)
        band.rect(cx - bw + 1 + tilt * 0.3 - sx, top - 1, cx + bw - 1 + tilt * 0.3 - sx, top)
        ps.append(band)
    elif k == 'straw':  # barret de palla amb cinta vermella
        c = (232, 196, 110); s = (190, 150, 70)
        crown = Part(c, s)
        crown.rect(cx - r * 0.6 - sx, top - 6, cx + r * 0.6 - sx, top)
        crown.rect(cx - r * 0.6 - sx, top - 2, cx + r * 0.6 - sx, top - 1, (200, 50, 40))
        brim = Part(c, s, hi=(250, 226, 160))
        brim.ellipse(cx - sx, top + 0.5, r + 4 if view != 'side' else r + 2, 2.2)
        ps += [brim, crown]
    elif k == 'bakercap':  # gorra rosa tova amb una maduixa
        c = (250, 180, 210); s = (220, 130, 170)
        cap = Part(c, s, hi=(255, 220, 236))
        cap.ellipse(cx - sx, top - 2, r * 0.95, 5.5)
        cap.rect(cx - r * 0.75 - sx, top - 1, cx + r * 0.75 - sx, top + 1.5, (236, 110, 160))
        ps.append(cap)
        if view != 'back':
            st = Part((230, 40, 50), (170, 20, 30), hi=(255, 140, 140))
            st.ellipse(cx + 3 - sx, top - 7, 2.2, 2.4)
            st.put(cx + 3 - sx, top - 10, (60, 160, 60)).put(cx + 2 - sx, top - 9, (60, 160, 60)).put(cx + 4 - sx, top - 9, (60, 160, 60))
            ps.append(st)
    elif k == 'headband':  # cabells de punxes i cinta blanca (sushiman)
        hair = Part((30, 26, 34), None)
        hair.ellipse(cx - sx, top + 3, r * 0.98, 5.5)
        hair.remove(lambda x, y: y > top + 3)
        for i in range(-3, 4):
            hair.tube([(cx + i * 2.6 - sx, top), (cx + i * 2.8 - sx, top - 3), (cx + i * 3.2 - sx, top - 5)], 0.9)
        ps.append(hair)
        band = Part((250, 250, 248), (210, 210, 214))
        band.rect(cx - r + 0.5 - sx, top + 1, cx + r - 0.5 - sx, top + 3)
        if view == 'front':
            band.rect(cx - 1, top + 1.5, cx + 1, top + 2.5, (220, 40, 40))
        if view == 'back':
            band.rect(cx - 1, top + 3, cx + 2, top + 7)
        ps.append(band)
    elif k == 'hair':  # cabells normals (clients)
        hc = ch.get('hair_c', (90, 60, 40))
        hair = Part(hc, darker(hc), hi=lighter(hc, 0.25))
        if view == 'back':
            hair.ellipse(cx, hy - 1, r, r * 0.93)
            hair.remove(lambda x, y: y > hy + r * 0.45)
        else:
            hair.ellipse(cx - sx, top + 3.5, r * 0.98, 5.5)
            hair.remove(lambda x, y: y > top + 4 + (2 if abs(x - cx) > r * 0.7 else 0))
        ps.append(hair)
        if ch.get('headband_c'):
            b = Part(ch['headband_c'], darker(ch['headband_c']))
            b.rect(cx - r + 0.5 - sx, top + 1, cx + r - 0.5 - sx, top + 2.5)
            ps.append(b)
    elif k == 'messy':  # cabells despentinats (borratxo)
        hc = ch.get('hair_c', (110, 70, 40))
        hair = Part(hc, darker(hc), hi=lighter(hc, 0.25))
        hair.ellipse(cx, top + 3, r, 5)
        for i, a in enumerate((-50, -20, 10, 40)):
            ang = math.radians(a - 90)
            hair.tube([(cx + (i - 1.5) * 3, top + 1), (cx + (i - 1.5) * 4, top - 2), (cx + (i - 1.5) * 4 + math.cos(ang) * 3, top - 3 + math.sin(ang) * 2)], 0.9)
        hair.remove(lambda x, y: y > top + 4 + (3 if abs(x - cx) > r * 0.75 else 0))
        ps.append(hair)
    elif k == 'bowler':  # bombí
        c = (36, 34, 42); s = (20, 20, 26)
        h = Part(c, s, hi=(90, 90, 110))
        h.ellipse(cx - sx, top - 1.5, r * 0.7, 5)
        h.remove(lambda x, y: y > top + 1)
        h.ellipse(cx - sx, top + 1.5, r + 1.5, 1.5)
        ps.append(h)
    elif k == 'bandana':  # mocador vermell amb punts
        c = (220, 50, 50); s = (160, 30, 30)
        h = Part(c, s)
        h.ellipse(cx - sx, top + 3, r, 6)
        h.remove(lambda x, y: y > top + 5)
        for (x, y) in list(h.px):
            if (x * 3 + y * 2) % 7 == 0 and h.px[(x, y)] == c:
                h.px[(x, y)] = WHITE
        if view == 'back':
            h.ellipse(cx, top + 6, 2, 1.5).tube([(cx, top + 6), (cx - 2, top + 9), (cx - 3, top + 11)], 0.8)
        ps.append(h)
    elif k == 'cloche':  # campana de safata al cap
        c = (190, 198, 210); s = (130, 138, 150)
        h = Part(c, s, hi=(245, 248, 252))
        h.ellipse(cx - sx, top + 1, r * 0.95, 7)
        h.remove(lambda x, y: y > top + 1)
        h.rect(cx - r - 1 - sx, top + 1, cx + r + 1 - sx, top + 2)
        h.ellipse(cx - sx, top - 7, 1.5, 1.2)
        ps.append(h)
    elif k == 'tophat':  # copalta
        c = (40, 32, 52); s = (24, 20, 32)
        h = Part(c, s, hi=(90, 80, 110))
        h.rect(cx - r * 0.62 - sx, top - 9, cx + r * 0.62 - sx, top)
        h.rect(cx - r * 0.62 - sx, top - 3, cx + r * 0.62 - sx, top - 2, (200, 40, 60))
        h.ellipse(cx - sx, top + 0.5, r + 2, 1.6)
        ps.append(h)
    elif k == 'policecap':  # gorra de plat (crític)
        c = (44, 56, 100); s = (28, 36, 70)
        h = Part(c, s, hi=(90, 104, 160))
        h.ellipse(cx - sx, top - 1, r + 1, 4.5)
        h.rect(cx - r * 0.8 - sx, top - 1, cx + r * 0.8 - sx, top + 2)
        if view == 'front':
            h.rect(cx - 1, top - 2, cx + 1, top, (240, 200, 60))
        ps.append(h)
        if view != 'back':
            v = Part((24, 24, 30))
            if view == 'side':
                v.rect(cx + 2, top + 2, cx + r + 2, top + 3)
            else:
                v.ellipse(cx, top + 3, r * 0.7, 1.4)
            ps.append(v)
    elif k == 'bun':  # monyo (inspectora)
        hc = ch.get('hair_c', (110, 70, 44))
        hair = Part(hc, darker(hc), hi=lighter(hc, 0.25))
        if view == 'back':  # d'esquena, tot el cap és cabell
            hair.ellipse(cx, hy - 1, r, r * 0.93)
            hair.remove(lambda x, y: y > hy + r * 0.45)
        else:
            hair.ellipse(cx - sx, top + 4, r, 6)
            hair.remove(lambda x, y: y > top + 4 + (4 if abs(x - cx) > r * 0.75 else 0))
        hair.ellipse(cx - sx * 2, top - 3, 4, 3.5)
        for yy in (top - 4, top - 2):
            hair.rect(cx - 3 - sx * 2, yy, cx + 3 - sx * 2, yy, darker(hc, 0.85))
        ps.append(hair)
    elif k == 'pizzacap':  # gorra de pizzer (blanca, baixa) amb la cinta tricolor
        h = Part((250, 250, 245), (210, 210, 214), hi=WHITE)
        h.ellipse(cx - sx, top - 1, r * 0.85, 5)
        h.remove(lambda x, y: y > top + 1.5)
        h.rect(cx - r * 0.8 - sx, top - 1, cx + r * 0.8 - sx, top + 1.5)
        ps.append(h)
        band = Part((60, 160, 70), None, outline=False)
        w3 = r * 1.6 / 3
        x0 = cx - r * 0.8 - sx
        band.rect(x0, top, x0 + w3, top + 1, (60, 160, 70))
        band.rect(x0 + w3, top, x0 + 2 * w3, top + 1, WHITE)
        band.rect(x0 + 2 * w3, top, x0 + 3 * w3, top + 1, (220, 50, 50))
        ps.append(band)
    elif k == 'cap':  # gorra de beisbol (nen, repartidor)
        c = ch.get('cap_c', (220, 60, 60))
        h = Part(c, darker(c), hi=lighter(c, 0.3))
        h.ellipse(cx - sx, top + 2, r * 0.95, 5.5)
        h.remove(lambda x, y: y > top + 3)
        if view != 'back':
            v = Part(darker(c, 0.85), None)
            if view == 'side':
                v.rect(cx + r * 0.3, top + 2, cx + r + 4, top + 3.5)
            else:
                v.ellipse(cx + d * 2, top + 3.5, r * 0.75, 1.5)
            ps.append(v)
        if view == 'back':
            h.rect(cx - 2, top + 2, cx + 2, top + 3, darker(c, 0.6))
        ps.append(h)
        hc = ch.get('hair_c')
        if hc and view != 'front':
            hair = Part(hc, darker(hc))
            hair.ellipse(cx, top + 6, r * 0.9, 3)
            hair.remove(lambda x, y: y < top + 4)
            ps.insert(0, hair)
    elif k == 'sunhat':  # barret de sol de turista
        c = ch.get('hat_c', (240, 220, 160))
        crown = Part(c, darker(c, 0.85))
        crown.ellipse(cx - sx, top - 1.5, r * 0.6, 4)
        crown.remove(lambda x, y: y > top + 0.5)
        crown.rect(cx - r * 0.6 - sx, top - 2, cx + r * 0.6 - sx, top - 1, (60, 140, 200))
        brim = Part(c, darker(c, 0.85), hi=lighter(c, 0.4))
        brim.ellipse(cx - sx, top + 0.5, r + 5 if view != 'side' else r + 3, 2.4)
        ps += [brim, crown]
    elif k == 'helmet':  # casc de moto (repartidor)
        c = ch.get('hat_c', (40, 160, 90))
        h = Part(c, darker(c), hi=lighter(c, 0.4))
        h.ellipse(cx - sx, top + 3, r * 1.02, 7.5)
        h.remove(lambda x, y: y > top + 4.5)
        h.rect(cx - 1 - sx, top - 4, cx + 1 - sx, top + 4, WHITE)
        ps.append(h)
    elif k == 'beret':  # boina negra (mim)
        h = Part((30, 28, 34), None, hi=(80, 80, 90))
        h.ellipse(cx - sx + 2, top + 0.5, r * 0.9, 3.4)
        h.put(cx - sx + 2, top - 3.5)
        ps.append(h)
    elif k == 'quiff':  # tupè d'influencer
        hc = ch.get('hair_c', (240, 200, 90))
        hair = Part(hc, darker(hc), hi=lighter(hc, 0.4))
        hair.ellipse(cx - sx, top + 3.5, r * 0.98, 5.5)
        hair.remove(lambda x, y: y > top + 4 + (2 if abs(x - cx) > r * 0.7 else 0))
        hair.tube([(cx - 4 - sx, top), (cx - sx + 1, top - 6), (cx + 6 - sx, top - 3)], 2.4)
        ps.append(hair)
    elif k == 'bob':  # mitja melena rossa (crítica de la tele)
        hc = ch.get('hair_c', (250, 210, 100))
        hair = Part(hc, darker(hc, 0.85), hi=lighter(hc, 0.4))
        hair.ellipse(cx - sx, top + 5, r * 1.05, 7)
        hair.remove(lambda x, y: y > top + 5 and abs(x - cx) < r * 0.7)
        if view != 'side':
            for side in (-1, 1):
                hair.ellipse(cx + side * (r - 0.5), hy + 3, 3, 6)
        else:
            hair.ellipse(cx - r * 0.6, hy + 3, 3.5, 6)
        if view == 'back':
            hair.ellipse(cx, hy, r, r * 0.95)
        ps.append(hair)
    return ps


# =====================================================================
#  LA CARA
# =====================================================================

BROWS = {'serious': (-1, 0), 'worried': (2, -1), 'grumpy': (-1, 0), 'angry': (-2, 1),
         'determined': (-2, 0), 'smug': (0, 1), 'drunk': (1, 0), 'none': None}


def face_parts(ch, cx, hy, r, view):
    """Torna (parts, parts_al_damunt). En perfil, la cara mira a +x."""
    ps, late = [], []
    expr = ch.get('expr', 'grumpy')
    eye_w, eye_h = ch.get('eye_w', 2.2), ch.get('eye_h', 3.8)
    ey = hy - r * 0.10
    ex = r * 0.34
    if view == 'side':
        eyes = [(cx + r * 0.18, 0.85), (cx + r * 0.62, 0.7)]
        nose_x = cx + r * 0.92; mouth_x = cx + r * 0.5
    else:
        eyes = [(cx - ex, 1.0), (cx + ex, 1.0)]
        nose_x = cx; mouth_x = cx
    lids = ch.get('lids', expr in ('grumpy', 'drunk', 'smug'))
    lid_c = ch.get('skin_s')
    glasses = ch.get('glasses')

    # ulls
    for i, (x, ws) in enumerate(eyes):
        w = eye_w * ws
        if glasses == 'shades':
            continue
        e = Part(INK, None, outline=False)
        e.ellipse(x, ey, w, eye_h)
        for (px, py) in list(e.px):  # la falca blanca, la marca d'aquest estil
            ang = math.degrees(math.atan2(-(py + 0.5 - ey), (px + 0.5 - x)))
            if 35 < ang < 62 and ((px + 0.5 - x) ** 2 + (py + 0.5 - ey) ** 2) > 1.2:
                e.px[(px, py)] = WHITE
        ps.append(e)
        if lids:  # parpella: tapa la part de dalt de l'ull
            lid = Part(lid_c, None, outline=False)
            cut = ey - eye_h * (0.25 if expr != 'drunk' else -0.1)
            lid.rect(x - 3, ey - eye_h - 1, x + 3, cut)
            lid.keep(lambda px, py: ((px + 0.5 - x) / w) ** 2 + ((py + 0.5 - ey) / eye_h) ** 2 <= 1)
            lid_line = Part(INK, None, outline=False)
            for (px, py) in lid.px:
                if (px, py + 1) not in lid.px and py + 1 <= ey + eye_h:
                    lid_line.put(px, py + 1)
            ps.append(lid)
            if expr == 'drunk':
                ps.append(lid_line)
        if ch.get('lashes') and (view != 'side' or i == 1):
            side = -1 if (view != 'side' and i == 0) else 1
            late.append(Part(INK, None, outline=False).put(x + side * (w + 0.5), ey - eye_h + 1).put(x + side * (w + 1.2), ey - eye_h))

    # celles
    br = BROWS.get(ch.get('brows', expr))
    if br and glasses != 'shades':
        etop = ey - eye_h
        for i, (x, ws) in enumerate(eyes):
            side = (-1 if i == 0 else 1) if view != 'side' else 1
            b = Part(ch.get('brow_c', INK), None, outline=False)
            xi = x - side * 2.2 * ws; xo = x + side * 2.4 * ws
            if view == 'side' and i == 0:
                xi, xo = x - 1.6, x + 1.6
            b.tube([(xi, etop - 1 - br[0]), ((xi + xo) / 2, etop - 1.6 - (br[0] + br[1]) / 2), (xo, etop - 1 - br[1])],
                   0.6 if not ch.get('bushy') else 1.0)
            late.append(b)

    # ulleres
    if glasses == 'round':
        g = Part(INK, None, outline=False)
        for (x, ws) in eyes:
            g.rect(x - eye_w * ws - 1.5, ey - eye_h + 0.5, x + eye_w * ws + 1.5, ey - eye_h + 0.5)
            g.rect(x - eye_w * ws - 1.5, ey + eye_h - 0.5, x + eye_w * ws + 1.5, ey + eye_h - 0.5)
            g.rect(x - eye_w * ws - 1.5, ey - eye_h + 0.5, x - eye_w * ws - 1.5, ey + eye_h - 0.5)
            g.rect(x + eye_w * ws + 1.5, ey - eye_h + 0.5, x + eye_w * ws + 1.5, ey + eye_h - 0.5)
        if view != 'side':
            g.rect(cx - 1, ey - 1, cx + 1, ey - 1)
        late.append(g)
    elif glasses in ('shades', 'red'):
        c = (20, 20, 26) if glasses == 'shades' else (210, 40, 50)
        g = Part(c, None, hi=(120, 120, 140) if glasses == 'shades' else (255, 150, 150))
        for (x, ws) in eyes:
            g.ellipse(x, ey, eye_w * ws + 1.6, eye_h * 0.75)
        x0 = eyes[0][0]; x1 = eyes[1][0]
        g.rect(x0, ey - 1, x1, ey - 1)
        late.append(g)
        if glasses == 'red':  # amb uns ulls petits enfadats a dins
            for (x, ws) in eyes:
                late.append(Part(INK, None, outline=False).rect(x - 1 * ws, ey - 0.5, x + 0.5, ey + 0.5))

    # nas
    nc = ch.get('nose_c', (240, 90, 90))
    ns = ch.get('nose', 2.3)
    nose = Part(nc, darker(nc), hi=lighter(nc, 0.5))
    ny = hy + r * 0.18
    nose.ellipse(nose_x, ny, ns, ns * 0.85)

    my = hy + r * 0.30
    # barba (carnisser): tapa la part de baix de la cara
    if ch.get('beard'):
        bc = ch['beard']
        b = Part(bc, darker(bc))
        b.ellipse(cx + (1 if view == 'side' else 0), hy + r * 0.45, r * (0.85 if view != 'side' else 0.75), r * 0.55)
        b.remove(lambda x, y: y < hy + r * 0.1)
        ps.append(b)

    # galtes vermelles
    if ch.get('blush'):
        for (x, ws) in eyes:
            ps.append(Part((240, 120, 120), None, outline=False).ellipse(x + (-1.5 if x < cx else 1.5) * (view != 'side'), ey + eye_h + 1.5, 1.8 * ws, 1))

    # boca
    mouth_kind = ch.get('mouth', {'grumpy': 'frown', 'serious': 'flat', 'worried': 'wavy', 'determined': 'teeth',
                                  'angry': 'yell', 'smug': 'smirk', 'drunk': 'smile', 'happy': 'smile'}.get(expr, 'flat'))
    m0 = my + 2.5
    mc = ch.get('mouth_c', (120, 20, 40))
    mouth = Part(mc, None, outline=False)
    if mouth_kind == 'flat':
        for x in range(-3, 4): mouth.put(mouth_x + x, m0)
    elif mouth_kind == 'frown':
        for x in range(-4, 5): mouth.put(mouth_x + x, m0 + 1 - (1 if abs(x) <= 2 else 0))
    elif mouth_kind == 'wavy':
        for x in range(-3, 4): mouth.put(mouth_x + x, m0 + (1 if x % 2 else 0))
    elif mouth_kind == 'smirk':
        for x in range(-3, 4): mouth.put(mouth_x + x, m0 - (1 if x >= 2 else 0))
    elif mouth_kind == 'smile':
        for x in range(-4, 5): mouth.put(mouth_x + x, m0 - (1 if abs(x) >= 3 else 0))
    elif mouth_kind == 'teeth':
        m = Part((250, 250, 245), None)
        m.rect(mouth_x - 3, m0 - 0.5, mouth_x + 3, m0 + 1.5)
        for x in (-1, 1):
            m.put(mouth_x + x * 1.5, m0 - 0.5, INK).put(mouth_x + x * 1.5, m0 + 1.5, INK)
        for x in range(-3, 4): m.put(mouth_x + x, m0 + 0.5, INK)
        late.append(m)
    elif mouth_kind in ('yell', 'drool'):  # boca oberta cridant (o amb gana)
        m = Part((90, 16, 30), None)
        m.ellipse(mouth_x, m0 + 1, 2.6, 2.0)
        m.rect(mouth_x - 1.5, m0 + 2, mouth_x + 1.5, m0 + 2.5, (240, 110, 120))
        m.rect(mouth_x - 1.5, m0 - 0.5, mouth_x + 1.5, m0 - 0.5, WHITE)
        late.append(m)
        if mouth_kind == 'drool':
            late.append(Part((160, 210, 250), None, outline=False).rect(mouth_x + 2, m0 + 3, mouth_x + 2, m0 + 5))
    elif mouth_kind == 'grin':  # somriure dolent amb dents (xef renegat)
        m = Part((250, 250, 245), None)
        for x in range(-4, 5):
            yy = m0 - (1 if abs(x) >= 3 else 0)
            m.rect(mouth_x + x, yy, mouth_x + x, yy + 1)
        for x in (-2, 0, 2): m.put(mouth_x + x, m0 + 1, INK)
        late.append(m)
    ps.append(mouth)

    # bigoti
    st = ch.get('stache')
    if st:
        sc = ch.get('stache_c', (90, 50, 30))
        s = Part(sc, None, outline=(st == 'big'))
        smy = hy + r * 0.40 - 1 + (0.5 if st == 'big' else 0)
        if view == 'side':
            s.tube([(nose_x - 3, smy), (nose_x - 1, smy + 1), (nose_x + 1, smy)], 0.75 if st != 'big' else 1.2)
        else:
            for side in (-1, 1):
                if st == 'big':
                    s.tube([(cx + side * 0.5, smy - 0.5), (cx + side * 4, smy + 2), (cx + side * 7, smy - 1)], 1.3)
                else:
                    s.tube([(cx + side * 1, smy), (cx + side * 3.5, smy + 1), (cx + side * 5, smy)], 0.75)
        late.insert(0, s)
    late.append(nose)
    return ps, late


# =====================================================================
#  EL COS: dibuixos (jaqueta, davantal...)
# =====================================================================

def body_pattern(ch, body, cx, by0, by1, view, belly):
    pat = ch.get('pattern')
    c2 = ch.get('pattern_c', (200, 40, 40))
    if pat == 'buttons' and view != 'back':
        bx = cx + (2 if view == 'side' else 0)
        for k in range(2):
            body.put(bx, by0 + 6 + k * 5, c2); body.put(bx, by0 + 7 + k * 5, c2)
    elif pat == 'stripes':  # davantal de ratlles (carnisser)
        if view == 'back':
            body.rect(cx - 0.5, by0 + 2, cx + 0.5, by1 - 4, c2)  # el llaç del davantal
            body.rect(cx - 3, by1 - 5, cx + 3, by1 - 4, c2)
        else:
            x0 = cx - (belly - 2) if view != 'side' else cx - 1
            for (x, y), c in list(body.px.items()):
                if y >= by0 + 3 and x >= x0 and x <= cx + belly - 2 and c == body.color:
                    body.px[(x, y)] = c2 if (x - int(cx)) % 3 == 0 else WHITE
    elif pat == 'dots':
        for (x, y), c in list(body.px.items()):
            if (x * 3 + y * 5) % 9 == 0 and c == body.color:
                body.px[(x, y)] = WHITE
    elif pat == 'kimono' and view != 'back':  # solapa en diagonal
        for i in range(8):
            body.put(cx - 3 + i + (2 if view == 'side' else 0), by0 + 1 + i, (230, 230, 240))
    elif pat == 'shirtfront' and view != 'back':  # americana amb camisa blanca (cambrer, vip)
        bx = cx + (2 if view == 'side' else 0)
        for y in range(by0 + 1, by1 - 3):
            w = 1 if y > by0 + 4 else 2 - (y - by0) * 0.3
            body.rect(bx - w, y, bx + w, y, WHITE)
        for k in range(3): body.put(bx, by0 + 6 + k * 3, INK)
        if ch.get('chain'):
            for i in range(-4, 5):
                body.put(bx + i, by0 + 9 + abs(i) * 0.5, (250, 210, 60))
    elif pat == 'bib' and view != 'back':  # tovalló al coll (golafre)
        bx = cx + (2 if view == 'side' else 0)
        for y in range(by0 + 1, by0 + 10):
            w = 5 - (y - by0) * 0.55
            body.rect(bx - w, y, bx + w, y, WHITE)
        body.put(bx, by0 + 5, (220, 40, 40))
    elif pat == 'apron' and view != 'back':  # davantal blanc tacat (xef renegat)
        bx = cx + (2 if view == 'side' else 0)
        body.rect(bx - 4, by0 + 4, bx + 4, by1 - 2, (246, 240, 230))
        for (x, y) in ((bx - 2, by0 + 7), (bx + 2, by0 + 10)):
            body.rect(x, y, x + 1, y, (190, 30, 30))
    elif pat == 'stains' and view != 'back':  # jaqueta tacada (borratxo)
        bx = cx + (2 if view == 'side' else 0)
        body.rect(bx - 2, by0 + 2, bx + 2, by1 - 3, (240, 226, 200))
        for (x, y) in ((bx - 1, by0 + 6), (bx + 1, by0 + 9), (bx - 4, by0 + 11)):
            body.rect(x, y, x + 1, y, (150, 110, 70))
    elif pat == 'uniform':  # cinturó amb sivella i botons daurats
        bx = cx + (2 if view == 'side' else 0)
        y = by1 - 4
        for (x, yy), c in list(body.px.items()):
            if yy in (y, y + 1): body.px[(x, yy)] = (40, 34, 40)
        if view != 'back':
            body.rect(bx - 1, y, bx + 1, y + 1, (240, 200, 60))
            for k in range(3): body.put(bx, by0 + 3 + k * 3, (240, 200, 60))
    elif pat == 'hawaiian':  # camisa de flors de turista
        for (x, y), c in list(body.px.items()):
            if c != body.color: continue
            h = (x * 7 + y * 13) % 11
            if h == 0: body.px[(x, y)] = (250, 240, 90)
            elif h == 5: body.px[(x, y)] = (240, 90, 140)
            elif h == 8: body.px[(x, y)] = WHITE
    elif pat == 'mime':  # ratlles horitzontals blanques i negres
        for (x, y), c in list(body.px.items()):
            if c == body.color and (y // 2) % 2 == 0: body.px[(x, y)] = (30, 30, 36)
    elif pat == 'cardigan' and view != 'back':  # jaqueta de punt oberta (iaia)
        bx = cx + (2 if view == 'side' else 0)
        body.rect(bx - 1, by0 + 1, bx + 1, by1 - 2, (240, 230, 220))
        for k in range(3): body.put(bx - 2, by0 + 4 + k * 3, (200, 160, 60))
    elif pat == 'suit' and view != 'back':  # jaqueta de presentadora amb solapes
        bx = cx + (2 if view == 'side' else 0)
        for i in range(6):
            body.put(bx - 1 - i * 0.5, by0 + 1 + i, darker(body.color, 0.7))
            body.put(bx + 1 + i * 0.5, by0 + 1 + i, darker(body.color, 0.7))
        body.rect(bx - 1, by0 + 1, bx + 1, by0 + 4, (250, 250, 250))
    if ch.get('belt'):  # faixa (sushiman)
        y = by1 - 5
        for (x, yy), c in list(body.px.items()):
            if y <= yy <= y + 2: body.px[(x, yy)] = ch['belt']


# =====================================================================
#  EL PERSONATGE SENCER
# =====================================================================

def character(ch, view='front', step=0):
    """view: 'front', 'back', 'side' (mira a la dreta). step: 0 dret, 1 i 2 les dues passes."""
    cx = 32
    r = ch.get('head', 11.5)
    hy = ch.get('head_y', 27)
    by0 = ch.get('body_top', 37); by1 = ch.get('body_bot', 50)
    shoulder = ch.get('shoulder', 5); belly = ch.get('belly', 9)
    skin, skin_s = ch['skin'], ch['skin_s']
    jacket = ch['jacket']; jacket_s = ch.get('jacket_s', darker(jacket, 0.8))
    sleeve = ch.get('sleeve', jacket); sleeve_s = darker(sleeve, 0.8)
    pants = ch['pants']; shoe = ch['shoe']
    limb = ch.get('limb', 1.0)
    ch = dict(ch, skin_s=skin_s)

    lower, upper, front = [], [], []

    # --- cames i sabates
    lt = ch.get('leg_top', by1 - 1); foot_y = 57
    legs = Part(pants, darker(pants, 0.75))
    shoes = []
    if view == 'side':
        for i, side in enumerate((-1, 1)):  # -1 cama del darrere
            fwd = {0: 0, 1: 4 * side, 2: -4 * side}[step]
            lift = 1.5 if (step == 1 and side == -1) or (step == 2 and side == 1) else 0
            fx = cx + side * 1 + fwd
            legs.tube([(cx + side * 1, lt), (cx + side * 1 + fwd * 0.5 + 1, (lt + foot_y) / 2), (fx, foot_y - 1 - lift)], limb)
            sh = Part(shoe if side == 1 else darker(shoe, 0.8), None, hi=lighter(shoe, 0.45))
            sh.ellipse(fx + 2, 59.5 - lift, ch.get('shoe_r', 4.8), 3.4)
            sh.ellipse(fx + 3.5, 58.5 - lift, 3.3, 3)
            shoes.append(sh)
    else:
        for side in (-1, 1):
            lift = 1.5 if (step == 1 and side == -1) or (step == 2 and side == 1) else 0
            legs.tube([(cx + side * 4, lt), (cx + side * (5 + ch.get('leg_bow', 3)), (lt + foot_y) / 2), (cx + side * 5, foot_y - 1 - lift)], limb)
            sh = Part(shoe, None, hi=lighter(shoe, 0.45))
            sh.ellipse(cx + side * 8, 59.5 - lift, ch.get('shoe_r', 4.8), 3.6)
            if view == 'front':
                sh.ellipse(cx + side * 10, 58.5 - lift, ch.get('shoe_r', 4.8) - 1.5, 3)
            shoes.append(sh)
    lower += [legs] + shoes

    # --- cos en forma de pera
    body = Part(jacket, jacket_s)
    bx = cx + (1 if view == 'side' else 0)
    for y in range(by0, by1 + 1):
        k = (y - by0) / (by1 - by0)
        w = shoulder + (belly - shoulder) * math.sin(min(1, k * 1.15) * math.pi / 2)
        if k > 0.85: w -= (k - 0.85) * 14
        if view == 'side':
            body.rect(bx - w * 0.8, y, bx + w * 0.95, y)
        else:
            body.rect(cx - w, y, cx + w, y)
    body_pattern(ch, body, cx, by0, by1, view, belly)

    # --- braços de goma i guants
    swing = {0: 0, 1: 1, 2: -1}[step]
    hand_x, hand_y = ch.get('hand_x', 16), ch.get('hand_y', 49)
    arms_back, arms_front = [], []
    items = ch.get('items', {})
    if view == 'side':
        # un sol braç (el del davant), que sosté l'objecte de la mà dreta (o el de l'esquerra)
        sx, sy = bx - 1, by0 + 3
        hx, hy2 = bx + 6 + swing * 2, hand_y - 2
        arm = Part(sleeve, sleeve_s)
        arm.tube([(sx, sy), (bx + 1, (sy + hy2) / 2 + 2), (hx, hy2)], limb)
        arms_front.append(arm)
        it = items.get(1) or items.get(-1)
        if it: arms_back += item_parts(it, hx, hy2 - 1, 1)
        g = Part(GLOVE, GLOVE_S)
        g.ellipse(hx, hy2 + 1, 3.2, 3.0)
        g.ellipse(hx + 2.4, hy2 - 1, 1.5, 1.5)
        arms_front.append(g)
    else:
        for side in (-1, 1):
            arm = Part(sleeve, sleeve_s)
            sx = cx + side * (shoulder - 1); sy = by0 + 3
            hx, hy2 = cx + side * hand_x, hand_y + swing * side
            arm.tube([(sx, sy), (cx + side * (shoulder + ch.get('arm_bow', 8)), (sy + hy2) / 2 - 2), (hx, hy2)], limb)
            it = items.get(side)
            # mirant cap amunt (d'esquena) els costats s'inverteixen: la mà dreta queda a l'esquerra
            if view == 'back':
                it = items.get(-side)
            its = item_parts(it, hx, hy2 - 1, side) if it else []
            g = Part(GLOVE, GLOVE_S)
            g.ellipse(hx, hy2 + 1, 3.4, 3.1)
            if view == 'front':
                g.ellipse(hx - side * 2.7, hy2 - 1, 1.6, 1.6)  # polze
            if view == 'back':
                arms_back += its  # els objectes queden darrere el personatge
                arms_front += [arm, g]
            else:
                arms_front += [arm] + its + [g]

    # --- coll (mocador, corbata, llacet)
    neck = []
    collar = ch.get('collar')
    if collar == 'scarf':
        sc = ch.get('scarf_c', (240, 60, 60))
        s = Part(sc, darker(sc, 0.7))
        s.ellipse(bx, by0 + 1, shoulder * 0.7, 2.2)
        if view == 'front': s.ellipse(cx + 2, by0 + 4, 1.8, 2.6)
        if view == 'side': s.ellipse(bx + 3, by0 + 3, 1.6, 2.4)
        neck.append(s)
    elif collar == 'bowtie' and view != 'back':
        s = Part((210, 40, 40), (150, 26, 26))
        s.ellipse(bx - 2, by0 + 1.5, 1.8, 1.5).ellipse(bx + 2, by0 + 1.5, 1.8, 1.5)
        neck.append(s)
    elif collar == 'tie' and view != 'back':
        s = Part(ch.get('tie_c', (200, 40, 50)), None)
        s.rect(bx - 0.5, by0, bx + 0.5, by0 + 8).ellipse(bx, by0 + 1, 1.2, 1)
        neck.append(s)
    elif collar == 'collar':
        s = Part(ch.get('collar_c', WHITE), None)
        s.ellipse(bx, by0 + 0.5, shoulder * 0.8, 1.6)
        neck.append(s)

    # --- objecte gran davant la panxa (l'olla)
    belly_item = []
    if ch.get('pot') and view != 'back':
        p = Part((170, 178, 190), (120, 126, 138), hi=(230, 236, 244))
        px = bx + (2 if view == 'side' else 0)
        p.rect(px - 7, by1 - 8, px + 7, by1 - 1)
        p.ellipse(px, by1 - 8, 7, 1.6, (90, 60, 40))
        if view != 'side': p.rect(px - 9, by1 - 7, px - 8, by1 - 6).rect(px + 8, by1 - 7, px + 9, by1 - 6)
        belly_item.append(p)

    # --- cap
    head_parts = []
    hx0 = cx + (1 if view == 'side' else 0)
    ears = []
    if view == 'side':
        ear = Part(skin_s, None, outline=False).ellipse(hx0 - r * 0.3, hy + 2, 1.5, 2.3)
        ear.put(hx0 - r * 0.3 + 1, hy + 1, INK).put(hx0 - r * 0.3 + 1, hy + 2, INK).put(hx0 - r * 0.3 + 1, hy + 3, INK)
        ears.append(ear)
    else:
        for side in (-1, 1):
            ears.append(Part(skin, skin_s).ellipse(cx + side * (r - 0.5), hy + 2, 2.2, 3))
    head = Part(skin, skin_s)
    head.ellipse(hx0, hy, r, r * 0.92)
    if view == 'side':  # el nas i la barbeta fan una mica de bony endavant
        head.ellipse(hx0 + 2, hy + 2, r - 0.5, r * 0.85)
    side_hair = []
    if ch.get('curls'):  # rínxols rossos (pastissera)
        cc = ch['curls']
        for side in ((-1, 1) if view != 'side' else (-1,)):
            c = Part(cc, darker(cc, 0.8), hi=lighter(cc, 0.4))
            c.ellipse(hx0 + side * (r - 0.5), hy + 1, 2.8, 4.5)
            c.ellipse(hx0 + side * (r - 1), hy + 5, 2.2, 2.2)
            side_hair.append(c)
    if view == 'back':
        fp, fl = [], []
        nape = Part(darker(skin, 0.93), None, outline=False)  # el clatell
        nape.ellipse(cx, hy + r * 0.6, r * 0.4, 1.2)
        fp.append(nape)
    else:
        fp, fl = face_parts(ch, hx0, hy, r, view)
    hp = hat_parts(ch, hx0, hy, r, view, 1)
    if ch.get('beard') and view == 'back':
        pass
    head_parts = (ears if view != 'side' else []) + [head] + (ears if view == 'side' else []) + side_hair + fp + hp + fl

    if view == 'back':
        upper = arms_back + [body] + neck + arms_front + head_parts
    elif view == 'side':
        upper = [body] + neck + belly_item + arms_back + head_parts + arms_front
    else:
        upper = [body] + arms_front + neck + belly_item + head_parts
    # en les passes, el cos puja un píxel
    if step:
        for p in upper: p.shift(0, -1)
    return composite(lower + upper)


def to_image(img, mirror=False):
    im = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    px = im.load()
    for y in range(N):
        for x in range(N):
            c = img[y][x]
            if c: px[x, y] = tuple(c) + (255,)
    if mirror: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    return im


# =====================================================================
#  EL REPARTIMENT
# =====================================================================

SKIN = dict(skin=(255, 196, 150), skin_s=(226, 150, 110))
SKIN2 = dict(skin=(236, 176, 130), skin_s=(200, 132, 96))
SKIN3 = dict(skin=(250, 210, 170), skin_s=(220, 168, 130))

CHEF = dict(SKIN, jacket=(255, 250, 240), jacket_s=(214, 206, 226), pants=(52, 64, 120), shoe=(150, 40, 40),
            hat='toque', collar='scarf', pattern='buttons', pattern_c=(250, 190, 40), expr='grumpy')

CHARACTERS = {
    # ---------------- XEFS (4 direccions) ----------------
    'Chef': dict(CHEF, stache='thin', items={}),
    'Carnisser': dict(SKIN2, jacket=(150, 190, 230), pants=(70, 60, 58), shoe=(60, 40, 30), hat='straw',
                      pattern='stripes', pattern_c=(210, 40, 40), beard=(50, 34, 28), expr='grumpy',
                      items={1: 'cleaver'}, belly=10),
    'Pastisser': dict(SKIN3, jacket=(250, 170, 200), jacket_s=(220, 130, 170), pants=(240, 240, 240), shoe=(200, 60, 120),
                      hat='bakercap', pattern='dots', curls=(250, 210, 90), lashes=True, expr='grumpy', blush=True,
                      nose_c=(250, 140, 130), nose=1.8, items={1: 'whisk'}, collar='collar'),
    'Sushiman': dict(SKIN, jacket=(50, 70, 150), pants=(40, 40, 60), shoe=(40, 36, 40), hat='headband', pattern='kimono',
                     belt=(210, 40, 40), expr='grumpy', items={1: 'knife'}),

    # ---------------- CLIENTS (davant i esquena) ----------------
    'Enemy': dict(SKIN, jacket=(240, 130, 50), pants=(50, 54, 80), shoe=(40, 34, 34), hat='hair', hair_c=(100, 64, 40),
                  expr='angry', collar='collar', collar_c=(250, 170, 100)),
    'EnemyFast': dict(SKIN3, jacket=(140, 80, 220), pants=(50, 50, 80), shoe=(240, 240, 250), hat='hair', hair_c=(60, 40, 30),
                      headband_c=(220, 40, 40), expr='determined', belly=7, shoulder=4, leg_bow=4),
    'EnemyShooter': dict(SKIN2, jacket=(60, 140, 230), pants=(50, 50, 70), shoe=(40, 34, 34), hat='hair', hair_c=(50, 36, 30),
                         expr='angry', items={1: 'card'}),
    'EnemyTank': dict(SKIN, jacket=(210, 60, 150), pants=(50, 44, 60), shoe=(40, 34, 34), hat='hair', hair_c=(90, 60, 36),
                      expr='grumpy', belly=13, shoulder=6, hand_x=19, head=11, head_y=27, shoe_r=5.4),
    'Cambrer': dict(SKIN3, jacket=(50, 50, 64), pants=(40, 40, 50), shoe=(20, 20, 24), hat='bowler', pattern='shirtfront',
                    collar='bowtie', expr='serious', stache='thin', stache_c=(40, 30, 30), items={1: 'tray'}),
    'Golafre': dict(SKIN, jacket=(240, 130, 170), pants=(60, 50, 70), shoe=(50, 40, 40), hat='hair', hair_c=(110, 70, 40),
                    pattern='bib', expr='angry', mouth='drool', belly=13, shoulder=6, hand_x=19, items={1: 'drumstick'}),
    'Olla': dict(SKIN2, jacket=(244, 240, 232), pants=(60, 60, 70), shoe=(40, 34, 34), hat='bandana', expr='determined',
                 pot=True),
    'Safata': dict(SKIN3, jacket=(70, 100, 160), pants=(44, 50, 80), shoe=(30, 30, 36), hat='cloche', pattern='uniform',
                   expr='serious'),

    # ---------------- CAPS (només de cara) ----------------
    'Borratxo': dict(SKIN, jacket=(130, 80, 50), pants=(60, 50, 50), shoe=(50, 36, 30), hat='messy', hair_c=(110, 70, 40),
                     pattern='stains', expr='drunk', blush=True, nose_c=(230, 60, 60), nose=2.8, stache='thin',
                     items={1: 'bottle', -1: 'mug'}, belly=12, shoulder=6, hand_x=18),
    'ClientVIP': dict(SKIN3, jacket=(70, 44, 110), pants=(40, 30, 60), shoe=(20, 20, 24), hat='tophat', pattern='shirtfront',
                      chain=True, collar='bowtie', glasses='shades', expr='smug', items={1: 'wine'}, belly=11),
    'Critic': dict(SKIN, jacket=(44, 56, 100), pants=(36, 40, 70), shoe=(20, 20, 24), hat='policecap', pattern='uniform',
                   glasses='red', expr='angry', mouth='frown', stache='big', stache_c=(70, 50, 40), nose_c=(230, 80, 80),
                   items={1: 'pen'}, belly=13, shoulder=6, hand_x=19),
    'Inspectora': dict(SKIN3, jacket=(50, 120, 80), pants=(40, 60, 50), shoe=(40, 30, 30), hat='bun', hair_c=(110, 70, 44),
                       glasses='round', lashes=True, expr='serious', collar='tie', tie_c=(240, 240, 240),
                       nose_c=(240, 150, 130), nose=1.8, items={-1: 'clipboard', 1: 'pen'}),
    'XefRenegat': dict(SKIN2, jacket=(140, 30, 40), pants=(40, 36, 50), shoe=(30, 26, 26), hat='toque', band=(200, 30, 30),
                       tilt=-3, pattern='apron', expr='angry', mouth='grin', stache='big', stache_c=(50, 34, 28), bushy=True,
                       items={1: 'cleaver'}, belly=11, shoulder=6, hand_x=18),
    # ---------------- NOUS ----------------
    'Pizzer': dict(SKIN, jacket=(250, 250, 245), jacket_s=(214, 210, 226), pants=(60, 140, 70), shoe=(110, 70, 40),
                   hat='pizzacap', collar='scarf', scarf_c=(220, 50, 50), pattern='buttons', pattern_c=(60, 160, 70),
                   expr='grumpy', stache='big', stache_c=(40, 30, 28), items={1: 'pizza'}),
    'Nen': dict(SKIN3, jacket=(250, 200, 60), pants=(60, 90, 160), shoe=(240, 80, 80), hat='cap', cap_c=(60, 120, 220),
                hair_c=(110, 70, 40), expr='angry', mouth='yell', belly=7, shoulder=4, head=12.5, items={1: 'lollipop'},
                blush=True, nose=1.8),
    'Turista': dict(SKIN, jacket=(70, 170, 210), pants=(230, 210, 160), shoe=(160, 110, 60), hat='sunhat',
                    pattern='hawaiian', glasses='shades', expr='smug', items={1: 'camera'}, belly=11, nose_c=(250, 110, 100)),
    'Influencer': dict(SKIN3, jacket=(230, 90, 170), pants=(40, 40, 60), shoe=(250, 250, 250), hat='quiff',
                       hair_c=(250, 210, 90), glasses='shades', expr='smug', items={1: 'phone'}, belly=8, shoulder=4),
    'Iaia': dict(SKIN3, jacket=(150, 110, 170), pants=(90, 80, 90), shoe=(70, 50, 50), hat='bun', hair_c=(220, 220, 225),
                 pattern='cardigan', glasses='round', expr='grumpy', items={1: 'handbag'}, belly=12, shoulder=6,
                 nose_c=(240, 150, 130), nose=2.0, lashes=True),
    'Repartidor': dict(SKIN2, jacket=(240, 170, 40), pants=(50, 50, 60), shoe=(30, 30, 34), hat='helmet', hat_c=(40, 160, 90),
                       expr='determined', items={1: 'foodbox'}, belly=8, leg_bow=4),
    'Mim': dict(skin=(246, 246, 248), skin_s=(214, 214, 222), jacket=(250, 250, 250), pants=(30, 30, 36), shoe=(30, 30, 36),
                hat='beret', pattern='mime', expr='worried', mouth='flat', nose_c=(230, 60, 60), collar='scarf',
                scarf_c=(220, 40, 40), belly=8, shoulder=4),
    'CriticaTV': dict(SKIN3, jacket=(210, 40, 60), pants=(40, 30, 40), shoe=(30, 20, 24), hat='bob', hair_c=(250, 210, 100),
                      pattern='suit', lashes=True, expr='smug', mouth='smirk', nose_c=(240, 150, 130), nose=1.8,
                      items={1: 'mic'}, belly=10, blush=True),
}

# on va cada sprite (relatiu a Assets/)
CHEF_NAMES = ['Chef', 'Carnisser', 'Pastisser', 'Sushiman', 'Pizzer']
ENEMY_DIRS = {'Enemy': 'Sprites/Enemies', 'EnemyFast': 'Sprites/Enemies', 'EnemyShooter': 'Sprites/Enemies',
              'EnemyTank': 'Sprites/Enemies', 'Cambrer': 'Resources/Enemies', 'Golafre': 'Resources/Enemies',
              'Olla': 'Resources/Enemies', 'Safata': 'Resources/Enemies',
              'Nen': 'Resources/Enemies', 'Turista': 'Resources/Enemies', 'Influencer': 'Resources/Enemies',
              'Iaia': 'Resources/Enemies', 'Repartidor': 'Resources/Enemies', 'Mim': 'Resources/Enemies'}
BOSSES = ['Borratxo', 'ClientVIP', 'Critic', 'Inspectora', 'XefRenegat']


def sprites():
    """Torna una llista de (camí relatiu a Assets/, nom del sprite, funció que el dibuixa per a un pas)."""
    out = []
    for n in CHEF_NAMES:
        ch = CHARACTERS[n]
        for d, view, mir in (('Down', 'front', False), ('Up', 'back', False), ('Right', 'side', False), ('Left', 'side', True)):
            out.append(('Resources/Chefs', n + '_' + d, lambda s, ch=ch, view=view, mir=mir: to_image(character(ch, view, s), mir)))
    for n, folder in ENEMY_DIRS.items():
        ch = CHARACTERS[n]
        for d, view in (('Front', 'front'), ('Back', 'back')):
            out.append((folder, n + '_' + d, lambda s, ch=ch, view=view: to_image(character(ch, view, s))))
    for n in BOSSES:
        ch = CHARACTERS[n]
        out.append(('Sprites/Boss', n, lambda s, ch=ch: to_image(character(ch, 'front', s))))
    # caps que es carreguen des de Resources (els que no són a l'escena)
    out.append(('Resources/Bosses', 'CriticaTV', lambda s: to_image(character(CHARACTERS['CriticaTV'], 'front', s))))
    return out


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    assets = os.path.normpath(os.path.join(here, '..', '..', 'Assets'))
    sheet_rows = []
    for folder, name, draw in sprites():
        frames = [draw(0), draw(1), draw(2)]
        os.makedirs(os.path.join(assets, folder), exist_ok=True)
        frames[0].save(os.path.join(assets, folder, name + '.png'))
        walk = os.path.join(assets, 'Resources', 'Walk')
        # les passes: sempre per als que es carreguen des de Resources (així els nous també en tenen)
        if folder.startswith('Resources') or os.path.exists(os.path.join(walk, name + '_1.png')):
            frames[1].save(os.path.join(walk, name + '_1.png'))
            frames[2].save(os.path.join(walk, name + '_2.png'))
        sheet_rows.append((name, frames))
    print('%d sprites' % len(sheet_rows))
    if len(sys.argv) > 1:
        make_sheet(sheet_rows, sys.argv[1])


def make_sheet(rows, path, scale=4, cols=8):
    from PIL import ImageDraw
    cw, chh = 64 * scale + 12, 64 * scale + 26
    W = cols * cw + 12
    H = ((len(rows) + cols - 1) // cols) * chh + 12
    out = Image.new('RGBA', (W, H), (200, 180, 145, 255))
    d = ImageDraw.Draw(out)
    for y in range(0, H, 24):
        for x in range(0, W, 24):
            if (x // 24 + y // 24) % 2: d.rectangle([x, y, x + 23, y + 23], fill=(188, 168, 134, 255))
    for i, (name, frames) in enumerate(rows):
        x = 12 + (i % cols) * cw; y = 12 + (i // cols) * chh
        d.text((x + 2, y), name, fill=(40, 28, 24))
        out.alpha_composite(frames[0].resize((64 * scale, 64 * scale), Image.NEAREST), (x, y + 14))
    out.save(path)


if __name__ == '__main__':
    main()
