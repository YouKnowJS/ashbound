# Verification

## Reproduce

Close the project in Unity, then run `Tools/Verify-Prototype.ps1` from PowerShell. It creates any missing content, runs Edit Mode tests, then Play Mode tests with the actual Unity Editor. It fails for compiler errors, a nonzero Unity exit code, missing test output, zero tests, or failed tests. It does not substitute a mock Unity engine.

Alternatively, open **Window → General → Test Runner** in Unity and run both tabs. For a Windows development build use **Ashbound → Build Windows prototype** or `Tools/Build-Prototype.ps1`.

Artifacts (git-ignored) are under `TestResults/`: `editmode.xml`, `playmode.xml`, and corresponding logs. Unity batch invocations intentionally omit `-quit` for test runs because the Test Runner owns shutdown.

## Automated coverage

v0.7 validation additionally checks camera tuning and world-depth data, Medium/Large scale separation, node-to-space categories, smooth solo transition follow, multiplayer centroid framing, spread-driven zoom caps, regrouping, and the soft spread hook. It retains the complete v0.6 route and service coverage.

### Edit Mode

- Co-op friendly-fire rejection in every pre-final state, even if a caller requests friendly fire.
- Final opposing-team damage, same-team protection, and solo reflection hostility.
- Self-hit rejection, combat-state gating, legal run transitions, boss-death latch, and reset.
- One corrupted player for two/three humans; one or two for four; seed reproducibility, no duplicate/foreign IDs, bounded forced selections.
- Unique lobby devices, capacity, locked joins/removals, and roster reuse after reset.
- Shields, invulnerability, healing/death, invalid damage, and maximum-health modifiers.
- Dominant tag analysis, stable ties, and simplified reflection relic mapping.
- Authored content references, IDs, room counts, and elite presence.
- Elemental weapon combinations, family/element separation, rarity coverage, and Rare-or-higher skill eligibility.
- Armor slot coverage, two/four-piece activation, and BuildAnalyzer input from relic, weapon, skill, armor, and set layers.
- Profile save/load, legacy normalization, invalid/missing fallback, reset, nonnegative spending, prerequisites, maximum facility levels, and unlock persistence.
- Failure/milestone/success retention, preparation selection, run-to-profile transfer, rarity salvage values, and merchant-price versus salvage invariants.

### Play Mode

- Full solo orchestration: both rooms, all four reward selections, gate interactions, boss death, transition, reflection, victory, and JSON telemetry output/round-trip.
- Two-player co-op protection, forced corruption after boss death, final bidirectional damage, winner, and no late joins.
- Four-player roster with virtual gamepads, two forced corrupted players, both teams' friendly-fire protection, cross-team damage, and draw resolution.
- Actual CharacterController dash displacement, partial invulnerability, attack cooldown, melee damage, shield ability, and death lockout.
- Bleed/rupture/lightning proc counts without recursive secondary-hit loops.
- Boss second phase, transition cancellation of live hazards/projectiles, and clean roster reset.
- Bound gamepad input reading and disconnect pause, using the Input System's isolated `InputTestFixture`.
- Source-attributed damage over time, pause behavior, slow, and stun expiry.
- An elemental Weapon Skill damaging/applying status in combat, four-piece equipment activation, solo reflection weapon/skill identity, and corruption layering.
- Encounter resource collection and a per-player equipment reward dismantle flow using the real run state machine.
- Data-driven encounter spawning with `EnemyBrain`/definition wiring and visible connected transition geometry.
- Schema-4 encounter telemetry with composition, role kills, and arena-category context.
- Seeded graph selection, route validation/visibility, local majority voting and host tie-break behavior.
- Node-driven reward cadence, regional-Boss/final-Boss gating, Cursed Chest bounded health cost, Merchant purchasing, and Common Mimic combat/reward flow.
- Schema-5 node, route-choice, vote, service, and menu-duration telemetry.

Runtime tests accelerate combat with large damage or debug death calls where they are testing progression. They do **not** prove a human can beat an untuned boss or that the combat feels good. Virtual-device tests do not replace a physical controller session.

## Manual acceptance checklist

1. Launch the Windows build and enter solo from the lobby. Confirm the gray-box arena, readable controls, and no early explanation of the final reveal.
2. Move diagonally, aim independently, hold attack, dash through danger, and use the shield pulse. Check that telegraphs appear before hits.
3. Clear a wave, choose a relic, and confirm another wave begins. Clear the next wave, approach the cyan northern gate, and interact.
4. Collect a fragment and inspect the Tab journal. Pause/resume via Esc. Open/close F1 and confirm timers stop/resume.
5. Use debug to jump to the boss. Trigger corruption must remain unavailable while the boss lives. Kill the boss, close debug, and confirm the pause, transition, and distinct solo reflection.
6. Start two local humans. Confirm P1/P2 input is separate, both select upgrades, ally hits do no damage, and final opposing teams can damage each other.
7. With physical gamepads, verify bindings, disconnect pause, reconnect behavior, three/four-player framing, and keyboard ghosting limits.
8. Finish/reset a run, inspect the local telemetry JSON, and start a second run to check cleanup.
9. Use F1 → Ecology to test every role, affinity, preset, Elite override, arena category, AI toggle, and telegraph toggle.
10. Walk the transition paths and inspect all five spaces for square-box presentation, camera loss, blocked spawn points, and projectile/obstacle behavior.
11. Open F1 → Camera. Test follow off/on, Solo and Party Centroid, forced min/max zoom, centroid/spread/clamp overlays, and both enlarged test spaces.
12. In local co-op, separate beyond the shown soft limit. Confirm zoom stops at the configured maximum, edge indicators remain readable, and regrouping zooms back in without teleporting either player.

