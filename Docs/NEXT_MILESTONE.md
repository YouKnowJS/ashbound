# Next milestone: camp usability and one authored presentation slice

1. Human-playtest the camp in English and Simplified Chinese with keyboard and physical gamepads; record collision, prompt, camera, wrapping, and upgrade-state findings.
2. Add controller-first focus/navigation for Camp, Route, Treasure, Merchant, Rest, Event, and equipment panels while preserving locked device ownership.
3. Replace one complete camp slice with authored modular environment art, resource sprites, and one rigged NPC/player set through `CampHub`, `ResourceIconLibrary`, and `ActorView` seams.
4. Move longer translated content into localized data assets and add screenshot/layout checks for common aspect ratios.
5. Tune route economy only after the existing telemetry and human sessions provide evidence.
6. Do not expand to all five regions or attempt a broad final-art pass until one camp and one-region loop are readable and stable.

## Initial tuning knobs

| Data / code | What to tune |
|---|---|
| `WayfarersEdge.asset` | Damage, attack interval, reach, arc, knockback |
| `CinderRegent.asset` | Health, phase threshold, area damage/radius/warning |
| `Ash.asset` | Health/damage/movement multipliers, burn, burst, trail toggles |
| `PrototypeCatalog.asset` | Corrupted count for four players |
| Room assets | Wave composition, spawn positions, room text |
| Item assets | Tags, modifiers, thresholds, prerequisite, status duration/stacks |
| `EntityFactory` / `ActorMotor` | Prototype enemy baselines, player speed, dash timing |

Keep baseline character stats comparable when adding characters. Identity should come from mechanics. Avoid permanent damage/health grinds that influence the final player encounter. No current automated result proves route pacing, economy, Mimic frequency, Challenge timing, or combat-space scale is balanced.
