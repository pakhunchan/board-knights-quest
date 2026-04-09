"""
Track 7: "River Song"
Key: C major pentatonic (C, D, E, G, A) | Tempo: 120 BPM | ~45 seconds
Mood: Flowing water, babbling brook, gentle and upbeat
Lead: Flute (flowing pentatonic melody) | Accompaniment: Piano (rolling arpeggios)
No percussion - purely melodic
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# C pentatonic: C4=60, D4=62, E4=64, G4=67, A4=69, C5=72
PENTA = SCALES["C_pentatonic"]  # [60, 62, 64, 67, 69, 72]

# Chord tones for piano arpeggios (octave 3 range for warmth)
CHORD_C  = [48, 52, 55, 60]  # C3 E3 G3 C4
CHORD_Am = [45, 48, 52, 57]  # A2 C3 E3 A3
CHORD_F  = [41, 45, 48, 53]  # F2 A2 C3 F3
CHORD_G  = [43, 47, 50, 55]  # G2 B2 D3 G3

PROGRESSION = [CHORD_C, CHORD_Am, CHORD_F, CHORD_G]


def build_rolling_arpeggios(midi, track, channel, start_time, num_bars):
    """
    Piano plays continuous eighth-note arpeggios flowing up and down through chord tones.
    Each bar = 4 beats = 8 eighth notes. Creates a water-like texture.
    """
    midi.addProgramChange(track, channel, 0, INSTRUMENTS['piano'])

    time = start_time

    for bar in range(num_bars):
        chord_tones = PROGRESSION[bar % 4]
        low, mid1, mid2, high = chord_tones

        # Vary the arpeggio pattern per chord position for flowing movement
        patterns = [
            # C chord: classic up-down wave
            [low, mid1, mid2, high, high, mid2, mid1, low],
            # Am chord: leap up then trickle down
            [low, mid1, high, mid2, low, mid1, mid2, high],
            # F chord: down-up wave (contrast)
            [high, mid2, mid1, low, low, mid1, mid2, high],
            # G chord: meandering stream
            [mid1, low, mid2, mid1, high, mid2, mid1, low],
        ]

        pattern = patterns[bar % 4]

        for i, note in enumerate(pattern):
            # Gentle velocity variation for natural water-like feel
            vel_variation = random.choice([-3, -1, 0, 1, 3])
            vel = max(38, min(45, 41 + vel_variation))
            midi.addNote(track, channel, note, time + i * 0.5, 0.55, vel)

        time += 4.0

    return time


def build_flute_melody(midi, track, channel, start_time):
    """
    Flute plays a flowing pentatonic melody like a babbling brook.
    Uses lots of eighth notes in gentle scalar passages.
    C4=60, D4=62, E4=64, G4=67, A4=69, C5=72
    """
    midi.addProgramChange(track, channel, 0, INSTRUMENTS['flute'])

    # Phrase 1 (bars 1-4): Gentle introduction, establishing the brook theme
    phrase1 = [
        # Bar 1 (C): rising stream
        (60, 0.5, 0),    # C4
        (62, 0.5, 2),    # D4
        (64, 0.5, 3),    # E4
        (67, 1.0, 5),    # G4 (linger)
        (64, 0.5, 0),    # E4
        (62, 0.5, -2),   # D4
        (60, 0.5, 0),    # C4
        # Bar 2 (Am): flowing up higher
        (62, 0.5, 0),    # D4
        (64, 0.5, 2),    # E4
        (67, 0.5, 3),    # G4
        (69, 1.0, 5),    # A4 (linger)
        (67, 0.5, 0),    # G4
        (64, 0.5, -2),   # E4
        (62, 0.5, 0),    # D4
        # Bar 3 (F): reaching peak
        (64, 0.5, 0),    # E4
        (67, 0.5, 3),    # G4
        (69, 0.5, 5),    # A4
        (72, 1.0, 7),    # C5 (peak, linger)
        (69, 0.5, 2),    # A4
        (67, 0.5, 0),    # G4
        (64, 0.5, -2),   # E4
        # Bar 4 (G): cascading down
        (67, 0.5, 3),    # G4
        (69, 0.5, 5),    # A4
        (67, 0.5, 0),    # G4
        (64, 0.5, -2),   # E4
        (62, 1.0, 0),    # D4 (settle)
        (None, 1.0, 0),  # rest (breathe)
    ]

    # Phrase 2 (bars 5-8): More playful, brook ripples
    phrase2 = [
        # Bar 5 (C): playful ripples
        (64, 0.5, 2),    # E4
        (62, 0.5, 0),    # D4
        (64, 0.5, 2),    # E4
        (67, 0.5, 3),    # G4
        (69, 0.5, 5),    # A4
        (67, 0.5, 2),    # G4
        (64, 0.5, 0),    # E4
        (62, 0.5, -2),   # D4
        # Bar 6 (Am): gentle undulation
        (60, 0.5, 0),    # C4
        (62, 0.5, 2),    # D4
        (67, 1.0, 5),    # G4 (hold)
        (69, 0.5, 3),    # A4
        (72, 1.0, 7),    # C5 (soar)
        (None, 0.5, 0),  # rest
        # Bar 7 (F): flowing descent
        (72, 0.5, 5),    # C5
        (69, 0.5, 3),    # A4
        (67, 0.5, 2),    # G4
        (64, 0.5, 0),    # E4
        (62, 0.5, -2),   # D4
        (60, 0.5, 0),    # C4
        (62, 0.5, 2),    # D4
        (64, 0.5, 0),    # E4
        # Bar 8 (G): resolution with motion
        (67, 0.5, 3),    # G4
        (64, 0.5, 0),    # E4
        (62, 0.5, -2),   # D4
        (60, 1.5, 0),    # C4 (longer)
        (None, 1.0, 0),  # rest
    ]

    # Phrase 3 (bars 9-12): Higher energy, more flowing eighth notes
    phrase3 = [
        # Bar 9 (C): ascending run
        (60, 0.5, 0),    # C4
        (62, 0.5, 2),    # D4
        (64, 0.5, 3),    # E4
        (67, 0.5, 5),    # G4
        (69, 0.5, 5),    # A4
        (72, 0.5, 7),    # C5
        (69, 0.5, 3),    # A4
        (67, 0.5, 2),    # G4
        # Bar 10 (Am): cascading brook
        (72, 0.5, 5),    # C5
        (69, 0.5, 3),    # A4
        (67, 0.5, 2),    # G4
        (64, 0.5, 0),    # E4
        (67, 0.5, 2),    # G4
        (69, 0.5, 5),    # A4
        (67, 1.0, 3),    # G4 (hold)
        # Bar 11 (F): swirling water
        (64, 0.5, 2),    # E4
        (67, 0.5, 3),    # G4
        (69, 0.5, 5),    # A4
        (67, 0.5, 2),    # G4
        (64, 0.5, 0),    # E4
        (62, 0.5, -2),   # D4
        (64, 0.5, 2),    # E4
        (67, 0.5, 3),    # G4
        # Bar 12 (G): settling pool
        (69, 0.5, 5),    # A4
        (67, 0.5, 3),    # G4
        (64, 1.0, 0),    # E4
        (62, 1.0, -2),   # D4
        (None, 1.0, 0),  # rest
    ]

    full_melody = phrase1 + phrase2 + phrase3

    time = start_time
    for pitch, duration, vel_offset in full_melody:
        if pitch is not None:
            vel = max(48, min(55, 51 + vel_offset))
            midi.addNote(track, channel, pitch, time, duration, vel)
        time += duration

    return time


def build_track7():
    # Structure:
    # - 4 bars piano intro (arpeggios alone) = 8 seconds
    # - 12 bars flute + piano (3 phrases of 4 bars) = 24 seconds
    # - 4 bars piano outro (arpeggios winding down) = 8 seconds
    # - 2 bars gentle ending = 4 seconds
    # Total: 22 bars at 120 BPM = 44 seconds
    midi = create_midi(num_tracks=2, tempo=120)

    # --- Track 0: Piano rolling arpeggios (continuous throughout) ---
    # 20 bars of flowing arpeggios (5 full cycles of the 4-chord progression)
    build_rolling_arpeggios(midi, track=0, channel=0, start_time=0, num_bars=20)

    # Add 2 final bars: gentle sustained C chord to end
    end_time = 20 * 4.0  # beat 80
    # Bar 21: slower arpeggiated C chord
    ending_notes = [48, 52, 55, 60]
    for i, note in enumerate(ending_notes):
        midi.addNote(0, 0, note, end_time + i * 1.0, 3.0, 40)
    # Bar 22: final sustained C chord
    for note in [48, 55, 60]:
        midi.addNote(0, 0, note, end_time + 4.0, 4.0, 38)

    # --- Track 1: Flute melody (enters after 4-bar piano intro) ---
    # 12 bars of melody starting at bar 5 (beat 16)
    flute_start = 4.0 * 4  # beat 16
    build_flute_melody(midi, track=1, channel=1, start_time=flute_start)

    return midi


if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print("Generating Track 7: River Song...")
    mp3_path = generate_track("track-7", build_track7)
    print(f"\nTrack 7 saved to: {mp3_path}")
