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
        public List<string> itemsSelected = new List<string>();
        public List<string> upgradesSelected = new List<string>();
        public List<string> dominantBuildTags = new List<string>();
        public float damageDealt, damageTaken, bossDamage;
        public bool corrupted;
    }
    [Serializable]
    public sealed class MatchRecord
    {
        public int schemaVersion = 1;
        public string runId, startedUtc, endedUtc, corruptionType, winner, outcome;
        public int seed;
        public float runDuration, finalPvPDuration;
        public bool aborted, debugUsed;
        public List<string> winningPlayerIds = new List<string>();
        public List<PlayerTelemetry> players = new List<PlayerTelemetry>();
    }
    public sealed class MatchTelemetry
    {
        public MatchRecord Record { get; private set; }
        public string LastPath { get; private set; }
        public string LastError { get; private set; }
        public bool Recording { get; private set; }
        public void Begin(int seed, IEnumerable<Combatant> players)
        {
            Record = new MatchRecord { runId = Guid.NewGuid().ToString("N"), startedUtc = DateTime.UtcNow.ToString("O"), seed = seed, debugUsed = Application.isBatchMode };
            foreach (var player in players) Record.players.Add(new PlayerTelemetry { playerId = player.Id });
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
            if (attacker != null) { attacker.damageDealt += damage.Loss.Total; if (damage.Target.IsBoss) attacker.bossDamage += damage.Loss.Total; }
            if (target != null) target.damageTaken += damage.Loss.Total;
        }
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
                record.dominantBuildTags = actor.Inventory.DominantTags().Select(x => x.ToString()).ToList();
                record.corrupted = actor.Corruption;
                if (actor.Faction.ToString() == winner) Record.winningPlayerIds.Add(actor.Id);
            }
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
    }
}
