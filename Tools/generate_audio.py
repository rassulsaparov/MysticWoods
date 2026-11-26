"""Mystic Woods - Sound Pass asset generator.

Synthesizes every sound effect and both music loops used by the game and
writes them as 16-bit mono WAV files to Assets/Resources/Audio.
Pure Python 3 standard library, fixed random seed, so running it again
produces exactly the same files.

    py -I Tools/generate_audio.py

All output is original work made for this project and released under CC0 1.0.
"""
import array
import math
import os
import random
import wave

SR = 44100
TAU = 2.0 * math.pi
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Resources", "Audio")


# ---------------------------------------------------------------- helpers

def n_of(seconds):
    return int(round(seconds * SR))


def silence(seconds):
    return [0.0] * n_of(seconds)


def osc(phase, shape):
    if shape == "sine":
        return math.sin(phase)
    if shape == "tri":
        return 2.0 / math.pi * math.asin(math.sin(phase))
    if shape == "soft":
        return math.tanh(2.5 * math.sin(phase))
    if shape == "saw":
        x = (phase / TAU) % 1.0
        return 2.0 * x - 1.0
    raise ValueError(shape)


def sweep(dur, f0, f1, shape="sine", curve=1.0, attack=0.004, decay=2.0, vib=0.0, vib_rate=0.0):
    """Oscillator with a pitch glide and a fast attack / power-curve decay."""
    n = n_of(dur)
    out = [0.0] * n
    phase = 0.0
    a = max(1, n_of(attack))
    for i in range(n):
        t = i / n
        f = f0 + (f1 - f0) * (t ** curve)
        if vib:
            f *= 1.0 + vib * math.sin(TAU * vib_rate * i / SR)
        phase += TAU * f / SR
        env = min(1.0, i / a) * (1.0 - t) ** decay
        out[i] = osc(phase, shape) * env
    return out


def noise(dur, fc0, fc1, attack=0.002, decay=2.0, seed=1):
    """White noise through a sweeping one-pole lowpass."""
    rnd = random.Random(seed)
    n = n_of(dur)
    out = [0.0] * n
    y = 0.0
    a = max(1, n_of(attack))
    for i in range(n):
        t = i / n
        fc = fc0 + (fc1 - fc0) * t
        k = 1.0 - math.exp(-TAU * fc / SR)
        y += k * (rnd.uniform(-1.0, 1.0) - y)
        env = min(1.0, i / a) * (1.0 - t) ** decay
        out[i] = y * env
    return out


def highpass(x, fc):
    k = math.exp(-TAU * fc / SR)
    out = [0.0] * len(x)
    prev_x = 0.0
    prev_y = 0.0
    for i, v in enumerate(x):
        prev_y = k * (prev_y + v - prev_x)
        prev_x = v
        out[i] = prev_y
    return out


def bell(dur, freq, decay=3.0, bright=0.5):
    """Inharmonic bell / chime: three partials that decay at different speeds."""
    n = n_of(dur)
    out = [0.0] * n
    parts = ((1.0, 1.0, 1.0), (2.76, 0.5 * bright, 1.8), (5.40, 0.25 * bright, 3.0))
    a = n_of(0.003)
    for i in range(n):
        t = i / SR
        env_a = min(1.0, i / a)
        s = 0.0
        for ratio, amp, speed in parts:
            s += amp * math.sin(TAU * freq * ratio * t) * math.exp(-decay * speed * t)
        out[i] = s * env_a
    return out


def mix(dst, src, at=0.0, gain=1.0, wrap=False):
    """Add src into dst starting at `at` seconds; wrap=True writes circularly (loops)."""
    start = n_of(at)
    size = len(dst)
    for i, v in enumerate(src):
        j = start + i
        if wrap:
            j %= size
        elif j >= size:
            dst.extend([0.0] * (j - size + 1))
            size = len(dst)
        dst[j] += v * gain
    return dst


