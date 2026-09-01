using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ashbound.Tests
{
    public sealed class PrototypeFlowTests
    {
        private PrototypeBootstrap bootstrap;
        private RunManager run;
        private readonly List<Gamepad> virtualPads = new List<Gamepad>();
        private InputTestFixture inputFixture;
        [UnitySetUp]
        public IEnumerator SpawnPrototype()
        {
            Time.timeScale = 1;
            inputFixture = new InputTestFixture(); inputFixture.Setup();
            bootstrap = new GameObject("Test bootstrap").AddComponent<PrototypeBootstrap>();
            run = bootstrap.Run;
            yield return null;
            Assert.That(run, Is.Not.Null);
        }
        [UnityTearDown]
        public IEnumerator DestroyPrototype()
        {
            if (run) run.ResetRun();
            if (bootstrap) Object.Destroy(bootstrap.gameObject);
            foreach (var pad in virtualPads) InputSystem.RemoveDevice(pad);
            virtualPads.Clear();
            foreach (var vfx in Object.FindObjectsByType<CombatVfx>()) Object.Destroy(vfx.gameObject);
            foreach (var lore in Object.FindObjectsByType<LoreFragment>()) Object.Destroy(lore.gameObject);
            Time.timeScale = 1;
            EnemyBrain.AiEnabled=true;EnemyBrain.TelegraphsEnabled=true;
            yield return null;
            inputFixture.TearDown();
        }
        private IEnumerator State(RunState state, float timeout = 8)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (run.Flow.State != state && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(run.Flow.State, Is.EqualTo(state));
        }
        private void KillHostiles()
        {
            foreach (var actor in run.Combat.Actors.Where(x => x.Faction == Faction.Hostiles && x.Alive).ToArray())
            {
                actor.SetTargetable(true);
                run.Combat.DealDamage(actor, new DamageInfo(run.Players[0], 10000, DamageKind.Weapon));
            }
        }

        [UnityTest]
        public IEnumerator FullSoloRunTraversesBranchingRegionRegionalBossFinalBossAndReflection()
        {
            Assert.That(run.StartRun(123), Is.True);
            Assert.That(run.Players.Count, Is.EqualTo(1));
            run.Players[0].Health.DebugInvulnerable = true;
            Assert.That(run.TryBeginCorruption(), Is.False);
            yield return State(RunState.Combat);KillHostiles();yield return State(RunState.Exploration);Assert.That(run.Draft.Active||run.EquipmentRewards.Active,Is.False,"normal combat has no automatic dual draft");
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);Assert.That(run.RouteSelectionOpen,Is.True);var hard=run.Route.Available.Single(x=>x.Definition.nodeType==ExpeditionNodeType.HardCombat);Assert.That(run.CastRouteVote("P1",hard.Definition.id),Is.True);
            yield return State(RunState.Combat);KillHostiles();yield return State(RunState.Reward);Assert.That(run.EquipmentRewards.Active,Is.True);Assert.That(run.Draft.Active,Is.False);Assert.That(run.EquipmentRewards.Leave(),Is.True);yield return State(RunState.Exploration);
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);var relic=run.Route.Available.Single(x=>x.Definition.nodeType==ExpeditionNodeType.Relic);run.CastRouteVote("P1",relic.Definition.id);yield return State(RunState.Reward);Assert.That(run.Draft.Choose(0),Is.True);yield return State(RunState.Exploration);
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);var rest=run.Route.Available.Single(x=>x.Definition.nodeType==ExpeditionNodeType.Rest);run.CastRouteVote("P1",rest.Definition.id);yield return new WaitUntil(()=>run.Rest!=null);Assert.That(run.ChooseRest(RestNodeChoice.Rest),Is.True);yield return State(RunState.Exploration);
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);run.CastRouteVote("P1",run.Route.Available.Single().Definition.id);yield return State(RunState.Combat);Assert.That(run.ChallengeActive,Is.True);KillHostiles();yield return State(RunState.Exploration);
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);run.CastRouteVote("P1",run.Route.Available.Single().Definition.id);yield return State(RunState.Combat);Assert.That(run.CurrentNode.Definition.nodeType,Is.EqualTo(ExpeditionNodeType.Boss));KillHostiles();yield return State(RunState.Reward);Assert.That(run.EquipmentRewards.Leave(),Is.True);yield return State(RunState.Exploration);Assert.That(run.RegionCompleteAwaitingFinalGate,Is.True);
            run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);
            yield return State(RunState.BossFight);
            Assert.That(run.Players[0].Inventory.Items.Count, Is.EqualTo(1));
            Assert.That(run.Corruption.Reflection, Is.Null);
            KillHostiles(); yield return State(RunState.BossDefeated);
            Assert.That(run.Combat.PvPEnabled, Is.False);
            yield return State(RunState.FinalPvP);
            var reflection = run.Corruption.Reflection;
            Assert.That(reflection, Is.Not.Null); Assert.That(reflection.IsPlayer, Is.False);
            Assert.That(reflection.Weapon, Is.EqualTo(run.Players[0].Weapon));
            Assert.That(reflection.Inventory.Items.Count, Is.LessThanOrEqualTo(4));
            Assert.That(reflection.Corruption, Is.EqualTo(run.Catalog.boss.corruption));
            reflection.GetComponent<ReflectionController>().enabled = false;
            yield return new WaitForSeconds(.3f);
            Assert.That(run.Combat.DealDamage(reflection, new DamageInfo(run.Players[0], 10000, DamageKind.Weapon)), Is.True);
            yield return State(RunState.RunComplete);
            Assert.That(run.Telemetry.Record.winner, Is.EqualTo("Wanderers"));
            Assert.That(run.Telemetry.Record.players[0].bossDamage, Is.GreaterThan(0));
            Assert.That(run.Telemetry.Record.players[0].upgradesSelected.Count, Is.EqualTo(1));
            Assert.That(run.Telemetry.Record.routeNodes.Count,Is.EqualTo(6));Assert.That(run.Telemetry.Record.trueFinalBossEntered,Is.True);
            Assert.That(System.IO.File.Exists(run.Telemetry.LastPath), Is.True);
            var saved = JsonUtility.FromJson<MatchRecord>(System.IO.File.ReadAllText(run.Telemetry.LastPath));
            Assert.That(saved.corruptionType, Is.EqualTo("ash"));
        }

        [UnityTest]
        public IEnumerator LocalCoopDamageChangesOnlyAfterBossDeathAndForcedSelectionWorks()
        {
            Assert.That(run.Lobby.TryJoin(InputKind.SecondKeyboard, -2, "Test P2"), Is.True);
            run.StartRun(42); run.DebugSkipToBoss(); yield return null;
            var first = run.Players[0]; var second = run.Players[1];
            Assert.That(run.Combat.DealDamage(second, new DamageInfo(first, 20, DamageKind.Weapon)), Is.False);
            Assert.That(run.TryBeginCorruption(), Is.False);
            run.Corruption.ForcedPlayerIds.Add(second.Id);
            run.Rooms.Boss.Health.DebugKill();
            yield return State(RunState.FinalPvP);
            Assert.That(second.Corruption, Is.Not.Null); Assert.That(first.Corruption, Is.Null);
            Assert.That(run.Corruption.Reflection, Is.Null);
            Assert.That(run.Lobby.TryJoin(InputKind.Gamepad, 90, "Late"), Is.False);
            yield return new WaitForSeconds(1.3f);
            Assert.That(run.Combat.DealDamage(second, new DamageInfo(first, 10, DamageKind.Weapon)), Is.True);
            Assert.That(run.Combat.DealDamage(first, new DamageInfo(second, 10, DamageKind.Weapon)), Is.True);
            second.Health.DebugKill(); yield return State(RunState.RunComplete);
            Assert.That(run.Telemetry.Record.winner, Is.EqualTo("Wanderers"));
        }

        [UnityTest]
        public IEnumerator WeaponDashInvulnerabilityShieldAndDeathUseRealComponents()
        {
            run.StartRun(17); run.DebugSkipToBoss(); yield return null;
            var player = run.Players[0]; var boss = run.Rooms.Boss;
            boss.SetTargetable(true);
            player.Motor.Teleport(boss.transform.position-Vector3.forward*.4f);player.Motor.SetFacing(Vector3.forward);Physics.SyncTransforms();Assert.That(run.Combat.AreEnemies(player,boss),Is.True);
            float hp = boss.Health.CurrentHealth;
            Assert.That(player.Attacks.TryAttack(), Is.True);
            Assert.That(boss.Health.CurrentHealth, Is.LessThan(hp));
            Assert.That(player.Attacks.TryAttack(), Is.False);
            Assert.That(player.Attacks.TryAbility(), Is.True); Assert.That(player.Health.Pool.Shield, Is.GreaterThan(0));
            player.Motor.SetMove(Vector3.right); Vector3 before = player.transform.position;
            Assert.That(player.Motor.TryDash(), Is.True);
            Assert.That(run.Combat.DealDamage(player, new DamageInfo(boss, 20, DamageKind.Weapon)), Is.False);
            yield return new WaitForSeconds(.19f);
            Assert.That(Vector3.Distance(player.transform.position, before), Is.GreaterThan(1));
            Assert.That(run.Combat.DealDamage(player, new DamageInfo(boss, 20, DamageKind.Weapon)), Is.True);
            player.Health.DebugKill(); yield return State(RunState.RunComplete);
            Assert.That(player.Attacks.TryAttack(), Is.False);
        }

        [UnityTest]
        public IEnumerator BleedRuptureAndLightningCannotRecursivelyProc()
        {
            run.StartRun(11); run.DebugSkipToBoss(); yield return null;
            var player = run.Players[0]; var boss = run.Rooms.Boss;
            foreach (string id in new[] { "thorn-rune", "rupture", "storm-coil", "forked-heart" }) Assert.That(player.Inventory.TryAdd(run.Catalog.FindItem(id)), Is.True);
            int hits = 0; run.Combat.DamageResolved += hit => hits++;
            float initial = boss.Health.CurrentHealth;
            for (int i = 0; i < 4; i++) run.Combat.DealDamage(boss, new DamageInfo(player, 1, DamageKind.Weapon, triggerEffects: true));
            Assert.That(boss.Statuses.StackCount(StatusKind.Bleed), Is.Zero);
            Assert.That(boss.Health.CurrentHealth, Is.LessThanOrEqualTo(initial - 60));
            Assert.That(hits, Is.EqualTo(6));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossPhaseAndTransitionCancelHazardsAndResetUnlocksRoster()
        {
            run.StartRun(19); run.DebugSkipToBoss(); yield return null;
            var boss = run.Rooms.Boss;
            run.Combat.DealDamage(boss, new DamageInfo(run.Players[0], boss.Health.MaxHealth * .65f, DamageKind.Weapon));
            yield return null;
            Assert.That(boss.GetComponent<CinderRegentController>().SecondPhase, Is.True);
            AreaAttack.Spawn(boss, Vector3.zero, 3, 10, 10);
            CombatProjectile.Spawn(boss, Vector3.back, 1, 1, Color.red);
            boss.Health.DebugKill(); yield return null;
            Assert.That(Object.FindObjectsByType<AreaAttack>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<CombatProjectile>(), Is.Empty);
            Assert.That(run.TryBeginCorruption(), Is.True);
            Assert.That(run.TryBeginCorruption(), Is.False);
            run.ResetRun(); yield return null;
            Assert.That(run.Flow.State, Is.EqualTo(RunState.Lobby)); Assert.That(run.Lobby.Locked, Is.False);
            Assert.That(run.Corruption.Reflection, Is.Null); Assert.That(run.Combat.PvPEnabled, Is.False);
            Assert.That(run.Combat.Actors.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FourPlayerLocalRunProtectsBothTeamsAndResolvesDraw()
        {
            for (int i = 0; i < 3; i++)
            {
                var pad = InputSystem.AddDevice<Gamepad>(); virtualPads.Add(pad);
                Assert.That(run.Lobby.TryJoin(InputKind.Gamepad, pad.deviceId, "Virtual pad " + i), Is.True);
            }
            run.StartRun(47); run.DebugSkipToBoss(); yield return null;
            Assert.That(run.Players.Count, Is.EqualTo(4)); Assert.That(run.Combat.Paused, Is.False);
            run.Corruption.FourPlayerCount = 2;
            run.Corruption.ForcedPlayerIds.AddRange(new[] { "P2", "P3" });
            run.Rooms.Boss.Health.DebugKill(); yield return State(RunState.FinalPvP);
            yield return new WaitForSeconds(1.3f);
            Assert.That(run.Corruption.CorruptedPlayerIds, Is.EquivalentTo(new[] { "P2", "P3" }));
            Assert.That(run.Combat.DealDamage(run.Players[2], new DamageInfo(run.Players[1], 10, DamageKind.Weapon)), Is.False);
            Assert.That(run.Combat.DealDamage(run.Players[3], new DamageInfo(run.Players[0], 10, DamageKind.Weapon)), Is.False);
            Assert.That(run.Combat.DealDamage(run.Players[1], new DamageInfo(run.Players[0], 10, DamageKind.Weapon)), Is.True);
            foreach (var player in run.Players) player.Health.DebugKill();
            yield return State(RunState.RunComplete);
            Assert.That(run.Telemetry.Record.winner, Is.EqualTo("Draw"));
        }

        [UnityTest]
        public IEnumerator GamepadAdapterReadsBoundDeviceAndDisconnectPausesRun()
        {
            var pad = InputSystem.AddDevice<Gamepad>(); virtualPads.Add(pad);
            var slot = new LobbySlot("P2", InputKind.Gamepad, pad.deviceId, "Virtual input");
            var input = new LocalPlayerInput(slot, null);
            inputFixture.Set(pad.leftStick, Vector2.right);
            inputFixture.Set(pad.rightStick, Vector2.up);
            inputFixture.Press(pad.rightShoulder);
            yield return null; // InputTestFixture queues events for the next player-loop update in UnityTest.
            var command = input.Read(Vector3.zero);
            Assert.That(command.Move.x, Is.GreaterThan(.9f), "movement");
            Assert.That(command.Aim.z, Is.GreaterThan(.9f), "aim");
            Assert.That(command.Attack, Is.True, "attack");
            run.Lobby.TryJoin(InputKind.Gamepad, pad.deviceId, "Virtual input"); run.StartRun(78); run.DebugSkipToBoss(); yield return null;
            InputSystem.RemoveDevice(pad); virtualPads.Remove(pad); yield return null;
            Assert.That(run.Combat.Paused, Is.True); Assert.That(run.MissingDevice, Is.EqualTo("P2"));
            Assert.That(input.Connected, Is.False);
        }

        [UnityTest]
        public IEnumerator StatusDamageRetainsSourceAndStopsWhilePaused()
        {
            run.StartRun(92); run.DebugSkipToBoss(); yield return null;
            var player = run.Players[0]; var boss = run.Rooms.Boss;
            boss.GetComponent<CinderRegentController>().enabled = false;
            player.Inventory.TryAdd(run.Catalog.FindItem("thorn-rune"));
            run.Combat.DealDamage(boss, new DamageInfo(player, 1, DamageKind.Weapon, triggerEffects: true));
            float afterHit = boss.Health.CurrentHealth;
            yield return new WaitForSeconds(1.1f);
            Assert.That(boss.Health.CurrentHealth, Is.EqualTo(afterHit - 3).Within(.01f));
            float beforePause = boss.Health.CurrentHealth;
            run.ManualPaused = true; yield return null;
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(boss.Health.CurrentHealth, Is.EqualTo(beforePause));
            run.ManualPaused = false; yield return null;
            player.Statuses.Apply(boss, new StatusPayload { kind = StatusKind.Slow, duration = 2, power = .5f, maxStacks = 1 });
            Assert.That(player.Statuses.MovementFactor, Is.EqualTo(.5f));
            player.Statuses.Apply(boss, new StatusPayload { kind = StatusKind.Stun, duration = .2f, maxStacks = 1 });
            Assert.That(player.Motor.IsStunned, Is.True);
            yield return new WaitForSeconds(.3f);
            Assert.That(player.Motor.IsStunned, Is.False);
            Assert.That(run.Telemetry.Record.players[0].damageDealt, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator ElementalWeaponSkillEquipmentAndReflectionRemainLayered()
        {
            run.StartRun(103);
            var player = run.Players[0];
            player.Attacks.SetWeapon(run.Catalog.FindWeapon("moonfrost"));
            foreach (var piece in run.Catalog.armor.Where(x => x.set.id == "winterglass"))
                Assert.That(player.Equipment.Equip(piece), Is.True);
            Assert.That(player.Equipment.ActiveBonuses().Select(x => x.Tier.pieces), Is.EqualTo(new[] { 2, 4 }));
            Assert.That(player.DominantBuildTags(), Does.Contain(BuildTag.Frost));

            run.DebugSkipToBoss(); yield return null;
            var boss = run.Rooms.Boss;
            boss.GetComponent<CinderRegentController>().enabled = false;
            player.Motor.Teleport(boss.transform.position - Vector3.forward);
            player.Motor.SetFacing(Vector3.forward);
            float before = boss.Health.CurrentHealth;
            Assert.That(player.Attacks.TryAbility(), Is.True);
            yield return new WaitForSeconds(.25f);
            Assert.That(boss.Health.CurrentHealth, Is.LessThan(before));
            Assert.That(boss.Statuses.StackCount(StatusKind.Chill), Is.GreaterThan(0));

            boss.Health.DebugKill(); yield return State(RunState.FinalPvP);
            var reflection = run.Corruption.Reflection;
            Assert.That(reflection, Is.Not.Null);
            Assert.That(reflection.Weapon.id, Is.EqualTo("moonfrost"));
            Assert.That(reflection.Weapon.skill.id, Is.EqualTo("moonfrost-draw"));
            Assert.That(reflection.DisplayName, Does.Contain("Frost"));
            Assert.That(reflection.Corruption, Is.EqualTo(run.Catalog.boss.corruption));
        }

        [UnityTest]
        public IEnumerator V06NodeIdentityControlsRewardCadence()
        {
            run.StartRun(204);yield return State(RunState.Combat);var player=run.Players[0];KillHostiles();yield return State(RunState.Exploration);
            Assert.That(run.Progression.RunResources.ash,Is.GreaterThan(0));Assert.That(run.Draft.Active||run.EquipmentRewards.Active,Is.False);run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(player);var hard=run.Route.Available.Single(x=>x.Definition.nodeType==ExpeditionNodeType.HardCombat);run.CastRouteVote(player.Id,hard.Definition.id);yield return State(RunState.Combat);KillHostiles();yield return State(RunState.Reward);
            Assert.That(run.Draft.Active,Is.False);Assert.That(run.EquipmentRewards.Active,Is.True);Assert.That(run.EquipmentRewards.CurrentPlayer,Is.EqualTo(player));var option=run.EquipmentRewards.Options[0];Assert.That(run.EquipmentRewards.Dismantle(0),Is.True);yield return State(RunState.Exploration);Assert.That(player.Equipment.Equipped.Count,Is.EqualTo(0));Assert.That(run.Progression.Profile.lifetime.equipmentDismantled,Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator V05EncounterSpawnsDataDrivenBrainsAndBuildsIrregularSpace()
        {
            EnemyBrain.AiEnabled=false;run.StartRun(305);yield return State(RunState.Combat);
            var hostiles=run.Combat.Actors.Where(x=>x.Faction==Faction.Hostiles).ToArray();Assert.That(hostiles,Is.Not.Empty);Assert.That(hostiles.All(x=>x.EnemyDefinition&&x.GetComponent<EnemyBrain>()),Is.True);
            Assert.That(run.Rooms.View.Definition,Is.Not.Null);Assert.That(run.Rooms.View.Definition.sections.Length,Is.GreaterThanOrEqualTo(3));Assert.That(GameObject.Find("Transition path"),Is.Not.Null);
            run.DebugSpawnEncounter(run.Catalog.encounters.First(x=>x.id=="collapse"));yield return null;hostiles=run.Combat.Actors.Where(x=>x.Faction==Faction.Hostiles).ToArray();Assert.That(hostiles.Select(x=>x.EnemyDefinition.role),Does.Contain(EnemyRole.Assassin));Assert.That(hostiles.Select(x=>x.EnemyDefinition.role),Does.Contain(EnemyRole.Bomber));
            EnemyBrain.AiEnabled=true;
        }

        [UnityTest]
        public IEnumerator V05EcologyTelemetryRecordsCompositionRoleKillsAndArenaContext()
        {
            EnemyBrain.AiEnabled=false;run.StartRun(306);yield return State(RunState.Combat);KillHostiles();yield return State(RunState.Exploration);
            Assert.That(run.Telemetry.Record.schemaVersion,Is.EqualTo(5));Assert.That(run.Telemetry.Record.encounters.Count,Is.EqualTo(1));var encounter=run.Telemetry.Record.encounters[0];Assert.That(encounter.encounterId,Is.EqualTo("frontline-pressure"));Assert.That(encounter.composition,Is.Not.Empty);Assert.That(encounter.arenaCategory,Is.EqualTo(CombatSpaceCategory.Small.ToString()));Assert.That(run.Telemetry.Record.routeNodes.Count,Is.EqualTo(1));Assert.That(run.Telemetry.Record.routeNodes[0].nodeType,Is.EqualTo(ExpeditionNodeType.NormalCombat.ToString()));
            run.Telemetry.Finish(run.Players,"Wanderers","Test");Assert.That(run.Telemetry.Record.enemyRoles.Sum(x=>x.kills),Is.GreaterThan(0));EnemyBrain.AiEnabled=true;
        }

        [UnityTest]
        public IEnumerator V06LocalRouteVotesWaitForEveryoneAndHostBreaksTie()
        {
            Assert.That(run.Lobby.TryJoin(InputKind.SecondKeyboard,-2,"Test P2"),Is.True);run.StartRun(606);yield return State(RunState.Combat);run.Players[0].Health.DebugInvulnerable=true;run.Players[1].Health.DebugInvulnerable=true;KillHostiles();yield return State(RunState.Exploration);run.Players[0].Motor.Teleport(run.Rooms.View.ExitPosition);run.Interact(run.Players[0]);Assert.That(run.RouteSelectionOpen,Is.True);var options=run.Route.Available.ToArray();Assert.That(options.Length,Is.EqualTo(2));Assert.That(run.CastRouteVote("P1",options[0].Definition.id),Is.True);Assert.That(run.RouteSelectionOpen,Is.True);Assert.That(run.CastRouteVote("P2",options[1].Definition.id),Is.True);Assert.That(run.RouteSelectionOpen,Is.False);Assert.That(run.Route.Current.Definition.id,Is.EqualTo(options[0].Definition.id));Assert.That(run.Telemetry.Record.routeNodes[0].routeVotes.Count,Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator V06CursedTreasureHealthCostMerchantAndMimicUseRuntimeServices()
        {
            EnemyBrain.AiEnabled=false;run.StartRun(609);yield return State(RunState.Combat);var player=run.Players[0];player.Health.DebugInvulnerable=true;run.DebugAddRunCurrency();Assert.That(run.DebugForceTreasure(TreasureVariantKind.CursedChest),Is.True);yield return new WaitUntil(()=>run.Treasure!=null);player.Health.DebugInvulnerable=false;player.Health.Pool.Damage(player.Health.MaxHealth-1);Assert.That(run.OpenTreasure(),Is.True);Assert.That(player.Health.CurrentHealth,Is.EqualTo(1));yield return State(RunState.Reward);run.EquipmentRewards.Leave();yield return State(RunState.Exploration);
            Assert.That(run.DebugForceNodeType(ExpeditionNodeType.Merchant),Is.True);yield return new WaitUntil(()=>run.Merchant!=null);int before=run.Progression.RunResources.ash;Assert.That(run.BuyMerchantOffer(0,0),Is.True);Assert.That(run.Progression.RunResources.ash,Is.LessThan(before));run.LeaveMerchant();yield return State(RunState.Exploration);
            Assert.That(run.DebugForceTreasure(TreasureVariantKind.Mimic),Is.True);yield return new WaitUntil(()=>run.Treasure!=null);Assert.That(run.OpenTreasure(),Is.True);yield return State(RunState.Combat);Assert.That(run.Combat.Actors.Any(x=>x.EnemyDefinition&&x.EnemyDefinition.id=="common-vault-mimic"),Is.True);KillHostiles();yield return State(RunState.Reward);Assert.That(run.EquipmentRewards.Active,Is.True);Assert.That(run.Telemetry.Record.routeNodes.Last().mimicDefeated,Is.True);EnemyBrain.AiEnabled=true;
        }

        [UnityTest]
        public IEnumerator V06NoHealingChallengeBlocksRecoveryOnlyDuringChallenge()
        {
            EnemyBrain.AiEnabled=false;run.StartRun(611);yield return State(RunState.Combat);Assert.That(run.DebugForceNodeType(ExpeditionNodeType.Challenge),Is.True);yield return State(RunState.Combat);Assert.That(run.CurrentNode.Definition.challenge.noHealing,Is.True);var player=run.Players[0];player.Health.Pool.Damage(20);float damaged=player.Health.CurrentHealth;player.Health.Heal(10);Assert.That(player.Health.CurrentHealth,Is.EqualTo(damaged));KillHostiles();yield return State(RunState.Exploration);player.Health.Heal(10);Assert.That(player.Health.CurrentHealth,Is.GreaterThan(damaged));EnemyBrain.AiEnabled=true;
        }
    }
}
