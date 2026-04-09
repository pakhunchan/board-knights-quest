"""
Track 9: "Dreaming Knight"
Key: G major pentatonic | Tempo: 92 BPM | ~65 seconds
Piano (gentle arpeggios) + Flute (dreamy melody) + Recorder (delayed echo)
The gentlest, most ambient track -- lots of space and breathing room.
"""

import os
import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# G pentatonic: G3=55, A3=57, B3=59, D4=62, E4=64, G4=67
G_PENT = SCALES["G_pentatonic"]

# Chord voicings (mid-low register)
CHORDS = {
    'G':  [43, 47, 50],   # G2, B2, D3
    'C':  [48, 52, 55],   # C3, E3, G3
    'Em': [52, 55, 59],   # E3, G3, B3
    'D':  [50, 54, 57],   # D3, F#3, A3
}

# 8-bar progression: G - C - Em - D - G - C - D - G
PROGRESSION = ['G', 'C', 'Em', 'D', 'G', 'C', 'D', 'G']

# 3 repeats of the 8-bar progression = 24 bars = 96 beats
# At 92 BPM: 96 beats * (60/92) = 62.6 seconds (close to 65s target)
TOTAL_BARS = 24
BEATS_PER_BAR = 4.0
TOTAL_BEATS = TOTAL_BARS * BEATS_PER_BAR  # 96


def build_track9():
    midi = create_midi(num_tracks=3, tempo=92)

    full_progression = PROGRESSION * 3  # 24 bars

    # ===== Track 0: Piano -- gentle arpeggios (velocity 35-40) =====
    # Sparse, with rests. Mid-low register.
    piano_track = 0
    piano_channel = 0
    midi.addProgramChange(piano_track, piano_channel, 0, INSTRUMENTS['piano'])

    time = 0.0
    for bar_idx, chord_name in enumerate(full_progression):
        chord_notes = CHORDS[chord_name]
        pattern_type = bar_idx % 6

        if pattern_type == 0:
            # Ascending arpeggio on beats 1, 2.5, 3
            midi.addNote(piano_track, piano_channel, chord_notes[0], time + 0.0, 1.5, 37)
            midi.addNote(piano_track, piano_channel, chord_notes[1], time + 1.5, 1.0, 35)
            midi.addNote(piano_track, piano_channel, chord_notes[2], time + 3.0, 1.0, 38)
        elif pattern_type == 1:
            # Root and fifth only, space in between
            midi.addNote(piano_track, piano_channel, chord_notes[0], time + 0.0, 2.0, 36)
            midi.addNote(piano_track, piano_channel, chord_notes[2], time + 2.5, 1.5, 38)
        elif pattern_type == 2:
            # Rest bar -- only one gentle note
            midi.addNote(piano_track, piano_channel, chord_notes[1], time + 1.0, 2.5, 35)
        elif pattern_type == 3:
            # Descending arpeggio
            midi.addNote(piano_track, piano_channel, chord_notes[2], time + 0.0, 1.0, 38)
            midi.addNote(piano_track, piano_channel, chord_notes[1], time + 1.5, 1.0, 36)
            midi.addNote(piano_track, piano_channel, chord_notes[0], time + 3.0, 1.0, 37)
        elif pattern_type == 4:
            # Wider spacing -- root and top
            midi.addNote(piano_track, piano_channel, chord_notes[0], time + 0.0, 1.5, 40)
            midi.addNote(piano_track, piano_channel, chord_notes[2], time + 2.0, 2.0, 36)
        else:
            # Near-silent bar, just root
            midi.addNote(piano_track, piano_channel, chord_notes[0], time + 0.5, 3.0, 35)

        time += BEATS_PER_BAR

    # ===== Track 1: Flute -- dreamy melody, long notes and rests (velocity 45-52) =====
    flute_track = 1
    flute_channel = 1
    midi.addProgramChange(flute_track, flute_channel, 0, INSTRUMENTS['flute'])

    # Three 8-bar phrases (32 beats each).
    # Melody uses G pentatonic: G3=55, A3=57, B3=59, D4=62, E4=64, G4=67

    # Phrase A (bars 1-8): gentle ascending theme
    phrase_a = [
        # (pitch, start_beat_in_phrase, duration, velocity)
        (62, 0.0, 3.0, 48),     # D4
        (64, 5.0, 2.5, 50),     # E4
        (67, 8.0, 4.0, 52),     # G4 -- held
        (64, 14.0, 2.0, 48),    # E4
        (62, 17.0, 3.0, 50),    # D4
        (59, 22.0, 2.5, 47),    # B3
        (57, 25.0, 3.0, 45),    # A3
        (55, 29.0, 3.0, 48),    # G3 -- resolve
    ]

    # Phrase B (bars 9-16): different contour
    phrase_b = [
        (55, 0.0, 2.5, 47),     # G3
        (59, 4.0, 3.0, 50),     # B3
        (62, 8.0, 3.5, 52),     # D4
        (64, 13.0, 2.0, 48),    # E4
        (62, 16.0, 3.0, 50),    # D4
        (67, 21.0, 4.0, 52),    # G4 -- climax
        (64, 26.0, 2.0, 47),    # E4
        (62, 29.0, 3.0, 48),    # D4 -- settle
    ]

    # Phrase C (bars 17-24): winding down, even more space
    phrase_c = [
        (62, 0.0, 3.5, 47),     # D4
        (59, 5.0, 3.0, 45),     # B3
        (57, 10.0, 2.5, 45),    # A3
        (55, 14.0, 4.0, 48),    # G3 -- long
        (57, 20.0, 2.0, 45),    # A3
        (59, 24.0, 3.0, 47),    # B3
        (55, 28.0, 4.0, 45),    # G3 -- final resolve
    ]

    phrases = [phrase_a, phrase_b, phrase_c]

    # Store all flute notes for the recorder echo
    all_flute_notes = []

    for phrase_idx, phrase in enumerate(phrases):
        phrase_start = phrase_idx * 32.0
        for pitch, beat_offset, duration, velocity in phrase:
            abs_time = phrase_start + beat_offset
            midi.addNote(flute_track, flute_channel, pitch,
                         abs_time, duration, velocity)
            all_flute_notes.append((pitch, abs_time, duration, velocity))

    # ===== Track 2: Recorder -- soft echoes, delayed by 2 beats (velocity 38-45) =====
    recorder_track = 2
    recorder_channel = 2
    midi.addProgramChange(recorder_track, recorder_channel, 0, INSTRUMENTS['recorder'])

    echo_delay = 2.0  # 2 beats behind flute

    # Echo roughly every other flute note for sparse fragments
    for i, (pitch, abs_time, duration, velocity) in enumerate(all_flute_notes):
        if i % 2 == 0:  # every other note
            echo_time = abs_time + echo_delay
            echo_vel = max(38, min(45, velocity - 7))
            echo_dur = max(1.5, duration - 0.5)

            # Don't go past the end
            if echo_time + echo_dur <= TOTAL_BEATS:
                midi.addNote(recorder_track, recorder_channel, pitch,
                             echo_time, echo_dur, echo_vel)

    return midi


if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    mp3_path = generate_track("track-9", build_track9)
    print(f"\nTrack 9 generated: {mp3_path}")