def edge_fade(x, ms=4.0):
    m = min(len(x) // 2, n_of(ms / 1000.0))
    for i in range(m):
        g = i / m
        x[i] *= g
        x[-1 - i] *= g
    return x


def write(name, x, peak=0.85, fade=True):
    if fade:
        edge_fade(x)
    top = max(abs(v) for v in x) or 1.0
    g = peak / top
    data = array.array("h", (int(max(-1.0, min(1.0, v * g)) * 32767) for v in x))
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print("wrote %-28s %6.2f s" % (name + ".wav", len(x) / SR))


def hz(note):
    """MIDI note number -> frequency."""
    return 440.0 * 2.0 ** ((note - 69) / 12.0)


# ----------------------------------------------------------- sound effects

def jump(variant):
    f0, f1 = (330, 760) if variant == 1 else (300, 690)
    x = sweep(0.17, f0, f1, "tri", curve=0.6, decay=1.6)
    mix(x, sweep(0.17, f0 * 2, f1 * 2, "sine", curve=0.6, decay=2.5), gain=0.25)
    mix(x, noise(0.05, 4000, 1500, decay=3, seed=10 + variant), gain=0.15)
    return x


def hit(variant):
    x = sweep(0.32, 170 if variant == 1 else 150, 55, "sine", curve=0.5, decay=2.2)
    mix(x, noise(0.16, 2600, 500, decay=2.5, seed=20 + variant), gain=0.75)
    mix(x, sweep(0.12, 520 if variant == 1 else 470, 300, "soft", decay=2.0), gain=0.3)
    return x


def pickup(variant):
    notes = (88, 95) if variant == 1 else (88, 93)  # E6->B6 or E6->A6
    x = silence(0.42)
    mix(x, bell(0.25, hz(notes[0]), decay=7.0, bright=0.6), 0.0, 0.8)
    mix(x, bell(0.36, hz(notes[1]), decay=5.5, bright=0.6), 0.07, 1.0)
    return x


def windup(variant):
    """Rising forest-creature growl: buzzy saw glide, tremolo and a breath of air."""
    dur = 0.5
    n = n_of(dur)
    base0, base1 = (85, 210) if variant == 1 else (70, 180)
    x = [0.0] * n
    phase = 0.0
    phase2 = 0.0
    for i in range(n):
        t = i / n
        f = base0 + (base1 - base0) * t * t
        phase += TAU * f / SR
        phase2 += TAU * f * 1.502 / SR
        trem = 0.65 + 0.35 * math.sin(TAU * (9 + 14 * t) * i / SR)
        env = min(1.0, t / 0.15) * (1.0 - max(0.0, (t - 0.85) / 0.15))
        x[i] = (osc(phase, "saw") * 0.6 + osc(phase2, "soft") * 0.35) * trem * env
    lp = 0.0
    k = 1.0 - math.exp(-TAU * 1400 / SR)
    for i in range(n):
        lp += k * (x[i] - lp)
        x[i] = lp
    air = noise(dur, 600, 3800, attack=0.2, decay=0.3, seed=30 + variant)
    mix(x, air, gain=0.45)
    return x


def enemy_death(variant):
    x = noise(0.45, 3200, 300, decay=2.0, seed=40 + variant)
    mix(x, sweep(0.42, 620 if variant == 1 else 540, 110, "tri", curve=0.7, decay=1.8), gain=0.8)
    mix(x, bell(0.4, hz(71 if variant == 1 else 69), decay=6.0, bright=0.3), 0.05, 0.25)
    return x


def ui_click():
    x = sweep(0.06, 1900, 1200, "sine", decay=4.0, attack=0.001)
    mix(x, noise(0.02, 6000, 3000, attack=0.0005, decay=3.0, seed=50), gain=0.35)
    return x


def shoot(variant):
    x = sweep(0.14, 980 if variant == 1 else 900, 380, "soft", curve=0.8, decay=2.0)
    mix(x, noise(0.08, 5000, 1500, decay=3.0, seed=60 + variant), gain=0.2)
    return x


def win():
    x = silence(2.2)
    for k, note in enumerate((72, 76, 79, 84)):
        mix(x, bell(1.6, hz(note), decay=2.2, bright=0.45), 0.13 * k, 0.8)
        mix(x, sweep(1.2, hz(note - 12), hz(note - 12), "tri", decay=2.5, attack=0.02), 0.13 * k, 0.15)
    mix(x, bell(1.5, hz(91), decay=2.5, bright=0.3), 0.55, 0.35)
    return x


def lose():
    x = silence(2.4)
    for k, note in enumerate((67, 63, 60)):
        mix(x, sweep(0.9, hz(note), hz(note) * 0.985, "tri", decay=1.6, attack=0.03), 0.32 * k, 0.8)
    mix(x, sweep(1.4, hz(47), hz(46), "soft", decay=1.4, attack=0.05), 0.96, 0.9)
    return x


# ------------------------------------------------------------------- music

def reverb_loop(x, wet=0.3, room=0.78):
    """Schroeder reverb run over the loop twice so its tail wraps seamlessly."""
    combs = [1557, 1617, 1491, 1422]
    alls = [225, 556]
    n = len(x)
    src = x + x
    wet_sig = [0.0] * len(src)
    for d in combs:
        buf = [0.0] * d
        idx = 0
        damp = 0.0
        for i, v in enumerate(src):
            out = buf[idx]
            damp = out * 0.8 + damp * 0.2
            buf[idx] = v + damp * room
            idx = (idx + 1) % d
            wet_sig[i] += out * 0.25
    for d in alls:
        buf = [0.0] * d
        idx = 0
        for i, v in enumerate(wet_sig):
            b = buf[idx]
            out = -v + b
            buf[idx] = v + b * 0.5
            idx = (idx + 1) % d
            wet_sig[i] = out
    return [x[i] * (1.0 - wet) + wet_sig[n + i] * wet for i in range(n)]


def pad_voice(dur, freq, attack, release):
    n = n_of(dur + release)
    out = [0.0] * n
    a = n_of(attack)
    sustain_end = n_of(dur)
    r = n_of(release)
    ph = [0.0, 0.0, 0.0]
    det = (1.0, 1.0016, 0.9984)
    for i in range(n):
        if i < a:
            env = i / a
        elif i < sustain_end:
            env = 1.0
        else:
            env = max(0.0, 1.0 - (i - sustain_end) / r)
        s = 0.0
        for v in range(3):
            ph[v] += TAU * freq * det[v] / SR
            s += math.sin(ph[v]) + 0.25 * osc(ph[v], "tri")
        out[i] = s * env * env / 3.0
    return out


def pluck(freq, dur, seed):
    """Karplus-Strong plucked string - a soft wooden harp sound."""
    rnd = random.Random(seed)
    period = max(2, int(SR / freq))
    buf = [rnd.uniform(-1, 1) for _ in range(period)]
    n = n_of(dur)
    out = [0.0] * n
    for i in range(n):
        j = i % period
        v = buf[j]
        buf[j] = 0.996 * 0.5 * (v + buf[(j + 1) % period])
        out[i] = v
    edge_fade(out, 2)
    return out


def menu_theme():
    """Slow, misty A-minor theme: soft pads, low drone and sparse chimes. 8 bars, 70 BPM."""
    bpm = 70
    beat = 60.0 / bpm
    bar = beat * 4
    bars = 8
    total = [0.0] * n_of(bar * bars)
    chords = [(57, 60, 64), (53, 57, 60), (48, 52, 55), (55, 59, 62),
              (57, 60, 64), (53, 57, 60), (50, 53, 57), (52, 56, 59)]
    rnd = random.Random(7)
    for b, chord in enumerate(chords):
        t = b * bar
        for note in chord:
            mix(total, pad_voice(bar, hz(note), 0.9, 1.3), t, 0.16, wrap=True)
        mix(total, pad_voice(bar, hz(chord[0] - 12), 0.6, 1.0), t, 0.14, wrap=True)
        hits = (0.0, 1.5, 2.0, 3.0) if b % 2 == 0 else (0.0, 1.0, 2.5)
        for h in hits:
            note = rnd.choice(chord) + 12 + (12 if rnd.random() < 0.3 else 0)
            mix(total, bell(2.2, hz(note), decay=1.6, bright=0.35), t + h * beat, 0.11, wrap=True)
    return reverb_loop(total, wet=0.38, room=0.82)


def level_theme():
    """Adventurous D-dorian groove: plucked harp, bass, soft kick and shaker. 16 bars, 104 BPM."""
    bpm = 104
    beat = 60.0 / bpm
    eighth = beat / 2
    bar = beat * 4
    progression = [(50, 53, 57), (48, 52, 55), (46, 50, 53), (48, 52, 55)] * 3 + \
                  [(50, 53, 57), (53, 57, 60), (48, 52, 55), (45, 49, 52)]
    total = [0.0] * n_of(bar * len(progression))
    pattern = (0, 2, 4, 2, 5, 2, 4, 2)  # indexes into an extended chord
    kick = sweep(0.2, 115, 42, "sine", curve=0.4, decay=2.5, attack=0.002)
    shaker_base = highpass(noise(0.07, 9000, 7000, decay=3.0, seed=99), 3000)
    lead_line = [74, 72, 69, 72, 74, 77, 76, 72, 74, 72, 69, 67, 69, 72, 69, 69]
    for b, chord in enumerate(progression):
        t0 = b * bar
        ext = [chord[0] + 12, chord[1] + 12, chord[2] + 12, chord[0] + 24, chord[1] + 24, chord[2] + 24]
        for k, p in enumerate(pattern):
            mix(total, pluck(hz(ext[p]), 0.9, seed=b * 8 + k), t0 + k * eighth, 0.22, wrap=True)
        root = chord[0] - 12
        for at, length in ((0.0, beat * 1.4), (beat * 2, beat * 1.1), (beat * 3.5, beat * 0.45)):
            mix(total, sweep(length, hz(root), hz(root), "tri", decay=0.8, attack=0.01), t0 + at, 0.3, wrap=True)
        for k in (0, 2):
            mix(total, kick, t0 + k * beat, 0.45, wrap=True)
        for k in range(4):
            mix(total, shaker_base, t0 + k * beat + eighth, 0.12, wrap=True)
        if b >= 8:
            note = lead_line[b - 8]
            mix(total, sweep(bar * 0.95, hz(note), hz(note), "sine", decay=0.6, attack=0.08,
                             vib=0.004, vib_rate=5.0), t0, 0.16, wrap=True)
            mix(total, sweep(bar * 0.45, hz(note + 7), hz(note + 7), "sine", decay=1.2, attack=0.05),
                t0 + beat * 2, 0.07, wrap=True)
    return reverb_loop(total, wet=0.22, room=0.72)


def main():
    os.makedirs(OUT, exist_ok=True)
    for v in (1, 2):
        write("jump_%d" % v, jump(v), 0.75)
        write("hit_%d" % v, hit(v), 0.9)
        write("pickup_%d" % v, pickup(v), 0.75)
        write("enemy_windup_%d" % v, windup(v), 0.9)
        write("enemy_death_%d" % v, enemy_death(v), 0.85)
        write("shoot_%d" % v, shoot(v), 0.55)
    write("ui_click", ui_click(), 0.6)
    write("win", win(), 0.85)
    write("lose", lose(), 0.85)
    write("music_menu", menu_theme(), 0.7, fade=False)
    write("music_level", level_theme(), 0.7, fade=False)


if __name__ == "__main__":
    main()
