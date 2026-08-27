# Next milestone: validate fairness before adding content

1. Play the existing loop repeatedly in solo, two-player shared keyboard, and 3–4 player gamepad sessions. Capture normal and debug telemetry separately.
2. Tune player response, enemy telegraph timing, boss downtime, reflection speed, final-phase health, and four-player corruption counts. Track which builds win before adding another character.
3. Test that the reveal is understandable through silhouettes, attack colors, and combat events. Avoid an early tutorial explaining the twist.
4. Add a minimal host-authoritative LAN transport behind `IPlayerInput` and the central mutation points. Validate commands against stable authenticated player IDs; replicate run state, health, item selections, and corruption assignments. Keep late join disabled. Plan reconnect separately.
5. Add controller-only lobby navigation and bindings/remapping, then automated virtual-device input tests and physical disconnect/reconnect tests.
6. Only after loop/balance validation, replace factories with authored prefabs and animation, add pooling where profiling shows allocation pressure, and expand toward 10–15 minute maps.

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

Keep baseline character stats comparable when adding characters. Identity should come from mechanics. Avoid permanent damage/health grinds that influence the final player encounter.
