# Makes the generated sounds and the menu music of Jump High (Assignment 12 - Sound Pass).
# Everything here is synthesized from sine / square / triangle waves and noise, so the
# result is my own work (CC0). No samples from the internet are used.
#
#   py Tools/make_sounds.py            (run from the project folder)
#
# Output: Assets/Audio/SFX/*.wav and Assets/Audio/Music/menu_theme.wav (44.1 kHz, 16-bit, mono).
# The random generator has a fixed seed, so running it again gives the same files.

import math
import os
import random
import struct
import wave

SR = 44100
random.seed(12)

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Audio")
SFX_DIR = os.path.join(ROOT, "SFX")
MUSIC_DIR = os.path.join(ROOT, "Music")


# ---------------------------------------------------------------- basics

def osc(shape, phase):
    """One sample of a waveform. phase is in cycles (0..1 repeats)."""
    p = phase % 1.0
    if shape == "sine":
        return math.sin(2 * math.pi * p)
    if shape == "square":                       # soft square: less harsh than a hard one
        return math.tanh(3.0 * math.sin(2 * math.pi * p))
    if shape == "tri":
        return 4 * abs(p - 0.5) - 1
    if shape == "saw":
        return 2 * p - 1
    raise ValueError(shape)


def tone(dur, f0, f1=None, shape="sine", vol=0.5, attack=0.005, release=0.05,
         vib=0.0, vib_rate=6.0, trem=0.0, trem_rate=12.0, noise=0.0, curve="exp"):
    """A note or a sweep from f0 to f1 Hz with an attack / release envelope."""
    f1 = f0 if f1 is None else f1
    n = int(dur * SR)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / SR
        k = i / max(1, n - 1)
        f = f0 * (f1 / f0) ** k if curve == "exp" else f0 + (f1 - f0) * k
        f *= 1 + vib * math.sin(2 * math.pi * vib_rate * t)
        phase += f / SR
        s = osc(shape, phase)
        if noise:
            s = (1 - noise) * s + noise * (random.random() * 2 - 1)
        env = min(1.0, t / attack if attack > 0 else 1.0) * min(1.0, (dur - t) / release if release > 0 else 1.0)
        if trem:
            env *= 1 - trem * 0.5 * (1 + math.sin(2 * math.pi * trem_rate * t * (1 + k)))
        out[i] = s * env * vol
    return out


def noise_burst(dur, vol=0.5, cutoff=4000.0, cutoff_end=None, release=None):
    """White noise through a one-pole lowpass whose cutoff can fall (explosions, hits)."""
    cutoff_end = cutoff if cutoff_end is None else cutoff_end
    release = dur if release is None else release
    n = int(dur * SR)
    out = [0.0] * n
    y = 0.0
    for i in range(n):
        t = i / SR
        k = i / max(1, n - 1)
        fc = cutoff * (cutoff_end / cutoff) ** k
        a = 1 - math.exp(-2 * math.pi * fc / SR)
        y += a * ((random.random() * 2 - 1) - y)
        env = min(1.0, t / 0.002) * max(0.0, 1 - t / release) ** 2
        out[i] = y * env * vol * 2.5
    return out


def silence(dur):
    return [0.0] * int(dur * SR)


def mix(*parts, offsets=None):
    """Adds sounds together. offsets = start time of every part in seconds."""
    offsets = offsets or [0.0] * len(parts)
    n = max(int(o * SR) + len(p) for p, o in zip(parts, offsets))
    out = [0.0] * n
    for p, o in zip(parts, offsets):
        s = int(o * SR)
        for i, v in enumerate(p):
            out[s + i] += v
    return out


def concat(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def normalize(x, peak=0.9):
    m = max(1e-9, max(abs(v) for v in x))
    return [v * peak / m for v in x]


def write(folder, name, samples, peak=0.9):
    os.makedirs(folder, exist_ok=True)
    samples = normalize(samples, peak)
    path = os.path.join(folder, name)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v)) * 32767)) for v in samples))
    print("wrote", os.path.relpath(path, os.path.join(ROOT, "..", "..")), "%.2f s" % (len(samples) / SR))


