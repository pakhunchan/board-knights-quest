"""
Track 12: "Moonlit Tower"
Key: G major, 108 BPM, 8 bars, piano only
Gentle arpeggios + sparse high melody hints
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

def build_track12():
    midi = create_midi(num_tracks=2, tempo=108)

    # --- Track 0: Left hand arpeggios (piano) ---
    track_lh = 0
    ch_lh = 0
    midi.addProgramChange(track_lh, ch_lh, 0, INSTRUMENTS["piano"])

    # Chord progression: G - Em - C - D - G - Em - Am - D
    arp_chords = [
        [43, 47, 50, 55],  # G
        [40, 43, 47, 52],  # Em
        [36, 40, 43, 48],  # C
        [38, 42, 45, 50],  # D
        [43, 47, 50, 55],  # G
        [40, 43, 47, 52],  # Em
        [33, 36, 40, 45],  # Am
        [38, 42, 45, 50],  # D
    ]

    # Each bar = 4 beats. Eighth notes = 0.5 beats each.
    # 4 notes per cycle, 2 cycles per bar = 8 eighth notes = 4 beats. Perfect.
    note_dur = 0.45  # slightly detached
    eighth = 0.5

    time = 0.0
    for chord in arp_chords:
        for cycle in range(2):
            for i, pitch in enumerate(chord):
                vel = 38 + (i % 3)  # 38-40 range, gentle variation
                midi.addNote(track_lh, ch_lh, pitch, time, note_dur, vel)
                time += eighth

    # --- Track 1: Right hand sparse melody hints (piano) ---
    track_rh = 1
    ch_rh = 1
    midi.addProgramChange(track_rh, ch_rh, 0, INSTRUMENTS["piano"])

    # Sparse notes: bar number -> list of (beat_offset, pitch, duration, velocity)
    # Bars 3, 6, 8 are silent (0-indexed: bars 2, 5, 7)
    # G4=67, A4=69, B4=71, C5=72, D5=74
    rh_notes = {
        0: [(2.0, 71, 1.5, 42)],          # Bar 1: B4 on beat 3
        1: [(1.0, 72, 2.0, 40)],          # Bar 2: C5 on beat 2
        # Bar 3: silent
        3: [(3.0, 69, 1.0, 41)],          # Bar 4: A4 on beat 4
        4: [(1.5, 74, 1.5, 43)],          # Bar 5: D5 on beat 2.5
        # Bar 6: silent
        6: [(2.5, 67, 1.5, 40)],          # Bar 7: G4 on beat 3.5
        # Bar 8: silent
    }

    for bar_idx, notes in rh_notes.items():
        bar_start = bar_idx * 4.0
        for beat_off, pitch, dur, vel in notes:
            midi.addNote(track_rh, ch_rh, pitch, bar_start + beat_off, dur, vel)

    return midi


if __name__ == "__main__":
    print("Generating Track 12: Moonlit Tower")
    mp3_path = generate_track("track-12", build_track12)
    print(f"Output: {mp3_path}")
