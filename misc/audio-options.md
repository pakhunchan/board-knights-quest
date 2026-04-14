# TTS Audio Strategy for Offline Narration

The game runs on offline devices, so TTS audio must be pre-generated and bundled at build time — no runtime API calls.

## Architecture (Already in Place)

- `LessonSequencer` has a `TTSProvider` delegate slot that runs alongside subtitles with barrier sync
- Setting the provider is all that's needed to enable audio — every manager already goes through the sequencer
- Subtitle text is the single source of truth for both display and narration

## Text-to-Audio Mapping

Use a **short hash of the subtitle text** as the audio filename:

```
"Two thirds is equal to how many ninths?" → a3f7c2e91b04.ogg
```

- First 12 hex characters of MD5, deterministic
- Fixed-length filenames, no filesystem issues
- Change the text → hash changes → old file stops matching → you know to regenerate
- Opaque filenames, so the generation script should output a manifest mapping hash → source text

```csharp
using System.Security.Cryptography;
using System.Text;

string hash = BitConverter.ToString(
    MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(subtitle))
).Replace("-", "").Substring(0, 12).ToLower();
```

## Two Categories of Subtitles

### Static lessons (Demo3, TotalFractions2, etc.)
- Subtitle text is hardcoded in `LessonStep` arrays
- Enumerate all steps at build time, generate audio for each

### Guided explanations (GuidedExplanationHelper)
- Subtitle text is generated dynamically at runtime using `NumberWords`
- But the input set is fixed — 10 `PracticeQuestion` objects in Demo5
- Each question produces ~8 subtitle lines (intro, multiply prompt, denominator hint, numerator prompt, multiply top, multiply result, move piece, summary)
- A build script can loop through all questions, run the same `NumberWords` logic, and enumerate every possible subtitle string

Total estimated clip count: ~100-150 lines across all lessons.

## Generation Pipeline

1. A script (Python or C#) enumerates all subtitle text from both static steps and the fixed question set
2. Feeds each line to a TTS engine (offline-capable, e.g., Piper, Coqui, or macOS `say`)
3. Outputs compressed `.ogg` files (mono, ~64kbps) named by hash
4. Outputs a `manifest.json` mapping hash → source text for debugging
5. Audio files go into `Assets/Audio/Narration/` or similar
6. A `NarrationLibrary` ScriptableObject or `Resources` folder makes them loadable at runtime

Estimated total size: 2-5MB for all clips at modest compression.

## Runtime Playback

- A `NarrationPlayer` component holds an `AudioSource` and implements the `TTSProvider` delegate
- On each step, it hashes the subtitle text, loads the matching clip, plays it
- If a clip is missing (text changed, audio not regenerated), it logs a warning and falls back to subtitle-only timing
- The clip's actual duration replaces `EstimatedDuration` for karaoke sync

## Validation

A build-time or on-demand validation script should:
- Hash every subtitle in code
- Check each hash has a corresponding audio file
- Flag missing clips (text exists, no audio) and orphaned clips (audio exists, no matching text)
- This catches drift when lesson content changes
