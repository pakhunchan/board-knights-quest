"""
Track 10: "Quest Complete"
Key: C major | Tempo: 126 BPM | ~35 seconds
Piano + Flute + Recorder — celebratory but SOFT
Chord progression: C - G - Am - F - C - G - F - C
"""

import sys
sys.path.insert(0, '/Users/jackie/src/board/game-music')
from music_gen import (
    create_midi, generate_track,
    INSTRUMENTS,
)

# Chords as specified
CHORDS = {
    'C':  [48, 52, 55],
    'G':  [43, 47, 50],
    'Am': [45, 48, 52],
    'F':  [41, 45, 48],
}

PROGRESSION = ['C', 'G', 'Am', 'F', 'C', 'G', 'F', 'C']
BEATS_PER_CHORD = 4
# At 126 BPM: 8 chords x 4 beats = 32 beats per pass = ~15.2 sec
# 2 passes = ~30.5 sec + 4-beat ending = ~33.4 sec total


def build():
    midi = create_midi(num_tracks=3, tempo=126)

    # =========================================================
    # TRACK 0: PIANO — gentle celebratory arpeggios (vel 42-52)
    # =========================================================
    track, channel = 0, 0
    midi.addProgramChange(track, channel, 0, INSTRUMENTS['piano'])

    time = 0.0
    for pass_num in range(2):
        for chord_name in PROGRESSION:
            cn = CHORDS[chord_name]
            # Arpeggio patterns — eighth notes (0.5 beats each), 8 per chord
            if pass_num == 0:
                arp = [
                    cn[0], cn[1], cn[2], cn[1],
                    cn[0], cn[2], cn[1], cn[0],
                ]
            else:
                # Second pass: slightly more active with upper octave touches
                arp = [
                    cn[0], cn[1], cn[2], cn[2] + 12,
                    cn[1], cn[0], cn[2], cn[1],
                ]
            for i, pitch in enumerate(arp):
                vel = 42 + (i % 4) * 3  # cycles 42, 45, 48, 51
                if pass_num == 1:
                    vel = min(52, vel + 1)
                midi.addNote(track, channel, pitch, time + i * 0.5, 0.45, vel)
            time += BEATS_PER_CHORD

    # Ending tag: gentle held C chord with upper octave
    for pitch in CHORDS['C']:
        midi.addNote(track, channel, pitch, time, 3.0, 45)
    midi.addNote(track, channel, 60, time, 3.0, 42)  # C4
    midi.addNote(track, channel, 64, time, 3.0, 40)  # E4

    # =========================================================
    # TRACK 1: FLUTE — bright ascending melody (vel 50-58)
    # Rising phrases landing on high C, stepping back down
    # =========================================================
    track, channel = 1, 1
    midi.addProgramChange(track, channel, 0, INSTRUMENTS['flute'])

    # Each sub-list = one chord's worth of melody: (pitch, duration, velocity)
    # Pass 1: gentle ascending introduction
    melody_pass1 = [
        # C: rise E4 -> G4 -> C5
        [(64, 1.0, 50), (67, 1.5, 52), (72, 1.5, 55)],
        # G: step through B4, up to D5
        [(71, 1.0, 50), (67, 1.0, 50), (69, 1.0, 52), (74, 1.0, 54)],
        # Am: gentle turn A4-C5
        [(69, 1.5, 52), (72, 1.5, 54), (69, 1.0, 50)],
        # F: rise F4 -> C5
        [(65, 1.0, 50), (67, 1.0, 52), (69, 1.0, 53), (72, 1.0, 55)],
        # C: land on C5, step down
        [(72, 2.0, 56), (71, 1.0, 52), (69, 1.0, 50)],
        # G: step G4 to B4
        [(67, 1.5, 50), (69, 1.0, 52), (71, 1.5, 54)],
        # F: gentle descent
        [(69, 1.5, 52), (67, 1.5, 50), (65, 1.0, 50)],
        # C: resolve on C5
        [(64, 1.0, 50), (67, 1.0, 52), (72, 2.0, 56)],
    ]

    # Pass 2: more triumphant, reaching higher
    melody_pass2 = [
        # C: leap to high C
        [(67, 0.5, 50), (69, 0.5, 52), (71, 1.0, 54), (72, 2.0, 57)],
        # G: bright run up to D5
        [(74, 1.0, 55), (72, 1.0, 53), (71, 1.0, 54), (74, 1.0, 56)],
        # Am: expressive turn
        [(72, 1.5, 54), (69, 1.0, 52), (72, 1.5, 55)],
        # F: build upward to E5
        [(69, 1.0, 52), (71, 1.0, 54), (72, 1.0, 55), (74, 1.0, 57)],
        # C: triumphant high E5, settle to C5
        [(76, 1.5, 58), (74, 1.0, 56), (72, 1.5, 55)],
        # G: graceful descent
        [(74, 1.0, 56), (72, 1.0, 54), (71, 1.5, 53), (69, 0.5, 50)],
        # F: gentle resolution approach
        [(67, 1.5, 50), (69, 1.5, 52), (71, 1.0, 54)],
        # C: warm final landing on C5
        [(72, 2.5, 57), (None, 0.5, 0), (72, 1.0, 55)],
    ]

    time = 0.0
    for phrase in melody_pass1:
        t = time
        for pitch, dur, vel in phrase:
            if pitch is not None:
                midi.addNote(track, channel, pitch, t, dur * 0.95, vel)
            t += dur
        time += BEATS_PER_CHORD

    for phrase in melody_pass2:
        t = time
        for pitch, dur, vel in phrase:
            if pitch is not None:
                midi.addNote(track, channel, pitch, t, dur * 0.95, vel)
            t += dur
        time += BEATS_PER_CHORD

    # Ending: flute holds high C5
    midi.addNote(track, channel, 72, time, 3.5, 55)

    # =========================================================
    # TRACK 2: RECORDER — harmonizes in thirds above flute (vel 42-50)
    # Enters after first 2 chords for a layered build
    # =========================================================
    track, channel = 2, 2
    midi.addProgramChange(track, channel, 0, INSTRUMENTS['recorder'])

    def third_above(pitch):
        """Diatonic third above in C major."""
        if pitch is None:
            return None
        c_major = [0, 2, 4, 5, 7, 9, 11]
        note_class = pitch % 12
        octave = pitch // 12
        if note_class in c_major:
            idx = c_major.index(note_class)
            new_idx = idx + 2
            new_octave = octave + (new_idx // 7)
            new_note_class = c_major[new_idx % 7]
            return new_octave * 12 + new_note_class
        return pitch + 4

    def vel_for_recorder(flute_vel):
        """Scale flute velocity down to recorder range 42-50."""
        return max(42, min(50, flute_vel - 8))

    # Pass 1: recorder enters at chord 3 (skip first 8 beats)
    time = 8.0
    for phrase in melody_pass1[2:]:
        t = time
        for pitch, dur, vel in phrase:
            hp = third_above(pitch)
            if hp is not None:
                midi.addNote(track, channel, hp, t, dur * 0.9, vel_for_recorder(vel))
            t += dur
        time += BEATS_PER_CHORD

    # Pass 2: recorder plays throughout
    for phrase in melody_pass2:
        t = time
        for pitch, dur, vel in phrase:
            hp = third_above(pitch)
            if hp is not None:
                midi.addNote(track, channel, hp, t, dur * 0.9, vel_for_recorder(vel))
            t += dur
        time += BEATS_PER_CHORD

    # Ending: recorder holds E5 (third above C5)
    midi.addNote(track, channel, 76, time, 3.5, 46)

    return midi


if __name__ == '__main__':
    print("Generating Track 10: Quest Complete...")
    mp3_path = generate_track("track-10", build)
    print(f"\nTrack 10 complete: {mp3_path}")
