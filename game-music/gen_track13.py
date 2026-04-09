"""
Track 13 — "Crystal Spring"
Key: C major, 120 BPM, 8 bars, piano only
Alberti-style arpeggios (root-5th-3rd-octave) + sustained bass root on beat 1
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# Alberti-style arpeggio patterns: root-5th-3rd-octave
ARPEGGIOS = {
    "C":  [48, 55, 52, 60],
    "Am": [45, 52, 48, 57],
    "F":  [41, 48, 45, 53],
    "G":  [43, 50, 47, 55],
    "Em": [40, 47, 43, 52],
}

# Bass notes (one octave below root)
BASS = {
    "C": 36, "Am": 33, "F": 29, "G": 31, "Em": 28,
}

# 8-bar progression
PROGRESSION = ["C", "Am", "F", "G", "C", "Em", "F", "G"]


def build():
    midi = create_midi(num_tracks=2, tempo=120)

    # Track 0: Alberti arpeggios (piano, channel 0)
    midi.addProgramChange(0, 0, 0, INSTRUMENTS["piano"])

    beat = 0.0
    note_dur = 0.45
    step = 0.5  # eighth notes

    for chord_name in PROGRESSION:
        pattern = ARPEGGIOS[chord_name]
        # 2 cycles of 4 notes = 8 eighth notes = 4 beats = 1 bar
        for cycle in range(2):
            for note in pattern:
                vel = random.randint(38, 42)
                midi.addNote(0, 0, note, beat, note_dur, vel)
                beat += step

    # Track 1: Bass root on beat 1, held 3 beats (piano, channel 1)
    midi.addProgramChange(1, 1, 0, INSTRUMENTS["piano"])

    beat = 0.0
    for chord_name in PROGRESSION:
        bass_note = BASS[chord_name]
        vel = random.randint(35, 38)
        midi.addNote(1, 1, bass_note, beat, 3.0, vel)
        beat += 4.0  # next bar

    return midi


if __name__ == "__main__":
    mp3 = generate_track("track-13", build)
    print(f"Generated: {mp3}")
