# v0.8.1 UI and boss-navigation hotfix

This focused release corrects UI text clipping and large-enemy terrain stalls without changing the expedition loop, combat rules, rewards, camera design, irregular spaces, corruption reveal, local multiplayer authority, or meta progression.

## UI cause and correction

The shared IMGUI button style previously reserved 18 vertical pixels for padding while several controls were only 27–31 pixels high. The preparation list also advanced rows by 32 pixels, leaving no room for longer English or Simplified Chinese labels. Independent panel styles made the problem inconsistent across screens.

`PrototypeGui` now owns the font hierarchy and the Title, Heading, Body, Small, Metadata, Resource, Tooltip, Section Header, Centered Body, and Button styles. It selects a dynamic OS font from an explicit English/zh-CN-capable list headed by Microsoft YaHei UI/YaHei, verifies both Latin and Simplified Chinese glyphs, wraps multi-line controls, and reduces font size to a safe minimum when either height or single-line width does not fit. Buttons use four-pixel vertical padding and a 30-pixel minimum. Preparation choices use 40-pixel buttons on a 44-pixel row pitch and localized names.

The logical UI remains 1280×720, but it now scales uniformly and centers within the real display. At ultrawide aspect ratios this preserves proportions and leaves side margins instead of stretching controls. `UILayoutAudit` validates representative major panels, the full preparation list, font coverage, and safe bounds at 1920×1080, 2560×1440, and 3440×1440 in both languages. The editor command is **Ashbound → Validate UI layout**. Runtime inspection is under **F1 → Camp → UI Layout Test**.

## Boss terrain-stall cause and correction

The old Cinder Regent used the same 0.4-unit `CharacterController` radius as ordinary enemies and moved directly toward the player. It had no obstacle probe, clearance model, alternate steering, stuck timer, or recovery. Its requested Divided Hall spawn at `(0, 0, 4.5)` also overlapped or closely bordered the central divider geometry, so a large visual body could begin in an unsafe route.

`LargeBodyNavigationSafety` is a reusable lightweight layer for Bosses, Mini-Bosses, Elites, and Bruisers. `BossDefinition` and `EnemyDefinition` now configure body radius, minimum obstacle clearance, preferred movement zone, optional allowed arena sections, stuck duration, minimum displacement, and attempts before emergency recovery. Spawn requests are validated against playable sections and physical obstacles, then moved to the nearest safe anchor when necessary.

Movement keeps the controller's tactical intent but scores clear forward and angled candidates. Lunges use the same body-aware full-distance probe. When an actor has sustained intent but moves less than the configured threshold, recovery escalates through:

1. an alternate clear steering candidate;
2. movement toward the best safe anchor;
3. an emergency reposition only after repeated failures.

Emergency destinations must be valid playable terrain, clear of obstacles, away from the original stuck point, and outside a safety radius around living players. No players are teleported and no authored obstacle or irregular boundary is removed.

The Ecology debug page can show body radius, clearance, desired/applied directions, probe candidates, stuck time, attempts, and safe anchors. It also offers **Force stuck** and **Force recovery**. Schema-5 telemetry gains a backward-compatible `navigationSafety` list with actor, arena, stuck events, detected-stuck time, recovery attempts, and emergency reposition count.

## Architecture seams

- `ActorMotor` applies the movement filter and body dimensions, so AI behavior code keeps expressing normal intent.
- `RoomView` owns section-aware, rotation-safe position validation and safe-anchor generation.
- `EntityFactory` attaches the reusable component according to data, rather than enemy-name checks.
- `RoomDirector` validates large-body and true-Boss spawn points.
- `MatchTelemetry` records recovery behavior locally with the existing run record.

The system intentionally avoids a NavMesh. It provides local avoidance and deterministic recovery suited to the current connected graybox spaces. It does not calculate a global route through a maze, predict moving crowds, or replace human tuning of arena clearance. Current authored floors are flat; multi-level traversal will need explicit vertical navigation data. `preferredMovementZone` influences anchor scoring rather than acting as a hard enclosure.

## Human acceptance checklist

1. At 1920×1080, 2560×1440, and 3440×1440, open the UI Layout Test and cycle every panel in English and Simplified Chinese. Confirm no clipped glyphs, overlapping rows, stretched controls, or hidden disabled-state text.
2. Open the real Expedition Table and select every preparation. Confirm its name, cost/state, selection marker, and neighboring rows remain readable.
3. In solo, fight the Cinder Regent around the Divided Hall divider, corners, and obstacles. Confirm pursuit and lunges steer around geometry without jittering in place.
4. Repeat with 2–4 local players spread around the room. Confirm recovery never lands on a living player and the camera remains readable.
5. Use the Ecology overlay, Force stuck, and Force recovery. Confirm the stages escalate, candidate and anchor diagnostics match the terrain, and emergency reposition happens only after the configured attempts.
6. Spawn a Bruiser, Elite, and Mini-Boss in Medium and Large spaces. Confirm the same safety layer is present without changing normal small-enemy behavior.
7. Complete the regional Boss, corruption transition, final PvP/reflection, settlement, and reset to confirm the existing loop is unchanged.
8. Inspect the local telemetry JSON and confirm `navigationSafety` entries identify the actor and arena without upload or serialization errors.

## Automated result

On 2026-09-06 with Unity 6000.4.11f1: content generation and compilation passed; Edit Mode passed 46/46; Play Mode passed 23/23; and the Windows x64 development build succeeded. See `Docs/VERIFICATION.md` for the durable record and the remaining human-only checks.
