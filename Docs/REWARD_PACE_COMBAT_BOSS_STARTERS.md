# Reward pace, combat density, Boss identity, and starter weapons

## Guaranteed combat rewards

Every completed Normal, Hard, Elite, Challenge, and regional Boss combat enters the same per-player sequence:

1. Relic draft.
2. Equipment draft.
3. Return to Exploration after every player resolves both drafts.

Normal rooms use the early rarity profile. Hard rooms increase Rare weighting. Elite rooms use a stronger profile and add Ember Shards plus Ancient Alloy. Regional and final Bosses grant a Relic draft, premium Equipment draft, and major resources. The final Cinder Regent rewards resolve in `BossDefeated` before corruption begins.

## Data-driven rarity

`ProgressionTuningDefinition.rarity` stores Early, Mid, Late, and Boss weights for Common through Legendary. `RewardRarityPolicy` combines those weights with region index, completed depth, node risk, Elite/Boss status, permanent Rare-weight progression, and the selected preparation.

Legendary weight is always zero until Legendary content is unlocked. Unlocking it only enables a small weighted chance; no route state guarantees Legendary.

## Threat-budget density

`ThreatBudget` assigns costs to enemy roles, elite status, and elemental variants, then expands an authored encounter toward a target budget. The target rises with depth, risk, Elite/Challenge/Boss status, and each additional local player. Extra units cycle through the authored role/element groups and arrive as telegraphed reinforcements. The cap remains data-driven to prevent runaway crowd counts.

## Cinder Regent identity

The Cinder Regent uses its own controller and five named attacks:

- Wide Sweep
- Ground Eruption
- Charge Rush
- Expanding Flame Ring
- Summon Hazards

Attack selection considers distance, avoids immediate repeats, and uses arena-safe points. Phase 2 shortens recovery, widens the sweep, adds eruption branches, expands the ring sequence, and unlocks persistent hazards. Each attack and the phase transition have separate VFX and audio assignment hooks in `BossDefinition`.

## Common starter weapons

At the Camp Expedition Table, select a party member and one of eight families: Sword, Spear, Greatsword, Katana, Dual Blades, Bow, Staff, or Spellblade. Each player stores an independent choice. All starter definitions are Common, physical/neutral, and have no Weapon Skill. The selected weapon is equipped at expedition creation and remains until an Equipment reward replaces it.
