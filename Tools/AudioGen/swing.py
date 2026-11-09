# -*- coding: utf-8 -*-
# =====================================================================
#  MÚSICA SWING DELS ANYS 30 (amb so de disc de gramòfon)
# =====================================================================
# Tot es genera amb síntesi additiva (numpy), sense mostres:
#   contrabaix caminant, piano "stride", bateria d'escombretes amb plat ride,
#   clarinet i trompeta amb sordina (pregunta i resposta), cops de metalls i
#   timbales per al cap. Al final, el so de disc: menys greus i aguts, i espetecs.
#
#   python3 swing.py ../../Assets/Resources/Audio/Music
#
# Cada tema té el tempo, la tonalitat, els acords i una llavor (seed): canviant la llavor
# surt una melodia diferent.
import sys, os
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synth import SR, note, norm, save_wav

OUT = sys.argv[1] if len(sys.argv) > 1 else 'out'
os.makedirs(OUT, exist_ok=True)

QUAL = {'maj': [0, 4, 7], 'min': [0, 3, 7], '6': [0, 4, 7, 9], 'm6': [0, 3, 7, 9], '7': [0, 4, 7, 10],
        'm7': [0, 3, 7, 10], 'maj7': [0, 4, 7, 11], 'dim7': [0, 3, 6, 9], 'm7b5': [0, 3, 6, 10], '9': [0, 4, 7, 10, 14]}
MAJOR = [0, 2, 4, 5, 7, 9, 11]
MINOR = [0, 2, 3, 5, 7, 8, 10]          # menor natural
HMINOR = [0, 2, 3, 5, 7, 8, 11]         # menor harmònica (la de l'swing gitano)
BLUES = [0, 2, 3, 4, 5, 7, 9, 10]       # major amb les notes "blues"

rng = np.random.default_rng(5)


def t_(n):
    return np.arange(n) / SR


# ---------------------------------------------------------------------
#  INSTRUMENTS (síntesi additiva)
# ---------------------------------------------------------------------

def adsr(n, a, r):
    e = np.ones(n)
    na = max(1, min(n, int(a * SR))); e[:na] = np.linspace(0, 1, na)
    nr = max(1, min(n, int(r * SR))); e[-nr:] *= np.linspace(1, 0, nr)
    return e


def additive(freq, dur, amps, decays=None, vib=0.0, vib_delay=0.15, vib_rate=5.5, bend=0.0, bend_time=0.05,
             a=0.01, r=0.06, inharm=0.0, bright=None):
    """Suma d'harmònics. amps[k] és l'amplitud de l'harmònic k+1; decays[k], com de pressa s'apaga."""
    n = int(dur * SR)
    if n <= 0:
        return np.zeros(1)
    t = t_(n)
    # to: una mica de vibrato que entra tard, i una entrada lliscada (bend, en semitons)
    f = np.full(n, freq)
    if bend:
        nb = min(n, int(bend_time * SR))
        f[:nb] *= 2 ** (np.linspace(bend, 0, nb) / 12)
    if vib:
        depth = np.clip((t - vib_delay) / 0.25, 0, 1) * vib
        f = f * 2 ** (depth * np.sin(2 * np.pi * vib_rate * t) / 12)
    phase = np.cumsum(f) / SR
    y = np.zeros(n)
    for k, amp in enumerate(amps):
        h = k + 1
        if freq * h * (1 + inharm * h * h) >= SR / 2 - 200:
            break
        partial = np.sin(2 * np.pi * phase * h * (1 + inharm * h * h))
        if decays is not None:
            partial = partial * np.exp(-t * decays[k])
        if bright is not None:   # brillantor que canvia amb el temps (sordina "wah", metalls)
            partial = partial * bright(t, h)
        y += amp * partial
    return y * adsr(n, a, r)


def piano(freq, dur, vel=1.0):
    k = np.arange(1, 11)
    amps = vel * (1.0 / k ** 1.25)
    decays = 2.2 + k * 1.6
    y = additive(freq, max(dur, 0.25), amps, decays, a=0.003, r=0.08, inharm=0.0004)
    click = rng.uniform(-1, 1, min(len(y), int(0.006 * SR))) * 0.15 * vel
    y[:len(click)] += click
    return y


