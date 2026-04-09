"""
Soft educational SFX synthesis engine.
Each set uses a unique musical palette to generate correct/incorrect/success-end sounds.
"""

import numpy as np
from scipy.io import wavfile
import os
import sys

SAMPLE_RATE = 44100

# --- Waveform generators ---

def sine(freq, duration, sr=SAMPLE_RATE):
    t = np.linspace(0, duration, int(sr * duration), endpoint=False)
    return np.sin(2 * np.pi * freq * t)

def triangle(freq, duration, sr=SAMPLE_RATE):
    t = np.linspace(0, duration, int(sr * duration), endpoint=False)
    return 2 * np.abs(2 * (t * freq - np.floor(t * freq + 0.5))) - 1

def soft_square(freq, duration, sr=SAMPLE_RATE):
    """Square wave with only first 3 harmonics — sounds softer."""
    t = np.linspace(0, duration, int(sr * duration), endpoint=False)
    sig = np.sin(2 * np.pi * freq * t)
    sig += np.sin(2 * np.pi * 3 * freq * t) / 3
    sig += np.sin(2 * np.pi * 5 * freq * t) / 5
    return sig / 1.5

# --- Envelope generators ---

def env_adsr(length, attack=0.02, decay=0.05, sustain_level=0.6, release=0.1):
    env = np.ones(length)
    a_samples = int(attack * SAMPLE_RATE)
    d_samples = int(decay * SAMPLE_RATE)
    r_samples = int(release * SAMPLE_RATE)
    # attack
    if a_samples > 0:
        env[:a_samples] = np.linspace(0, 1, a_samples)
    # decay
    d_end = a_samples + d_samples
    if d_samples > 0 and d_end <= length:
        env[a_samples:d_end] = np.linspace(1, sustain_level, d_samples)
    # sustain
    if d_end < length - r_samples:
        env[d_end:length - r_samples] = sustain_level
    # release
    if r_samples > 0:
        env[-r_samples:] = np.linspace(env[max(0, length - r_samples - 1)], 0, r_samples)
    return env

def env_decay(length, decay_rate=4.0):
    t = np.linspace(0, length / SAMPLE_RATE, length)
    env = np.exp(-decay_rate * t)
    # soft attack to avoid click
    attack = min(int(0.008 * SAMPLE_RATE), length)
    env[:attack] *= np.linspace(0, 1, attack)
    return env

def env_swell(length, peak_pos=0.3):
    t = np.linspace(0, 1, length)
    peak_idx = int(peak_pos * length)
    env = np.zeros(length)
    env[:peak_idx] = np.linspace(0, 1, peak_idx)
    env[peak_idx:] = np.linspace(1, 0, length - peak_idx)
    return env ** 0.7  # soften the curve

# --- Mixing helpers ---

def normalize(sig, peak=0.75):
    mx = np.max(np.abs(sig))
    if mx > 0:
        sig = sig / mx * peak
    return sig

def mix(*signals):
    max_len = max(len(s) for s in signals)
    out = np.zeros(max_len)
    for s in signals:
        out[:len(s)] += s
    return out

def concat(*signals, gap=0.0):
    gap_samples = int(gap * SAMPLE_RATE)
    parts = []
    for i, s in enumerate(signals):
        parts.append(s)
        if i < len(signals) - 1 and gap_samples > 0:
            parts.append(np.zeros(gap_samples))
    return np.concatenate(parts)

def note(waveform_fn, freq, duration, envelope_fn=env_decay, **env_kwargs):
    sig = waveform_fn(freq, duration)
    env = envelope_fn(len(sig), **env_kwargs)
    return sig * env

def chord(waveform_fn, freqs, duration, envelope_fn=env_decay, **env_kwargs):
    signals = [note(waveform_fn, f, duration, envelope_fn, **env_kwargs) for f in freqs]
    return mix(*signals)

def save_wav(filepath, signal, sr=SAMPLE_RATE):
    signal = normalize(signal, 0.75)
    signal_16 = np.int16(signal * 32767)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    wavfile.write(filepath, sr, signal_16)

# --- Musical constants ---
# Note frequencies (octave 4 and 5)
C4=261.63; D4=293.66; E4=329.63; F4=349.23; G4=392.00; A4=440.00; B4=493.88
C5=523.25; D5=587.33; E5=659.25; F5=698.46; G5=783.99; A5=880.00; B5=987.77
C6=1046.50
Eb4=311.13; Bb4=466.16; Ab4=415.30; Gb4=369.99
Eb5=622.25; Bb5=932.33; Ab5=830.61

