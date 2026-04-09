"""
Track 14: "Whispering Woods"
Key: D minor (natural minor), 112 BPM, 8 bars (~17s)
Piano arpeggios + gentle flute motif. Loops cleanly.
"""
import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *
import random

random.seed(14)


def build_track14():
    midi = create_midi(num_tracks=2, tempo=112)

    # Beat duration at 112 BPM: quarter = 1 beat, bar = 4 beats
    # 8 bars = 32 beats total

    # Chord voicings: root-3rd-5th-octave
    chords = {
        'Dm': [50, 53, 57, 62],
        'Bb': [46, 50, 53, 58],
        'C':  [48, 52, 55, 60],
        'Gm': [43, 46, 50, 55],
    }

    progression = ['Dm', 'Bb', 'C', 'Dm', 'Gm', 'Bb', 'C', 'Dm']

    # --- Track 0: Piano arpeggios (channel 0) ---
    midi.addProgramChange(0, 0, 0, INSTRUMENTS['piano'])

    eighth = 0.5  # eighth note duration in beats
    note_dur = 0.45  # slightly detached

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

    # --- Track 1: Flute melody (channel 1) ---
    # Plays in bars 1-2 and bars 5-6 (0-indexed: 0-1, 4-5), silent otherwise
    midi.addProgramChange(1, 1, 0, INSTRUMENTS['flute'])

    def add_flute_phrase(start_beat):
        # Bar 1: D4(62) half, F4(65) half
        midi.addNote(1, 1, 62, start_beat + 0.0, 2.0, random.randint(42, 45))
        midi.addNote(1, 1, 65, start_beat + 2.0, 2.0, random.randint(42, 45))
        # Bar 2: E4(64) dotted quarter (1.5), D4(62) quarter (1.0), C4(60) half (2.0)
        # total = 1.5 + 1.0 + 1.5 = 4.0 (adjust last note to fill bar)
        midi.addNote(1, 1, 64, start_beat + 4.0, 1.5, random.randint(40, 44))
        midi.addNote(1, 1, 62, start_beat + 5.5, 1.0, random.randint(40, 44))
        midi.addNote(1, 1, 60, start_beat + 6.5, 1.5, random.randint(40, 44))

    # First phrase: bars 1-2 (beats 0-7)
    add_flute_phrase(0.0)
    # Second phrase: bars 5-6 (beats 16-23)
    add_flute_phrase(16.0)

    return midi


if __name__ == '__main__':
    print("Generating Track 14: Whispering Woods")
    mp3_path = generate_track('track-14', build_track14)
    print(f"Output: {mp3_path}")