def bass(freq, dur):
    y = additive(freq, dur, [1.0, 0.45, 0.18, 0.07], [3.5, 5, 8, 12], a=0.006, r=0.05, bend=0.4, bend_time=0.03)
    return y


def clarinet(freq, dur, vel=1.0, scoop=False):
    amps = [1.0, 0.0, 0.55, 0.0, 0.32, 0.0, 0.16, 0.0, 0.08, 0.0, 0.04]   # sobretot harmònics senars
    y = additive(freq, dur, [a * vel for a in amps], vib=0.25 if dur > 0.25 else 0.0, vib_delay=0.12,
                 bend=-1.0 if scoop else 0.0, bend_time=0.06, a=0.025, r=0.05)
    breath = rng.uniform(-1, 1, len(y)) * 0.02 * vel * adsr(len(y), 0.01, 0.05)
    return y + breath


def muted_trumpet(freq, dur, vel=1.0, wah=True, scoop=False):
    def bright(t, h):
        # la sordina que s'obre i es tanca (uà-uà)
        open_ = 0.5 + 0.5 * np.sin(2 * np.pi * 3.0 * t - np.pi / 2) if wah else 0.6 * np.ones_like(t)
        return np.exp(-(h - 1) * (0.9 - 0.6 * open_))
    amps = [vel * 1.0 / (k ** 0.6) for k in range(1, 13)]
    return additive(freq, dur, amps, vib=0.2 if dur > 0.3 else 0.0, bend=-1.5 if scoop else 0.0, bend_time=0.07,
                    a=0.02, r=0.05, bright=bright) * 0.6


def brass(freq, dur, vel=1.0):
    # metalls: s'obren de cop i es tanquen (per als cops de la secció)
    def bright(t, h):
        sw = np.clip(t / 0.04, 0, 1) * np.exp(-t * 3)
        return np.exp(-(h - 1) * (1.2 - 0.9 * sw))
    amps = [vel / k for k in range(1, 14)]
    return additive(freq, dur, amps, a=0.012, r=0.06, bright=bright) * 0.5


def trombone(freq, dur, bend_down=0.0, wah=False):
    def bright(t, h):
        o = (0.5 + 0.5 * np.sin(2 * np.pi * 2.2 * t - np.pi / 2)) if wah else 0.7
        return np.exp(-(h - 1) * (1.0 - 0.6 * o))
    n = int(dur * SR)
    y = additive(freq, dur, [1.0 / (k ** 0.8) for k in range(1, 12)], vib=0.35 if dur > 0.6 else 0.0,
                 vib_delay=0.25, vib_rate=5.0, a=0.04, r=0.12, bright=bright)
    if bend_down:  # el lliscament final cap avall (trombó trist)
        t = t_(n)
        f = freq * 2 ** (-bend_down * np.clip((t - dur * 0.55) / (dur * 0.45), 0, 1) / 12)
        ph = np.cumsum(f) / SR
        z = sum((1.0 / (k ** 0.8)) * np.sin(2 * np.pi * ph * k) * np.exp(-(k - 1) * 0.5) for k in range(1, 10))
        y = z * adsr(n, 0.04, 0.2)
    return y * 0.6


def banjo(freq, dur):
    k = np.arange(1, 12)
    return additive(freq, max(dur, 0.2), 1.0 / k ** 0.9, 6 + k * 4, a=0.002, r=0.04) * 0.6


# ---------- bateria ----------

def ride():
    n = int(0.9 * SR); t = t_(n)
    y = sum(np.sin(2 * np.pi * f * t + p) for f, p in ((3150, 0), (4370, 1), (5210, 2), (6430, 3), (7380, 4), (8800, 5)))
    noise = rng.uniform(-1, 1, n)
    noise = noise - np.convolve(noise, np.ones(4) / 4, 'same')
    return (y * 0.10 + noise * 0.35) * np.exp(-t * 5.5) * adsr(n, 0.001, 0.05)