# --- 10 unique set palettes ---

SET_CONFIGS = {
    1: {
        "name": "Warm Marimba",
        "wave": sine,
        "correct_notes": [(C5, 0.15), (E5, 0.35)],
        "correct_gap": 0.02,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 3.5},
        "incorrect_notes": [(E4, 0.25), (Eb4, 0.4)],
        "incorrect_gap": 0.05,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 2.5},
        "success_notes": [(C4, 0.3), (E4, 0.3), (G4, 0.3), (C5, 0.5), (E5, 0.7)],
        "success_gap": 0.05,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 2.0},
    },
    2: {
        "name": "Music Box",
        "wave": sine,
        "correct_notes": [(G5, 0.12), (C6, 0.4)],
        "correct_gap": 0.03,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 4.0},
        "incorrect_notes": [(A4, 0.3), (Ab4, 0.5)],
        "incorrect_gap": 0.06,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
        "success_notes": [(E5, 0.25), (G5, 0.25), (C6, 0.25), (E5, 0.25), (G5, 0.25), (C6, 0.6)],
        "success_gap": 0.04,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 3.5},
    },
    3: {
        "name": "Soft Bells",
        "wave": triangle,
        "correct_notes": [(E5, 0.2), (G5, 0.5)],
        "correct_gap": 0.01,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
        "incorrect_notes": [(D5, 0.3), (C5, 0.5)],
        "incorrect_gap": 0.08,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 2.0},
        "success_notes": [(C5, 0.35), (E5, 0.35), (G5, 0.35), (C6, 0.8)],
        "success_gap": 0.06,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 1.8},
    },
    4: {
        "name": "Bright Xylophone",
        "wave": sine,
        "correct_notes": [(A5, 0.1), (C6, 0.3)],
        "correct_gap": 0.02,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 5.0},
        "incorrect_notes": [(F4, 0.2), (E4, 0.4)],
        "incorrect_gap": 0.1,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 3.5},
        "success_notes": [(F4, 0.2), (A4, 0.2), (C5, 0.2), (F5, 0.3), (A5, 0.3), (C6, 0.6)],
        "success_gap": 0.03,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
    },
    5: {
        "name": "Gentle Harp",
        "wave": sine,
        "correct_notes": [(D5, 0.18), (A5, 0.45)],
        "correct_gap": 0.04,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 2.5},
        "incorrect_notes": [(G4, 0.35), (Gb4, 0.5)],
        "incorrect_gap": 0.07,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 2.0},
        "success_notes": [(D4, 0.25), (A4, 0.25), (D5, 0.25), (F5, 0.25), (A5, 0.5), (D5, 0.7)],
        "success_gap": 0.05,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 1.5},
    },
    6: {
        "name": "Soft Flute",
        "wave": triangle,
        "correct_notes": [(F5, 0.2), (A5, 0.4)],
        "correct_gap": 0.03,
        "correct_env": {"envelope_fn": env_adsr, "attack": 0.04, "decay": 0.05, "sustain_level": 0.5, "release": 0.15},
        "incorrect_notes": [(Bb4, 0.3), (A4, 0.45)],
        "incorrect_gap": 0.06,
        "incorrect_env": {"envelope_fn": env_adsr, "attack": 0.05, "decay": 0.06, "sustain_level": 0.4, "release": 0.2},
        "success_notes": [(F4, 0.3), (A4, 0.3), (C5, 0.3), (F5, 0.4), (A5, 0.6)],
        "success_gap": 0.05,
        "success_env": {"envelope_fn": env_adsr, "attack": 0.04, "decay": 0.08, "sustain_level": 0.5, "release": 0.25},
    },
    7: {
        "name": "Celesta Chime",
        "wave": sine,
        "correct_notes": [(B4, 0.15), (E5, 0.4)],
        "correct_gap": 0.02,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 4.5},
        "incorrect_notes": [(E5, 0.25), (Eb5, 0.45)],
        "incorrect_gap": 0.05,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
        "success_notes": [(E4, 0.25), (G4, 0.2), (B4, 0.2), (E5, 0.3), (G5, 0.3), (B5, 0.6)],
        "success_gap": 0.04,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 2.5},
    },
    8: {
        "name": "Kalimba",
        "wave": soft_square,
        "correct_notes": [(G4, 0.15), (B4, 0.35)],
        "correct_gap": 0.03,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 5.0},
        "incorrect_notes": [(C5, 0.2), (Bb4, 0.45)],
        "incorrect_gap": 0.06,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 3.5},
        "success_notes": [(G4, 0.2), (B4, 0.2), (D5, 0.2), (G5, 0.3), (B5, 0.5)],
        "success_gap": 0.04,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
    },
    9: {
        "name": "Vibraphone",
        "wave": sine,
        "correct_notes": [(C5, 0.2), (G5, 0.45)],
        "correct_gap": 0.02,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 2.0},
        "incorrect_notes": [(A4, 0.3), (Ab4, 0.5)],
        "incorrect_gap": 0.08,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 1.8},
        "success_notes": [(C4, 0.3), (E4, 0.25), (G4, 0.25), (C5, 0.3), (E5, 0.3), (G5, 0.8)],
        "success_gap": 0.06,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 1.5},
    },
    10: {
        "name": "Wind Chimes",
        "wave": sine,
        "correct_notes": [(E5, 0.1), (A5, 0.15), (C6, 0.35)],
        "correct_gap": 0.02,
        "correct_env": {"envelope_fn": env_decay, "decay_rate": 4.0},
        "incorrect_notes": [(D5, 0.2), (Bb4, 0.5)],
        "incorrect_gap": 0.1,
        "incorrect_env": {"envelope_fn": env_decay, "decay_rate": 2.5},
        "success_notes": [(C5, 0.2), (E5, 0.15), (G5, 0.15), (A5, 0.2), (C6, 0.15), (E5, 0.2), (G5, 0.15), (C6, 0.6)],
        "success_gap": 0.03,
        "success_env": {"envelope_fn": env_decay, "decay_rate": 3.0},
    },
}

