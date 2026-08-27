Build a playable prototype for a multiplayer roguelike action game with the following design goals.

## Core Concept

The game is a 1–4 player top-down / 3/4 isometric action roguelike inspired by the camera and combat readability of games like Hades II and Shape of Dreams.

The core loop is:

1. Players enter a run together.
2. They fight through multiple maps cooperatively.
3. Each map lasts roughly 10–15 minutes.
4. Players collect weapons, equipment, upgrades, and build synergies.
5. Early and mid-run gameplay is fully cooperative.
6. No friendly fire during the normal run.
7. Players defeat a final boss at the end of the run.
8. Only after the final boss is defeated does the corruption phase begin.
9. In multiplayer, 1–2 players are randomly corrupted depending on player count.
10. Corrupted players receive boss-specific buffs or altered abilities.
11. PvP is enabled only during this final corruption phase.
12. The final fight is corrupted players versus non-corrupted players.
13. In solo mode, no AI teammates are used.
14. Instead, the player fights a corrupted reflection of their own build after defeating the final boss.

Do not reveal or heavily explain the final PvP twist through tutorial text. The final corruption phase should feel like something players discover naturally through gameplay and story progression.

---

# Technical Direction

Use a modular architecture so systems can be expanded later.

Preferred engine:

Unity 6 with C#.

Use placeholder assets and primitive models initially.

The goal of this prototype is gameplay architecture, not final visuals.

---

# Prototype Scope

Create only enough content to validate the game loop.

Prototype content:

* 1 playable character
* 1 basic weapon
* 3 normal enemy types
* 1 elite enemy
* 2 small combat rooms
* 1 final boss
* 5–8 upgrades/items
* 3 simple build archetypes
* solo mode
* local prototype support for multiplayer-ready architecture
* corruption phase
* corrupted reflection for solo mode
* basic room/lobby structure

Do NOT attempt to build the full game yet.

---

# Player Design

Do not use strict RPG classes such as Tank, Healer, DPS.

Characters should instead have different combat traits while remaining reasonably balanced in:

* survivability
* damage potential
* mobility
* ability to fight alone

Avoid extreme stat differences.

Example design philosophy:

Character identity should come from combat mechanics, not massive base-stat differences.

The final game may have multiple playable characters later, but the prototype only needs one.

---

# Core Player Controller

Implement:

* WASD movement
* mouse aiming
* basic attack
* dodge / dash
* one active ability
* health
* damage
* hit stun
* invulnerability frames during part of dodge
* knockback
* death state

The movement should feel fast and responsive.

The camera should be a fixed angled top-down camera similar to an isometric action roguelike.

---

# Combat System

Create a reusable combat system supporting:

* PvE damage
* PvP damage
* friendly fire toggle
* damage sources
* critical hits
* status effects
* damage-over-time
* crowd control
* shields
* healing
* damage modifiers

Friendly fire must be disabled during the cooperative portion of the run.

PvP damage must become enabled only during the corruption phase.

Use interfaces or modular components where possible.

Example:

IDamageable
DamageInfo
HealthComponent
StatusEffectController

---

# Roguelike Upgrade System

Create an upgrade system where players periodically choose 1 of 3 upgrades.

Upgrades should modify behavior, not only add flat numbers.

Prototype upgrade categories:

## Critical Build

Examples:

* increased critical chance
* critical hits trigger bonus damage
* critical hits reduce cooldown

## Bleed Build

Examples:

* attacks apply bleed
* hitting bleeding enemies deals bonus damage
* reaching X bleed stacks triggers an explosion

## Lightning Build

Examples:

* every X attacks triggers chain lightning
* chain lightning can bounce additional times
* critical hits count as multiple lightning charges

Builds should be able to overlap.

Avoid rigid set bonuses.

The player should feel like they are discovering combinations rather than completing predefined armor sets.

---

# Item System

Create a data-driven item system using ScriptableObjects.

Each item should contain:

* id
* display name
* description
* rarity
* tags
* stat modifiers
* optional triggered effects
* optional status effects

Suggested tags:

* Critical
* Bleed
* Lightning
* Fire
* Frost
* Mobility
* Shield
* Sustain
* Summon
* Curse

Items should support cross-build interactions.

---

# Room System

Use a room-based progression system.

Prototype structure:

Room 1
→ Room 2
→ Final Boss Room
→ Corruption Phase

Later this system should support multiple maps, each around 15 minutes long.

Rooms should support:

* enemy spawn points
* room completion conditions
* rewards
* doors / exits
* combat lock
* elite encounters

Do not create a giant open world.

---

# Run Structure

Create a RunManager with clear states:

Lobby
StartingRun
Exploration
Combat
Reward
BossFight
BossDefeated
CorruptionTransition
FinalPvP
RunComplete

The RunManager should control:

* friendly fire
* room progression
* boss state
* corruption activation
* end-of-run state

---

# Final Boss

Create one prototype final boss.

Boss should have:

* basic melee or ranged attack
* telegraphed area attack
* dash or movement ability
* second phase at low health
* clear attack indicators

The boss should be designed so its powers can later be reused during player corruption.

---

# Corruption System

Create a modular CorruptionSystem.

After the final boss dies:

1. pause combat briefly
2. play a short transition
3. select corrupted player(s)
4. apply boss-specific corruption modifiers
5. visually identify corrupted players
6. enable PvP
7. begin final fight