def chick():  # charles amb el peu, a la 2 i a la 4
    n = int(0.06 * SR); x = rng.uniform(-1, 1, n)
    x = x - np.convolve(x, np.ones(3) / 3, 'same')
    return x * np.exp(-t_(n) * 60)


def brush_slap():
    n = int(0.18 * SR); x = rng.uniform(-1, 1, n)
    x = np.convolve(x, np.ones(3) / 3, 'same')
    return x * np.exp(-t_(n) * 22)


def brush_swish(dur):  # el remenar de les escombretes
    n = int(dur * SR); x = rng.uniform(-1, 1, n)
    x = np.convolve(x, np.ones(6) / 6, 'same')
    e = np.sin(np.linspace(0, np.pi, n)) ** 2
    return x * e


def kick():
    n = int(0.25 * SR); t = t_(n)
    f = 55 + 60 * np.exp(-t * 30)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 14)


def tom(freq=110):
    n = int(0.4 * SR); t = t_(n)
    f = freq * (1 + 0.5 * np.exp(-t * 25))
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 9)
    hit = rng.uniform(-1, 1, n) * np.exp(-t * 60) * 0.3
    return body + hit


# ---------------------------------------------------------------------
#  LA PISTA (amb swing)
# ---------------------------------------------------------------------

class Track:
    def __init__(s, bpm, bars, swing=0.66):
        s.beat = 60.0 / bpm
        s.bars = bars
        s.swing = swing  # on cau la corxera de contratemps (0,5 = recte, 0,66 = tresets)
        s.n = int(round(s.beat * 4 * bars * SR))
        s.buf = np.zeros(s.n)

    def time(s, beat):
        """beat en temps (0, 0,5, 1...): les corxeres de contratemps es retarden."""
        whole = np.floor(beat); frac = beat - whole
        if abs(frac - 0.5) < 1e-6:
            frac = s.swing
        return (whole + frac) * s.beat

    def dur(s, beats):
        return beats * s.beat

    def add(s, x, beat, gain=1.0):
        p = int(s.time(beat) * SR) % s.n
        x = x * gain; L = len(x)
        end = min(s.n, p + L); s.buf[p:end] += x[:end - p]
        if p + L > s.n:  # el que sobra torna al principi: el bucle no talla
            rest = x[end - p:]
            s.buf[:len(rest)] += rest[:s.n]


# ---------------------------------------------------------------------
#  HARMONIA I MELODIA
# ---------------------------------------------------------------------

def chord_tones(key, ch):
    root, q = ch
    return [key + root + i for i in QUAL[q]]


def nearest(p, pitches_mod12, lo, hi):
    cands = [n for n in range(lo, hi + 1) if n % 12 in pitches_mod12]
    return min(cands, key=lambda n: (abs(n - p), n))


