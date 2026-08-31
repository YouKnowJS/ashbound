using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class PlayerTelemetry
    {
        public string playerId;
        public string weaponFamily;
        public string weaponRarity, weaponElement, weaponSkillId;
        public List<string> equippedArmorIds = new List<string>();
        public List<string> activeSetBonuses = new List<string>();
        public List<string> itemsSelected = new List<string>();
        public List<string> upgradesSelected = new List<string>();
        public List<string> dominantBuildTags = new List<string>();
        public float damageDealt, damageTaken, bossDamage;
        public float criticalDamage, statusDamage;
        public float areaDamage, healingRecovery, crowdControlSeconds;
        public List<string> relicTagCounts = new List<string>();
        public List<string> damageBySource = new List<string>();
        public List<string> damageByElement = new List<string>();
        public List<string> buildProcCounts = new List<string>();
        public bool corrupted;
    }
    [Serializable]
    public sealed class MatchRecord
    {
        public int schemaVersion = 2;
        public string runId, startedUtc, endedUtc, corruptionType, winner, outcome;
        public int seed;
        public float runDuration, finalPvPDuration;
        public float miniBossKillTime, finalBossKillTime;
        public string finalResultType;
        public bool aborted, debugUsed;
        public List<string> winningPlayerIds = new List<string>();
        public List<PlayerTelemetry> players = new List<PlayerTelemetry>();
    }
    public sealed class MatchTelemetry
    {
        private readonly Dictionary<string, Dictionary<string, float>> sources = new Dictionary<string, Dictionary<string, float>>();
        private readonly Dictionary<string, Dictionary<string, float>> elements = new Dictionary<string, Dictionary<string, float>>();
        private readonly Dictionary<string, Dictionary<string, int>> procs = new Dictionary<string, Dictionary<string, int>>();
        public MatchRecord Record { get; private set; }
        public string LastPath { get; private set; }
        public string LastError { get; private set; }
        public bool Recording { get; private set; }
        public void Begin(int seed, IEnumerable<Combatant> players)
        {
            Record = new MatchRecord { runId = Guid.NewGuid().ToString("N"), startedUtc = DateTime.UtcNow.ToString("O"), seed = seed, debugUsed = Application.isBatchMode };
            foreach (var player in players)
            {
                Record.players.Add(new PlayerTelemetry { playerId = player.Id, weaponFamily = player.Weapon ? player.Weapon.family.ToString() : "None",
                    weaponRarity=player.Weapon?player.Weapon.rarity.ToString():"None",weaponElement=player.Weapon?player.Weapon.PrimaryElement.ToString():"None",weaponSkillId=player.Weapon&&player.Weapon.skill?player.Weapon.skill.id:"" });
                sources[player.Id] = new Dictionary<string, float>(); elements[player.Id] = new Dictionary<string, float>(); procs[player.Id] = new Dictionary<string, int>();
            }
            LastPath = LastError = null; Recording = true;
        }
        public void Tick(float delta, RunState state)
        {
            if (!Recording) return;
            Record.runDuration += delta;
            if (state == RunState.FinalPvP) Record.finalPvPDuration += delta;
        }
        public void Damage(DamageEvent damage)
        {
            if (!Recording) return;
            var attacker = Record.players.Find(x => x.playerId == damage.Info.Source.Id);
            var target = Record.players.Find(x => x.playerId == damage.Target.Id);
            if (attacker != null)
            {
                attacker.damageDealt += damage.Loss.Total; if (damage.Target.IsBoss) attacker.bossDamage += damage.Loss.Total;
                if (damage.Critical) attacker.criticalDamage += damage.Loss.Total;
                if (damage.Info.Kind == DamageKind.Bleed || damage.Info.Kind == DamageKind.Burning || damage.Info.Kind == DamageKind.Poison) attacker.statusDamage += damage.Loss.Total;
                if(damage.Info.Impact>=ImpactTier.Ability||damage.Info.Kind==DamageKind.Rupture||damage.Info.Kind==DamageKind.Shockwave)attacker.areaDamage+=damage.Loss.Total;
                Add(sources[attacker.playerId], damage.Info.Kind.ToString(), damage.Loss.Total); Add(elements[attacker.playerId], damage.Info.Element.ToString(), damage.Loss.Total);
            }
            if (target != null) target.damageTaken += damage.Loss.Total;
        }
        public void Proc(Combatant actor, string id)
        {
            if (!Recording || !procs.TryGetValue(actor.Id, out var values)) return;
            values[id] = values.TryGetValue(id, out int count) ? count + 1 : 1;
        }
        public void Recovery(Combatant actor,float amount){if(!Recording)return;var record=Record.players.Find(x=>x.playerId==actor.Id);if(record!=null)record.healingRecovery+=Mathf.Max(0,amount);}
        public void Control(Combatant actor,float seconds){if(!Recording)return;var record=Record.players.Find(x=>x.playerId==actor.Id);if(record!=null)record.crowdControlSeconds+=Mathf.Max(0,seconds);}
        public void MiniBossKilled() { if (Recording && Record.miniBossKillTime <= 0) Record.miniBossKillTime = Record.runDuration; }
        public void FinalBossKilled() { if (Recording && Record.finalBossKillTime <= 0) Record.finalBossKillTime = Record.runDuration; }
        public void Item(Combatant player, ItemDefinition item, bool upgrade)
        {
            if (!Recording) return;
            var record = Record.players.Find(x => x.playerId == player.Id);
            if (record == null) return;
            if (upgrade) record.upgradesSelected.Add(item.id); else record.itemsSelected.Add(item.id);
        }
        public void Finish(IEnumerable<Combatant> players, string winner, string outcome, bool aborted = false)
        {
            if (!Recording) return;
            Recording = false; Record.winner = winner; Record.outcome = outcome; Record.aborted = aborted; Record.endedUtc = DateTime.UtcNow.ToString("O");
            foreach (var actor in players)
            {
                var record = Record.players.Find(x => x.playerId == actor.Id);
                if (record == null) continue;
                record.dominantBuildTags = actor.DominantBuildTags().Select(x => x.ToString()).ToList();
                record.weaponFamily = actor.Weapon ? actor.Weapon.family.ToString() : "None";
                record.weaponRarity=actor.Weapon?actor.Weapon.rarity.ToString():"None";record.weaponElement=actor.Weapon?actor.Weapon.PrimaryElement.ToString():"None";record.weaponSkillId=actor.Weapon&&actor.Weapon.skill?actor.Weapon.skill.id:"";
                record.equippedArmorIds=actor.Equipment.Equipped.Values.Where(x=>x).Select(x=>x.id).ToList();record.activeSetBonuses=actor.Equipment.ActiveBonuses().Select(x=>x.Key).ToList();
                record.relicTagCounts = BuildAnalyzer.CountTags(actor.Inventory.Items.Select(x => x.tags)).Select(x => x.Tag + ":" + x.Count).ToList();
                record.damageBySource = sources.TryGetValue(actor.Id, out var bySource) ? bySource.Select(x => x.Key + ":" + x.Value.ToString("0.##")).ToList() : new List<string>();
                record.damageByElement = elements.TryGetValue(actor.Id, out var byElement) ? byElement.Select(x => x.Key + ":" + x.Value.ToString("0.##")).ToList() : new List<string>();
                record.buildProcCounts = procs.TryGetValue(actor.Id, out var byProc) ? byProc.Select(x => x.Key + ":" + x.Value).ToList() : new List<string>();
                record.corrupted = actor.Corruption;
                if (actor.Faction.ToString() == winner) Record.winningPlayerIds.Add(actor.Id);
            }
            Record.finalResultType = players.Count() == 1 ? (winner == "Wanderers" ? "SoloReflectionDefeated" : "SoloReflectionWon") :
                (winner == "Corrupted" ? "CorruptedWon" : winner == "Wanderers" ? "UnboundWon" : "Draw");
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "Telemetry");
                Directory.CreateDirectory(directory);
                string destination = Path.Combine(directory, "run-" + Record.runId + ".json");
                string temporary = destination + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Record, true));
                File.Move(temporary, destination); LastPath = destination;
                Debug.Log("Match telemetry: " + destination);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { LastError = exception.Message; Debug.LogWarning("Unable to save match telemetry: " + LastError); }
        }
        private static void Add(Dictionary<string, float> values, string key, float amount) => values[key] = values.TryGetValue(key, out float current) ? current + amount : amount;
    }
}
