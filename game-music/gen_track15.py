"""
Track 15: "Golden Fields"
Key: Eb major, 116 BPM, 12 bars (~24s)
Piano arpeggios + gentle descending figure. Loops cleanly.
"""
import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *
import random

random.seed(15)


def build_track15():
    midi = create_midi(num_tracks=2, tempo=116)

    # Chord voicings: root-3rd-5th-octave
    chords = {
        'Eb': [51, 55, 58, 63],
        'Ab': [44, 48, 51, 56],
        'Bb': [46, 50, 53, 58],
        'Cm': [48, 51, 55, 60],
        'Fm': [41, 44, 48, 53],
    }

    # 12-bar progression
    progression = ['Eb', 'Ab', 'Bb', 'Eb', 'Cm', 'Ab', 'Fm', 'Bb', 'Eb', 'Ab', 'Bb', 'Eb']

    # --- Track 0: Piano arpeggios (channel 0) ---
    midi.addProgramChange(0, 0, 0, INSTRUMENTS['piano'])

    eighth = 0.5
    note_dur = 0.45

    for bar_idx, chord_name in enumerate(progression):
        bar_start = bar_idx * 4.0
        notes = chords[chord_name]
        # 2 arpeggio cycles per bar = 8 eighth notes
        for cycle in range(2):
            cycle_start = bar_start + cycle * 2.0
            for i, pitch in enumerate(notes):
                t = cycle_start + i * eighth
                vel = random.randint(38, 42)
                midi.addNote(0, 0, pitch, t, note_dur, vel)

    # --- Track 1: Gentle descending figure (channel 1) ---
    # Only in bars 1, 5, 9 (0-indexed: 0, 4, 8)
    midi.addProgramChange(1, 1, 0, INSTRUMENTS['piano'])

    # Bar 1 (beat 0): Eb5 quarter, D5 quarter, Bb4 half
    midi.addNote(1, 1, 75, 0.0, 1.0, random.randint(40, 44))
    midi.addNote(1, 1, 74, 1.0, 1.0, random.randint(40, 44))
    midi.addNote(1, 1, 70, 2.0, 2.0, random.randint(40, 44))

    # Bar 5 (beat 16): C5 quarter, Bb4 quarter, Ab4 half
    midi.addNote(1, 1, 72, 16.0, 1.0, random.randint(38, 42))
    midi.addNote(1, 1, 70, 17.0, 1.0, random.randint(38, 42))
    midi.addNote(1, 1, 68, 18.0, 2.0, random.randint(38, 42))

    # Bar 9 (beat 32): Eb5 quarter, D5 quarter, Bb4 half (same as bar 1 for clean loop)
    midi.addNote(1, 1, 75, 32.0, 1.0, random.randint(40, 44))
    midi.addNote(1, 1, 74, 33.0, 1.0, random.randint(40, 44))
    midi.addNote(1, 1, 70, 34.0, 2.0, random.randint(40, 44))

    return midi


if __name__ == '__main__':
    print("Generating Track 15: Golden Fields")
    mp3_path = generate_track('track-15', build_track15)
    print(f"Output: {mp3_path}")
