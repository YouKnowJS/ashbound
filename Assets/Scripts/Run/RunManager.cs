using System;
using System.Collections;
using System.Collections.Generic;
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
            Corruption = new CorruptionSystem(catalog, factory);
            Flow.Changed += state => { Combat.State = state; Combat.FriendlyFire = false; StateChanged?.Invoke(state); };
            Rooms.WaveCleared += OnWaveCleared; Rooms.BossDied += OnBossDied; Combat.DamageResolved += Telemetry.Damage; Combat.BuildProc += Telemetry.Proc; Combat.ControlApplied+=Telemetry.Control;
            Rooms.Load(0);
        }

        public bool StartRun(int seed = 0)
        {
            if (Flow.State != RunState.Lobby) return false;
            Seed = seed == 0 ? Environment.TickCount & int.MaxValue : seed;
            corruptionRandom = new System.Random(Seed ^ 7243); Combat.Seed(Seed); Lobby.Lock(); Journal.Clear(); Outcome = null;
            Rooms.Load(0);
            for (int i = 0; i < Lobby.Slots.Count; i++)
            {
                var player = factory.Player(Lobby.Slots[i], i, new Vector3((i - (Lobby.Slots.Count - 1) * .5f) * 2, 0, -6), view);
                players.Add(player); player.GetComponent<PlayerController>().Interacted += Interact;
                player.Inventory.Added += item => Telemetry.Item(player, item, false);
            }
            Telemetry.Begin(Seed, players);
            Draft = new UpgradeDraft(Catalog, Seed ^ 8721);
            Draft.Selected += (player, item) => Telemetry.Item(player, item, true);
            Draft.Finished += OnRewardsFinished;
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
            Rooms.ClearTransientCombat();
            foreach (var player in players)
            {
                if (!player.Alive) player.Restore(.45f); else player.Health.Heal(player.Health.MaxHealth * .25f);
                player.Motor.Stop();
            }
            Message = "Something remains in the ashes.";
            Draft.Begin(players);
        }
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
            Outcome = outcome; Message = outcome; Rooms.ClearTransientCombat(); Telemetry.Finish(players, winner, outcome);
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
        public void ResetRun()
        {
            StopAllCoroutines();
            Telemetry.Finish(players, "None", "Run reset", true);
            Rooms.Clear(); Corruption.Reset(); Draft?.Cancel();
            foreach (var player in players) { Combat.Unregister(player); player.gameObject.SetActive(false); Destroy(player.gameObject); }
            players.Clear(); foreach (var item in FindObjectsByType<ItemPickup>()) Destroy(item.gameObject);
            if (fragment) Destroy(fragment.gameObject);
            ManualPaused = DebugOpen = false; MissingDevice = null; Time.timeScale = 1;
            Lobby.Unlock(); Flow.Reset(); Rooms.Load(0); Message = "The Cinder Vault";
        }
        private void OnApplicationQuit() { Telemetry.Finish(players, "None", "Application closed", true); }
        private void OnDestroy()
        {
            Telemetry.Finish(players.Where(x => x && x.Inventory).ToArray(), "None", "Scene closed", true);
            Time.timeScale = 1;
        }
    }
}
