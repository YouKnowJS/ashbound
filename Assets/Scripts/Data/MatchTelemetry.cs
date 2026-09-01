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
        public List<string> equipmentAcquired = new List<string>();
        public List<string> equipmentDismantled = new List<string>();
        public int shopRerolls;
        public bool corrupted;
    }
    [Serializable]
    public sealed class EnemyRoleTelemetry
    {
        public string role;
        public int kills,playerDeathsCaused;
        public float damageToPlayers;
    }
    [Serializable]
    public sealed class EnemyElementTelemetry
    {
        public string element;
        public float damageToPlayers;
        public int playerDeathsCaused;
    }
    [Serializable]
    public sealed class EncounterTelemetry
    {
        public string encounterId,arenaCategory,eliteOutcome;
        public List<string> composition=new List<string>();
        public float duration,playerDamageTaken,averageRemainingPlayerHp,averagePlayerSeparation;
        public bool elitePresent;
    }
    [Serializable]
    public sealed class MatchRecord
    {
        public int schemaVersion = 4;
        public string runId, startedUtc, endedUtc, corruptionType, winner, outcome;
        public int seed;
        public float runDuration, finalPvPDuration;
        public float miniBossKillTime, finalBossKillTime;
        public string finalResultType;
        public bool aborted, debugUsed;
        public ResourceWallet resourcesCollected = new ResourceWallet();
        public ResourceWallet resourcesRetained = new ResourceWallet();
        public ResourceWallet resourcesLost = new ResourceWallet();
        public ResourceWallet materialsSpentInHub = new ResourceWallet();
        public List<string> facilityLevels = new List<string>();
        public List<string> bossesDefeated = new List<string>();
        public string activePreparation, runCompletionStatus;
        public int highestRegionReached;
        public List<string> winningPlayerIds = new List<string>();
        public List<PlayerTelemetry> players = new List<PlayerTelemetry>();
        public List<EnemyRoleTelemetry> enemyRoles = new List<EnemyRoleTelemetry>();
        public List<EnemyElementTelemetry> enemyElements = new List<EnemyElementTelemetry>();
        public List<EncounterTelemetry> encounters = new List<EncounterTelemetry>();
    }
    public sealed class MatchTelemetry
    {
        private readonly Dictionary<string, Dictionary<string, float>> sources = new Dictionary<string, Dictionary<string, float>>();
        private readonly Dictionary<string, Dictionary<string, float>> elements = new Dictionary<string, Dictionary<string, float>>();
        private readonly Dictionary<string, Dictionary<string, int>> procs = new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<EnemyRole, EnemyRoleTelemetry> enemyRoles = new Dictionary<EnemyRole, EnemyRoleTelemetry>();
        private readonly Dictionary<ElementTag, EnemyElementTelemetry> enemyElements = new Dictionary<ElementTag, EnemyElementTelemetry>();
        private Combatant[] playerActors=Array.Empty<Combatant>();
        private EncounterTelemetry activeEncounter;
        private float encounterStart,encounterSeparationTotal;
        private int encounterSeparationSamples;
        public MatchRecord Record { get; private set; }
        public string LastPath { get; private set; }
        public string LastError { get; private set; }
        public bool Recording { get; private set; }
        public void Begin(int seed, IEnumerable<Combatant> players, MetaProgressionService progression)
        {
            sources.Clear();elements.Clear();procs.Clear();enemyRoles.Clear();enemyElements.Clear();activeEncounter=null;playerActors=players.ToArray();
            Record = new MatchRecord { runId = Guid.NewGuid().ToString("N"), startedUtc = DateTime.UtcNow.ToString("O"), seed = seed, debugUsed = Application.isBatchMode,
                activePreparation=progression.ActivePreparation?progression.ActivePreparation.id:PreparationKind.None.ToString(),materialsSpentInHub=progression.ConsumeMaterialsSpent(),highestRegionReached=1 };
            Record.facilityLevels=progression.Profile.facilities.Select(x=>x.facilityId+":"+x.level).ToList();Record.bossesDefeated=progression.Profile.defeatedBosses.ToList();
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
            if(activeEncounter!=null&&CombatRules.IsCombatState(state)&&playerActors.Length>1)
            {
                float total=0;int pairs=0;for(int i=0;i<playerActors.Length;i++)for(int j=i+1;j<playerActors.Length;j++)if(playerActors[i]&&playerActors[j]){total+=Vector3.Distance(playerActors[i].transform.position,playerActors[j].transform.position);pairs++;}
                if(pairs>0){encounterSeparationTotal+=total/pairs;encounterSeparationSamples++;}
            }
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
            if(damage.Info.Source&&damage.Info.Source.EnemyDefinition&&damage.Target.IsPlayer)
            {
                var definition=damage.Info.Source.EnemyDefinition;var role=Role(definition.role);role.damageToPlayers+=damage.Loss.Total;var element=Element(definition.element);element.damageToPlayers+=damage.Loss.Total;if(activeEncounter!=null)activeEncounter.playerDamageTaken+=damage.Loss.Total;
                if(!damage.Target.Alive){role.playerDeathsCaused++;element.playerDeathsCaused++;}
            }
            if(damage.Target.EnemyDefinition&&damage.Info.Source&&damage.Info.Source.IsPlayer&&!damage.Target.Alive)Role(damage.Target.EnemyDefinition.role).kills++;
        }
        private EnemyRoleTelemetry Role(EnemyRole role){if(!enemyRoles.TryGetValue(role,out var value)){value=new EnemyRoleTelemetry{role=role.ToString()};enemyRoles[role]=value;}return value;}
        private EnemyElementTelemetry Element(ElementTag element){if(!enemyElements.TryGetValue(element,out var value)){value=new EnemyElementTelemetry{element=element.ToString()};enemyElements[element]=value;}return value;}
        public void EncounterBegin(EncounterDefinition encounter,CombatSpaceDefinition space)
        {
            if(!Recording||!encounter)return;activeEncounter=new EncounterTelemetry{encounterId=encounter.id,arenaCategory=space?space.category.ToString():"Unknown",elitePresent=encounter.groups.Any(g=>g.enemy&&g.enemy.elite)};activeEncounter.composition=encounter.groups.Where(g=>g.enemy).Select(g=>g.enemy.role+":"+g.enemy.element+"x"+g.count).ToList();encounterStart=Record.runDuration;encounterSeparationTotal=0;encounterSeparationSamples=0;
        }
        public void EncounterEnd(EncounterDefinition encounter)
        {
            if(!Recording||activeEncounter==null)return;activeEncounter.duration=Mathf.Max(0,Record.runDuration-encounterStart);var party=playerActors.Where(x=>x).ToArray();int alive=party.Count(x=>x.Alive);activeEncounter.averageRemainingPlayerHp=party.Length==0?0:party.Average(x=>x.Alive?x.Health.CurrentHealth/x.Health.MaxHealth:0);activeEncounter.averagePlayerSeparation=encounterSeparationSamples==0?0:encounterSeparationTotal/encounterSeparationSamples;activeEncounter.eliteOutcome=activeEncounter.elitePresent?(alive>0?"Defeated":"PartyDefeated"):"NotPresent";Record.encounters.Add(activeEncounter);activeEncounter=null;
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
        public void Equipment(Combatant player,EquipmentRewardOption option,bool dismantled)
        {
            if(!Recording)return;var record=Record.players.Find(x=>x.playerId==player.Id);if(record==null)return;var target=dismantled?record.equipmentDismantled:record.equipmentAcquired;target.Add(option.DisplayName+":"+option.Rarity);
        }
        public void ShopReroll(Combatant player){if(!Recording)return;var record=Record.players.Find(x=>x.playerId==player.Id);if(record!=null)record.shopRerolls++;}
        public void Progression(MetaProgressionService progression,ResourceSettlement settlement)
        {
            if(Record==null)return;Record.resourcesCollected=settlement.Collected.Copy();Record.resourcesRetained=settlement.Retained.Copy();Record.resourcesLost=settlement.Lost.Copy();Record.highestRegionReached=progression.Profile.lifetime.highestRegionReached;Record.bossesDefeated=progression.Profile.defeatedBosses.ToList();Record.facilityLevels=progression.Profile.facilities.Select(x=>x.facilityId+":"+x.level).ToList();
        }
        public void Finish(IEnumerable<Combatant> players, string winner, string outcome, bool aborted = false)
        {
            if (!Recording) return;
            Recording = false; Record.winner = winner; Record.outcome = outcome; Record.aborted = aborted; Record.endedUtc = DateTime.UtcNow.ToString("O");Record.runCompletionStatus=aborted?"Abandoned":winner=="Hostiles"?"Failed":"Completed";
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
            Record.enemyRoles=enemyRoles.Values.OrderBy(x=>x.role).ToList();Record.enemyElements=enemyElements.Values.OrderBy(x=>x.element).ToList();
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
