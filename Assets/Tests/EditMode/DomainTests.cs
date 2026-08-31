using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Ashbound.Tests
{
    public sealed class DomainTests
    {
        [TestCase(RunState.Lobby)]
        [TestCase(RunState.Exploration)]
        [TestCase(RunState.Combat)]
        [TestCase(RunState.Reward)]
        [TestCase(RunState.BossFight)]
        [TestCase(RunState.BossDefeated)]
        [TestCase(RunState.CorruptionTransition)]
        public void PlayerDamageIsNeverAllowedBeforeFinalPhase(RunState state)
        {
            Assert.That(CombatRules.CanDamage("P1", "P2", Faction.Wanderers, Faction.Wanderers, true, true, state, true), Is.False);
            Assert.That(CombatRules.CanDamage("P1", "P2", Faction.Corrupted, Faction.Wanderers, true, true, state, true), Is.False);
        }
        [Test]
        public void FinalDamageIsTeamBasedAndReflectionIsHostile()
        {
            Assert.That(CombatRules.CanDamage("P1", "P2", Faction.Corrupted, Faction.Wanderers, true, true, RunState.FinalPvP), Is.True);
            Assert.That(CombatRules.CanDamage("P2", "P1", Faction.Wanderers, Faction.Corrupted, true, true, RunState.FinalPvP), Is.True);
            Assert.That(CombatRules.CanDamage("P1", "R1", Faction.Wanderers, Faction.Corrupted, true, false, RunState.FinalPvP), Is.True);
            Assert.That(CombatRules.CanDamage("P1", "P2", Faction.Corrupted, Faction.Corrupted, true, true, RunState.FinalPvP), Is.False);
        }
        [Test]
        public void DamageRejectsSelfAndInactiveRun()
        {
            Assert.That(CombatRules.CanDamage("P1", "P1", Faction.Wanderers, Faction.Hostiles, true, false, RunState.Combat), Is.False);
            Assert.That(CombatRules.CanDamage("P1", "E1", Faction.Wanderers, Faction.Hostiles, true, false, RunState.RunComplete), Is.False);
            Assert.That(CombatRules.CanDamage("P1", "E1", Faction.Wanderers, Faction.Hostiles, true, false, RunState.Combat), Is.True);
        }
        [Test]
        public void CorruptionCannotSkipTheBossDeathGate()
        {
            var flow = new RunStateMachine();
            Assert.That(flow.TryAdvance(RunState.CorruptionTransition), Is.False);
            flow.TryAdvance(RunState.StartingRun); flow.TryAdvance(RunState.Exploration); flow.TryAdvance(RunState.BossFight);
            Assert.That(flow.TryAdvance(RunState.FinalPvP), Is.False);
            Assert.That(flow.TryAdvance(RunState.CorruptionTransition), Is.False);
            Assert.That(flow.TryAdvance(RunState.BossDefeated), Is.True);
            Assert.That(flow.BossWasDefeated, Is.True);
            Assert.That(flow.TryAdvance(RunState.CorruptionTransition), Is.True);
            Assert.That(flow.TryAdvance(RunState.FinalPvP), Is.True);
            Assert.That(flow.TryAdvance(RunState.CorruptionTransition), Is.False);
        }
        [Test]
        public void ResetClearsBossGateAndCompletionIsTerminal()
        {
            var flow = new RunStateMachine();
            flow.TryAdvance(RunState.StartingRun); flow.TryAdvance(RunState.Exploration); flow.DebugSkipToBoss();
            flow.TryAdvance(RunState.BossDefeated); flow.TryAdvance(RunState.RunComplete);
            Assert.That(flow.TryAdvance(RunState.CorruptionTransition), Is.False);
            Assert.That(flow.DebugSkipToBoss(), Is.False);
            flow.Reset(); Assert.That(flow.BossWasDefeated, Is.False); Assert.That(flow.State, Is.EqualTo(RunState.Lobby));
        }
        [TestCase(1, 2, 0)]
        [TestCase(2, 2, 1)]
        [TestCase(3, 2, 1)]
        [TestCase(4, 1, 1)]
        [TestCase(4, 2, 2)]
        public void CorrectNumberOfPlayersAreCorrupted(int players, int setting, int expected)
        {
            var ids = Enumerable.Range(1, players).Select(x => "P" + x).ToArray();
            for (int seed = 0; seed < 64; seed++)
            {
                var chosen = CorruptionSelector.Select(ids, setting, new System.Random(seed));
                Assert.That(chosen.Length, Is.EqualTo(expected)); Assert.That(chosen.Distinct().Count(), Is.EqualTo(expected));
                Assert.That(chosen.All(ids.Contains), Is.True);
            }
        }
        [Test]
        public void ForcedSelectionIsBoundedAndSeeded()
        {
            var ids = new[] { "P1", "P2", "P3", "P4" };
            var chosen = CorruptionSelector.Select(ids, 2, new System.Random(42), new[] { "bogus", "P3", "P3" });
            Assert.That(chosen, Does.Contain("P3")); Assert.That(chosen.Length, Is.EqualTo(2));
            Assert.That(chosen, Is.EqualTo(CorruptionSelector.Select(ids, 2, new System.Random(42), new[] { "bogus", "P3", "P3" })));
        }
        [Test]
        public void LobbyEnforcesDeviceUniquenessCapacityAndNoJoinInProgress()
        {
            var lobby = new LobbySession();
            Assert.That(lobby.TryJoin(InputKind.MouseKeyboard, -1, "Duplicate"), Is.False);
            Assert.That(lobby.TryJoin(InputKind.SecondKeyboard, -2, "P2 keyboard"), Is.True);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 10, "Pad 1"), Is.True);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 10, "Duplicate pad"), Is.False);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 11, "Pad 2"), Is.True);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 12, "Pad 3"), Is.False);
            lobby.Lock(); Assert.That(lobby.RemoveLast(), Is.False);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 13, "Late"), Is.False);
            lobby.Unlock(); Assert.That(lobby.RemoveLast(), Is.True);
            Assert.That(lobby.TryJoin(InputKind.Gamepad, 13, "Replacement"), Is.True);
            Assert.That(lobby.Slots.Select(x => x.PlayerId).Distinct().Count(), Is.EqualTo(4));
        }
        [Test]
        public void ShieldsInvulnerabilityDeathAndHealingAreBounded()
        {
            var hp = new HealthPool(100); hp.AddShield(100);
            Assert.That(hp.Shield, Is.EqualTo(50)); Assert.That(hp.Damage(30, true).Total, Is.Zero);
            var hit = hp.Damage(65); Assert.That(hit.Shield, Is.EqualTo(50)); Assert.That(hit.Health, Is.EqualTo(15));
            Assert.That(hp.Heal(100), Is.EqualTo(15));
            Assert.That(hp.Damage(float.NaN).Total, Is.Zero);
            Assert.That(hp.Damage(-1).Total, Is.Zero);
            Assert.That(hp.Damage(1000).Health, Is.EqualTo(100));
            Assert.That(hp.Alive, Is.False); Assert.That(hp.Heal(100), Is.Zero);
            hp.Reset(120); Assert.That(hp.Alive, Is.True); Assert.That(hp.Shield, Is.Zero);
        }
        [Test]
        public void BuildAnalysisCountsPerItemAndCreatesSimplifiedHybrid()
        {
            var tags = new[] { new[] { BuildTag.Critical, BuildTag.Critical }, new[] { BuildTag.Lightning, BuildTag.Critical }, new[] { BuildTag.Lightning } };
            var counts = BuildAnalyzer.CountTags(tags);
            Assert.That(counts[0].Count, Is.EqualTo(2));
            Assert.That(BuildAnalyzer.Dominant(tags), Is.EqualTo(new[] { BuildTag.Critical, BuildTag.Lightning }));
            Assert.That(BuildAnalyzer.ReflectionItems(BuildAnalyzer.Dominant(tags)), Is.EquivalentTo(new[] { "glass-sigil", "echo-edge", "storm-coil", "forked-heart" }));
            Assert.That(BuildAnalyzer.Dominant(new[] { new[] { BuildTag.Mobility } }), Is.Empty);
        }
        [Test]
        public void ExpandedBuildAnalysisIncludesEveryV02Theme()
        {
            var themes = new[]{BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void,
                BuildTag.Critical,BuildTag.Bleed,BuildTag.Heavy,BuildTag.Combo,BuildTag.DashPrecision};
            foreach(var theme in themes)
            {
                Assert.That(BuildAnalyzer.Dominant(new[]{new[]{theme}}),Is.EqualTo(new[]{theme}));
                Assert.That(BuildAnalyzer.ReflectionItems(new[]{theme}),Is.Not.Empty,theme.ToString());
            }
        }
        [Test]
        public void HealthModifiersPreserveHealthFractionAndDoNotRevive()
        {
            var hp = new HealthPool(100); hp.Damage(50); hp.Resize(200);
            Assert.That(hp.Current, Is.EqualTo(100)); Assert.That(hp.Maximum, Is.EqualTo(200));
            hp.Damage(100); hp.Resize(300); Assert.That(hp.Alive, Is.False);
        }
        [Test]
        public void AuthoredContentIsCompleteAndAllIdsAreUnique()
        {
            var catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            Assert.That(catalog, Is.Not.Null); Assert.That(catalog.items.Length, Is.InRange(35,45));
            Assert.That(catalog.items.Select(x => x.id).Distinct().Count(), Is.EqualTo(catalog.items.Length));
            Assert.That(catalog.rooms.Length, Is.EqualTo(7)); Assert.That(catalog.rooms.Last().isBoss, Is.True);
            Assert.That(catalog.boss.corruption, Is.Not.Null); Assert.That(catalog.weapon, Is.Not.Null);
            Assert.That(catalog.weapons.Select(x => x.family).Distinct().Count(), Is.EqualTo(8));
            Assert.That(catalog.rooms.SelectMany(x => x.waves).SelectMany(x => x.enemies), Does.Contain(EnemyKind.MiniBoss));
            foreach (var tag in new[]{BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void})
                Assert.That(catalog.items.Count(x=>x.tags.Contains(tag)),Is.GreaterThanOrEqualTo(6),tag.ToString());
        }

        [Test]
        public void V03ElementalWeaponsCoverIdentityRarityAndSkills()
        {
            var catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            var elemental = catalog.weapons.Where(x => x.PrimaryElement != ElementTag.None).ToArray();
            Assert.That(catalog.weapons.Length, Is.EqualTo(20));
            Assert.That(elemental.Length, Is.EqualTo(12));
            Assert.That(catalog.weapons.Select(x => x.rarity).Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(WeaponRarity)).Cast<WeaponRarity>()));
            Assert.That(elemental.Count(x => x.rarity == WeaponRarity.Legendary), Is.EqualTo(2));
            Assert.That(elemental.Where(x => x.rarity >= WeaponRarity.Rare).All(x => x.skill), Is.True);
            Assert.That(catalog.weapons.Where(x => x.rarity < WeaponRarity.Rare).All(x => !x.skill), Is.True);
            Assert.That(catalog.weaponSkills.Length, Is.EqualTo(12));
            Assert.That(catalog.weaponSkills.All(x => x.minimumRarity == WeaponRarity.Rare), Is.True);
            foreach (ElementTag element in Enum.GetValues(typeof(ElementTag)).Cast<ElementTag>().Where(x => x != ElementTag.None))
                Assert.That(elemental.Count(x => x.elements.Contains(element)), Is.GreaterThanOrEqualTo(2), element.ToString());
            Assert.That(elemental.Any(x => x.family == WeaponFamily.Staff && x.PrimaryElement == ElementTag.Fire), Is.True);
            Assert.That(elemental.Any(x => x.family == WeaponFamily.Staff && x.PrimaryElement == ElementTag.Void), Is.True);
        }

        [Test]
        public void V03ArmorCoversSlotsAndEvaluatesTwoAndFourPieceBonuses()
        {
            var catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            Assert.That(catalog.armorSets.Length, Is.EqualTo(5));
            Assert.That(catalog.armor.Length, Is.EqualTo(20));
            foreach (var set in catalog.armorSets)
            {
                var pieces = catalog.armor.Where(x => x.set == set).ToArray();
                Assert.That(pieces.Select(x => x.slot), Is.EquivalentTo(Enum.GetValues(typeof(ArmorSlot)).Cast<ArmorSlot>()), set.displayName);
                Assert.That(set.twoPiece.pieces, Is.EqualTo(2));
                Assert.That(set.fourPiece.pieces, Is.EqualTo(4));
            }

            var holder = new GameObject("equipment-test");
            try
            {
                var equipment = holder.AddComponent<PlayerEquipment>();
                var ashwalker = catalog.armor.Where(x => x.set.id == "ashwalker").ToArray();
                equipment.Equip(ashwalker[0]); equipment.Equip(ashwalker[1]);
                Assert.That(equipment.ActiveBonuses().Select(x => x.Tier.pieces), Is.EqualTo(new[] { 2 }));
                equipment.Equip(ashwalker[2]); equipment.Equip(ashwalker[3]);
                Assert.That(equipment.ActiveBonuses().Select(x => x.Tier.pieces), Is.EqualTo(new[] { 2, 4 }));
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }

        [Test]
        public void V03BuildAnalysisReadsEveryEquipmentLayer()
        {
            var dominant = BuildAnalyzer.Analyze(
                new[] { BuildTag.Fire, BuildTag.Critical },
                new[] { BuildTag.Fire, BuildTag.Heavy },
                new[] { BuildTag.Fire, BuildTag.Area },
                new[] { BuildTag.Fire, BuildTag.Sustain },
                new[] { BuildTag.Fire, BuildTag.DamageOverTime }, 10);
            Assert.That(dominant.First(), Is.EqualTo(BuildTag.Fire));
            Assert.That(dominant, Does.Contain(BuildTag.Critical));
            Assert.That(dominant, Does.Contain(BuildTag.Heavy));
        }
    }
}
