# -*- coding: utf-8 -*-
# =====================================================================
#  EFECTES DE SO DE DIBUIX ANIMAT (anys 30)
# =====================================================================
# Xiulets de flauta de lliscament, "boings" de molla, clàxons, blocs de fusta, xilòfons,
# campanetes, plats de bateria i metalls. Tot amb síntesi (numpy), amb els mateixos noms
# que fa servir AudioManager.Play("nom").
#
#   python3 cartoon_sfx.py ../../Assets/Resources/Audio/Sfx
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synth import SR, note, norm, save_wav
from swing import brass, piano, adsr, tom, kick, onepole_lp

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out_sfx'
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(11)


def t_(d):
    return np.arange(int(SR * d)) / SR


def dec(n, k):
    return np.exp(-np.arange(n) / SR * k)


def at(x, delay):
    """El so x, començant més tard (en segons)."""
    return np.concatenate([np.zeros(int(delay * SR)), x])


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def seq(*parts):
    return np.concatenate(parts)


# ---------------------------------------------------------------------
#  INSTRUMENTS DE DIBUIX ANIMAT
# ---------------------------------------------------------------------

def slide_whistle(f0, f1, d, vib=0.0, shape=1.0):
    """Flauta de lliscament: un xiulet que puja o baixa."""
    t = t_(d)
    k = (t / d) ** shape
    f = f0 * (f1 / f0) ** k
    if vib:
        f = f * (1 + vib * np.sin(2 * np.pi * 7 * t))
    ph = np.cumsum(f) / SR
    y = np.sin(2 * np.pi * ph) + 0.12 * np.sin(4 * np.pi * ph)
    breath = onepole_lp(rng.uniform(-1, 1, len(t)), 3000) * 0.05
    return (y + breath) * adsr(len(t), 0.015, 0.04)


def boing(f=180, d=0.45, depth=0.5, rate=16):
    """Molla: un to que tremola i s'apaga."""
    t = t_(d)
    wob = 1 + depth * np.exp(-t * 5) * np.sin(2 * np.pi * rate * t)
    fr = f * wob * (1 + 0.4 * np.exp(-t * 18))
    ph = np.cumsum(fr) / SR
    y = np.sin(2 * np.pi * ph) + 0.3 * np.sin(4 * np.pi * ph) + 0.15 * np.sin(6 * np.pi * ph)
    return y * dec(len(t), 6) * adsr(len(t), 0.003, 0.05)


def woodblock(f=900, d=0.12):
    t = t_(d)
    y = np.sin(2 * np.pi * f * t) * dec(len(t), 45) + 0.5 * np.sin(2 * np.pi * f * 2.7 * t) * dec(len(t), 70)
    click = rng.uniform(-1, 1, len(t)) * dec(len(t), 300) * 0.4
    return y + click


def xylo(n, d=0.35):
    """Una tecla de xilòfon."""
    f = note(n); t = t_(d)
    y = np.sin(2 * np.pi * f * t) * dec(len(t), 9) + 0.35 * np.sin(2 * np.pi * f * 3.93 * t) * dec(len(t), 25)
    y += 0.15 * np.sin(2 * np.pi * f * 9.2 * t) * dec(len(t), 60)
    return y * adsr(len(t), 0.001, 0.03)


def bell(n, d=0.8):
    """Campaneta (harmònics que no quadren, com les de debò)."""
    f = note(n); t = t_(d)
    y = sum(a * np.sin(2 * np.pi * f * r * t) * dec(len(t), k)
            for r, a, k in ((1, 1, 4), (2.76, 0.5, 7), (5.4, 0.3, 12), (8.9, 0.15, 20)))
    return y * adsr(len(t), 0.001, 0.05)


def cymbal(d=0.9, bright=1.0):
    t = t_(d)
    x = rng.uniform(-1, 1, len(t))
    x = x - onepole_lp(x, 3500 * bright)                # només els aguts
    metal = sum(np.sin(2 * np.pi * f * t + p) for f, p in ((3920, 0), (5270, 1), (6630, 2), (8100, 3)))
    return (x * 0.8 + metal * 0.05) * dec(len(t), 3.5 / d) * adsr(len(t), 0.001, 0.05)


def horn(f=330, d=0.25, nasal=True):
    """Clàxon de bicicleta / botzina: un to nasal (quadrat filtrat)."""
    t = t_(d)
    ph = np.cumsum(f * (1 + 0.02 * np.sin(2 * np.pi * 30 * t))) / SR
    y = np.sign(np.sin(2 * np.pi * ph)) * 0.6 + np.sign(np.sin(2 * np.pi * ph * 1.5)) * 0.3
    y = onepole_lp(y, 2200 if nasal else 4000)
    return y * adsr(len(t), 0.01, 0.05)


def awooga(d=0.9):
    """La botzina dels cotxes antics: auu-uu-GA."""
    t = t_(d)
    f = np.where(t < d * 0.6, 160 + 260 * (t / (d * 0.6)) ** 0.7, 420 - 80 * (t - d * 0.6) / (d * 0.4))
    ph = np.cumsum(f) / SR
    y = np.sign(np.sin(2 * np.pi * ph)) + 0.5 * np.sign(np.sin(4 * np.pi * ph))
    wah = 0.5 + 0.5 * np.sin(np.pi * np.clip(t / d, 0, 1))
    y = onepole_lp(y * wah, 1800)
    return y * adsr(len(t), 0.02, 0.08)