def chords_at(prog, bar, beat):
    """L'acord que sona en un compàs i temps (cada compàs pot tenir 1 o 2 acords)."""
    cs = prog[bar % len(prog)]
    if not isinstance(cs, list):
        cs = [cs]
    return cs[min(len(cs) - 1, int(beat // (4 / len(cs))))]


RHYTHMS = [  # figures de dos compassos, en corxeres (16 posicions); 1 = nota, 2 = nota llarga
    [1, 0, 1, 1, 0, 1, 2, 0, 0, 0, 1, 1, 1, 0, 2, 0],
    [0, 1, 1, 0, 1, 1, 1, 0, 2, 0, 0, 0, 1, 0, 2, 0],
    [1, 1, 1, 1, 2, 0, 0, 1, 1, 0, 1, 0, 2, 0, 0, 0],
    [2, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1, 1, 2, 0, 0, 0],
    [1, 0, 1, 0, 1, 1, 1, 1, 2, 0, 0, 0, 1, 1, 2, 0],
    [0, 0, 1, 1, 1, 0, 1, 0, 2, 0, 0, 1, 1, 1, 2, 0],
]


def make_motif(r, density):
    base = RHYTHMS[r.integers(len(RHYTHMS))][:]
    # una mica de variació, i més o menys notes segons el tema
    for i in range(16):
        if base[i] == 1 and r.random() > 0.55 + density * 0.45:
            base[i] = 0
        elif base[i] == 0 and r.random() < density * 0.18 and i % 2 == 1:
            base[i] = 1
    return base


def realize(T, key, scale, prog, bar0, rhythm, contour_seed, lo, hi, inst, gain, end_on_root=False, bars=2, last=None):
    """Toca una figura (rhythm) a partir del compàs bar0, triant les notes perquè lliguin amb els acords."""
    r = np.random.default_rng(contour_seed)
    prev = last if last is not None else (lo + hi) // 2
    notes = []
    idx = [i for i in range(len(rhythm)) if rhythm[i]]
    for j, i in enumerate(idx):
        bar = bar0 + i // 8; beat = (i % 8) / 2
        ch = chords_at(prog, bar, beat)
        tones = {n % 12 for n in chord_tones(key, ch)}
        nxt = idx[j + 1] if j + 1 < len(idx) else len(rhythm) + 2
        length = (nxt - i) / 2
        if rhythm[i] == 2:
            length = max(length, 1.0)
        strong = i % 2 == 0
        step = int(r.choice([-4, -3, -2, -1, 1, 2, 3, 4, 5, -5]))
        target = prev + step
        if j == len(idx) - 1 and end_on_root:
            p = nearest(target, {(key + ch[0]) % 12}, lo, hi)
        elif strong or rhythm[i] == 2:
            p = nearest(target, tones, lo, hi)
        else:
            # a contratemps: escala o una nota cromàtica d'aproximació a la següent
            if r.random() < 0.25:
                p = target
            else:
                p = nearest(target, {(key + s) % 12 for s in scale}, lo, hi)
        p = int(np.clip(p, lo, hi))
        notes.append((bar, beat, p, length))
        prev = p
    for (bar, beat, p, length) in notes:
        d = T.dur(min(length, 3.0)) * 0.92
        T.add(inst(note(p), d, length), bar * 4 + beat, gain)
    return prev


def riff(T, key, prog, bar, lo, hi, inst, gain, seed):
    """Una resposta curta (3-4 notes de l'acord) a la segona meitat d'una frase."""
    r = np.random.default_rng(seed)
    ch = chords_at(prog, bar, 0)
    tones = sorted(n for n in range(lo, hi + 1) if n % 12 in {x % 12 for x in chord_tones(key, ch)})
    start = r.integers(0, max(1, len(tones) - 4))
    pat = [tones[start], tones[start + 1], tones[start + 2], tones[start + 1]] if len(tones) > start + 2 else tones[:4]
    beats = [0.5, 1.0, 1.5, 2.0] if r.random() < 0.5 else [0, 0.5, 1.5, 2.5]
    for p, b in zip(pat, beats):
        T.add(inst(note(p), T.dur(0.45), 0.5), bar * 4 + b, gain)


# ---------------------------------------------------------------------
#  UN TEMA SENCER
# ---------------------------------------------------------------------

def compose(name, bpm, key, scale, prog, bars=32, seed=1, lead='clarinet', answer='trumpet', swing=0.66,
            comp='stride', drums='brush', stabs=False, density=0.6, lead_range=(65, 84), form='AABA'):
    T = Track(bpm, bars, swing)
    r = np.random.default_rng(seed)

    LEADS = {
        'clarinet': lambda f, d, L: clarinet(f, d, 1.0, scoop=L >= 1 and r.random() < 0.5),
        'trumpet': lambda f, d, L: muted_trumpet(f, d, 1.0, wah=L >= 1, scoop=L >= 1 and r.random() < 0.4),
        'open': lambda f, d, L: muted_trumpet(f, d, 1.0, wah=False, scoop=L >= 1 and r.random() < 0.3),
        'banjo': lambda f, d, L: banjo(f, d),
    }

    # ---- secció rítmica, compàs a compàs
    for b in range(bars):
        for beat in range(4):
            ch = chords_at(prog, b, beat)
            tones = chord_tones(key, ch)
            root = key + ch[0]
            # contrabaix caminant: fonamental al primer temps, notes de l'acord i una aproximació a l'acord següent
            nb, nbeat = (b, beat + 1) if beat < 3 else (b + 1, 0)
            nxt_root = key + chords_at(prog, nb, nbeat)[0]
            if beat == 0:
                bn = root
            elif beat == 3:
                bn = nxt_root + int(r.choice([-1, 1]))
            else:
                bn = int(r.choice(tones[1:]))
            while bn > 52: bn -= 12
            while bn < 36: bn += 12
            T.add(bass(note(bn), T.dur(0.95)), b * 4 + beat, 0.42)

            # piano
            if comp == 'stride':
                if beat % 2 == 0:  # baix a l'esquerra
                    ln = root - 24 + (7 if beat == 2 else 0)
                    while ln > 47: ln -= 12
                    while ln < 33: ln += 12
                    T.add(piano(note(ln), T.dur(0.9), 0.9) + piano(note(ln + 12), T.dur(0.9), 0.5), b * 4 + beat, 0.20)
                else:              # acord a la dreta
                    voicing = sorted(nearest(t, {t % 12}, 55, 67) for t in tones[:4])
                    for v in voicing:
                        T.add(piano(note(v), T.dur(0.6), 0.6), b * 4 + beat, 0.12)
            elif comp == 'charleston' and beat in (0, 1):  # acord a la 1 i al contratemps de la 2
                voicing = sorted(nearest(t, {t % 12}, 55, 67) for t in tones[:4])
                for v in voicing:
                    T.add(piano(note(v), T.dur(0.5), 0.6), b * 4 + (0 if beat == 0 else 1.5), 0.13)

            # bateria
            if drums in ('brush', 'hot'):
                T.add(ride(), b * 4 + beat, 0.055)
                if beat in (1, 3):
                    T.add(ride(), b * 4 + beat + 0.5, 0.035)   # el "ding-a-ding"
                    T.add(chick(), b * 4 + beat, 0.10)
                    T.add(brush_slap(), b * 4 + beat, 0.06 if drums == 'brush' else 0.11)
                T.add(kick(), b * 4 + beat, 0.10 if drums == 'brush' else 0.16)
            elif drums == 'toms':  # timbales de la selva, per al cap
                pat = [1.0, 0.6, 0.8, 0.6]
                T.add(tom(110), b * 4 + beat, 0.30 * pat[beat])
                T.add(tom(110), b * 4 + beat + 0.5, 0.18)
                if beat in (1, 3):
                    T.add(chick(), b * 4 + beat, 0.10)
                T.add(kick(), b * 4 + beat, 0.14)
        if drums in ('brush', 'hot'):
            T.add(brush_swish(T.dur(1.8)), b * 4, 0.012)
            T.add(brush_swish(T.dur(1.8)), b * 4 + 2, 0.012)

        # cops de metalls al final de cada frase de 4 compassos (anticipant el següent)
        if stabs and b % 4 == 3:
            ch = chords_at(prog, b + 1, 0)
            for v in sorted(nearest(t, {t % 12}, 58, 72) for t in chord_tones(key, ch)[:4]):
                T.add(brass(note(v), T.dur(0.4)), b * 4 + 3.5, 0.09)

    # ---- melodia: frases de 8 compassos (pregunta, resposta, pregunta, final)
    lo, hi = lead_range
    motif_a = make_motif(r, density)
    motif_b = make_motif(r, min(1.0, density + 0.2))
    sections = {'AABA': 'AABA', 'AB': 'AB', 'blues': 'AA'}[form]
    sec_len = bars // len(sections)
    last = None
    for si, sec in enumerate(sections):
        b0 = si * sec_len
        motif = motif_a if sec == 'A' else motif_b
        sseed = seed * 100 + (1 if sec == 'A' else 2) * 10
        lead_fn = LEADS[lead]; ans_fn = LEADS[answer]
        for ph in range(0, sec_len, 4):
            final = ph + 4 >= sec_len
            last = realize(T, key, scale, prog, b0 + ph, motif, sseed + ph, lo, hi, lead_fn, 0.30,
                           end_on_root=final and (si == len(sections) - 1 or sec == 'A'), last=last)
            # resposta de l'altre instrument als dos compassos següents
            riff(T, key, prog, b0 + ph + 2, lo - 5, hi - 5, ans_fn, 0.22, sseed + ph + 50)
            if not final:
                riff(T, key, prog, b0 + ph + 3, lo - 5, hi - 5, ans_fn, 0.16, sseed + ph + 51)

    save_ogg(name, record(T.buf))


# ---------------------------------------------------------------------
#  SO DE DISC
# ---------------------------------------------------------------------

def onepole_lp(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x); acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def record(y, crackle=True):
    y = y - np.mean(y)
    y = y - onepole_lp(y, 160)              # fora els greus més baixos
    y = onepole_lp(onepole_lp(y, 4300), 5200)  # i els aguts: sona com un gramòfon
    y = norm(y, 0.8)
    if crackle:
        n = len(y)
        c = np.zeros(n)
        pos = rng.integers(0, n, int(n / SR * 9))          # uns 9 espetecs per segon
        amp = rng.uniform(0.02, 0.12, len(pos)) * rng.choice([-1, 1], len(pos))
        c[pos] = amp
        pops = rng.integers(0, n, int(n / SR * 0.5))        # i algun de més fort
        c[pops] = rng.uniform(0.15, 0.3, len(pops))
        c = np.convolve(c, np.exp(-np.arange(40) / 6.0), 'same')
        hiss = onepole_lp(rng.uniform(-1, 1, n), 3000) * 0.012
        y = y + c + hiss
    return norm(y, 0.85)


def save_ogg(name, y):
    wav = os.path.join(OUT, name + '.wav')
    save_wav(wav, y)
    os.system(f'ffmpeg -loglevel error -y -i "{wav}" -c:a libvorbis -q:a 4 "{os.path.join(OUT, name + ".ogg")}"')
    os.remove(wav)
    print('  ', name, '%.1f s' % (len(y) / SR))


# ---------------------------------------------------------------------
#  SINTONIES (no fan bucle)
# ---------------------------------------------------------------------

def gameover():
    # el trombó trist: uà, uà, uà, uàààà
    bpm = 80; beat = 60 / bpm
    notes = [(67, 0.9, 0), (66, 0.9, 0), (65, 0.9, 0), (64, 2.6, 2.5)]
    total = sum(d for _, d, _ in notes) * beat + 1.5
    buf = np.zeros(int(total * SR)); p = 0.0
    for n, d, bend in notes:
        x = trombone(note(n - 12), d * beat * 0.95, bend_down=bend, wah=True)
        s = int(p * SR); buf[s:s + len(x)] += x
        p += d * beat
    save_ogg('gameover', record(buf))


def win():
    # final de big band i, per acabar, "afaitar-se i tallar-se els cabells... dos rals"
    bpm = 150; beat = 60 / bpm
    T = np.zeros(int(8 * SR))

    def put(x, at, g):
        s = int(at * SR); e = min(len(T), s + len(x)); T[s:e] += x[:e - s] * g
    key = 60
    run = [0, 4, 7, 12, 16, 19]
    for i, iv in enumerate(run):
        put(clarinet(note(key + iv + 12), beat * 0.45), i * beat * 0.5, 0.35)
    t0 = len(run) * beat * 0.5
    for v in (key + 4, key + 7, key + 12, key + 16):          # gran acord de metalls
        put(brass(note(v), beat * 2.5, 1.0), t0, 0.22)
    put(bass(note(key - 24), beat * 2.5), t0, 0.5)
    put(ride(), t0, 0.12)
    t1 = t0 + beat * 3
    shave = [(0, 1), (7, 0.5), (7, 0.5), (9, 1), (7, 1), (None, 1), (11, 1), (12, 1)]  # sol do do re do · si do
    t = t1
    for iv, d in shave:
        if iv is not None:
            put(muted_trumpet(note(key + iv), beat * d * 0.85, wah=False), t, 0.4)
            put(piano(note(key + iv - 12), beat * d * 0.8, 0.8), t, 0.2)
        t += beat * d
    put(kick(), t - beat, 0.3); put(brush_slap(), t - beat, 0.3)
    put(kick(), t - 2 * beat, 0.3)
    save_ogg('win', record(T[:int((t + 1.2) * SR)]))


# ---------------------------------------------------------------------
#  ELS TEMES
# ---------------------------------------------------------------------
I, ii, iii, IV, V, vi = 0, 2, 4, 5, 7, 9

if __name__ == '__main__':
    print('música swing:')
    # menú: swing tranquil amb piano stride i clarinet
    compose('menu', 118, 53, MAJOR, [(I, '6'), (vi, '7'), (ii, 'm7'), (V, '7'), (I, '6'), (vi, '7'), (ii, 'm7'), (V, '7'),
                                     (I, 'maj7'), (I, '7'), (IV, '6'), (IV, 'm6'), (I, '6'), (V, '7'), (I, '6'), (V, '7')],
            bars=32, seed=3, lead='clarinet', answer='trumpet', drums='brush', density=0.45)
    # la sala: swing alegre (els "rhythm changes")
    compose('act0', 150, 58, MAJOR, [(I, '6'), (vi, '7'), (ii, 'm7'), (V, '7'), (iii, 'm7'), (vi, '7'), (ii, 'm7'), (V, '7')],
            bars=32, seed=12, lead='clarinet', answer='trumpet', drums='brush', density=0.6)
    # la barra: un blues de 12 compassos, amb la trompeta amb sordina
    blues = [(I, '7'), (IV, '7'), (I, '7'), (I, '7'), (IV, '7'), (IV, '7'), (I, '7'), (vi, '7'), (ii, 'm7'), (V, '7'), [(I, '7'), (vi, '7')], [(ii, 'm7'), (V, '7')]]
    compose('act1', 116, 55, BLUES, blues, bars=24, seed=21, lead='trumpet', answer='clarinet', comp='charleston',
            drums='brush', density=0.5, form='blues', lead_range=(62, 80))
    # la cuina: swing gitano en menor, ràpid i frenètic
    compose('act2', 190, 62, HMINOR, [(0, 'm6'), (0, 'm6'), (5, 'm6'), (5, 'm6'), (0, 'm6'), (0, 'm6'), (7, '7'), (7, '7')],
            bars=32, seed=33, lead='clarinet', answer='open', drums='hot', density=0.75, stabs=True)
    # la terrassa: aire de ragtime (cercle de quintes), amb banjo
    compose('act3', 132, 60, MAJOR, [(iii, '7'), (iii, '7'), (vi, '7'), (vi, '7'), (ii, '7'), (ii, '7'), (V, '7'), (V, '7')],
            bars=32, seed=44, lead='open', answer='banjo', drums='brush', density=0.55, form='AB')
    # la teulada: swing menor tens
    compose('act4', 164, 52, HMINOR, [(0, 'm6'), (8, '7'), (2, 'm7b5'), (7, '7'), (0, 'm6'), (5, 'm6'), (2, 'm7b5'), (7, '7')],
            bars=32, seed=55, lead='trumpet', answer='clarinet', drums='hot', density=0.65, stabs=True)
    # el cap: timbales de la selva, metalls i clarinet esgarrifós
    compose('boss', 184, 48, HMINOR, [(0, 'm6'), (0, 'm6'), (8, '7'), (7, '7')],
            bars=32, seed=66, lead='clarinet', answer='open', drums='toms', density=0.8, stabs=True, lead_range=(67, 86), form='AB')
    gameover()
    win()
    print('fet')
