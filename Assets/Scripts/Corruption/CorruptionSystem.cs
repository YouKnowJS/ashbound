using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class CorruptionSystem
    {
        private readonly PrototypeCatalog catalog;
        private readonly EntityFactory factory;
        public int FourPlayerCount { get; set; }
        public List<string> ForcedPlayerIds { get; } = new List<string>();
        public Combatant Reflection { get; private set; }
        public string[] CorruptedPlayerIds { get; private set; } = System.Array.Empty<string>();
        public CorruptionSystem(PrototypeCatalog catalog, EntityFactory factory)
        { this.catalog = catalog; this.factory = factory; FourPlayerCount = catalog.corruptedAtFourPlayers; }

        public bool Activate(RunStateMachine flow, IReadOnlyList<Combatant> players, System.Random random)
        {
            if (flow.State != RunState.CorruptionTransition || !flow.BossWasDefeated || Reflection || CorruptedPlayerIds.Length > 0) return false;
            foreach (var player in players) player.Restore();
            if (players.Count == 1)
            {
                var player = players[0];
                Reflection = factory.Reflection(player, new Vector3(0, 0, 5));
                var tags = player.Inventory.DominantTags();
                foreach (string id in BuildAnalyzer.ReflectionItems(tags)) Reflection.Inventory.TryAdd(catalog.FindItem(id));
                Reflection.DisplayName = "Your reflection" + (tags.Length > 0 ? " · " + string.Join(" / ", tags) : "");
                Reflection.BaseSpeed = 5.6f;
                Reflection.gameObject.AddComponent<CorruptionAbilities>().Apply(Reflection, catalog.boss.corruption);
                Reflection.gameObject.AddComponent<ReflectionController>().Configure(Reflection);
            }
            else
            {
                CorruptedPlayerIds = CorruptionSelector.Select(players.Select(x => x.Id).ToArray(), FourPlayerCount, random, ForcedPlayerIds);
                foreach (var player in players.Where(x => CorruptedPlayerIds.Contains(x.Id)))
                    player.gameObject.AddComponent<CorruptionAbilities>().Apply(player, catalog.boss.corruption);
            }
            return true;
        }
        public void Reset()
        {
            if (Reflection) { Reflection.Combat.Unregister(Reflection); Reflection.gameObject.SetActive(false); Object.Destroy(Reflection.gameObject); }
            Reflection = null; CorruptedPlayerIds = System.Array.Empty<string>(); ForcedPlayerIds.Clear();
        }
    }
}
