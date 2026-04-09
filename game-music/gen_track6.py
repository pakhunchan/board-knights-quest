"""
Track 6: "Starlight Waltz"
Key: A major | Tempo: 96 BPM | 3/4 waltz time | ~60 seconds
Mood: Gentle, magical dance under starlight
Instruments: Piano (GM 0) + Flute (GM 73) ONLY
All velocities 35-60. No percussion.
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# A major scale: A4=69, B4=71, C#5=73, D5=74, E5=76, F#5=78, G#5=80, A5=81

# Chords for piano accompaniment (beats 2-3)
A_chord   = [57, 61, 64]   # A3 C#4 E4
E_chord   = [52, 56, 59]   # E3 G#3 B3
D_chord   = [50, 54, 57]   # D3 F#3 A3
Fsm_chord = [54, 57, 61]   # F#3 A3 C#4

# 8-bar progression: A - E - D - A - F#m - D - E - A
CHORD_PROG = [A_chord, E_chord, D_chord, A_chord, Fsm_chord, D_chord, E_chord, A_chord]

# Bass notes for beat 1 (low register)
BASS_NOTES = [45, 40, 38, 45, 42, 38, 40, 45]  # A2, E2, D2, A2, F#2, D2, E2, A2

# At 96 BPM, 3 beats/bar: 32 bars = 96 beats => 60 seconds
BEATS_PER_BAR = 3.0
TOTAL_BARS = 32
REPEATS = TOTAL_BARS // 8  # 4


def build_track6():
    midi = create_midi(num_tracks=2, tempo=96)

    # === Track 0: Piano waltz accompaniment ===
    # Beat 1: bass note; Beats 2 & 3: chord tones
    midi.addProgramChange(0, 0, 0, INSTRUMENTS["piano"])

    time = 0.0
    for rep in range(REPEATS):
        for bar_idx in range(8):
            chord = CHORD_PROG[bar_idx]
            bass = BASS_NOTES[bar_idx]

            # Dynamic shaping: softer at start and end
            if rep == 0 and bar_idx < 4:
                bass_vel = 38
                chord_vel = 38
            elif rep == 3 and bar_idx >= 4:
                # Gentle fade for ending
                bass_vel = 38
                chord_vel = 36
            else:
                bass_vel = 42
                chord_vel = 40

            # Beat 1: bass note (held ~1.5 beats)
            midi.addNote(0, 0, bass, time, 1.5, bass_vel)

            # Beat 2: chord hit
            for note in chord:
                midi.addNote(0, 0, note, time + 1.0, 0.85, chord_vel)

            # Beat 3: chord hit (slightly softer)
            for note in chord:
                midi.addNote(0, 0, note, time + 2.0, 0.85, max(35, chord_vel - 2))

            time += BEATS_PER_BAR

    # === Track 1: Flute melody ===
    # Graceful waltz melody, each phrase = 8 bars = 24 beats
    midi.addProgramChange(1, 1, 0, INSTRUMENTS["flute"])

    # Phrase A (bars 1-8): opening theme
    phrase_a = [
        # Bar 1 (A): gentle opening
        (73, 1.5, 0),   (71, 0.75, -3),  (69, 0.75, -2),
        # Bar 2 (E): rising
        (71, 1.0, 0),   (73, 1.0, 2),    (76, 1.0, 3),
        # Bar 3 (D): graceful turn
        (74, 1.5, 2),   (73, 0.75, 0),   (71, 0.75, -2),
        # Bar 4 (A): settling
        (69, 2.0, -2),  (71, 1.0, 0),
        # Bar 5 (F#m): yearning upward
        (73, 1.0, 2),   (74, 1.0, 3),    (76, 1.0, 5),
        # Bar 6 (D): peak and float
        (78, 1.5, 5),   (76, 0.75, 2),   (74, 0.75, 0),
        # Bar 7 (E): winding down
        (73, 1.0, 0),   (71, 1.0, -2),   (69, 1.0, -3),
        # Bar 8 (A): resolve with breath
        (69, 2.0, -2),  (None, 1.0, 0),
    ]

    # Phrase B (bars 9-16): higher variation, more ornamental
    phrase_b = [
        # Bar 9 (A): start higher
        (76, 1.0, 3),   (73, 0.5, 0),    (74, 0.5, 2),    (76, 1.0, 3),
        # Bar 10 (E): dancing figure
        (78, 0.75, 5),  (76, 0.75, 2),   (73, 0.75, 0),   (71, 0.75, -2),
        # Bar 11 (D): sweeping line
        (74, 1.5, 2),   (76, 0.75, 3),   (78, 0.75, 5),
        # Bar 12 (A): arc down
        (81, 1.5, 5),   (78, 0.75, 2),   (76, 0.75, 0),
        # Bar 13 (F#m): tender moment
        (73, 1.5, 0),   (74, 0.75, 2),   (73, 0.75, 0),
        # Bar 14 (D): gentle movement
        (71, 1.0, -2),  (74, 1.0, 2),    (73, 1.0, 0),
        # Bar 15 (E): building toward resolution
        (76, 1.0, 3),   (74, 0.75, 2),   (73, 0.75, 0),   (71, 0.5, -2),
        # Bar 16 (A): resolve and breathe
        (69, 2.5, -2),  (None, 0.5, 0),
    ]

    # Phrase C (bars 17-24): reprise with variation
    phrase_c = [
        # Bar 17 (A): pickup into melody
        (69, 0.75, -3), (71, 0.75, -2),  (73, 1.5, 2),
        # Bar 18 (E): flowing up
        (74, 1.0, 2),   (76, 1.0, 3),    (78, 1.0, 5),
        # Bar 19 (D): graceful turn
        (76, 1.0, 3),   (74, 0.5, 0),    (73, 0.5, -2),   (74, 1.0, 2),
        # Bar 20 (A): settling
        (73, 1.5, 0),   (71, 1.5, -2),
        # Bar 21 (F#m): expressive leap
        (66, 1.0, -5),  (73, 1.0, 2),    (76, 1.0, 3),
        # Bar 22 (D): sustained peak
        (78, 2.0, 5),   (76, 1.0, 2),
        # Bar 23 (E): winding down
        (74, 1.0, 0),   (73, 1.0, -2),   (71, 1.0, -3),
        # Bar 24 (A): resolve
        (69, 2.0, -3),  (None, 1.0, 0),
    ]

    # Phrase D (bars 25-32): gentle ending, trailing off
    phrase_d = [
        # Bar 25 (A): soft recall
        (73, 1.5, -2),  (71, 0.75, -5),  (69, 0.75, -5),
        # Bar 26 (E): echo
        (71, 1.5, -3),  (73, 1.5, -2),
        # Bar 27 (D): floating
        (74, 2.0, -2),  (73, 1.0, -3),
        # Bar 28 (A): gentle descent
        (71, 1.5, -5),  (69, 1.5, -5),
        # Bar 29 (F#m): wistful
        (66, 1.5, -5),  (69, 1.5, -5),
        # Bar 30 (D): fading
        (74, 2.0, -3),  (71, 1.0, -5),
        # Bar 31 (E): penultimate
        (73, 1.5, -5),  (71, 1.5, -5),
        # Bar 32 (A): final long note
        (69, 3.0, -5),
    ]

    flute_melody = phrase_a + phrase_b + phrase_c + phrase_d
    add_melody(midi, track=1, channel=1, instrument=INSTRUMENTS["flute"],
               notes=flute_melody, start_time=0, velocity_base=52)

    return midi


if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print("Generating Track 6: Starlight Waltz...")
    mp3_path = generate_track("track-6", build_track6)
    print(f"\nTrack 6 saved to: {mp3_path}")