def harp(notes, step=0.045, d=0.6):
    parts = []
    for i, n in enumerate(notes):
        f = note(n); t = t_(d)
        y = sum((1 / k) * np.sin(2 * np.pi * f * k * t) * dec(len(t), 3 + k * 2) for k in range(1, 6))
        parts.append(at(y * adsr(len(t), 0.002, 0.05), i * step))
    return mix(*parts)


def thud(f=90, d=0.25):
    t = t_(d)
    fr = f * (1 + 1.5 * np.exp(-t * 30))
    return np.sin(2 * np.pi * np.cumsum(fr) / SR) * dec(len(t), 14)


def zip_(d=0.22):
    """El "fiuuu" ràpid del dash."""
    t = t_(d)
    whistle = slide_whistle(600, 2400, d, shape=0.6) * 0.5
    air = rng.uniform(-1, 1, len(t))
    air = onepole_lp(air, 2500) - onepole_lp(air, 600)
    air *= np.sin(np.pi * t / d) ** 1.5
    return whistle + air * 1.2


def brass_chord(notes, d, vel=1.0):
    return mix(*[brass(note(n), d, vel) for n in notes])


# ---------------------------------------------------------------------
#  ELS SONS
# ---------------------------------------------------------------------
S = {}
# trets i impactes
S['shoot'] = slide_whistle(700, 1300, 0.09, shape=0.5) * 0.6                          # fiu!
S['hit'] = mix(woodblock(760, 0.12), thud(150, 0.12) * 0.6)                             # bonk
S['crit'] = mix(woodblock(980, 0.14), thud(160, 0.15) * 0.7, at(cymbal(0.35, 1.2) * 0.5, 0.01))  # bonk + plat
S['enemy_die'] = mix(boing(220, 0.42, 0.55), at(slide_whistle(900, 1800, 0.18) * 0.35, 0.05))    # boing! i surt volant
# el xef
S['hurt'] = mix(seq(horn(392, 0.12), horn(311, 0.2)), thud(110, 0.2) * 0.5)             # mec-mec
S['death'] = seq(slide_whistle(1400, 180, 1.1, vib=0.02, shape=1.4) * 0.8,
                 mix(thud(70, 0.4), woodblock(300, 0.2) * 0.6, cymbal(0.8, 0.8) * 0.4))   # fiuuuuu... PATAM
S['dash'] = zip_(0.22)
# premis i menús
S['pickup'] = mix(bell(88, 0.5), at(bell(95, 0.6) * 0.8, 0.06))                        # ding-ding
S['coin'] = mix(bell(91, 0.45) * 0.9, at(bell(96, 0.5) * 0.7, 0.05))
S['coin_big'] = mix(*[at(bell(n, 0.7) * 0.7, i * 0.06) for i, n in enumerate((84, 88, 91, 96, 100))])
S['boon'] = harp([72, 76, 79, 84, 88, 91], 0.04, 0.8)                                   # arpa que puja
S['set'] = mix(*[at(xylo(n, 0.3), i * 0.06) for i, n in enumerate((72, 76, 79, 84, 88, 91))],
               at(bell(96, 0.9) * 0.6, 0.4))
S['wave_clear'] = seq(brass_chord([60, 64, 67], 0.14), brass_chord([65, 69, 72], 0.45)) * 0.8      # ta-dà
S['wave_perfect'] = mix(*[at(xylo(n, 0.25), i * 0.05) for i, n in enumerate((72, 74, 76, 79, 81, 84))],
                        at(seq(brass_chord([64, 67, 72], 0.14), brass_chord([67, 71, 74, 79], 0.6)) * 0.8, 0.32))
S['combo'] = slide_whistle(900, 1900, 0.16, shape=0.7) * 0.6
S['challenge'] = mix(seq(brass_chord([60, 64, 67], 0.12), brass_chord([60, 64, 67], 0.12),
                         brass_chord([65, 69, 72], 0.12), brass_chord([67, 72, 76, 79], 0.7)),
                     at(cymbal(1.0) * 0.4, 0.36))
S['event'] = awooga(0.9)                                                                  # auu-GA!
S['ui_click'] = woodblock(1250, 0.06) * 0.8
S['ui_back'] = woodblock(820, 0.07) * 0.8
# caps, explosions i escenari
roll = mix(*[at(tom(70) * (0.3 + 0.7 * i / 14), i * 0.045) for i in range(14)])        # redoblament de timbal
S['boss_intro'] = mix(roll, at(brass_chord([48, 51, 55, 60], 0.25), 0.7), at(brass_chord([47, 50, 53, 59], 0.25), 1.0),
                      at(brass_chord([48, 51, 55, 60, 63], 0.9), 1.3), at(cymbal(1.4) * 0.6, 1.3), at(kick() * 1.2, 1.3))  # dun dun DUUN
