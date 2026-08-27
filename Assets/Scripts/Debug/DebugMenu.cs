using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U = Ashbound.PrototypeGui;

namespace Ashbound
{
    public sealed class DebugMenu : MonoBehaviour
    {
        private RunManager run;
        private int selectedPlayer;
        private string result = "Changes apply to the selected player. Close F1 to resume simulation.";
        public void Configure(RunManager manager) { run = manager; }
        private void Update()
        {
            if (!run || Keyboard.current == null) return;
            if (Keyboard.current.f1Key.wasPressedThisFrame) run.DebugOpen = !run.DebugOpen;
        }
        private void Mark() { if (run.Telemetry.Record != null) run.Telemetry.Record.debugUsed = true; }
        private void OnGUI()
        {
            if (!run || !run.DebugOpen) return;
            GUI.depth = -20; var old = U.Scale();
            U.Box(new Rect(0, 0, 1280, 720), new Color(0, 0, 0, .65f));
            U.Panel(new Rect(140, 50, 1000, 625));
            U.Label(165, 68, 840, 35, "DEVELOPER TOOLS", U.Heading);
            if (U.Click(new Rect(1000, 67, 110, 34), "Close F1")) run.DebugOpen = false;
            U.Label(165, 113, 945, 26, "State: " + run.Flow.State + "   |   Boss defeated: " + run.Flow.BossWasDefeated + "   |   PvP: " + run.Combat.PvPEnabled + "   |   Friendly fire: " + run.Combat.FriendlyFire, U.Small);
            if (U.Click(new Rect(165, 154, 220, 40), "Jump to final boss")) { result = run.DebugSkipToBoss() ? "Boss encounter ready." : "Reset first to leave the final phase."; Mark(); }
            GUI.enabled = run.Rooms.Boss && run.Rooms.Boss.Alive;
            if (U.Click(new Rect(403, 154, 220, 40), "Kill boss instantly")) { run.Rooms.Boss.Health.DebugKill(); Mark(); result = "Boss death event emitted. Close debug or trigger transition."; }
            GUI.enabled = run.Flow.State == RunState.BossDefeated;
            if (U.Click(new Rect(641, 154, 220, 40), "Trigger corruption")) { run.TryBeginCorruption(); Mark(); result = "Transition queued. Close debug to advance time."; }
            GUI.enabled = true;
            if (U.Click(new Rect(879, 154, 230, 40), "Reset run")) { run.ResetRun(); run.DebugOpen = true; result = "Run reset. Roster unlocked."; }
            U.Label(165, 209, 950, 30, "Corruption is guarded by the actual boss death event, including from this menu.", U.Small);
            U.Label(165, 247, 310, 30, "4-player corruption count:", U.Small);
            if (U.Click(new Rect(426, 242, 160, 34), run.Corruption.FourPlayerCount + " (click to toggle)")) { run.Corruption.FourPlayerCount = run.Corruption.FourPlayerCount == 1 ? 2 : 1; Mark(); }
            if (run.Players.Count > 0)
            {
                for (int i = 0; i < run.Players.Count; i++)
                    if (U.Click(new Rect(165 + i * 112, 292, 102, 33), run.Players[i].Id + (i == selectedPlayer ? " selected" : ""))) selectedPlayer = i;
                selectedPlayer = Mathf.Clamp(selectedPlayer, 0, run.Players.Count - 1);
                var player = run.Players[selectedPlayer];
                bool forced = run.Corruption.ForcedPlayerIds.Contains(player.Id);
                GUI.enabled = run.Flow.State != RunState.FinalPvP && run.Players.Count > 1;
                if (U.Click(new Rect(650, 292, 220, 33), forced ? "Unforce " + player.Id : "Force " + player.Id + " corruption"))
                { if (forced) run.Corruption.ForcedPlayerIds.Remove(player.Id); else run.Corruption.ForcedPlayerIds.Add(player.Id); Mark(); }
                GUI.enabled = true;
                bool god = GUI.Toggle(new Rect(905, 297, 200, 26), player.Health.DebugInvulnerable, " Invulnerable " + player.Id);
                if (god != player.Health.DebugInvulnerable) { player.Health.DebugInvulnerable = god; Mark(); }
                for (int i = 0; i < run.Catalog.items.Length; i++)
                {
                    var item = run.Catalog.items[i]; int column = i % 2, row = i / 2;
                    float x = 165 + column * 476, y = 347 + row * 48;
                    U.Label(x, y + 8, 239, 29, item.displayName, U.Small);
                    GUI.enabled = player.Inventory.CanAdd(item);
                    if (U.Click(new Rect(x + 240, y, 88, 35), "Add")) { player.Inventory.TryAdd(item); Mark(); }
                    if (U.Click(new Rect(x + 338, y, 100, 35), "Spawn"))
                    {
                        var obj = PrimitiveFactory.Shape(item.displayName, PrimitiveType.Cube, null, player.transform.position + Vector3.right * 2 + Vector3.up * .4f, Vector3.one * .5f, Palette.Gold);
                        obj.AddComponent<ItemPickup>().Configure(player, item); Mark();
                    }
                    GUI.enabled = true;
                }
            }
            else U.Label(165, 327, 900, 80, "Enter a run or jump to the boss to inspect player builds and spawn items.");
            U.Label(165, 560, 940, 65, result + "\nForced IDs: " + (run.Corruption.ForcedPlayerIds.Count == 0 ? "random" : string.Join(", ", run.Corruption.ForcedPlayerIds)), U.Small);
            U.Label(165, 632, 950, 24, "DEBUG runs are marked in telemetry. Invulnerability persists until reset; turn it off for balance tests.", U.Small);
            GUI.enabled = true; GUI.matrix = old;
        }
    }
}