Do not trigger corruption before the final boss is defeated.

Suggested multiplayer rules:

2 players:

* 1 corrupted

3 players:

* 1 corrupted

4 players:

* 1 or 2 corrupted

Make this configurable.

---

# Boss-Specific Corruption

Create corruption as a reusable boss-defined data object.

Example:

BossCorruptionProfile

Fields:

* health multiplier
* damage multiplier
* movement modifier
* ability overrides
* passive effects
* VFX reference
* corruption tags

For the prototype boss, create one corruption profile.

Example:

Ash Corruption

Effects:

* increased movement speed
* attacks apply burning
* dash leaves a burning trail
* gains a short-range fire burst ability

Do not hardcode all corruption logic into the player class.

---

# Solo Mode

Solo mode must NOT use AI companions.

After the player defeats the final boss:

Create a Corrupted Reflection.

The reflection should:

* copy the player's core weapon
* copy important build tags
* inherit a simplified version of the player's upgrades
* receive the final boss corruption profile
* behave as an enemy

Do not attempt to copy every item exactly.

Instead, analyze the player's build and generate a readable combat archetype.

Example:

If the player's strongest tags are:

Critical + Lightning

Then the reflection should use:

* increased attack frequency
* critical-based burst
* lightning chain effects
* boss corruption abilities

Create a BuildAnalyzer system that counts tags and identifies the dominant build archetypes.

---

# Multiplayer Architecture

Do NOT build full online multiplayer networking yet unless necessary.

However, structure code so it can later support networked multiplayer.

Avoid writing systems that assume only one player exists.

Examples:

* RunManager should maintain a list of players.
* Health and damage should identify attacker and target.
* Items should belong to a player entity.
* corruption should target player IDs, not global singleton references.

The final game should eventually support:

* private lobbies
* public lobbies
* quick matchmaking
* 2–4 players
* no new players joining after a run begins
* reconnecting existing players should be considered separately later

Do not implement join-in-progress.

---

# Progression

Do not create heavy permanent stat progression.

Long-term progression should eventually focus on:

* unlocking weapons
* unlocking new upgrades
* unlocking new items
* unlocking maps
* unlocking bosses
* cosmetics
* additional build options

Avoid permanent +50% damage systems that would make multiplayer PvP unfair.

For the prototype, only create a simple unlock data structure.

---

# Story Architecture

Do not write a full story yet.

Create lightweight support for environmental storytelling:

* lore entries
* map descriptions
* boss descriptions
* collectible text fragments

The final PvP/corruption mechanic should not be explicitly spoiled through early tutorial text.

The game should allow players to gradually discover that defeating corruption does not necessarily destroy it.

---

# Visual Direction

Use placeholder visuals but structure the presentation around:

* top-down angled camera
* dark fantasy
* stylized environments
* strong character silhouettes
* clear VFX readability
* corruption visually distinct from normal combat

Important gameplay readability rules:

* player attacks must be easy to distinguish
* boss telegraphs must be clear
* corrupted players must be immediately recognizable
* avoid excessive screen-filling VFX

---

# Audio Hooks

Create events/hooks for:

* exploration music
* combat music
* boss music
* boss death
* corruption transition
* PvP music

Do not implement a full soundtrack.

The boss death → corruption transition should support:

* music fade out
* brief silence
* corruption sound cue
* new final-phase music

---

# Debug Tools

Create a simple debug menu allowing:

* jump directly to final boss
* kill boss instantly
* trigger corruption
* force specific player corruption
* spawn items
* add upgrades
* reset run

This is important for rapid balancing and testing.

---

# Data Logging

Create lightweight match telemetry.

Track:

* run duration
* items selected
* upgrades selected
* dominant build tags
* damage dealt
* damage taken
* boss damage
* corruption type
* final PvP duration
* winner

Output locally as JSON.

This will later be used for balance analysis.

---

# Folder Structure

Use a clean project structure similar to:

Assets/
Scripts/
Core/
Player/
Combat/
Enemies/
Bosses/
Roguelike/
Items/
Rooms/
Run/
Corruption/
Solo/
Multiplayer/
UI/
Audio/
Data/
Debug/

ScriptableObjects/
Items/
Bosses/
Corruption/
Upgrades/

Prefabs/
Player/
Enemies/
Bosses/
Rooms/
Items/

Scenes/
MainMenu
PrototypeRun
TestArena

---

# Coding Standards

Use:

* clear class responsibilities
* ScriptableObjects for data
* events instead of excessive direct dependencies
* interfaces for reusable gameplay interactions
* comments only where architecture is non-obvious
* no giant GameManager containing every system
* avoid unnecessary overengineering

The code should be readable enough that a junior software engineer can study and explain the architecture.

---

# First Milestone

Build the following first:

1. player movement
2. basic attack
3. dash
4. enemy
5. health/damage system
6. one combat room
7. upgrade selection
8. final boss
9. boss death event
10. corruption transition
11. solo corrupted reflection
12. PvP toggle architecture
13. basic debug tools

The result should be a playable gray-box prototype.

Do not spend time on polished graphics yet.

At the end, provide:

* architecture summary
* scene setup instructions
* important scripts and what each does
* how to run the prototype
* known limitations
* recommended next milestone