def add_vibrato(sig, rate=5.0, depth=0.02):
    """Add gentle vibrato to a signal."""
    t = np.linspace(0, len(sig) / SAMPLE_RATE, len(sig))
    mod = 1.0 + depth * np.sin(2 * np.pi * rate * t)
    return sig * mod

def add_harmonics(sig, freq, duration, wave_fn, harmonic_weights=None):
    """Add soft harmonics to enrich timbre."""
    if harmonic_weights is None:
        harmonic_weights = {2: 0.15, 3: 0.05}
    for h, w in harmonic_weights.items():
        sig = sig + w * wave_fn(freq * h, duration)[:len(sig)]
    return sig

def generate_set(set_num, output_dir="/Users/jackie/src/board/sfx-sets"):
    cfg = SET_CONFIGS[set_num]
    wave_fn = cfg["wave"]
    set_dir = os.path.join(output_dir, f"sfx-set-{set_num}")

    # --- Correct ---
    parts = []
    for freq, dur in cfg["correct_notes"]:
        n = note(wave_fn, freq, dur, **cfg["correct_env"])
        # Add subtle harmonics for richness
        n = n + 0.1 * note(wave_fn, freq * 2, dur, **cfg["correct_env"])
        parts.append(n)
    correct_sig = concat(*parts, gap=cfg["correct_gap"])
    save_wav(os.path.join(set_dir, "correct.wav"), correct_sig)

    # --- Incorrect ---
    parts = []
    for freq, dur in cfg["incorrect_notes"]:
        n = note(wave_fn, freq, dur, **cfg["incorrect_env"])
        parts.append(n)
    incorrect_sig = concat(*parts, gap=cfg["incorrect_gap"])
    # Make incorrect slightly quieter to feel less harsh
    incorrect_sig *= 0.85
    save_wav(os.path.join(set_dir, "incorrect.wav"), incorrect_sig)

    # --- Success End ---
    parts = []
    for freq, dur in cfg["success_notes"]:
        n = note(wave_fn, freq, dur, **cfg["success_env"])
        n = n + 0.12 * note(wave_fn, freq * 2, dur, **cfg["success_env"])
        parts.append(n)
    success_sig = concat(*parts, gap=cfg["success_gap"])

    # Set 9 gets vibrato
    if set_num == 9:
        success_sig = add_vibrato(success_sig, rate=4.5, depth=0.015)

    save_wav(os.path.join(set_dir, "success-end.wav"), success_sig)

    return cfg["name"]

if __name__ == "__main__":
    if len(sys.argv) > 1:
        set_num = int(sys.argv[1])
        name = generate_set(set_num)
        print(f"Generated set {set_num}: {name}")
    else:
        for i in range(1, 11):
            name = generate_set(i)
            print(f"Generated set {i}: {name}")
