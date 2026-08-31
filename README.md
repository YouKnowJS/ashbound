# Ashbound — Unity 6 prototype

A modular, primitive-mesh action roguelike with a persistent expedition Hub, four-resource economy, seven-encounter run, eight weapon families, 45 relics, five elemental identities, weapon skills, armor sets, the Cinder Regent, and a boss-gated final encounter. Supports solo or 2–4 humans on the same computer. No online networking or AI companions.

## Play

**Windows build:** run `Builds/Windows/Ashbound.exe` when present. Keep the executable beside its `_Data` folder and DLLs.

**Unity:**

1. In Unity Hub, **Add → Add project from disk**, and select this repository folder.
2. Open with **Unity 6000.4.11f1** (the installed/tested editor version). An active Unity license is required.
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**.
4. Use the Hub to spend resources and choose one preparation, then launch solo or add a second keyboard player and/or connected gamepads.

The project uses the built-in renderer, Input System 1.19.0, and Test Framework 1.6.0. Unity restores these pinned packages on import. No Asset Store assets, API keys, services, or paid plugins are required.

### Controls

| Action | Player 1 | Optional shared-keyboard player | Gamepad |
|---|---|---|---|
| Move | WASD | Arrow keys | Left stick |
| Aim | Mouse | IJKL, retains last direction | Right stick |
| Attack | Hold left mouse | Hold right Ctrl | RT / RB |
| Dash | Space | Right Shift | LB / X |
| Active ability | E | Enter | Y |
| Interact | F | Right Alt | A |
| Choose relic | Click / 1, 2, 3 | Shared selection controls | A, B, Y for current player |
| Pause | Esc | Shared | Keyboard Esc |
| Build and fragments | Tab | Shared | Keyboard Tab |
| Debug menu | F1 | Shared | Keyboard F1 |

Rare-or-higher elemental weapons replace the common shield burst with their data-driven Weapon Skill. Dash protects you for the first 0.15 seconds of its 0.22-second duration. Defeat each wave, choose a relic, then approach the cyan northern exit and interact after each encounter. Pick up gold text fragments by interacting nearby.

Local rewards are selected sequentially for each player. The mouse/number keys can choose for any player. Downed allies return with 45% health at the next reward; surviving players heal 25%. The party loses if everyone falls during combat. Gamepad disconnection pauses the whole run; reconnect the same device or return to the lobby. There is no join-in-progress.

## Scenes and setup

| Scene | Entry behavior |
|---|---|
| `Assets/Scenes/MainMenu.unity` | Local lobby and normal run entry |
| `Assets/Scenes/PrototypeRun.unity` | Starts solo immediately |
| `Assets/Scenes/TestArena.unity` | Seed 42, final boss, debug menu open |

Each scene contains **one `PrototypeBootstrap`** with the catalog assigned. It creates the camera, light, room geometry, party, UI, and system components. There is no manual prefab wiring. If generated assets are missing, use **Ashbound → Create prototype content**. That command creates missing assets without replacing existing tuning. It also sets input to Both, the build scene list, window defaults, and the Mono Windows backend.

The runtime factories intentionally generate primitive actors and rooms. Prefab authoring and animation are deferred; `EntityFactory`, `RoomView`, and `ActorView` are the replacement points. The actual item, weapon, boss, room, lore, and corruption data are saved ScriptableObject assets under `Assets/ScriptableObjects`.

## Developer guide

See [architecture and important scripts](Docs/ARCHITECTURE.md), [v0.4 progression guide](Docs/V0.4_META_PROGRESSION.md), [v0.3 element/equipment guide](Docs/V0.3_ELEMENT_EQUIPMENT.md), and [verification instructions](Docs/VERIFICATION.md).

For the original design brief, complete user prompt history, and a suggested prompt for working from another device, see [project prompts and continuation guide](Docs/Prompts/README.md).

**The architecture guide describes the end-of-run reveal. Keep it out of player-facing tutorials.**

### Debugging

F1 pauses simulation and exposes:

- Jump to the final boss; kill it through its real death event.
- Start the corruption transition **only after** that death event.
- Force specific player IDs for the next selection, and choose one or two corrupted players in a four-player party.
- Select a player, add a relic, spawn an owned pickup, or toggle invulnerability.
- Force weapon family, rarity, element, and Weapon Skill; equip two or four pieces of any authored set; clear equipment.
- Display dominant BuildAnalyzer themes, force relic themes, spawn elemental test enemies, and toggle status/VFX feedback.
- Add/zero persistent resources, set facility levels, unlock equipment, reset the profile, force preparations/reward rarity, and simulate expedition outcomes.
- Reset the run and unlock the roster.

Close F1 to let timers and transitions continue. Debug-modified runs are marked in telemetry. Item prerequisites and duplicate prevention apply to debug grants too. Clear invulnerability before balancing combat.

### Telemetry

One local JSON file is written on completion, reset, or application exit, under:

```text
%USERPROFILE%/AppData/LocalLow/AshboundPrototype/Ashbound/Telemetry/run-<id>.json
```

The authoritative directories are `Application.persistentDataPath/Profile` and `Application.persistentDataPath/Telemetry`. The Hub and result screen show paths and resource settlement. Records include combat/build data plus collected/retained/lost resources, Hub spending, facility levels, preparation, equipment acquisition/salvage, rerolls, progression depth, bosses, outcome, and a debug flag. Nothing is uploaded.

### Build / test from PowerShell

```powershell
./Tools/Verify-Prototype.ps1
./Tools/Build-Prototype.ps1
```

Close this project in the Editor before running batch commands. Set `-UnityPath` on either script for a different Unity installation. Generated test XML/logs go to `TestResults`; the development build goes to `Builds/Windows`. Both directories are git-ignored.

## Scope and known limitations

- This is a compact gray-box loop, not the full 10–15 minute-per-map game. Expect a short, untuned run.
- Multiplayer is local and shares one fixed camera. Player 1 uses mouse/keyboard; up to three additional seats use a second keyboard layout and/or gamepads. No transport, matchmaking, public/private online lobbies, rollback, reconnect identity service, or late join.
- Enemies steer directly toward targets; there is no navigation mesh or sophisticated avoidance.
- UI uses immediate-mode GUI. No animation, soundtrack, gamepad-only menu navigation, accessibility remapping, or polished controller feedback.
- Twenty prototype weapons, 12 Weapon Skills, four armor slots, and five armor sets prove the equipment architecture. Loot acquisition, equipment selection UI, inventory persistence, final models, and save/load of active runs are deferred.
- `UnlockData` is a data structure only, with no permanent stat bonuses or unlock-grind implementation.
- Damage, mutation, item grants, and state changes run on one local authority. The architecture exposes seams for networking; it is not network-ready synchronization code.
- Four-player corruption balance, weapon tuning, and reflection difficulty require human playtests. Shared keyboards may ghost certain simultaneous key combinations.
- A physical multi-gamepad session still needs hardware testing; automated virtual-device coverage cannot establish controller ergonomics or real disconnect behavior.

Ashbound v0.4 adds the persistent host profile, Expedition Hub, four-resource economy, preparations, data-driven facilities, unlock pools, failure retention, and personal equipment reward/salvage flow while preserving the v0.3 build systems. See [the v0.4 guide](Docs/V0.4_META_PROGRESSION.md).

Next milestone: collect repeated human solo and 2–4 player run telemetry, tune weapon/build/boss/reflection fairness and control feel, then prototype an authoritative networking transport without adding more content.