## Verification status

### Camera and larger-world update — 2026-08-31

Using Unity **6000.4.11f1** after implementing the v0.7 moving camera and scaled combat spaces:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **42 passed**, 0 failed, 0 skipped |
| Play Mode | **18 passed**, 0 failed, 0 skipped |

The added Edit Mode coverage validates data-driven camera ranges, world depth, node context sizing, and that Large arenas are at least 35% greater in effective area than the largest Medium arena. The added Play Mode coverage verifies damped solo follow through combat and transition, clamping, two-player centroid tracking, spread zoom, its maximum cap, regroup zoom-in, and the future soft-tether event hook.

### Floor-seam correction — 2026-08-31

Using Unity **6000.4.11f1** after correcting unsafe combat-space entrances, the Divided Hall divider seam, and missing-floor recovery:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **41 passed**, 0 failed, 0 skipped |
| Play Mode | **16 passed**, 0 failed, 0 skipped |

The added Edit Mode test checks a controller-radius-safe spawn footprint for all four local players in every combat space and verifies that the Divided Hall divider covers both adjoining floor edges. The added Play Mode test moves a live player below missing geometry and verifies one recovery to the last fully supported position.

### v0.6 verification — 2026-08-31

Using Unity **6000.4.11f1** on Windows after the expedition-route and node-identity expansion:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **40 passed**, 0 failed, 0 skipped |
| Play Mode | **15 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Executable smoke launch | Process remained running for eight seconds |
| Human route pacing, economy, Mimic frequency, physical multiplayer voting | Not completed |

Automated tests validate graph constraints, deterministic routing, visibility, vote resolution, node reward separation, service integration, economy invariants, final-Boss gating, and telemetry wiring. They do not establish route pacing, reward balance, combat-space scale, or good Treasure/Mimic frequency.

### v0.5 verification — 2026-08-31

Using Unity **6000.4.11f1** on Windows after the enemy-ecology and combat-space expansion:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **36 passed**, 0 failed, 0 skipped |
| Play Mode | **12 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Executable smoke launch | Process remained running for six seconds; no managed exception signature found in the dedicated player log |
| Human role balance, map scale, physical 2–4 player framing | Not completed |

Automated checks establish content completeness, integration, and the preserved end-to-end run. They do not establish that enemy overlap, telegraph readability, combat-space scale, or multiplayer camera framing is balanced.

### v0.4 verification — 2026-08-31

Using Unity **6000.4.11f1** on Windows after the Hub/meta-progression expansion:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **33 passed**, 0 failed, 0 skipped |
| Play Mode | **10 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Executable smoke launch | Process remained running for six seconds; no managed exception signature found in `Player.log` |
| Human progression/economy pacing and physical controllers | Not completed |

The automated suite verifies persistence, currency invariants, facilities, unlocks, preparations, retention, salvage, resource settlement, equipment rewards, the v0.3 build systems, and the final corruption encounter. It does not establish balanced progression pacing.

### v0.3 verification — 2026-08-31

Using Unity **6000.4.11f1** on Windows after the element/equipment expansion:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed |
| Edit Mode | **26 passed**, 0 failed, 0 skipped |
| Play Mode | **9 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Executable smoke launch | Process remained running for six seconds; no managed exception signature found in `Player.log` |
| Human equipment/element balance and physical controllers | Not completed |

Automated checks prove the data paths and playable runtime flow; they do not establish final balance, feel, visual clarity, or controller ergonomics.

### v0.2 verification — 2026-08-31

Using Unity **6000.4.11f1** on Windows after the combat/build expansion:

| Check | Result |
|---|---|
| Unity content generation and script compilation | Passed; no C# compiler warnings found |
| Edit Mode | **23 passed**, 0 failed, 0 skipped |
| Play Mode | **8 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Executable smoke launch | Process launched and remained responsive; no managed exception found in `Player.log` |
| Human weapon/build balance and physical controllers | Not completed |

The existing machine-specific D3D12 info-queue warning remains non-fatal. Automated tests validate rules and orchestration; they do not establish the 20–25 minute pacing target or combat feel.

Recorded on **2026-08-27**, using Unity **6000.4.11f1** on Windows:

| Check | Result |
|---|---|
| Unity script compilation | Passed; no C# compiler warnings in the final content/test runs |
| Edit Mode | **22 passed**, 0 failed, 0 skipped |
| Play Mode | **8 passed**, 0 failed, 0 skipped |
| Windows x64 development build | **Succeeded**, Mono backend |
| Actual Windows executable | Launched successfully |
| Visual observation | Lobby and active solo arena rendered; actor silhouettes, enemies, gate, health/ability HUD, and controls visible |
| Managed runtime exceptions on launch/observed play | None found in Player.log |
| Physical multi-gamepad and human balance tests | **Not completed** |

During verification, the solo completion test initially attacked a reflection during its valid dash invulnerability. The test now waits for that window to end. Virtual input tests now use an isolated InputTestFixture with correctly ordered teardown and wait for the queued input frame. These are test-timing/fixture corrections, not disabled assertions.

The native graphics log contains `d3d12: failed to query info queue interface (0x80004002)` on this machine. The game continued and rendered successfully; no graphics-debug-layer configuration was changed.

The desktop tool reported external input in the game window during visual verification. Further automated UI input was stopped to avoid interfering. The full progression and final phase were verified by the runtime tests, not claimed as a completed human playthrough.
