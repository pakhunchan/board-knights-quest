#!/usr/bin/env python3
"""Generate TTS audio for all static lesson strings using content-addressable hashing.

Strategy: hash(text) → Assets/Resources/TTS/{hash}.mp3
See memory-bank/tts_hash_strategy.md for details.
"""

import hashlib
import json
import os
import requests

API_KEY = os.environ.get("ELEVENLABS_API_KEY") or os.environ["ELEVEN_LABS_API_KEY"]
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "TTS")

# Jessica voice settings (from memory-bank/voices.md)
VOICE_ID = "cgSgspJ2msm6clMCkdW9"
MODEL_ID = "eleven_multilingual_v2"
VOICE_SETTINGS = {
    "stability": 0.5,
    "similarity_boost": 0.75,
    "speed": 0.8,
}

# Brian voice settings (from memory-bank/voices.md) — for knight narration
BRIAN_VOICE_ID = "nPczCjzI2devNBz1zQrb"
BRIAN_MODEL_ID = "eleven_monolingual_v1"
BRIAN_VOICE_SETTINGS = {
    "stability": 0.3,
    "similarity_boost": 0.85,
}


def tts_hash(text: str) -> str:
    """Truncated SHA-256 (first 16 hex chars) of UTF-8 encoded text."""
    return hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


# ── All static strings ──────────────────────────────────────────────

# Knight narration (Brian voice)
KNIGHT_LINES = [
    # Intro3
    "Brave adventurer, you've already conquered tricky paths, "
    "discovered treasure, and proven your courage. "
    "Now it's time to face your greatest quest yet: "
    "mastering the magic of math!",
    # Outro1
    "You did it, brave knight! Every math challenge you conquer makes you "
    "sharper, stronger, and one step closer to becoming a true Math Knight\u2014"
    "so return soon for your next adventure.",
]

# TotalFractions2Manager.cs — intro card subtitle
TOTAL_FRACTIONS2_INTRO = [
    "Hello! Welcome to today's lesson on completing equivalent fractions. We'll jump right in.",
]

# TotalFractions2Manager.cs — Phase 1: Circles (steps 0–11)
TOTAL_FRACTIONS2_PHASE1 = [
    "Let's start with a circle.",
    "If we draw a line down the middle and",
    "shade the left side, we'll have two pieces and one will be shaded",
    "In math, we say that one-half is shaded",
    "Here is another circle.",
    "If we draw lines to split the circle into 6 pieces",
    "and shade the 3 pieces to the left",
    "we get three-sixths",
    "You can see that the shaded area in these two circles are equal. This means that their fractions, one half and three sixths, are equal too.",
    "But is there a simple way to go from one fraction to another fraction without drawing it out every time?",
    "There is, and we will learn that today",
    "Let's jump into an example",
]

# TotalFractions2Manager.cs — Phase 2: 1/2 = ?/6 (steps 13–21)
TOTAL_FRACTIONS2_PHASE2 = [
    "One half equals how many sixths? We know the answer is three sixths because of the circles before, but let's see how we can calculate this in a mathematical way",
    "Let's start by shifting this over to the right to make space",
    "We need to multiply one half by something to turn it into something over six.",
    "First, focus on the bottom numbers, the denominators.",
    "Two times three equals six.",
    "Let's now come back to the full equation. The rule is: whatever you multiply the bottom by, you have to multiply the top by the same value.",
    "Since we multiplied the bottom by three, we have to multiply the top by three.",
    "Multiplying the top is one times three, which is equal to three.",
    "So now we know that one-half is equal to three-sixths.",
]

# TotalFractions2Manager.cs — Phase 3: 2/3 = ?/9 (steps 23–35)
TOTAL_FRACTIONS2_PHASE3 = [
    "You're doing amazing, so let's continue with another example",
    "Two thirds is equal to how many ninths?",
    "First we'll make some space",
    "We have to multiply two thirds by something to figure out how many ninths it's equal to",
    "Three times what equals nine?",
    "Three times three is equal to nine, so move your piece to the three",
    "Whatever value we used for the bottom, we have to use for the top, so put a three on the top as well",
    "Let's now multiply the top values",
    "Two times three equals what?",
    "Two times three equals six",
    "Move your piece to the six",
    "Good job",
    "So now we know that two thirds is equal to six ninths",
]

