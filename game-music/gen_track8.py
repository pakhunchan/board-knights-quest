"""
Track 8: "Enchanted Path"
Key: Bb major, 108 BPM, ~50 seconds
Recorder melody (shepherd's tune) + Piano Alberti bass accompaniment
Only piano (GM 0) and recorder (GM 74). No percussion.
All velocities 35-60.
"""
import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# Bb major scale: Bb3=58, C4=60, D4=62, Eb4=63, F4=65, G4=67, A4=69, Bb4=70

# Alberti bass patterns per chord (root-5th-3rd-5th)
# Bb major: Bb2=46, D3=50, F3=53
# Eb major: Eb2=39, G2=43, Bb2=46
# F major:  F2=41, A2=45, C3=48
# Gm:       G2=43, Bb2=46, D3=50
ALBERTI = {
    "Bb": [46, 53, 50, 53],
    "Eb": [39, 46, 43, 46],
    "F":  [41, 48, 45, 48],
    "Gm": [43, 50, 46, 50],
}

# 8-bar progression, repeated 3 times = 24 bars
# 24 bars * 4 beats / 1.8 bps = ~53 seconds
PROGRESSION = ["Bb", "Eb", "F", "Bb", "Gm", "Eb", "F", "Bb"]
REPEATS = 3
TOTAL_BARS = len(PROGRESSION) * REPEATS


