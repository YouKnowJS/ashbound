using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashbound
{
    public sealed class RunManager : MonoBehaviour
    {
        public RunStateMachine Flow { get; } = new RunStateMachine();
        public LobbySession Lobby { get; } = new LobbySession();
        private readonly List<Combatant> players = new List<Combatant>();
        public IReadOnlyList<Combatant> Players => players;
        public List<LoreEntry> Journal { get; } = new List<LoreEntry>();
        public PrototypeCatalog Catalog { get; private set; }
        public CombatService Combat { get; private set; }
        public RoomDirector Rooms { get; private set; }
        public CorruptionSystem Corruption { get; private set; }
        public MatchTelemetry Telemetry { get; } = new MatchTelemetry();
        public UpgradeDraft Draft { get; private set; }
        public EquipmentRewardDraft EquipmentRewards { get; private set; }
        public MetaProgressionService Progression { get; private set; }
        public string Message { get; private set; } = "The Cinder Vault";
        public string Outcome { get; private set; }
        public string MissingDevice { get; private set; }
        public bool ManualPaused { get; set; }
        public bool DebugOpen { get; set; }
        public int Seed { get; private set; }
        private EntityFactory factory;
        private Camera view;
        private System.Random corruptionRandom;
        private LoreFragment fragment;
        public event Action<RunState> StateChanged;

        public void Configure(PrototypeCatalog catalog, CombatService combat, RoomDirector rooms, EntityFactory factory, Camera view)
        {
            Catalog = catalog; Combat = combat; Rooms = rooms; this.factory = factory; this.view = view;
            string profilePath=Application.isBatchMode?Path.Combine(Application.temporaryCachePath,"AshboundTests","profile-"+Guid.NewGuid().ToString("N")+".json"):null;
            Progression = new MetaProgressionService(catalog,profilePath);
            Corruption = new CorruptionSystem(catalog, factory);
            Flow.Changed += state => { Combat.State = state; Combat.FriendlyFire = false; StateChanged?.Invoke(state); };
            Rooms.WaveCleared += OnWaveCleared; Rooms.BossDied += OnBossDied;Rooms.EncounterStarted+=encounter=>Telemetry.EncounterBegin(encounter,Rooms.Current.combatSpace);Rooms.EncounterCompleted+=Telemetry.EncounterEnd; Combat.DamageResolved += Telemetry.Damage; Combat.BuildProc += Telemetry.Proc; Combat.ControlApplied+=Telemetry.Control;
            Rooms.Load(0);
        }

        public bool StartRun(int seed = 0)
        {
            if (Flow.State != RunState.Lobby) return false;
            Seed = seed == 0 ? Environment.TickCount & int.MaxValue : seed;
            corruptionRandom = new System.Random(Seed ^ 7243); Combat.Seed(Seed); Lobby.Lock(); Journal.Clear(); Outcome = null;
            Progression.BeginExpedition();Progression.RecordProgress(1,1);
            Rooms.Load(0);
            for (int i = 0; i < Lobby.Slots.Count; i++)
            {
                var player = factory.Player(Lobby.Slots[i], i, new Vector3((i - (Lobby.Slots.Count - 1) * .5f) * 2, 0, -6), view);
                players.Add(player); player.GetComponent<PlayerController>().Interacted += Interact;
                player.Inventory.Added += item => Telemetry.Item(player, item, false);
                Progression.ApplyRunStart(player);
            }
            Telemetry.Begin(Seed, players, Progression);
            Draft = new UpgradeDraft(Catalog, Progression, Seed ^ 8721);
            Draft.Selected += (player, item) => Telemetry.Item(player, item, true);
            Draft.Rerolled += player=>Telemetry.ShopReroll(player);
            Draft.Finished += BeginEquipmentRewards;
            EquipmentRewards=new EquipmentRewardDraft(Catalog,Progression,Seed^3187);
            EquipmentRewards.Equipped+=(player,option)=>Telemetry.Equipment(player,option,false);
            EquipmentRewards.Dismantled+=(player,option,value)=>Telemetry.Equipment(player,option,true);
            EquipmentRewards.Finished+=OnRewardsFinished;
            Flow.TryAdvance(RunState.StartingRun); StartCoroutine(EnterRoom()); return true;
        }

        private IEnumerator EnterRoom()
        {
            yield return new WaitForSeconds(.25f);
            Flow.TryAdvance(RunState.Exploration); Message = Rooms.Current.displayName + "\n" + Rooms.Current.description;
            if (fragment) Destroy(fragment.gameObject);
            if (Rooms.Current.fragment) fragment = LoreFragment.Spawn(Rooms.Current.fragment, new Vector3(-5.5f, 0, -4));
            yield return new WaitForSeconds(1.2f);
            if (Rooms.Current.isBoss) { Flow.TryAdvance(RunState.BossFight); Rooms.SpawnBoss(players.Count); Message = Catalog.boss.displayName; }
            else { Flow.TryAdvance(RunState.Combat); Rooms.SpawnNextWave(players.Count); Message = "Break the seal."; }
        }

        private void OnWaveCleared()
        {
            if (Rooms.RoomIndex == 4) Telemetry.MiniBossKilled();
            if (!Flow.TryAdvance(RunState.Reward)) return;
            AwardEncounterResources();Progression.RecordProgress(1,Rooms.RoomIndex+1);
            Rooms.ClearTransientCombat();
            foreach (var player in players)
            {
                if (!player.Alive) player.Restore(.45f); else player.Health.Heal(player.Health.MaxHealth * .25f*(1+Progression.EffectPower(MetaEffectKind.RestRecovery)));
                player.Motor.Stop();
            }
            Message = "Something remains in the ashes.";
            Draft.Begin(players);
        }
        private void BeginEquipmentRewards(){EquipmentRewards.Begin(players,Rooms.RoomIndex+1);Message="Equipment remains among the ashes.";}
        private void OnRewardsFinished()
        {
            if (!Flow.TryAdvance(RunState.Exploration)) return;
            if (Rooms.HasMoreWaves) StartCoroutine(NextWave());
            else { Rooms.UnlockExit(); Message = "Seal broken · Approach the northern gate and interact."; }
        }
        private IEnumerator NextWave()
        {
            Message = "The seal stirs again."; yield return new WaitForSeconds(1);
            if (Flow.TryAdvance(RunState.Combat)) Rooms.SpawnNextWave(players.Count);
        }

        public void Interact(Combatant actor)
        {
            if (fragment && Vector3.Distance(actor.transform.position, fragment.transform.position) < 2.2f)
            {
                if (!Journal.Contains(fragment.Entry)) Journal.Add(fragment.Entry);
                Progression.RecordLore(fragment.Entry.id);
                Message = fragment.Entry.title + "\n" + fragment.Entry.text;
                Destroy(fragment.gameObject); fragment = null; return;
            }
            if (Flow.State != RunState.Exploration || !Rooms.ExitOpen || Vector3.Distance(actor.transform.position, Rooms.View.ExitPosition) > 3) return;
            Rooms.Load(Rooms.RoomIndex + 1);
            for (int i = 0; i < players.Count; i++) players[i].Motor.Teleport(new Vector3((i - (players.Count - 1) * .5f) * 2, 0, -6));
            StartCoroutine(EnterRoom());
        }

        private void OnBossDied()
        {
            Telemetry.FinalBossKilled();
            Progression.Award(ExpeditionResource.Ash,28);Progression.Award(ExpeditionResource.EmberShards,7);Progression.Award(ExpeditionResource.AncientAlloy,2);Progression.Award(ExpeditionResource.CorruptionFragments,1);Progression.RecordBoss(Catalog.boss.id);
            if (!Flow.TryAdvance(RunState.BossDefeated)) return;
            Rooms.ClearTransientCombat(); foreach (var player in players) player.Motor.Stop();
            Message = "The keeper falls.\nFor a moment, the vault is silent.";
            StartCoroutine(AfterBossDeath());
        }
        private IEnumerator AfterBossDeath() { yield return new WaitForSeconds(2); TryBeginCorruption(); }
        public bool TryBeginCorruption()
        {
            if (!Flow.TryAdvance(RunState.CorruptionTransition)) return false;
            Message = "The fire looks for another vessel."; StartCoroutine(Transition()); return true;
        }
        private IEnumerator Transition()
        {
            Rooms.ClearTransientCombat();
            yield return new WaitForSeconds(1.6f);
            if (!Corruption.Activate(Flow, players, corruptionRandom)) yield break;
            for (int i = 0; i < players.Count; i++)
            {
                float angle = i * Mathf.PI * 2 / players.Count;
                players[i].Motor.Teleport(new Vector3(Mathf.Sin(angle), 0, -Mathf.Cos(angle)) * 5.5f);
                players[i].Health.InvulnerableUntil = Time.time + 1.25f;
            }
            Telemetry.Record.corruptionType = Catalog.boss.corruption.id;
            Flow.TryAdvance(RunState.FinalPvP);
            Message = players.Count == 1 ? "It wears your shape.\nFace what the fire remembers." : "Ash has chosen " + string.Join(" + ", Corruption.CorruptedPlayerIds) + ".\nThe violet-crowned stand against the unbound.";
        }

        private void Update()
        {
            if (!Combat) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Flow.State != RunState.Lobby)
            { if (DebugOpen) DebugOpen = false; else ManualPaused = !ManualPaused; }
            MissingDevice = players.FirstOrDefault(x => x && !x.GetComponent<PlayerController>().InputSource.Connected)?.Id;
            Combat.Paused = ManualPaused || DebugOpen || !string.IsNullOrEmpty(MissingDevice);
            Time.timeScale = Combat.Paused || (Combat.Feedback && Combat.Feedback.HitStopped) ? 0 : 1;
            if (!Combat.Paused) Telemetry.Tick(Time.unscaledDeltaTime, Flow.State);
        }
        private void LateUpdate()
        {
            if (!Combat || !Combat.Active || players.Count == 0) return;
            if (Flow.State == RunState.FinalPvP)
            {
                bool unbound = players.Any(x => x.Alive && x.Faction == Faction.Wanderers);
                bool ash = players.Any(x => x.Alive && x.Faction == Faction.Corrupted) || (Corruption.Reflection && Corruption.Reflection.Alive);
                if (!unbound || !ash) Finish(unbound ? "Wanderers" : ash ? "Corrupted" : "Draw",
                    unbound ? "The unbound endure." : ash ? "The fire has found its heir." : "Only ashes remain.");
            }
            else if (players.All(x => !x.Alive)) Finish("Hostiles", "The vault keeps its silence.");
        }
        private void Finish(string winner, string outcome)
        {
            if (!Flow.TryAdvance(RunState.RunComplete)) return;
            Outcome = outcome; Message = outcome; Rooms.ClearTransientCombat();var settlement=Progression.ResolveRun(winner!="Hostiles",Flow.BossWasDefeated);Telemetry.Progression(Progression,settlement);Telemetry.Finish(players, winner, outcome);
        }
        public bool DebugSkipToBoss()
        {
            if (Flow.State == RunState.Lobby) StartRun();
            if (Flow.State == RunState.StartingRun) Flow.TryAdvance(RunState.Exploration);
            if (!Flow.DebugSkipToBoss()) return false;
            StopAllCoroutines(); Draft?.Cancel(); Rooms.Load(Catalog.rooms.Length - 1);
            if (fragment) Destroy(fragment.gameObject);
            for (int i = 0; i < players.Count; i++) { players[i].Restore(); players[i].Motor.Teleport(new Vector3(i * 2 - players.Count + 1, 0, -6)); }
            Rooms.SpawnBoss(players.Count); Message = Catalog.boss.displayName; Telemetry.Record.debugUsed = true; return true;
        }
        public bool DebugJumpToRoom(int index)
        {
            if (Flow.State == RunState.Lobby) StartRun();
            if (Flow.State == RunState.StartingRun) Flow.TryAdvance(RunState.Exploration);
            index = Mathf.Clamp(index, 0, Catalog.rooms.Length - 2);
            if (!Flow.DebugJumpToCombat()) return false;
            StopAllCoroutines(); Draft?.Cancel(); Rooms.Load(index);
            for (int i = 0; i < players.Count; i++) { players[i].Restore(); players[i].Motor.Teleport(new Vector3(i * 2 - players.Count + 1, 0, -6)); }
            Rooms.SpawnNextWave(players.Count); Message = Catalog.rooms[index].displayName; Telemetry.Record.debugUsed = true; return true;
        }
        public void DebugSpawnElementalGroup(ElementTag element){if(players.Count>0&&CombatRules.IsCombatState(Flow.State)){Rooms.DebugSpawnElementalGroup(element,players.Count);Telemetry.Record.debugUsed=true;}}
        public Combatant DebugSpawnEnemy(EnemyDefinition definition,bool elite=false)
        {
            if(!definition)return null;if(Flow.State==RunState.Lobby)StartRun();if(!CombatRules.IsCombatState(Flow.State))Flow.DebugJumpToCombat();var enemy=Rooms.DebugSpawnEnemy(definition,Mathf.Max(1,players.Count),elite);if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;return enemy;
        }
        public void DebugSpawnEncounter(EncounterDefinition encounter)
        {
            if(!encounter)return;if(Flow.State==RunState.Lobby)StartRun();if(!CombatRules.IsCombatState(Flow.State))Flow.DebugJumpToCombat();Rooms.DebugSpawnEncounter(encounter,Mathf.Max(1,players.Count));if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;
        }
        public void DebugLoadCombatSpace(CombatSpaceDefinition space)
        {
            if(!space)return;Rooms.View.Build(space,Rooms.RoomIndex);Rooms.View.SetGate(Rooms.ExitOpen);for(int i=0;i<players.Count;i++)players[i].Motor.Teleport(space.entrancePosition+Vector3.right*(i-(players.Count-1)*.5f)*1.5f);if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;
        }
        public void DebugForceEquipmentReward(WeaponRarity rarity){if(players.Count==0)return;if(Flow.State!=RunState.Reward)Flow.DebugJumpToCombat();Flow.TryAdvance(RunState.Reward);Draft?.Cancel();EquipmentRewards??=new EquipmentRewardDraft(Catalog,Progression,Seed^3187);EquipmentRewards.ForcedRarity=rarity;EquipmentRewards.Begin(players,Rooms.RoomIndex+1);DebugOpen=true;if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;}
        public void ResetRun()
        {
            StopAllCoroutines();
            if(Progression.RunOpen){var settlement=Progression.ResolveRun(false,Flow.BossWasDefeated,true);Telemetry.Progression(Progression,settlement);}
            Telemetry.Finish(players, "None", "Run reset", true);
            Rooms.Clear(); Corruption.Reset(); Draft?.Cancel();EquipmentRewards?.Cancel();
            foreach (var player in players) { Combat.Unregister(player); player.gameObject.SetActive(false); Destroy(player.gameObject); }
            players.Clear(); foreach (var item in FindObjectsByType<ItemPickup>()) Destroy(item.gameObject);
            if (fragment) Destroy(fragment.gameObject);
            ManualPaused = DebugOpen = false; MissingDevice = null; Time.timeScale = 1;
            Lobby.Unlock(); Flow.Reset(); Rooms.Load(0); Message = "The Cinder Vault";
        }
        private void AwardEncounterResources()
        {
            int room=Rooms.RoomIndex;Progression.Award(ExpeditionResource.Ash,room==4?18:room==2||room==5?14:10);
            if(room==2||room==5)Progression.Award(ExpeditionResource.EmberShards,3);
            if(room==4){Progression.Award(ExpeditionResource.EmberShards,5);Progression.Award(ExpeditionResource.AncientAlloy,1);}
        }
        private void OnApplicationQuit() { if(Progression!=null&&Progression.RunOpen){var settlement=Progression.ResolveRun(false,Flow.BossWasDefeated,true);Telemetry.Progression(Progression,settlement);}Telemetry.Finish(players, "None", "Application closed", true); }
        private void OnDestroy()
        {
            Telemetry.Finish(players.Where(x => x && x.Inventory).ToArray(), "None", "Scene closed", true);
            Time.timeScale = 1;
        }
    }
}