# TotalFractions2Manager.cs — Phase 4: 3/5 = ?/20 (steps 37–42)
TOTAL_FRACTIONS2_PHASE4 = [
    "You're doing great, so let's try another example",
    "Three fifths is equal to how many twentieths?",
    "First, look at the bottom numbers, the denominators.",
    "Second, find the times number. Five times what number equals twenty?",
    "Third, multiple the top by that times number to get the answer",
    "How many twentieths is three fifths?",
]

# FractionsDemo5Manager.cs — question subtitles
FRACTIONS_DEMO5_QUESTIONS = [
    "One half is equal to how many sixths?",
    "three fifths is equal to how many tenths?",
    "two thirds is equal to how many sixths?",
    "four fifths is equal to how many tenths?",
    "three sixths is equal to how many eighteenths?",
    "four sevenths is equal to how many thirty-fifths?",
    "two ninths is equal to how many forty-fifths?",
    "five eighths is equal to how many sixty fourths?",
    "three sevenths is equal to how many forty ninths?",
    "three eighths is equal to how many forty eighths?",
]

# FractionsDemo5Manager.cs — encouragement phrases
FRACTIONS_DEMO5_ENCOURAGEMENT = [
    "Not quite, but good try.",
    "Not quite, but nice effort.",
    "Close, but not quite.",
]

# FractionsDemo5Manager.cs — other static
FRACTIONS_DEMO5_OTHER = [
    "Let's review this together.",
    "Let's now multiply the top values",
    "Remove your piece to continue",
    "Amazing! Great work today. You answered all ten questions!",
]

# Combine all lesson lines (Jessica voice)
ALL_LESSON_LINES = (
    TOTAL_FRACTIONS2_INTRO
    + TOTAL_FRACTIONS2_PHASE1
    + TOTAL_FRACTIONS2_PHASE2
    + TOTAL_FRACTIONS2_PHASE3
    + TOTAL_FRACTIONS2_PHASE4
    + FRACTIONS_DEMO5_QUESTIONS
    + FRACTIONS_DEMO5_ENCOURAGEMENT
    + FRACTIONS_DEMO5_OTHER
)


def generate(text: str, voice_id: str, model_id: str, voice_settings: dict) -> None:
    h = tts_hash(text)
    out_path = os.path.join(OUTPUT_DIR, f"{h}.mp3")
    if os.path.exists(out_path):
        print(f"  [{h}] exists, skipping: {text[:60]}")
        return

    print(f"  [{h}] generating: {text[:60]}...")
    resp = requests.post(
        f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}",
        headers={
            "xi-api-key": API_KEY,
            "Content-Type": "application/json",
        },
        json={
            "text": text,
            "model_id": model_id,
            "voice_settings": voice_settings,
        },
        stream=True,
    )
    resp.raise_for_status()

    with open(out_path, "wb") as f:
        for chunk in resp.iter_content(chunk_size=4096):
            f.write(chunk)


def write_manifest(all_texts: list[str]) -> None:
    manifest = {}
    for text in all_texts:
        h = tts_hash(text)
        manifest[h] = text
    manifest_path = os.path.join(OUTPUT_DIR, "manifest.json")
    with open(manifest_path, "w") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
    print(f"\nManifest written: {len(manifest)} entries → {manifest_path}")


if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    all_texts = KNIGHT_LINES + ALL_LESSON_LINES

    # Deduplicate
    seen = set()
    unique = []
    for t in all_texts:
        if t not in seen:
            seen.add(t)
            unique.append(t)
    print(f"Total lines: {len(all_texts)}, unique: {len(unique)}\n")

    # Generate knight lines (Brian)
    print("── Knight narration (Brian) ──")
    for text in KNIGHT_LINES:
        generate(text, BRIAN_VOICE_ID, BRIAN_MODEL_ID, BRIAN_VOICE_SETTINGS)

    # Generate lesson lines (Jessica)
    print("\n── Lesson narration (Jessica) ──")
    for text in ALL_LESSON_LINES:
        if text in KNIGHT_LINES:
            continue
        generate(text, VOICE_ID, MODEL_ID, VOICE_SETTINGS)

    # Write manifest
    write_manifest(unique)
    print("Done!")
