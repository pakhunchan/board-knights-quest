"""
Shared music generation utilities for knight/adventure math game tracks.
Generates MIDI → WAV → MP3 pipeline using midiutil + fluidsynth + ffmpeg.
"""

import os
import subprocess
import random
from midiutil import MIDIFile

# Paths
SOUNDFONT = "/opt/homebrew/Cellar/fluid-synth/2.5.3/share/fluid-synth/sf2/VintageDreamsWaves-v2.sf2"
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "tracks")

# General MIDI instrument numbers
INSTRUMENTS = {
    "piano": 0,
    "bright_piano": 1,
    "celesta": 8,
    "glockenspiel": 9,
    "music_box": 10,
    "vibraphone": 11,
    "marimba": 12,
    "xylophone": 13,
    "tubular_bells": 14,
    "nylon_guitar": 24,
    "acoustic_guitar": 25,
    "pizzicato_strings": 45,
    "harp": 46,
    "strings": 48,
    "slow_strings": 49,
    "flute": 73,
    "recorder": 74,
    "pan_flute": 75,
    "blown_bottle": 76,
    "whistle": 78,
    "ocarina": 79,
    "pad_warm": 89,
}

# Musical scales (MIDI note numbers relative to root)
SCALES = {
    "C_major": [60, 62, 64, 65, 67, 69, 71, 72],
    "G_major": [55, 57, 59, 60, 62, 64, 66, 67],
    "F_major": [53, 55, 57, 58, 60, 62, 64, 65],
    "D_major": [50, 52, 54, 55, 57, 59, 61, 62],
    "Bb_major": [58, 60, 62, 63, 65, 67, 69, 70],
    "Eb_major": [51, 53, 55, 56, 58, 60, 62, 63],
    "A_major": [57, 59, 61, 62, 64, 66, 68, 69],
    "C_pentatonic": [60, 62, 64, 67, 69, 72],
    "G_pentatonic": [55, 57, 59, 62, 64, 67],
    "F_pentatonic": [53, 55, 58, 60, 62, 65],
}


def create_midi(num_tracks=4, tempo=110, time_sig=(4, 4)):
    """Create a new MIDI file."""
    midi = MIDIFile(num_tracks)
    midi.addTempo(0, 0, tempo)
    return midi


def add_melody(midi, track, channel, instrument, notes, start_time=0, velocity_base=80):
    """
    Add a melody line to the MIDI file.
    notes: list of (pitch, duration, velocity_offset) tuples
    """
    midi.addProgramChange(track, channel, 0, instrument)
    time = start_time
    for pitch, duration, vel_offset in notes:
        if pitch is not None:  # None = rest
            vel = max(40, min(127, velocity_base + vel_offset))
            midi.addNote(track, channel, pitch, time, duration, vel)
        time += duration
    return time


def add_chord_progression(midi, track, channel, instrument, chords, start_time=0,
                          duration=4.0, velocity=60, arpeggio_delay=0):
    """
    Add chord progression.
    chords: list of lists of MIDI pitches
    """
    midi.addProgramChange(track, channel, 0, instrument)
    time = start_time
    for chord in chords:
        t = time
        for note in chord:
            midi.addNote(track, channel, note, t, duration, velocity)
            t += arpeggio_delay
        time += duration
    return time


def add_bass_line(midi, track, channel, instrument, notes, start_time=0, velocity=70):
    """Add a bass line. notes: list of (pitch, duration) tuples."""
    midi.addProgramChange(track, channel, 0, instrument)
    time = start_time
    for pitch, duration in notes:
        if pitch is not None:
            midi.addNote(track, channel, pitch, time, duration, velocity)
        time += duration
    return time