boom = thud(55, 0.6) * 1.2
S['explosion'] = mix(boom, onepole_lp(rng.uniform(-1, 1, int(SR * 0.7)), 1400) * dec(int(SR * 0.7), 5),
                     cymbal(1.1, 0.9) * 0.5)                                              # cata-PUM
S['table'] = mix(thud(120, 0.2), woodblock(420, 0.15) * 0.7)
S['box_break'] = mix(*[at(woodblock(500 + 140 * i, 0.08) * 0.8, i * 0.035) for i in range(5)], thud(100, 0.2) * 0.6)  # cric-crac
S['bounce'] = boing(330, 0.25, 0.45, 20) * 0.8
t = t_(0.8)
hiss = rng.uniform(-1, 1, len(t)); hiss = hiss - onepole_lp(hiss, 3000)
crackle = (rng.random(len(t)) < 0.004) * rng.uniform(-1, 1, len(t)) * 3
S['fuse'] = (hiss * 0.5 + crackle) * np.linspace(0.3, 1, len(t))                          # la metxa que xiuxiueja
S['heal'] = mix(harp([79, 83, 86, 91], 0.05, 0.7), at(bell(98, 0.6) * 0.5, 0.15))


# ---- veus de dibuix animat (els caps parlen fent "bla-bla"): síl·labes curtes que el joc encadena
def syllable(f, d, vowel):
    t = t_(d)
    fr = f * (1 + 0.12 * np.exp(-t * 25)) * (1 + 0.03 * np.sin(2 * np.pi * 7 * t))
    ph = np.cumsum(fr) / SR
    y = 2 * (ph % 1) - 1                                   # dent de serra: sona a veu
    y = onepole_lp(y, vowel) + 0.3 * onepole_lp(y, vowel * 2.6)
    return y * adsr(len(t), 0.008, 0.03)


for i, (f0, v) in enumerate(((180, 900), (210, 1300), (240, 700), (200, 1700))):
    S['voice_%d' % i] = syllable(f0, 0.085, v) * 0.9

# ---- el jingle de cada cap (sona quan surt el seu cartell)
S['jingle_0'] = seq(brass_chord([67, 71, 74], 0.12), brass_chord([65, 69, 72], 0.12), brass_chord([64, 67, 72, 76], 0.5))       # Crític: fanfàrria estirada
S['jingle_1'] = mix(seq(slide_whistle(500, 260, 0.35, vib=0.06), slide_whistle(260, 520, 0.3, vib=0.06), slide_whistle(520, 180, 0.5, vib=0.08)),
                    *[at(woodblock(600 - i * 60, 0.1) * 0.6, i * 0.29) for i in range(4)])                                         # Borratxo: xiulet que fa tentines
S['jingle_2'] = mix(*[at(xylo(n, 0.2), i * 0.08) for i, n in enumerate((69, 72, 76, 75, 72, 69, 68, 69))], at(cymbal(0.5) * 0.4, 0.62))  # Xef: xilòfon gitano
S['jingle_3'] = mix(*[at(bell(n, 0.5) * 0.8, i * 0.09) for i, n in enumerate((84, 88, 91, 96, 91, 96, 100))])                    # Client VIP: dringar de monedes
S['jingle_4'] = seq(horn(392, 0.1), horn(392, 0.1), horn(392, 0.1), horn(311, 0.5))                                              # Inspectora: ta-ta-ta-TAA
S['jingle_5'] = mix(harp([67, 71, 74, 79, 83, 86], 0.07, 0.9), at(bell(91, 0.8) * 0.5, 0.45))                                     # Sommelier: arpa elegant
S['jingle_6'] = mix(*[at(bell(n, 0.4) * 0.7, i * 0.07) for i, n in enumerate((100, 96, 103, 98, 105, 100))], at(cymbal(0.6, 1.4) * 0.3, 0.42))  # Gelater: campanetes glaçades
S['jingle_7'] = mix(seq(brass_chord([60, 64, 67], 0.15), brass_chord([60, 64, 67], 0.15), brass_chord([65, 69, 72, 77], 0.6)),
                    at(cymbal(1.0) * 0.5, 0.3), at(kick(), 0.3))                                                                  # Crítica TV: entrada de programa
S['phase'] = mix(brass_chord([48, 51, 54, 57], 0.6), at(cymbal(1.0) * 0.6, 0.0), thud(60, 0.5))                                   # el cap s'enfada (2a i 3a fase)
S['jingle_8'] = mix(slide_whistle(300, 600, 1.2, vib=0.05, shape=1.0) * 0.6, *[at(bell(n, 0.9) * 0.5, i * 0.22) for i, n in enumerate((81, 80, 77, 73, 72))])  # Fantasma: uuuuh
S['recipe'] = mix(harp([72, 79, 84, 88, 91, 96], 0.05, 0.9), at(bell(100, 0.8) * 0.6, 0.3))                                     # recepta perduda

if __name__ == '__main__':
    for k, v in S.items():
        save_wav(os.path.join(OUT, k + '.wav'), norm(v, 0.8))
    print(len(S), 'sons')
