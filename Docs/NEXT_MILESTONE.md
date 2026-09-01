# Next milestone: v0.7 route validation and presentation

1. Human-playtest the complete v0.6 region across solo, two-player shared keyboard, and 3–4 physical gamepads. Compare at least three seeds and record route decisions, health entering nodes, wallet spend, skipped rewards, and menu time.
2. Tune only after evidence: route reveal, Normal/Hard/Elite resource spacing, Merchant prices, Treasure weights and health costs, Greedy decisions, Challenge timers, Rest recovery, and regional Boss reward.
3. Add controller-first focus/navigation for Route, Treasure, Merchant, Rest, Event, and equipment screens. Preserve locked device ownership and the all-player voting requirement.
4. Author a small set of non-combat camp, shrine, vault-pocket, and merchant-space presentation variants while keeping the v0.5 irregular-space and transition-path model.
5. Improve Challenge runtime rules for survival, no-healing, priority targets, and defend-point goals; retain nonfatal failure unless explicitly authored otherwise.
6. Use schema-5 telemetry to compare time in combat, route selection, rewards, and shops. Keep debug and normal runs separate.
7. Do not begin all five regions until one-region pacing and economy are understandable in repeated human sessions.

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
