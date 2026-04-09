"""
Track 3: "Castle Garden"
Knight/adventure math game for 3rd graders.

Key: F major | Tempo: 112 BPM | ~45 seconds
Instruments: Piano only (two hands / two tracks)
  Track 0 (right hand): Light melody in upper register (F4-F5), velocity 45-55
  Track 1 (left hand): Gentle arpeggiated accompaniment (F3-F4), velocity 35-45
Chord progression: F - Dm - Bb - C (repeat 4x + ending)
Melody: Playful but soft, like a music box -- short phrases with rests
Arpeggio: root-3rd-5th-octave repeating eighth notes
Structure: 4-bar intro (LH only) + 4 cycles melody + 2-bar ending = ~47 sec
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import (
    create_midi, INSTRUMENTS, generate_track,
)

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
TEMPO = 112
BEATS_PER_BAR = 4
BARS_PER_CYCLE = 4          # F - Dm - Bb - C
INTRO_BARS = 4              # LH arpeggio only
MAIN_CYCLES = 4             # 4 repetitions of the 4-bar progression
ENDING_BARS = 2
# Total: 4 + 16 + 2 = 22 bars * (4 beats / 112 bpm) * 60 = ~47 sec

# Chord definitions for left-hand arpeggios (F3-F4 range)
# Each chord: (root, 3rd, 5th, octave)
ARPEGGIO_CHORDS = {
    "F":  [53, 57, 60, 65],   # F3, A3, C4, F4
    "Dm": [50, 53, 57, 62],   # D3, F3, A3, D4
    "Bb": [46, 50, 53, 58],   # Bb2, D3, F3, Bb3
    "C":  [48, 52, 55, 60],   # C3, E3, G3, C4
}

PROGRESSION = ["F", "Dm", "Bb", "C"]


# ---------------------------------------------------------------------------
# Left hand: arpeggiated accompaniment
# root-3rd-5th-octave as repeating eighth notes (0.5 beat each)
# 4 beats per bar = 8 eighth notes = 2 full arpeggio cycles per bar
# ---------------------------------------------------------------------------
def add_left_hand_bar(midi, start_time, chord_name, velocity_base=40):
    """Add one bar of arpeggiated accompaniment."""
    track = 1
    channel = 1
    notes = ARPEGGIO_CHORDS[chord_name]
    time = start_time
    for cycle in range(2):  # 2 arpeggio cycles per bar
        for i, note in enumerate(notes):
            # Subtle dynamics: first note of each cycle slightly stronger
            vel = velocity_base + (3 if i == 0 else 0) + (-2 if cycle == 1 else 0)
            vel = max(35, min(45, vel))
            midi.addNote(track, channel, note, time, 0.45, vel)  # slightly detached
            time += 0.5
    return time


# ---------------------------------------------------------------------------
# Right hand: playful music-box melody in F4-F5 range
# Short phrases with rests between them, velocity 45-55
# ---------------------------------------------------------------------------
def get_melody_phrases():
    """
    Return melody phrases for each cycle of the chord progression.
    Each cycle = 4 bars (F - Dm - Bb - C), each bar = 4 beats.
    Returns list of 4 cycles, each cycle is a list of 4 bars,
    each bar is a list of (pitch, duration, velocity) tuples.
    None pitch = rest.
    """
    # Cycle 1: Introduce the theme
    cycle1 = [
        # Bar 1 (F): Gentle opening motif
        [
            (65, 0.5, 50),   # F4
            (69, 0.5, 48),   # A4
            (72, 1.0, 52),   # C5 (held)
            (None, 0.5, 0),  # rest
            (69, 0.5, 47),   # A4
            (65, 1.0, 50),   # F4 (resolve)
        ],
        # Bar 2 (Dm): Descending reply
        [
            (74, 0.5, 48),   # D5
            (72, 0.5, 46),   # C5
            (69, 1.0, 50),   # A4 (held)
            (None, 1.0, 0),  # rest
            (67, 0.5, 45),   # G4
            (65, 0.5, 47),   # F4
        ],
        # Bar 3 (Bb): Playful bounce
        [
            (70, 0.5, 50),   # Bb4
            (None, 0.25, 0),
            (70, 0.25, 45),  # Bb4 (echo)
            (69, 0.5, 48),   # A4
            (67, 0.5, 46),   # G4
            (None, 0.5, 0),
            (65, 0.5, 50),   # F4
            (67, 1.0, 48),   # G4 (held to fill bar)
        ],
        # Bar 4 (C): Rising anticipation
        [
            (72, 0.75, 52),  # C5
            (None, 0.25, 0),
            (74, 0.5, 48),   # D5
            (72, 0.5, 46),   # C5
            (None, 0.5, 0),
            (69, 0.5, 50),   # A4
            (72, 1.0, 48),   # C5 (linger)
        ],
    ]

    # Cycle 2: Develop -- higher register, more motion
    cycle2 = [
        # Bar 1 (F): Higher start
        [
            (72, 0.5, 52),   # C5
            (74, 0.5, 50),   # D5
            (77, 1.0, 55),   # F5 (peak)
            (None, 0.5, 0),
            (74, 0.5, 48),   # D5
            (72, 1.0, 50),   # C5
        ],
        # Bar 2 (Dm): Stepwise descent with ornament
        [
            (74, 0.5, 50),   # D5
            (72, 0.25, 46),  # C5
            (74, 0.25, 46),  # D5 (turn)
            (72, 0.5, 48),   # C5
            (69, 1.0, 50),   # A4
            (None, 0.5, 0),
            (67, 0.5, 45),   # G4
            (65, 0.5, 47),   # F4
        ],
        # Bar 3 (Bb): Gentle rocking
        [
            (70, 0.75, 50),  # Bb4
            (67, 0.75, 46),  # G4
            (70, 0.75, 48),  # Bb4
            (None, 0.25, 0),
            (69, 0.5, 50),   # A4
            (67, 1.0, 48),   # G4
        ],
        # Bar 4 (C): Build to resolution
        [
            (72, 0.5, 52),   # C5
            (69, 0.5, 48),   # A4
            (72, 0.5, 50),   # C5
            (74, 0.5, 52),   # D5
            (None, 0.5, 0),
            (72, 0.75, 50),  # C5
            (None, 0.25, 0),
            (69, 0.5, 48),   # A4
        ],
    ]

    # Cycle 3: Quieter variation, more space
    cycle3 = [
        # Bar 1 (F): Spacious
        [
            (65, 1.0, 48),   # F4 (held)
            (None, 0.5, 0),
            (69, 0.75, 46),  # A4
            (72, 0.75, 48),  # C5
            (None, 1.0, 0),  # generous rest
        ],
        # Bar 2 (Dm): Gentle answer
        [
            (74, 0.75, 46),  # D5
            (72, 0.75, 45),  # C5
            (None, 0.5, 0),
            (69, 1.0, 48),   # A4
            (67, 1.0, 46),   # G4
        ],
        # Bar 3 (Bb): Tender
        [
            (70, 1.0, 48),   # Bb4
            (69, 0.5, 46),   # A4
            (67, 0.5, 45),   # G4
            (None, 0.5, 0),
            (65, 0.75, 48),  # F4
            (67, 0.75, 46),  # G4
        ],
        # Bar 4 (C): Transition
        [
            (72, 1.0, 50),   # C5
            (None, 0.5, 0),
            (74, 0.5, 48),   # D5
            (72, 0.5, 46),   # C5
            (69, 0.5, 48),   # A4
            (72, 1.0, 46),   # C5
        ],
    ]

    # Cycle 4: Return to opening, gentle wrap-up
    cycle4 = [
        # Bar 1 (F): Original motif, ornamented
        [
            (65, 0.5, 50),   # F4
            (67, 0.25, 46),  # G4 (passing)
            (69, 0.25, 46),  # A4
            (72, 1.0, 52),   # C5
            (None, 0.5, 0),
            (74, 0.5, 48),   # D5
            (72, 1.0, 50),   # C5
        ],
        # Bar 2 (Dm): Echo
        [
            (74, 0.5, 48),   # D5
            (72, 0.5, 46),   # C5
            (69, 0.75, 50),  # A4
            (None, 0.25, 0),
            (67, 0.5, 45),   # G4
            (65, 0.5, 47),   # F4
            (None, 1.0, 0),  # rest
        ],
        # Bar 3 (Bb): Winding down
        [
            (70, 0.75, 48),  # Bb4
            (69, 0.75, 46),  # A4
            (67, 0.5, 45),   # G4
            (None, 0.5, 0),
            (65, 0.75, 48),  # F4
            (67, 0.75, 46),  # G4
        ],
        # Bar 4 (C): Approach final resolution
        [
            (72, 1.0, 52),   # C5
            (None, 0.5, 0),
            (69, 0.5, 48),   # A4
            (67, 0.5, 46),   # G4
            (65, 1.5, 50),   # F4 (held, anticipating ending)
        ],
    ]

    return [cycle1, cycle2, cycle3, cycle4]


def add_right_hand(midi, start_beat, cycles_data):
    """Add right-hand melody across all cycles starting at start_beat."""
    track = 0
    channel = 0
    time = start_beat
    for cycle in cycles_data:
        for bar_notes in cycle:
            bar_start = time
            for pitch, dur, vel in bar_notes:
                if pitch is not None:
                    v = max(45, min(55, vel))
                    midi.addNote(track, channel, pitch, time, dur * 0.85, v)
                time += dur
            # Ensure we advance exactly 4 beats per bar even if notes are short
            time = bar_start + BEATS_PER_BAR
    return time


# ---------------------------------------------------------------------------
# Ending: 2 bars -- gentle F major resolution
# ---------------------------------------------------------------------------
def add_ending(midi, start_time):
    """Add a gentle 2-bar ending on F major."""
    track_rh = 0
    ch_rh = 0
    track_lh = 1
    ch_lh = 1

    # Right hand: slow ascending F major arpeggio then settle
    ending_rh = [
        (65, 1.0, 50),   # F4
        (69, 1.0, 48),   # A4
        (72, 1.5, 50),   # C5
        (None, 0.5, 0),
        # Bar 2
        (77, 2.0, 52),   # F5 (gentle peak)
        (72, 1.0, 46),   # C5
        (65, 1.0, 45),   # F4 (final, soft)
    ]

    t = start_time
    for pitch, dur, vel in ending_rh:
        if pitch is not None:
            v = max(45, min(55, vel))
            midi.addNote(track_rh, ch_rh, pitch, t, dur * 0.9, v)
        t += dur

    # Left hand: slow F arpeggio then held chord
    arp = ARPEGGIO_CHORDS["F"]  # F3, A3, C4, F4
    t = start_time
    for note in arp:
        midi.addNote(track_lh, ch_lh, note, t, 1.5, 38)
        t += 1.0
    # Final held chord
    for note in arp:
        midi.addNote(track_lh, ch_lh, note, t, 3.5, 35)


# ---------------------------------------------------------------------------
# Build the full track
# ---------------------------------------------------------------------------
def build_track():
    midi = create_midi(num_tracks=2, tempo=TEMPO)

    # Set both tracks to piano (GM 0)
    midi.addProgramChange(0, 0, 0, INSTRUMENTS["piano"])  # RH
    midi.addProgramChange(1, 1, 0, INSTRUMENTS["piano"])  # LH

    # --- INTRO: 4 bars of LH arpeggios only (F - Dm - Bb - C) ---
    intro_time = 0.0
    for chord_name in PROGRESSION:
        intro_time = add_left_hand_bar(midi, intro_time, chord_name, velocity_base=38)

    # --- MAIN: 4 cycles of melody + arpeggios ---
    main_start = INTRO_BARS * BEATS_PER_BAR  # beat 16

    # Right hand melody
    melody_cycles = get_melody_phrases()
    add_right_hand(midi, main_start, melody_cycles)

    # Left hand arpeggios for all 4 main cycles
    lh_time = main_start
    for cycle_idx in range(MAIN_CYCLES):
        # Slightly softer in cycle 3 (quiet variation)
        vb = 38 if cycle_idx == 2 else 40
        for chord_name in PROGRESSION:
            lh_time = add_left_hand_bar(midi, lh_time, chord_name, velocity_base=vb)

    # --- ENDING: 2 bars ---
    ending_start = (INTRO_BARS + MAIN_CYCLES * BARS_PER_CYCLE) * BEATS_PER_BAR
    add_ending(midi, ending_start)

    return midi


# ---------------------------------------------------------------------------
# Generate
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    print("Generating Track 3: Castle Garden")
    mp3_path = generate_track("track-3", build_track)
    print(f"\nTrack 3 ready: {mp3_path}")