def add_percussion(midi, track, channel, pattern, bars=8, start_time=0, velocity=50):
    """
    Add percussion pattern. Uses channel 9 (GM drums).
    pattern: list of (beat_offset, drum_note, velocity_offset) per bar
    """
    # GM drum notes
    # 35=bass drum, 38=snare, 42=closed hi-hat, 44=pedal hi-hat
    # 46=open hi-hat, 56=cowbell, 75=claves, 76=hi wood block
    # 54=tambourine, 60=hi bongo, 61=lo bongo, 80=mute triangle, 81=open triangle
    time = start_time
    for bar in range(bars):
        for beat_offset, drum_note, vel_off in pattern:
            vel = max(30, min(100, velocity + vel_off))
            midi.addNote(track, channel, drum_note, time + beat_offset, 0.25, vel)
        time += 4.0  # 4 beats per bar
    return time


def save_midi(midi, filename):
    """Save MIDI file."""
    path = os.path.join(OUTPUT_DIR, filename)
    with open(path, "wb") as f:
        midi.writeFile(f)
    return path


def midi_to_wav(midi_path, wav_path=None):
    """Convert MIDI to WAV using fluidsynth."""
    if wav_path is None:
        wav_path = midi_path.replace(".mid", ".wav")
    cmd = [
        "fluidsynth",
        "-F", wav_path,
        "-r", "44100",
        "-g", "1.0",
        "-i",
        SOUNDFONT,
        midi_path,
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if not os.path.exists(wav_path):
        raise RuntimeError(f"fluidsynth failed: {result.stderr}")
    return wav_path


def wav_to_mp3(wav_path, mp3_path=None):
    """Convert WAV to MP3 using ffmpeg."""
    if mp3_path is None:
        mp3_path = wav_path.replace(".wav", ".mp3")
    cmd = [
        "ffmpeg", "-y", "-i", wav_path,
        "-q:a", "2",
        mp3_path,
    ]
    subprocess.run(cmd, check=True, capture_output=True)
    return mp3_path


def generate_track(track_name, build_fn):
    """
    Full pipeline: build MIDI → WAV → MP3.
    build_fn(midi) should populate the MIDI file.
    Returns the MP3 path.
    """
    midi_file = f"{track_name}.mid"
    midi = build_fn()
    midi_path = save_midi(midi, midi_file)
    print(f"  MIDI saved: {midi_path}")

    wav_path = midi_to_wav(midi_path)
    print(f"  WAV rendered: {wav_path}")

    mp3_path = wav_to_mp3(wav_path)
    print(f"  MP3 encoded: {mp3_path}")

    # Clean up intermediate files
    os.remove(midi_path)
    os.remove(wav_path)
    print(f"  Done: {mp3_path}")
    return mp3_path


# === Musical helpers ===

def make_simple_melody(scale, pattern, octave_shift=0):
    """
    Create melody notes from scale indices.
    pattern: list of (scale_index, duration, velocity_offset)
    Returns list of (pitch, duration, velocity_offset) or (None, duration, 0) for rests.
    """
    result = []
    for idx, dur, vel in pattern:
        if idx is None:
            result.append((None, dur, 0))
        else:
            # Wrap around scale
            octave = idx // len(scale)
            note_idx = idx % len(scale)
            pitch = scale[note_idx] + (octave * 12) + (octave_shift * 12)
            result.append((pitch, dur, vel))
    return result


def make_chord(root, chord_type="major", inversion=0):
    """Create a chord from root note."""
    intervals = {
        "major": [0, 4, 7],
        "minor": [0, 3, 7],
        "sus2": [0, 2, 7],
        "sus4": [0, 5, 7],
        "maj7": [0, 4, 7, 11],
        "min7": [0, 3, 7, 10],
        "add9": [0, 4, 7, 14],
    }
    chord = [root + i for i in intervals.get(chord_type, intervals["major"])]
    # Apply inversion
    for i in range(inversion):
        chord[i % len(chord)] += 12
    return chord


def repeat_pattern(pattern, times):
    """Repeat a note pattern multiple times."""
    return pattern * times
