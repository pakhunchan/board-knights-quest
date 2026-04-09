"""
Track 5: "Puzzle Time"
Key: Eb major | Tempo: 116 BPM | ~40 seconds
Piano (gentle oom-pah) + Recorder (catchy hook melody)
Only piano (GM 0) and recorder (GM 74). No percussion.
All velocities 35-60.
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# Note map
Eb4 = 63; F4 = 65; G4 = 67; Ab4 = 68; Bb4 = 70; C5 = 72; D5 = 74; Eb5 = 75

# Bass notes
Eb2 = 39; Ab2 = 44; Bb2 = 46; C3 = 48

# Chords as specified
Eb_chord = [51, 55, 58]
Ab_chord = [56, 60, 63]
Bb_chord = [58, 62, 65]
Cm_chord = [48, 51, 55]

# Progression: Eb - Ab - Bb - Eb - Cm - Ab - Bb - Eb
progression = [
    (Eb2, Eb_chord),
    (Ab2, Ab_chord),
    (Bb2, Bb_chord),
    (Eb2, Eb_chord),
    (C3,  Cm_chord),
    (Ab2, Ab_chord),
    (Bb2, Bb_chord),
    (Eb2, Eb_chord),
]

# 20 bars at 116 BPM => 80 beats => 80/116*60 = ~41.4 seconds
TOTAL_BARS = 20


def build_track5():
    midi = create_midi(num_tracks=2, tempo=116)

    PIANO = INSTRUMENTS["piano"]
    RECORDER = INSTRUMENTS["recorder"]

    # ---- PIANO: gentle oom-pah (track 0, channel 0) ----
    midi.addProgramChange(0, 0, 0, PIANO)

    time = 0.0
    for bar_idx in range(TOTAL_BARS):
        chord_idx = bar_idx % 8
        bass_note, chord_notes = progression[chord_idx]

        bass_vel = 40 + (bar_idx % 3)       # 40-42
        chord_vel = 38 + (bar_idx % 4)      # 38-41

        # Beat 1: bass note
        midi.addNote(0, 0, bass_note, time + 0.0, 0.9, bass_vel)
        # Beat 2: chord (pah)
        for n in chord_notes:
            midi.addNote(0, 0, n, time + 1.0, 0.8, chord_vel)
        # Beat 3: bass (alternate octave every other pair of bars)
        bass2 = bass_note + 12 if (bar_idx % 4 >= 2) else bass_note
        midi.addNote(0, 0, bass2, time + 2.0, 0.9, bass_vel - 2)
        # Beat 4: chord (pah)
        for n in chord_notes:
            midi.addNote(0, 0, n, time + 3.0, 0.8, chord_vel - 2)

        time += 4.0

    # ---- RECORDER: catchy hook melody (track 1, channel 1) ----
    midi.addProgramChange(1, 1, 0, RECORDER)

    # Phrase A (2 bars): the main hook - catchy ascending motif
    phrase_a = [
        # Bar 1: "da da da-da da" - bouncy ascending
        (Bb4, 0.5, 55), (G4,  0.5, 50), (Ab4, 0.5, 52), (Bb4, 0.5, 55),
        (Eb5, 1.0, 58), (D5,  0.5, 52), (C5,  0.5, 50),
        # Bar 2: descending answer
        (Bb4, 0.75, 55), (Ab4, 0.75, 50), (G4, 0.5, 48),
        (Eb4, 1.0, 55), (None, 1.0, 0),
    ]

    # Phrase A' (2 bars): slight variation on the hook
    phrase_a_var = [
        # Bar 1: same opening
        (Bb4, 0.5, 55), (G4,  0.5, 50), (Ab4, 0.5, 52), (Bb4, 0.5, 55),
        (C5,  1.0, 56), (Bb4, 0.5, 52), (Ab4, 0.5, 50),
        # Bar 2: different resolution
        (G4,  0.75, 52), (F4,  0.75, 48), (Eb4, 1.5, 55),
        (None, 1.0, 0),
    ]

    # Phrase B (2 bars): contrasting (for Cm - Ab bars)
    phrase_b = [
        # Bar 1
        (C5,  0.5, 55), (D5,  0.5, 52), (Eb5, 1.0, 58),
        (D5,  0.5, 52), (C5,  0.5, 50), (Bb4, 0.5, 48), (Ab4, 0.5, 50),
        # Bar 2
        (Ab4, 0.5, 50), (Bb4, 0.5, 52), (C5,  0.75, 55),
        (Bb4, 0.75, 52), (Ab4, 0.5, 48), (G4,  0.5, 50),
    ]

    # Phrase B' (2 bars): variation resolving to Eb
    phrase_b_var = [
        # Bar 1
        (C5,  0.5, 55), (D5,  0.5, 52), (Eb5, 1.0, 58),
        (D5,  0.5, 52), (C5,  0.5, 50), (Bb4, 0.5, 48), (G4,  0.5, 50),
        # Bar 2: resolves to Eb
        (Ab4, 0.75, 50), (Bb4, 0.75, 55), (Eb5, 1.5, 58),
        (None, 1.0, 0),
    ]

    def add_phrase(notes, start):
        t = start
        for pitch, dur, vel in notes:
            if pitch is not None:
                midi.addNote(1, 1, pitch, t, dur * 0.9, vel)
            t += dur
        return t

    melody_time = 0.0

    # Pass 1 (bars 1-8): A A' B B'
    melody_time = add_phrase(phrase_a, melody_time)
    melody_time = add_phrase(phrase_a_var, melody_time)
    melody_time = add_phrase(phrase_b, melody_time)
    melody_time = add_phrase(phrase_b_var, melody_time)

    # Pass 2 (bars 9-16): A A' B B'
    melody_time = add_phrase(phrase_a, melody_time)
    melody_time = add_phrase(phrase_a_var, melody_time)
    melody_time = add_phrase(phrase_b, melody_time)
    melody_time = add_phrase(phrase_b_var, melody_time)

    # Pass 3 partial (bars 17-20): A + gentle ending
    melody_time = add_phrase(phrase_a, melody_time)
    ending = [
        (Bb4, 0.5, 52), (G4,  0.5, 48), (Ab4, 0.5, 50), (Bb4, 0.5, 52),
        (Eb5, 2.0, 55), (D5,  0.5, 48), (Eb5, 0.5, 45),
        (Eb5, 3.0, 50), (None, 1.0, 0),
    ]
    melody_time = add_phrase(ending, melody_time)

    return midi


if __name__ == "__main__":
    print("Generating Track 5: Puzzle Time")
    mp3_path = generate_track("track-5", build_track5)
    print(f"Output: {mp3_path}")
