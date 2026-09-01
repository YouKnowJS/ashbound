# Ashbound presentation foundation

This milestone turns the generated MainMenu runtime into a walkable expedition camp while preserving the existing local profile, route, combat, final encounter, and telemetry architecture. All visuals remain replaceable prototype presentation.

## 1. Commit

The exact delivered commit and remote branch are reported with the release verification. Use `git show --stat` to inspect the complete change.

## 2. Build and test result

Unity 6000.4.11f1 content generation, Edit Mode, Play Mode, Windows development build, and executable smoke launch are the release gates. Current automated coverage is recorded in [VERIFICATION.md](VERIFICATION.md).

## 3. Camp scene changes

`CampHub` builds a runtime camp beneath the existing `PrototypeBootstrap`: irregular ground and paths, central animated fire, smoke and ember particles, tents, crates, boundary stones, station props, warm practical lights, cool fill light, and distant ruin silhouettes. The combat room root is hidden only while the run state is Lobby and is restored synchronously before an expedition loads.

## 4. NPC and station mapping

| NPC | Station | Existing system |
|---|---|---|
| Mara, Expedition Leader / Cartographer | Expedition Table | preparation, local roster, launch, records |
| Bran, Forge Master | Forge | weapon, skill, element, rarity, and armor research |
| Evie, Quartermaster | Quartermaster supplies | merchant, salvage, and economy upgrades |
| Saren, Field Medic | Infirmary | recovery, retention, emergency, and health upgrades |
| Veyl, Researcher | Research Station | relics, appraisal, element bias, and route knowledge |
| Oren, Archivist | Archive | expedition statistics, recovered lore, and Boss records |

## 5. Interaction system

Player one walks with WASD or the current gamepad left stick. A station within 3.2 world units shows a localized name, title, and `F / A` prompt. Interaction opens only that station's panel, shifts the damped camera partway toward its NPC, and triggers a short NPC gesture. Escape closes the panel or opens camp settings.

## 6. Resource HUD

The persistent wallet is fixed at the upper right during camp movement and station panels. Each resource renders as icon plus number. Hovering an icon shows a localized name, description, and primary use. `MetaProgressionService.ProfileChanged` drives a restrained pulse and gain line, so presentation never owns a second balance.

## 7. Resource icons

`ResourceIconLibrary` generates four 32-pixel replaceable textures: an ash pile, orange shard, silver ingot, and fractured violet sigil. Costs reuse the same textures. The runtime library centralizes creation and cleanup so authored sprites can replace it without changing station UI.

## 8. Upgrade UI

The common station panel displays facility level, next tier, localized effect, icon costs, named prerequisite, and one of four clear states: available, insufficient resources, prerequisite locked, or complete. Buttons call `MetaProgressionService.TryUpgrade`; preparations call `SelectPreparation`; expedition launch calls `RunManager.StartRun`.

## 9. Localization architecture

`LocalizationService` owns keys, English fallbacks, the Simplified Chinese table, enum terminology helpers, facility/tier helpers, wallet formatting, persistence through PlayerPrefs, and a change event. Camp, settings, NPCs, facilities, tiers, resources, preparations, main combat status, route types, weapon families, elements, rarities, and armor terminology use this boundary. `PrototypeGui` selects Microsoft YaHei or another installed CJK fallback before Arial and rebuilds styles after a language change.

## 10. English to Chinese terminology

| English | 简体中文 | English | 简体中文 |
|---|---|---|---|
| Ash | 灰烬 | Ember Shards | 余烬碎片 |
| Ancient Alloy | 古代合金 | Corruption Fragment | 腐化碎片 |
| Forge | 锻造 | Research Station | 研究站 |
| Quartermaster | 军需官 | Infirmary | 医疗帐篷 |
| Archive | 档案馆 | Expedition | 远征 |
| Relic | 遗物 | Elite | 精英 |
| Treasure | 宝藏 | Temper | 淬炼 |
| Void | 虚空 | Fire | 火焰 |
| Frost | 寒霜 | Lightning | 雷电 |
| Poison | 剧毒 | Legendary | 传说 |

## 11. Player and NPC animation foundation

`ActorView` adds procedural idle/run bob, dash lean, attack swing, weapon-skill pulse, hit recoil, and retains the existing death pose. Enemy brains and the Cinder Regent trigger the shared attack presentation; existing movement, hit flash, telegraphs, and death events supply the other role reads. Camp placeholders breathe, highlight, turn toward the player, and gesture on interaction.

## 12. Lighting and environment

The camp combines a flickering warm campfire, forge glow, cool moon fill, flat dark ambient light, smoke, embers, equipment props, tents, paths, rocks, and distant silhouettes. The 3/4 orthographic camera follows the camp avatar with the existing smooth damping and uses bounded interaction focus instead of cuts.

## 13. Files added

- `Assets/Scripts/Camp/CampHub.cs`
- `Assets/Scripts/Data/LocalizationService.cs`
- `Assets/Scripts/UI/ResourceIconLibrary.cs`
- `Docs/PRESENTATION_FOUNDATION.md`

Unity-generated `.meta` files accompany new asset paths.

## 14. Files modified

Composition, camera, audio hooks, progression debug controls, old lobby suppression, procedural actor presentation, debug menu, localization-aware HUD text, tests, and developer documentation were updated. See the delivered commit for the exact file list.

## 15. Known limitations

The camp uses primitive graybox geometry, procedural icons, IMGUI panels, generated particle effects, placeholder NPCs, and optional empty audio clip slots. There is no final modeling, rigged animation, soundtrack, dialogue tree, navigation mesh, menu blur, controller-only focus system, online networking, or claim of final art quality. Authored proper nouns and longer narrative content remain source-language data unless a localized content asset is supplied.

## 16. Human playtest checklist

1. Launch in English and Chinese; check glyphs, wrapping, tooltips, and panel clipping at common resolutions.
2. Walk to every NPC with keyboard and a physical gamepad; confirm prompt radius, facing, gesture, highlight, and camera damping.
3. Upgrade one affordable, one unaffordable, one prerequisite-locked, and one maximum-level facility; verify wallet and save persistence.
4. Change all four resources and check icon identity, fixed HUD visibility, costs, pulse, and gain feedback.
5. Configure a 1–4 player roster and launch only through the Expedition Table; complete/reset a run and confirm the camp returns cleanly.
6. Open settings repeatedly, switch languages, restart the executable, and verify selection persistence.
7. Use F1 Camp tools, TestArena, combat, Boss, corruption, and Reflection flows; check for presentation regressions and mark subjective issues separately from debug runs.

## 17. Recommended next milestone

Run the checklist with physical controllers and representative displays, fix readability and collision findings, then replace one complete camp slice with authored modular art and rigged characters through the existing presentation seams. Add controller-first menu focus and localized content assets before expanding regions or attempting a broad final-art pass.
