"""
Track 4: "Hilltop Breeze"
Key: D major, 104 BPM, ~55 seconds
Flute main melody, recorder counter-melody in thirds, piano sustained chord pads.
All three instruments. Soft and gentle. No percussion.
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import *

# D major scale reference:
# D4=62, E4=64, F#4=66, G4=67, A4=69, B4=71, C#5=73, D5=74
# D3=50, E3=52, F#3=54, G3=55, A3=57, B3=59

# Chord voicings for piano pads (low-mid register, spread for warmth)
CHORDS = {
    'D':  [50, 54, 57, 62],   # D3, F#3, A3, D4
    'G':  [43, 47, 55, 59],   # G2, B2, G3, B3
    'A':  [45, 49, 57, 61],   # A2, C#3, A3, C#4
    'Bm': [47, 50, 54, 59],   # B2, D3, F#3, B3
}

# Progression: D - G - A - D - Bm - G - A - D (8 bars), repeat twice = 16 bars
PROGRESSION = ['D', 'G', 'A', 'D', 'Bm', 'G', 'A', 'D']


def build_track4():
    midi = create_midi(num_tracks=3, tempo=104)

    # 16 bars * 4 beats / 104 bpm * 60 = ~36.9s -- too short
    # 24 bars = 96 beats / 104 * 60 = ~55.4s -- perfect!
    total_bars = 24
    chord_seq = PROGRESSION * 3  # 24 bars

    # --- Track 0: Flute lead melody (velocity 50-58) ---
    flute_notes = _build_flute_melody()
    add_melody(midi, 0, 0, INSTRUMENTS['flute'], flute_notes, start_time=0, velocity_base=54)

    # --- Track 1: Recorder counter-melody in thirds (velocity 40-50) ---
    recorder_notes = _build_recorder_counter()
    add_melody(midi, 1, 1, INSTRUMENTS['recorder'], recorder_notes, start_time=0, velocity_base=45)

    # --- Track 2: Piano sustained chord pads (velocity 35-42) ---
    piano_chords = [CHORDS[c] for c in chord_seq]
    add_chord_progression(midi, 2, 2, INSTRUMENTS['piano'], piano_chords,
                          start_time=0, duration=4.0, velocity=38, arpeggio_delay=0)

    return midi


def _build_flute_melody():
    """
    Soaring flute melody with long notes and gentle ornamental turns.
    Velocity offsets kept so that velocity_base=54 + offset stays in 50-58.
    """
    # D4=62, E4=64, F#4=66, G4=67, A4=69, B4=71, C#5=73, D5=74, E5=76

    # --- Pass 1 (bars 1-8): D - G - A - D - Bm - G - A - D ---
    pass1 = [
        # Bar 1 (D): Open gently on D, rise to F#
        (62, 2.0, -2),     # D4 half
        (64, 0.5, -4),     # E4 eighth ornament
        (66, 1.5, 0),      # F#4 dotted quarter
        # Bar 2 (G): Float up to B, settle on G
        (67, 1.5, 0),      # G4 dotted quarter
        (69, 0.5, 2),      # A4 eighth ornament
        (71, 1.0, 4),      # B4 quarter
        (67, 1.0, -2),     # G4 quarter
        # Bar 3 (A): Soar to A, gentle turn
        (69, 2.5, 2),      # A4 long
        (71, 0.5, 4),      # B4 eighth ornament
        (69, 1.0, 0),      # A4 quarter
        # Bar 4 (D): Resolve phrase to D
        (66, 1.5, -2),     # F#4 dotted quarter
        (64, 0.5, -4),     # E4 eighth
        (62, 2.0, -2),     # D4 half resolve
        # Bar 5 (Bm): Rise expressively from B
        (71, 2.0, 4),      # B4 half
        (69, 1.0, 0),      # A4 quarter
        (71, 1.0, 2),      # B4 quarter
        # Bar 6 (G): Gentle descent through G
        (67, 2.0, -2),     # G4 half
        (69, 0.5, 0),      # A4 eighth ornament
        (67, 1.0, -2),     # G4 quarter
        (66, 0.5, -4),     # F#4 eighth
        # Bar 7 (A): Push up, ornamental turn
        (69, 1.5, 0),      # A4 dotted quarter
        (71, 0.5, 2),      # B4 eighth
        (73, 1.0, 4),      # C#5 quarter
        (71, 1.0, 2),      # B4 quarter
        # Bar 8 (D): Resolve first section
        (69, 1.5, 0),      # A4 dotted quarter
        (66, 0.5, -2),     # F#4 eighth
        (62, 1.5, -4),     # D4 dotted quarter
        (None, 0.5, 0),    # rest - breathe
    ]

    # --- Pass 2 (bars 9-16): Higher register, more soaring ---
    pass2 = [
        # Bar 9 (D): Start on A, reach for D5
        (69, 1.5, 0),      # A4 dotted quarter
        (71, 0.5, 2),      # B4 eighth
        (74, 2.0, 4),      # D5 half
        # Bar 10 (G): Sustain high, gentle dip
        (74, 2.0, 4),      # D5 half
        (71, 1.0, 2),      # B4 quarter
        (67, 1.0, -2),     # G4 quarter
        # Bar 11 (A): Ornamental rise to E5
        (69, 1.0, 0),      # A4 quarter
        (73, 0.5, 4),      # C#5 eighth ornament
        (74, 0.5, 4),      # D5 eighth ornament
        (76, 2.0, 4),      # E5 half - peak!
        # Bar 12 (D): Float down from peak
        (74, 1.5, 4),      # D5 dotted quarter
        (73, 0.5, 2),      # C#5 eighth grace
        (71, 1.0, 0),      # B4 quarter
        (69, 1.0, -2),     # A4 quarter
        # Bar 13 (Bm): Expressive phrase
        (71, 2.5, 2),      # B4 long
        (74, 0.5, 4),      # D5 eighth
        (71, 1.0, 0),      # B4 quarter
        # Bar 14 (G): Floating descent
        (67, 2.0, -2),     # G4 half
        (69, 1.0, 0),      # A4 quarter
        (71, 1.0, 2),      # B4 quarter
        # Bar 15 (A): Gentle rise with turn
        (73, 1.5, 4),      # C#5 dotted quarter
        (71, 0.5, 2),      # B4 eighth ornament
        (69, 1.0, 0),      # A4 quarter
        (66, 1.0, -2),     # F#4 quarter
        # Bar 16 (D): Resolve mid-section
        (67, 1.0, -2),     # G4 quarter
        (66, 0.5, -4),     # F#4 eighth
        (64, 0.5, -4),     # E4 eighth
        (62, 1.5, -2),     # D4 dotted quarter
        (None, 0.5, 0),    # rest
    ]

    # --- Pass 3 (bars 17-24): Gentle conclusion ---
    pass3 = [
        # Bar 17 (D): Recall opening theme
        (62, 2.0, -2),     # D4 half
        (64, 0.5, -4),     # E4 eighth
        (66, 1.5, 0),      # F#4 dotted quarter
        # Bar 18 (G): Warm echo
        (67, 1.5, -2),     # G4 dotted quarter
        (71, 1.0, 2),      # B4 quarter
        (69, 1.0, 0),      # A4 quarter
        (67, 0.5, -2),     # G4 eighth
        # Bar 19 (A): Gentle rise
        (69, 2.5, 0),      # A4 long
        (71, 0.5, 2),      # B4 eighth
        (73, 1.0, 4),      # C#5 quarter
        # Bar 20 (D): Sustain, settling
        (74, 3.0, 4),      # D5 dotted half
        (71, 1.0, 0),      # B4 quarter
        # Bar 21 (Bm): Last expressive rise
        (71, 1.5, 2),      # B4 dotted quarter
        (74, 0.5, 4),      # D5 eighth
        (76, 2.0, 4),      # E5 half - final peak
        # Bar 22 (G): Floating down
        (74, 1.5, 4),      # D5 dotted quarter
        (71, 0.5, 2),      # B4 eighth
        (67, 2.0, -2),     # G4 half
        # Bar 23 (A): Descending toward home
        (69, 1.5, 0),      # A4 dotted quarter
        (66, 0.5, -2),     # F#4 eighth
        (64, 1.0, -4),     # E4 quarter
        (62, 1.0, -4),     # D4 quarter
        # Bar 24 (D): Final resolve
        (64, 0.5, -4),     # E4 eighth - tiny lift
        (62, 3.5, -2),     # D4 long hold - home
    ]

    return pass1 + pass2 + pass3


def _build_recorder_counter():
    """
    Recorder counter-melody, mostly in thirds above or below the flute.
    Sparser than the flute, answering phrases. Velocity offsets for base=45
    keep range 40-50.
    """
    # Thirds above flute D major notes:
    # D4->F#4(66), E4->G4(67), F#4->A4(69), G4->B4(71), A4->C#5(73), B4->D5(74)
    # Thirds below:
    # D4->B3(59), F#4->D4(62), G4->E4(64), A4->F#4(66), B4->G4(67)

    notes = []

    def rest(beats):
        notes.append((None, beats, 0))

    def note(pitch, dur, vel=0):
        notes.append((pitch, dur, vel))

    # --- Pass 1 (bars 1-8): Sparse, echoing in thirds ---
    # Bar 1 (D): Let flute establish, join late
    rest(2.0)
    note(66, 2.0, -3)     # F#4 - third above D4

    # Bar 2 (G): Third above G melody
    rest(1.5)
    note(71, 1.0, 0)      # B4 - third above G4
    note(73, 0.5, 2)      # C#5 - third above A4
    note(71, 1.0, -3)     # B4

    # Bar 3 (A): Echo the A soar
    rest(1.0)
    note(73, 2.0, 2)      # C#5 - third above A4
    note(74, 1.0, 5)      # D5 - third above B4

    # Bar 4 (D): Descend with flute
    note(69, 1.5, 0)      # A4 - third above F#4
    rest(0.5)
    note(66, 2.0, -5)     # F#4 - third above D4

    # Bar 5 (Bm): Answer B phrase
    rest(2.0)
    note(74, 1.5, 5)      # D5 - third above B4
    note(73, 0.5, 2)      # C#5

    # Bar 6 (G): Gentle thirds below
    note(64, 2.0, -5)     # E4 - third below G4
    rest(1.0)
    note(62, 1.0, -5)     # D4 - third below F#4

    # Bar 7 (A): Rise with flute in thirds
    rest(1.5)
    note(73, 1.0, 2)      # C#5 - third above A4
    note(74, 1.0, 5)      # D5 - third above B4
    note(73, 0.5, 2)      # C#5

    # Bar 8 (D): Resolve
    note(73, 1.5, 0)      # C#5
    note(69, 1.0, -3)     # A4
    rest(0.5)
    note(66, 1.0, -5)     # F#4

    # --- Pass 2 (bars 9-16): More active, closer interplay ---
    # Bar 9 (D): Third above flute
    note(73, 1.5, 2)      # C#5
    rest(0.5)
    note(74, 1.0, 5)      # D5
    note(76, 1.0, 5)      # E5 - third above C#5(approx)

    # Bar 10 (G): Float with thirds
    note(76, 1.5, 5)      # E5
    note(74, 1.0, 2)      # D5
    rest(0.5)
    note(71, 1.0, -2)     # B4

    # Bar 11 (A): Echo the peak
    rest(1.0)
    note(73, 1.0, 2)      # C#5
    note(74, 0.5, 5)      # D5
    rest(0.5)
    note(73, 1.0, 2)      # C#5 - below E5 peak

    # Bar 12 (D): Descend in parallel
    rest(1.5)
    note(76, 0.5, 5)      # E5
    note(74, 1.0, 2)      # D5
    note(73, 1.0, 0)      # C#5

    # Bar 13 (Bm): Answering phrase
    note(74, 2.0, 5)      # D5 - third above B4
    rest(1.0)
    note(74, 1.0, 2)      # D5

    # Bar 14 (G): Below flute
    rest(1.0)
    note(64, 1.5, -5)     # E4 - third below G4
    note(66, 1.0, -3)     # F#4
    note(67, 0.5, -2)     # G4

    # Bar 15 (A): Gentle turn
    note(69, 1.5, 0)      # A4
    note(67, 0.5, -2)     # G4
    note(66, 1.0, -3)     # F#4
    note(69, 1.0, 0)      # A4

    # Bar 16 (D): Settle
    rest(1.0)
    note(71, 1.0, 0)      # B4
    note(69, 0.5, -2)     # A4
    note(66, 1.0, -5)     # F#4
    rest(0.5)

    # --- Pass 3 (bars 17-24): Fading, gentle ---
    # Bar 17 (D): Echo opening
    rest(2.5)
    note(69, 1.5, -3)     # A4 - third above F#4

    # Bar 18 (G): Soft answer
    rest(1.5)
    note(71, 1.5, -2)     # B4
    note(73, 1.0, 0)      # C#5

    # Bar 19 (A): Gentle thirds
    note(73, 2.0, 0)      # C#5
    rest(1.0)
    note(74, 1.0, 2)      # D5

    # Bar 20 (D): Sustain underneath
    rest(1.0)
    note(76, 2.0, 2)      # E5 - third above D5
    note(74, 1.0, 0)      # D5

    # Bar 21 (Bm): Last echo
    note(74, 1.5, 2)      # D5
    rest(0.5)
    note(73, 1.0, 0)      # C#5 - below E5
    note(74, 1.0, 2)      # D5

    # Bar 22 (G): Floating
    rest(1.5)
    note(76, 1.0, 2)      # E5
    note(74, 0.5, 0)      # D5
    note(71, 1.0, -3)     # B4

    # Bar 23 (A): Descend gently
    note(73, 1.0, 0)      # C#5
    note(69, 1.0, -3)     # A4
    rest(1.0)
    note(66, 1.0, -5)     # F#4

    # Bar 24 (D): Final
    rest(1.0)
    note(66, 1.0, -5)     # F#4 - third above D4
    note(62, 2.0, -5)     # D4 - unison with flute at end


    return notes


if __name__ == '__main__':
    print("Generating Track 4: Hilltop Breeze")
    mp3_path = generate_track("track-4", build_track4)
    print(f"\nTrack 4 complete: {mp3_path}")
