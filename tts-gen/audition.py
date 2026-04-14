#!/usr/bin/env python3
"""Generate ElevenLabs TTS audition samples for the knight narrator voice."""

import os
import requests

API_KEY = os.environ.get("ELEVENLABS_API_KEY") or os.environ["ELEVEN_LABS_API_KEY"]
OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "audition")

TEXT = (
    "Brave adventurer, you've already conquered tricky paths, "
    "discovered treasure, and proven your courage. "
    "Now it's time to face your greatest quest yet: "
    "mastering the magic of math!"
)

TEXT_EPIC = (
    "Brave adventurer, <break time=\"0.3s\"/> "
    "you've already conquered tricky paths, <break time=\"0.2s\"/> "
    "discovered treasure, <break time=\"0.2s\"/> "
    "and proven your courage. <break time=\"0.4s\"/> "
    "Now it's time to face your greatest quest yet: <break time=\"0.3s\"/> "
    "mastering the magic of math!"
)

TEXT_OUTRO = (
    "You did it, brave knight! <break time=\"0.2s\"/> "
    "Every math challenge you conquer makes you sharper, <break time=\"0.2s\"/> "
    "stronger, <break time=\"0.2s\"/> "
    "and one step closer to becoming a true Math Knight\u2014 <break time=\"0.3s\"/> "
    "so return soon for your next adventure."
)

TEXT_LESSON_SAMPLES = [
    "This is a circle. If we draw a line down the middle and shade the left side, we get one-half.",
    "You can see that these fractions are equal. But how do we go from one fraction to another fraction?",
    "Focus first on the bottom numbers, the denominators. Two times three equals six.",
    "So now we know that one-half is equal to three-sixths.",
]
TEXT_LESSON = " ".join(TEXT_LESSON_SAMPLES)

VOICES = [
    ("Brian", "nPczCjzI2devNBz1zQrb", {}),
    ("Adam", "pNInz6obpgDQGcFmaJgB", {}),
    ("George", "JBFqnCBsd6RMkjVDRZzb", {}),
    ("Clyde", "2EiwWnXFnvU5JabPnv8n", {}),
    ("Brian-Epic-v1", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_EPIC,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.8,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Brian-Epic-v2", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_EPIC,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.8,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Brian-Epic-v3", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_EPIC,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.65,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Outro1-Brian-Epic-v1", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_OUTRO,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.65,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Outro1-Brian-Epic-v2", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_OUTRO,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.65,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Outro1-Brian-Epic-v3", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_OUTRO,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.65,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
    ("Lesson-Rachel", "21m00Tcm4TlvDq8ikWAM", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Bella", "EXAVITQu4vr4xnSDxMaL", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Elli", "MF3mGyEYCl7XYWbV9V6O", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Dorothy", "ThT5KcBeYPX3keUQqHPh", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Charlotte", "XB0fDUnXU5powFXDhCwa", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Alice", "Xb7hH8MSUJpSbSDYk0k2", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Matilda", "XrExE9yKIg1WjnnlVkGX", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Lily", "pFZP5JQG7iQjIQuC4Bku", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Jessica-v1", "cgSgspJ2msm6clMCkdW9", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Jessica-v2", "cgSgspJ2msm6clMCkdW9", {
        "text": TEXT_LESSON,
        "speed": 0.7,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Jessica-v3", "cgSgspJ2msm6clMCkdW9", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
            "speed": 0.7,
        },
    }),
    ("Lesson-Jessica-v4", "cgSgspJ2msm6clMCkdW9", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
            "speed": 0.8,
        },
    }),
    ("Lesson-Grace", "oWAxZDx7w5VEj9dCyTzz", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Lesson-Serena", "pMsXgVXv3BLzUgSXRplE", {
        "text": TEXT_LESSON,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }),
    ("Brian-Epic-v4", "nPczCjzI2devNBz1zQrb", {
        "text": TEXT_EPIC,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
            "speed": 0.7,
        },
    }),
    ("George-Epic", "JBFqnCBsd6RMkjVDRZzb", {
        "text": TEXT_EPIC,
        "model_id": "eleven_monolingual_v1",
        "speed": 0.7,
        "voice_settings": {
            "stability": 0.3,
            "similarity_boost": 0.85,
        },
    }),
]

def generate(name: str, voice_id: str, overrides: dict = None) -> None:
    out_path = os.path.join(OUTPUT_DIR, f"{name}.mp3")
    if os.path.exists(out_path):
        print(f"  {name}: already exists, skipping")
        return

    print(f"  {name}: generating...")
    payload = {
        "text": TEXT,
        "model_id": "eleven_multilingual_v2",
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
        },
    }
    if overrides:
        for k, v in overrides.items():
            payload[k] = v

    resp = requests.post(
        f"https://api.elevenlabs.io/v1/text-to-speech/{voice_id}",
        headers={
            "xi-api-key": API_KEY,
            "Content-Type": "application/json",
        },
        json=payload,
        stream=True,
    )
    resp.raise_for_status()

    with open(out_path, "wb") as f:
        for chunk in resp.iter_content(chunk_size=4096):
            f.write(chunk)
    print(f"  {name}: saved to {out_path}")

if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print("Generating audition samples...")
    for name, voice_id, overrides in VOICES:
        generate(name, voice_id, overrides)
    print("Done!")