def note(name):
    """'A4' -> Hz"""
    names = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}
    semis = names[name[0]]
    rest = name[1:]
    if rest.startswith("#"):
        semis += 1
        rest = rest[1:]
    elif rest.startswith("b"):
        semis -= 1
        rest = rest[1:]
    octave = int(rest)
    return 440.0 * 2 ** ((semis + (octave - 4) * 12) / 12)


# ---------------------------------------------------------------- sound effects

def make_sfx():
    # jump: second variant next to the old jump.wav (a rising "boing")
    write(SFX_DIR, "jump_2.wav", mix(
        tone(0.20, 280, 760, "square", 0.35, release=0.08),
        tone(0.20, 560, 1520, "sine", 0.15, release=0.08)))

    # pickup: second coin variant, two quick high notes
    write(SFX_DIR, "coin_2.wav", concat(
        tone(0.07, note("B5"), shape="square", vol=0.35, release=0.02),
        tone(0.22, note("E6"), shape="square", vol=0.35, release=0.18)), peak=0.75)

    # hurt: second variant next to the bone-break sound (thump + falling tone)
    write(SFX_DIR, "hurt_2.wav", mix(
        noise_burst(0.10, 0.6, 3000, 600),
        tone(0.28, 420, 130, "square", 0.45, release=0.15)))

    # enemy wind-up: a rising "charging" whine with a tremolo that speeds up.
    # It must be easy to hear and different from every other sound: it is the warning.
    write(SFX_DIR, "enemy_windup_1.wav", mix(
        tone(0.60, 170, 900, "square", 0.40, attack=0.03, release=0.04, trem=0.6, trem_rate=14),
        tone(0.60, 340, 1800, "sine", 0.12, attack=0.03, release=0.04)))
    write(SFX_DIR, "enemy_windup_2.wav", mix(
        tone(0.60, 150, 760, "tri", 0.55, attack=0.03, release=0.04, vib=0.04, vib_rate=18, trem=0.5, trem_rate=11),
        tone(0.60, 300, 1520, "square", 0.12, attack=0.03, release=0.04)))

    # boss wind-up: low and heavy, so the boss sounds bigger than the robots
    write(SFX_DIR, "boss_windup.wav", mix(
        tone(0.90, 55, 230, "saw", 0.45, attack=0.08, release=0.06, trem=0.4, trem_rate=8),
        tone(0.90, 110, 460, "square", 0.25, attack=0.08, release=0.06),
        noise_burst(0.90, 0.15, 400, 1800, release=1.2)))

    # enemy hit: short metallic "tock", two variants
    write(SFX_DIR, "enemy_hit_1.wav", mix(
        tone(0.10, 210, 90, "sine", 0.7, release=0.06),
        tone(0.05, 1400, 900, "square", 0.15, release=0.03),
        noise_burst(0.03, 0.4, 6000)))
    write(SFX_DIR, "enemy_hit_2.wav", mix(
        tone(0.11, 250, 110, "sine", 0.7, release=0.06),
        tone(0.05, 1700, 1100, "square", 0.15, release=0.03),
        noise_burst(0.03, 0.4, 5000)))

    # enemy death: small explosion + falling tone
    write(SFX_DIR, "enemy_death.wav", mix(
        noise_burst(0.70, 0.8, 5000, 200),
        tone(0.50, 520, 55, "square", 0.35, release=0.25)))

    # UI click: very short, soft
    write(SFX_DIR, "ui_click.wav", mix(
        tone(0.035, 1900, 1500, "sine", 0.6, attack=0.001, release=0.03),
        noise_burst(0.012, 0.2, 8000)), peak=0.6)

    # lose: falling "wah-wah" melody
    lose = concat(
        tone(0.28, note("G4"), shape="tri", vol=0.6, release=0.06),
        tone(0.28, note("F#4"), shape="tri", vol=0.6, release=0.06),
        tone(0.28, note("F4"), shape="tri", vol=0.6, release=0.06),
        tone(0.80, note("E4"), note("D4"), shape="tri", vol=0.6, release=0.35, vib=0.02, vib_rate=5))
    write(SFX_DIR, "lose.wav", mix(lose, [v * 0.5 for v in tone(len(lose) / SR, note("C3"), shape="sine", vol=0.4, release=0.4)]))


