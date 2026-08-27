# Project prompts and continuation guide

Saved on **2026-08-27** for the private [YouKnowJS/ashbound repository](https://github.com/YouKnowJS/ashbound).

## Your original prompts

- [Original game brief](ORIGINAL_GAME_BRIEF.md): the complete attached design request, with its wording preserved.
- [User prompt history](USER_PROMPTS.md): all 8 user messages from **Build roguelike PvP prototype**, including the attachment reference and subsequent license, GitHub, and remote-work requests.

These files contain prompts from this game task only. They do not include unrelated conversations, app/runtime metadata, assistant/tool transcripts, or authentication data. This export is a snapshot; later messages are not uploaded automatically.

The remaining sections are an assistant-written handoff, not additional historical user prompts. The design brief describes the final reveal and is intended for development, not player tutorials.

## Continue on another device

1. Sign in to GitHub with an account that has access to this private repository, then clone it:

   ```sh
   git clone https://github.com/YouKnowJS/ashbound.git
   cd ashbound
   ```

2. Open the cloned folder in your editor or coding assistant. Use the continuation prompt below, replacing the final placeholder with your next request.
3. To play, test, or build, follow the [Unity setup and controls](../../README.md). The tested Editor is **Unity 6000.4.11f1**. Open `Assets/Scenes/MainMenu.unity` and press Play after setup.

If the repository is already cloned, inspect `git status` and use `git pull --ff-only` when the working tree is clean. Commit and push changes before switching devices. GitHub contains the project and prompt files, but not the local Unity installation, generated Windows build, caches, test artifacts, or saved telemetry.

## Current project state

At the initial source upload, commit `b056d9b3e76607c6108c03ba81a9639468dd6a2e`:

- A playable Unity 6 C# prototype exists, with primitive visuals and solo or 2–4 human local play. Online networking is not implemented.
- The loop includes two combat rooms, upgrade choices, the Cinder Regent boss, and a boss-death-gated corruption finale. Solo uses a corrupted reflection, with no AI companions.
- Combat, items, rooms, run state, corruption, and telemetry are separate systems. Content is stored in ScriptableObjects.
- Recorded verification: **22 Edit Mode + 8 Play Mode tests passed**; the Windows build succeeded and its lobby and solo arena were visually observed. These are prior results, not verification of future changes.
- Human balance playtests and physical multiplayer controller testing remain. The recommended next milestone is playtesting and tuning before networking or additional content.

For details, read [architecture](../ARCHITECTURE.md), [verification and limitations](../VERIFICATION.md), and [next milestone](../NEXT_MILESTONE.md).

## Suggested continuation prompt

This is a new prompt you can copy and adapt; it is not a message from the original conversation.

```text
Continue development of Ashbound in this repository.

First read:
- Docs/Prompts/ORIGINAL_GAME_BRIEF.md
- Docs/Prompts/USER_PROMPTS.md
- README.md
- Docs/ARCHITECTURE.md
- Docs/VERIFICATION.md
- Docs/NEXT_MILESTONE.md

Inspect the current files and Git status before editing. Work from the existing
Unity 6 C# prototype and preserve unrelated changes.

Keep the original scope and design constraints: modular systems, placeholder
visuals, no co-op friendly fire, corruption only after final boss death, no early
tutorial spoilers for the reveal, no solo AI companions, no join-in-progress,
and no permanent stat grind that makes the final PvP unfair. Full online
networking and full-game content expansion are deferred unless requested.

Use the existing test and build tools when Unity is available. Report what you
actually verified, and state any unavailable checks. Do not treat old test
results as proof that new changes work.

My next task: [replace this with the change you want to make]
```
