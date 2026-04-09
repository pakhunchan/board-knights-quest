"""
Track 11 — "Sunlit Glade"
Key: F major, 112 BPM, 8 bars, piano-only arpeggios
Chord progression: F - Dm - Bb - C - F - Am - Bb - C
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

CHORDS = {
    "F":  [53, 57, 60, 65],
    "Dm": [50, 53, 57, 62],
    "Bb": [46, 50, 53, 58],
    "C":  [48, 52, 55, 60],
    "Am": [45, 48, 52, 57],
}

PROGRESSION = ["F", "Dm", "Bb", "C", "F", "Am", "Bb", "C"]

# Velocity per bar: start 38, swell to 42 in bars 4-5, back to 38 by bar 8
BAR_VELOCITIES = [38, 39, 40, 42, 42, 41, 39, 38]

NOTE_DUR = 0.45       # slightly detached
EIGHTH = 0.5          # eighth note at any tempo = 0.5 beats


def build():
    midi = create_midi(num_tracks=1, tempo=112)
    track = 0
    channel = 0
    midi.addProgramChange(track, channel, 0, INSTRUMENTS["piano"])

    beat = 0.0
    for bar_idx, chord_name in enumerate(PROGRESSION):
        notes = CHORDS[chord_name]
        vel = BAR_VELOCITIES[bar_idx]
        # 2 cycles of root-3rd-5th-octave per bar = 8 eighth notes = 4 beats
        for cycle in range(2):
            for note in notes:
                midi.addNote(track, channel, note, beat, NOTE_DUR, vel)
                beat += EIGHTH

    return midi


if __name__ == "__main__":
    print("Generating Track 11: Sunlit Glade")
    mp3 = generate_track("track-11", build)
    print(f"Output: {mp3}")
