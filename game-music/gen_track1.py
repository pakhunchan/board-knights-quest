"""
Track 1: "Morning Meadow"
Soft, gentle background music for a knight/adventure math game.
Key: C major | Tempo: 108 BPM | ~50 seconds
Instruments: Piano (arpeggiated chords) + Flute (sweet melody)
Chord progression: C - Am - F - G (x4 = 16 bars)
"""

import sys
import os
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import (
    create_midi, add_melody, save_midi, generate_track,
    INSTRUMENTS, OUTPUT_DIR,
)


TEMPO = 108
BEATS_PER_BAR = 4
BARS = 20  # 4 chords x 5 repeats (incl. intro + ending)


def build_track():
    midi = create_midi(num_tracks=2, tempo=TEMPO)

    # -----------------------------------------------------------------
    # Piano: gentle arpeggiated chords (track 0, channel 0)
    # Warm voicings in C3-C4 range, one note at a time per eighth note
    # -----------------------------------------------------------------
    piano = INSTRUMENTS["piano"]
    midi.addProgramChange(0, 0, 0, piano)

    # Chord voicings (4 notes each, low-to-high)
    chord_voicings = {
        "C":  [48, 52, 55, 60],   # C3 E3 G3 C4
        "Am": [45, 48, 52, 57],   # A2 C3 E3 A3
        "F":  [41, 48, 53, 57],   # F2 C3 F3 A3
        "G":  [43, 47, 50, 55],   # G2 B2 D3 G3
    }

    progression = ["C", "Am", "F", "G"]

    # Arpeggio index patterns (into the 4-note voicing) - vary for interest
    arp_patterns = [
        [0, 1, 2, 3, 2, 1, 0, 1],  # up-down-up
        [0, 2, 1, 3, 2, 0, 1, 2],  # skip pattern
        [0, 1, 2, 3, 2, 1, 0, 2],  # variant
        [0, 1, 2, 3, 3, 2, 1, 0],  # up then down
    ]

    time = 0.0
    for rep in range(5):
        for ci, chord_name in enumerate(progression):
            notes = chord_voicings[chord_name]
            pattern = arp_patterns[(rep + ci) % len(arp_patterns)]
            for i, idx in enumerate(pattern):
                pitch = notes[idx]
                # Gentle velocity: cycles through 42, 45, 48 (all within 40-50)
                vel = 42 + (i % 3) * 3
                midi.addNote(0, 0, pitch, time + i * 0.5, 0.9, vel)
            time += 4.0  # one bar = 4 beats

    # -----------------------------------------------------------------
    # Flute: sweet stepwise melody (track 1, channel 1)
    # Mostly quarter and half notes, singable, velocity 45-55
    # Sits in octave 4-5 range above the piano
    # -----------------------------------------------------------------
    flute = INSTRUMENTS["flute"]

    # Melody as (pitch, duration, velocity_offset) -- velocity_base=48
    # None pitch = rest

    # Phrase 1 (bars 1-4): gentle ascending opening
    phrase1 = [
        # Bar 1 (C): C5 D5 E5 G5
        (72, 1.0, 0), (74, 1.0, -3), (76, 1.0, 2), (79, 1.0, -2),
        # Bar 2 (Am): A4 C5 B4 A4
        (69, 1.0, -3), (72, 1.0, 0), (71, 1.0, -5), (69, 1.0, -2),
        # Bar 3 (F): F4 A4 G4(half)
        (65, 1.0, 2), (69, 1.0, 0), (67, 2.0, -3),
        # Bar 4 (G): G4 A4 B4 C5
        (67, 1.0, 0), (69, 1.0, 2), (71, 1.0, -2), (72, 1.0, 0),
    ]

    # Phrase 2 (bars 5-8): lyrical answer with half notes
    phrase2 = [
        # Bar 5 (C): E5(dotted q) D5 C5(dotted q)
        (76, 1.5, 2), (74, 1.0, -2), (72, 1.5, 0),
        # Bar 6 (Am): A4(dotted q) B4 C5(dotted q)
        (69, 1.5, -3), (71, 1.0, 0), (72, 1.5, 2),
        # Bar 7 (F): F4 G4 A4 G4
        (65, 1.0, 0), (67, 1.0, 2), (69, 1.0, -2), (67, 1.0, 0),
        # Bar 8 (G): G4(half) B4 C5
        (67, 2.0, -3), (71, 1.0, 2), (72, 1.0, 0),
    ]

    # Phrase 3 (bars 9-12): slightly more movement
    phrase3 = [
        # Bar 9 (C): C5 E5 D5 C5
        (72, 1.0, 0), (76, 1.0, 3), (74, 1.0, -2), (72, 1.0, 0),
        # Bar 10 (Am): A4(half) C5 B4
        (69, 2.0, -3), (72, 1.0, 2), (71, 1.0, 0),
        # Bar 11 (F): A4 G4(dotted q) F4(dotted q)
        (69, 1.0, 2), (67, 1.5, 0), (65, 1.5, -3),
        # Bar 12 (G): G4 A4 B4(half)
        (67, 1.0, 0), (69, 1.0, 2), (71, 2.0, -2),
    ]

    # Phrase 4 (bars 13-16): gentle resolution
    phrase4 = [
        # Bar 13 (C): E5 D5 C5(half)
        (76, 1.0, 3), (74, 1.0, 0), (72, 2.0, -2),
        # Bar 14 (Am): C5 B4 A4(half)
        (72, 1.0, 0), (71, 1.0, -3), (69, 2.0, 0),
        # Bar 15 (F): F4 G4 A4(half)
        (65, 1.0, -2), (67, 1.0, 0), (69, 2.0, 2),
        # Bar 16 (G->C): G4(half) rest C5
        (67, 2.0, -3), (None, 1.0, 0), (72, 1.0, -5),
    ]

    # Phrase 5 (bars 17-20): warm coda, fading out
    phrase5 = [
        # Bar 17 (C): C5(half) E5(half)
        (72, 2.0, -2), (76, 2.0, -5),
        # Bar 18 (Am): A4(half) C5(half)
        (69, 2.0, -5), (72, 2.0, -3),
        # Bar 19 (F): F4 G4 A4(half)
        (65, 1.0, -5), (67, 1.0, -3), (69, 2.0, -5),
        # Bar 20 (G->C): G4(half) rest C5(long)
        (67, 1.5, -5), (None, 0.5, 0), (72, 2.0, -3),
    ]

    melody = phrase1 + phrase2 + phrase3 + phrase4 + phrase5
    add_melody(midi, 1, 1, flute, melody, start_time=0.0, velocity_base=48)

    return midi


if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print("Generating Track 1: Morning Meadow")
    mp3_path = generate_track("track-1", build_track)
    print(f"\nOutput: {mp3_path}")