def build_track8():
    midi = create_midi(num_tracks=2, tempo=108)

    # === Track 0: Piano Alberti bass ===
    midi.addProgramChange(0, 0, 0, INSTRUMENTS["piano"])
    time = 0.0
    for _rep in range(REPEATS):
        for chord_name in PROGRESSION:
            pattern = ALBERTI[chord_name]
            # 8 eighth notes per bar (root-5th-3rd-5th repeated twice)
            for i in range(8):
                note = pattern[i % 4]
                vel = 37 + (i % 3)  # subtle variation 37-39, well within 35-42
                midi.addNote(0, 0, note, time, 0.45, vel)
                time += 0.5

    # === Track 1: Recorder melody ===
    midi.addProgramChange(1, 1, 0, INSTRUMENTS["recorder"])

    # Melody: gentle, stepwise, shepherd's tune with sweet thirds
    # velocity_base=50, offsets kept to -5..+5 so range is 45-55

    # Section A (bars 1-8): Opening shepherd's call
    phrase_a = [
        # bar 1 (Bb): gentle stepwise ascent
        (62, 0.75, 3),   # D
        (63, 0.25, 0),   # Eb
        (65, 1.0, 5),    # F
        (67, 0.75, 3),   # G
        (65, 0.5, 0),    # F
        (62, 0.75, -3),  # D
        # bar 2 (Eb): settling
        (63, 0.5, 0),    # Eb
        (62, 0.5, -3),   # D
        (60, 1.0, 0),    # C
        (58, 0.75, -3),  # Bb
        (60, 0.5, 0),    # C
        (62, 0.75, 0),   # D
        # bar 3 (F): rising gently
        (60, 0.5, -3),   # C
        (62, 0.5, 0),    # D
        (65, 1.0, 5),    # F
        (67, 0.75, 3),   # G
        (65, 0.5, 0),    # F
        (63, 0.75, 0),   # Eb
        # bar 4 (Bb): gentle landing with breath
        (62, 1.0, 0),    # D
        (58, 0.75, -5),  # Bb
        (None, 0.25, 0),
        (60, 0.75, -3),  # C
        (62, 1.25, 0),   # D
        # bar 5 (Gm): warm minor moment
        (67, 0.75, 3),   # G
        (65, 0.5, 0),    # F
        (62, 0.75, 0),   # D
        (63, 0.5, -3),   # Eb
        (62, 0.5, -3),   # D
        (60, 1.0, -3),   # C
        # bar 6 (Eb): tender descent
        (63, 0.75, 0),   # Eb
        (62, 0.5, -3),   # D
        (60, 0.75, 0),   # C
        (58, 0.5, -5),   # Bb
        (55, 0.5, -5),   # F3 (lower)
        (58, 1.0, -3),   # Bb
        # bar 7 (F): lifting
        (60, 0.5, -3),   # C
        (62, 0.5, 0),    # D
        (63, 0.5, 0),    # Eb
        (65, 0.5, 3),    # F
        (67, 1.0, 5),    # G
        (69, 0.5, 3),    # A
        (67, 0.5, 0),    # G
        # bar 8 (Bb): resolution with rest
        (65, 0.75, 0),   # F
        (62, 0.75, -3),  # D
        (58, 1.5, 0),    # Bb (held)
        (None, 1.0, 0),
    ]

    # Section B (bars 9-16): Blossoming higher register
    phrase_b = [
        # bar 9 (Bb): higher singing
        (65, 0.5, 3),    # F
        (67, 0.75, 5),   # G
        (69, 0.75, 5),   # A
        (70, 1.0, 5),    # Bb high
        (69, 0.5, 3),    # A
        (67, 0.5, 0),    # G
        # bar 10 (Eb): flowing down
        (65, 0.75, 0),   # F
        (63, 0.5, -3),   # Eb
        (62, 0.75, 0),   # D
        (60, 0.75, -3),  # C
        (58, 0.5, -5),   # Bb
        (60, 0.75, 0),   # C
        # bar 11 (F): graceful arc
        (62, 0.5, 0),    # D
        (63, 0.5, 0),    # Eb
        (65, 1.0, 3),    # F
        (67, 0.75, 5),   # G
        (69, 0.75, 5),   # A
        (70, 0.5, 5),    # Bb
        # bar 12 (Bb): tender peak and settle
        (69, 0.5, 3),    # A
        (67, 0.75, 3),   # G
        (65, 0.75, 0),   # F
        (62, 1.0, 0),    # D
        (None, 1.0, 0),
        # bar 13 (Gm): expressive minor
        (67, 1.0, 5),    # G
        (65, 0.5, 0),    # F
        (62, 0.75, 0),   # D
        (60, 0.75, -3),  # C
        (58, 0.5, -5),   # Bb
        (60, 0.5, -3),   # C
        # bar 14 (Eb): gentle sighing
        (63, 0.75, 0),   # Eb
        (62, 0.5, -3),   # D
        (60, 1.0, 0),    # C
        (58, 0.5, -5),   # Bb
        (55, 0.5, -5),   # F3
        (58, 0.75, -3),  # Bb
        # bar 15 (F): building to close
        (60, 0.5, -3),   # C
        (62, 0.5, 0),    # D
        (65, 0.75, 3),   # F
        (67, 0.5, 5),    # G
        (69, 1.0, 5),    # A
        (67, 0.75, 3),   # G
        # bar 16 (Bb): warm resolution
        (65, 0.75, 0),   # F
        (62, 1.0, 0),    # D
        (58, 1.5, 0),    # Bb
        (None, 0.75, 0),
    ]

    # Section C (bars 17-24): Gentle farewell, fading
    phrase_c = [
        # bar 17 (Bb): soft echo of opening
        (62, 0.75, 0),   # D
        (63, 0.25, -3),  # Eb
        (65, 1.0, 3),    # F
        (67, 0.75, 3),   # G
        (65, 0.5, 0),    # F
        (62, 0.75, -3),  # D
        # bar 18 (Eb): settling down
        (63, 0.5, -3),   # Eb
        (62, 0.5, -5),   # D
        (60, 1.0, -3),   # C
        (58, 0.75, -5),  # Bb
        (60, 0.5, -3),   # C
        (62, 0.75, -3),  # D
        # bar 19 (F): tender rise
        (60, 0.5, -3),   # C
        (62, 0.5, 0),    # D
        (65, 1.0, 3),    # F
        (67, 0.75, 3),   # G
        (65, 0.5, 0),    # F
        (63, 0.75, -3),  # Eb
        # bar 20 (Bb): peaceful breath
        (62, 1.0, 0),    # D
        (58, 1.0, -5),   # Bb
        (None, 0.5, 0),
        (60, 0.75, -3),  # C
        (62, 0.75, -3),  # D
        # bar 21 (Gm): quiet minor
        (67, 0.75, 0),   # G
        (65, 0.5, -3),   # F
        (62, 0.75, -3),  # D
        (60, 1.0, -5),   # C
        (58, 1.0, -5),   # Bb
        # bar 22 (Eb): lullaby descent
        (63, 0.75, -3),  # Eb
        (62, 0.5, -5),   # D
        (60, 1.0, -3),   # C
        (58, 1.0, -5),   # Bb
        (None, 0.75, 0),
        # bar 23 (F): last gentle lift
        (60, 0.75, -3),  # C
        (62, 0.75, -3),  # D
        (65, 1.0, 0),    # F
        (67, 1.0, 0),    # G
        (None, 0.5, 0),
        # bar 24 (Bb): final long held notes
        (65, 1.0, 0),    # F
        (62, 1.0, -3),   # D
        (58, 2.0, 0),    # Bb (long held)
    ]

    recorder_notes = phrase_a + phrase_b + phrase_c
    add_melody(midi, track=1, channel=1, instrument=INSTRUMENTS["recorder"],
               notes=recorder_notes, start_time=0, velocity_base=50)

    return midi


if __name__ == "__main__":
    print("Generating Track 8: Enchanted Path...")
    mp3_path = generate_track("track-8", build_track8)
    print(f"\nTrack 8 complete: {mp3_path}")