# ---------------------------------------------------------------- menu music

def pluck(dur, f, vol):
    """Short plucked note (arpeggio): fast attack, exponential decay."""
    n = int(dur * SR)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / SR
        phase += f / SR
        s = 0.7 * osc("sine", phase) + 0.3 * osc("tri", phase * 2)
        env = min(1.0, t / 0.004) * math.exp(-t * 7) * min(1.0, (dur - t) / 0.02)
        out[i] = s * env * vol
    return out


def pad(dur, freqs, vol):
    """Soft chord with slow attack and release, ends at 0 inside its bar (clean loop)."""
    n = int(dur * SR)
    out = [0.0] * n
    phases = [0.0] * len(freqs)
    for i in range(n):
        t = i / SR
        s = 0.0
        for j, f in enumerate(freqs):
            phases[j] += f * (1 + 0.002 * math.sin(2 * math.pi * 0.5 * t + j)) / SR
            s += osc("tri", phases[j])
        env = min(1.0, t / 0.25) * min(1.0, (dur - t) / 0.3)
        out[i] = s / len(freqs) * env * vol
    return out


def make_menu_music():
    bpm = 96
    beat = 60.0 / bpm
    bar = 4 * beat
    # Am - F - C - G, twice; calm, for the menu (the level track is faster)
    chords = [
        ["A3", "C4", "E4"], ["F3", "A3", "C4"], ["C4", "E4", "G4"], ["G3", "B3", "D4"],
        ["A3", "C4", "E4"], ["F3", "A3", "C4"], ["C4", "E4", "G4"], ["E3", "G#3", "B3"],
    ]
    bass = ["A2", "F2", "C3", "G2", "A2", "F2", "C3", "E2"]
    melody = [  # (beat in the bar, note, beats) - a short hook in bars 1-2 and 5-6
        (0, [(0, "E5", 1.5), (1.5, "D5", 0.5), (2, "C5", 1), (3, "A4", 1)]),
        (1, [(0, "C5", 1.5), (1.5, "A4", 0.5), (2, "F4", 2)]),
        (4, [(0, "E5", 1.5), (1.5, "G5", 0.5), (2, "E5", 1), (3, "C5", 1)]),
        (5, [(0, "D5", 1), (1, "C5", 1), (2, "A4", 2)]),
        (7, [(2, "B4", 1), (3, "G#4", 1)]),
    ]

    parts, offsets = [], []
    for b, ch in enumerate(chords):
        start = b * bar
        freqs = [note(n) for n in ch]
        parts.append(pad(bar, freqs, 0.22)); offsets.append(start)
        # bass on beats 1 and 3
        for k in (0, 2):
            parts.append(tone(beat * 1.8, note(bass[b]), shape="sine", vol=0.45, attack=0.01, release=0.3))
            offsets.append(start + k * beat)
        # arpeggio in eighth notes, one octave up
        arp = [freqs[0] * 2, freqs[1] * 2, freqs[2] * 2, freqs[1] * 2]
        for e in range(8):
            parts.append(pluck(beat * 0.5, arp[e % 4], 0.16)); offsets.append(start + e * beat * 0.5)

    for b, notes in melody:
        for at, n, length in notes:
            parts.append(tone(beat * length, note(n), shape="tri", vol=0.22, attack=0.02,
                              release=min(0.25, beat * length * 0.5), vib=0.004, vib_rate=5))
            offsets.append(b * bar + at * beat)

    total = int(len(chords) * bar * SR)
    music = mix(*parts, offsets=offsets)[:total]
    music += [0.0] * (total - len(music))
    write(MUSIC_DIR, "menu_theme.wav", music, peak=0.8)


if __name__ == "__main__":
    make_sfx()
    make_menu_music()
