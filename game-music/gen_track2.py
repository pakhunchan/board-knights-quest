"""
Track 2: "Woodland Stroll"
Knight/adventure math game for 3rd graders.

Key: G major | Tempo: 100 BPM | ~60 seconds | Loop-friendly
Instruments: Recorder (melody) + Piano (sustained chords)
Chord progression: G - Em - C - D (repeated 5 times)
Melody: Pentatonic-leaning (G, A, B, D, E), lilting dotted rhythms
Feel: A leisurely walk through a friendly forest

CONSTRAINTS:
- ONLY piano (GM 0) and recorder (GM 74)
- NO percussion
- All velocities 35-60
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import (
    create_midi, add_melody, add_chord_progression,
    generate_track, INSTRUMENTS,
)

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
TEMPO = 100
BEATS_PER_BAR = 4

# Structure: 4-bar intro (piano only) + 5x4-bar body + 2-bar outro = 26 bars
# 26 bars * 4 beats / 100 BPM * 60 = 62.4 seconds
INTRO_BARS = 4
BODY_REPS = 5
OUTRO_BARS = 2

# Chord voicings (low register for soft sustained pad)
# G major: G2-B2-D3-G3
# Em:      E2-G2-B2-E3
# C major: C3-E3-G3-C4
# D major: D3-F#3-A3-D4
CHORD_G  = [43, 47, 50, 55]
CHORD_EM = [40, 43, 47, 52]
CHORD_C  = [48, 52, 55, 60]
CHORD_D  = [50, 54, 57, 62]

PROGRESSION = [CHORD_G, CHORD_EM, CHORD_C, CHORD_D]


# ---------------------------------------------------------------------------
# Recorder melody phrases (5 variations, one per cycle)
# Each phrase = 4 bars = 16 beats
# Format: (MIDI_pitch or None, duration, velocity_offset)
# velocity_base = 50; offsets keep final vel in 45-55 range
# Dotted rhythms: dotted quarter = 1.5, eighth = 0.5
# Pentatonic pitches: G4=67, A4=69, B4=71, D5=74, E5=76
#   Also G3=55, E4=64 for lower color
# ---------------------------------------------------------------------------

def phrase_A():
    """Opening - simple ascending, establishes the tune."""
    return [
        # Bar 1 (G): gentle rise
        (67, 1.5, 0),   # G4 dotted quarter
        (69, 0.5, -3),  # A4 eighth
        (71, 1.5, 2),   # B4 dotted quarter
        (69, 0.5, -3),  # A4 eighth
        # Bar 2 (Em): settle
        (74, 1.5, 3),   # D5 dotted quarter
        (71, 0.5, 0),   # B4 eighth
        (69, 1.5, 0),   # A4 dotted quarter
        (None, 0.5, 0), # rest
        # Bar 3 (C): lilting up
        (67, 1.0, 0),   # G4 quarter
        (69, 0.5, -2),  # A4 eighth
        (71, 0.5, 0),   # B4 eighth
        (74, 1.5, 3),   # D5 dotted quarter
        (None, 0.5, 0), # rest
        # Bar 4 (D): resolve back
        (76, 1.5, 5),   # E5 dotted quarter (peak)
        (74, 0.5, 0),   # D5 eighth
        (71, 1.0, 0),   # B4 quarter
        (69, 1.0, -3),  # A4 quarter
    ]

def phrase_B():
    """Variation - starts higher, descends."""
    return [
        # Bar 1 (G)
        (74, 1.5, 2),   # D5
        (71, 0.5, 0),   # B4
        (69, 1.0, 0),   # A4
        (67, 1.0, 0),   # G4
        # Bar 2 (Em)
        (64, 1.5, -2),  # E4
        (67, 0.5, 0),   # G4
        (69, 1.5, 0),   # A4
        (None, 0.5, 0), # rest
        # Bar 3 (C)
        (71, 1.0, 2),   # B4
        (69, 0.5, 0),   # A4
        (67, 0.5, -2),  # G4
        (69, 1.5, 0),   # A4
        (71, 0.5, 0),   # B4
        # Bar 4 (D)
        (74, 2.0, 3),   # D5 half note
        (None, 0.5, 0), # rest
        (71, 1.0, 0),   # B4
        (None, 0.5, 0), # rest
    ]

def phrase_C():
    """Variation - more rhythmic, playful."""
    return [
        # Bar 1 (G)
        (67, 1.5, 0),   # G4 dotted quarter
        (71, 0.5, 2),   # B4 eighth
        (74, 1.5, 3),   # D5 dotted quarter
        (71, 0.5, 0),   # B4 eighth
        # Bar 2 (Em)
        (69, 1.5, 0),   # A4 dotted quarter
        (67, 0.5, -2),  # G4 eighth
        (69, 1.5, 0),   # A4 dotted quarter
        (None, 0.5, 0), # rest
        # Bar 3 (C)
        (67, 0.5, -2),  # G4
        (69, 0.5, 0),   # A4
        (71, 1.5, 2),   # B4 dotted quarter
        (74, 1.0, 3),   # D5 quarter
        (None, 0.5, 0), # rest
        # Bar 4 (D)
        (76, 1.0, 5),   # E5 quarter
        (74, 1.5, 2),   # D5 dotted quarter
        (71, 1.0, 0),   # B4 quarter
        (None, 0.5, 0), # rest
    ]

def phrase_D():
    """Variation - longer notes, breathing room."""
    return [
        # Bar 1 (G)
        (71, 2.0, 2),   # B4 half note
        (74, 1.5, 3),   # D5 dotted quarter
        (71, 0.5, 0),   # B4 eighth
        # Bar 2 (Em)
        (69, 2.0, 0),   # A4 half note
        (67, 1.5, -2),  # G4 dotted quarter
        (None, 0.5, 0), # rest
        # Bar 3 (C)
        (69, 1.0, 0),   # A4 quarter
        (71, 0.5, 2),   # B4 eighth
        (69, 0.5, 0),   # A4 eighth
        (67, 1.5, 0),   # G4 dotted quarter
        (None, 0.5, 0), # rest
        # Bar 4 (D)
        (69, 1.0, 0),   # A4
        (71, 1.5, 2),   # B4 dotted quarter
        (74, 1.0, 3),   # D5 quarter
        (None, 0.5, 0), # rest
    ]

def phrase_E():
    """Final phrase - resolves to G."""
    return [
        # Bar 1 (G): recap opening
        (67, 1.5, 0),   # G4 dotted quarter
        (69, 0.5, -3),  # A4 eighth
        (71, 1.5, 2),   # B4 dotted quarter
        (69, 0.5, -3),  # A4 eighth
        # Bar 2 (Em): arc up
        (74, 1.5, 3),   # D5
        (76, 0.5, 5),   # E5 (highest point)
        (74, 1.0, 2),   # D5
        (71, 1.0, 0),   # B4
        # Bar 3 (C): wind down
        (69, 1.5, 0),   # A4 dotted quarter
        (67, 0.5, -2),  # G4
        (69, 1.0, 0),   # A4
        (67, 1.0, -2),  # G4
        # Bar 4 (D -> G): resolution
        (71, 1.5, 2),   # B4 dotted quarter
        (69, 0.5, 0),   # A4
        (67, 2.0, 0),   # G4 half note (home)
    ]


MELODY_PHRASES = [phrase_A, phrase_B, phrase_C, phrase_D, phrase_E]


# ---------------------------------------------------------------------------
# Build the full track
# ---------------------------------------------------------------------------
def build_track():
    midi = create_midi(num_tracks=2, tempo=TEMPO)

    body_start = INTRO_BARS * BEATS_PER_BAR  # beat 16

    # === PIANO: sustained whole-note chords (track 0, channel 0) ===
    piano = INSTRUMENTS["piano"]

    # Intro: 4 bars of G - Em - C - D, piano alone
    add_chord_progression(
        midi, track=0, channel=0, instrument=piano,
        chords=PROGRESSION[:], start_time=0,
        duration=4.0, velocity=40, arpeggio_delay=0,
    )

    # Body: 5 repetitions
    for rep in range(BODY_REPS):
        offset = body_start + rep * 16
        vel = 38 + (rep % 3) * 2  # cycles 38, 40, 42, 38, 40
        add_chord_progression(
            midi, track=0, channel=0, instrument=piano,
            chords=PROGRESSION[:], start_time=offset,
            duration=4.0, velocity=vel, arpeggio_delay=0,
        )

    # Outro: 2 bars of G chord, softer
    outro_start = body_start + BODY_REPS * 16
    add_chord_progression(
        midi, track=0, channel=0, instrument=piano,
        chords=[CHORD_G, CHORD_G], start_time=outro_start,
        duration=4.0, velocity=36, arpeggio_delay=0,
    )

    # === RECORDER MELODY (track 1, channel 1) ===
    recorder = INSTRUMENTS["recorder"]

    # No melody during intro (piano alone sets the mood)
    for rep in range(BODY_REPS):
        offset = body_start + rep * 16
        phrase_fn = MELODY_PHRASES[rep]
        notes = list(phrase_fn())
        add_melody(
            midi, track=1, channel=1, instrument=recorder,
            notes=notes, start_time=offset, velocity_base=50,
        )

    # Outro: gentle G landing
    outro_melody = [
        (71, 1.5, -2),  # B4
        (69, 0.5, -3),  # A4
        (67, 2.0, -2),  # G4 half
        (67, 3.0, -5),  # G4 dotted half (fade)
        (None, 1.0, 0), # rest
    ]
    add_melody(
        midi, track=1, channel=1, instrument=recorder,
        notes=outro_melody, start_time=outro_start, velocity_base=45,
    )

    return midi


# ---------------------------------------------------------------------------
# Generate
# ---------------------------------------------------------------------------
if __name__ == "__main__":
    print("Generating Track 2: Woodland Stroll")
    mp3_path = generate_track("track-2", build_track)
    print(f"\nTrack 2 ready: {mp3_path}")
