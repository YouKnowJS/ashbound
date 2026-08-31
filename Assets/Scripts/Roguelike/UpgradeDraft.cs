using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class UpgradeDraft
    {
        private readonly PrototypeCatalog catalog;
        private readonly MetaProgressionService progression;
        private readonly Queue<Combatant> queue = new Queue<Combatant>();
        private readonly System.Random random;
        public Combatant CurrentPlayer { get; private set; }
        public ItemDefinition[] Options { get; private set; } = Array.Empty<ItemDefinition>();
        public bool Active => CurrentPlayer;
        public int RerollsRemaining { get; private set; }
        public event Action Finished;
        public event Action<Combatant, ItemDefinition> Selected;
        public event Action<Combatant> Rerolled;
        public UpgradeDraft(PrototypeCatalog catalog, MetaProgressionService progression, int seed) { this.catalog = catalog; this.progression=progression; random = new System.Random(seed); }
        public void Begin(IEnumerable<Combatant> players)
        {
            queue.Clear(); foreach (var player in players) queue.Enqueue(player); NextPlayer();
        }
        public bool Choose(int index)
        {
            if (!CurrentPlayer || index < 0 || index >= Options.Length) return false;
            var item = Options[index];
            if (!CurrentPlayer.Inventory.TryAdd(item)) return false;
            Selected?.Invoke(CurrentPlayer, item); NextPlayer(); return true;
        }
        public bool Reroll()
        {
            if(!CurrentPlayer||RerollsRemaining<=0)return false;RerollsRemaining--;RollOptions();Rerolled?.Invoke(CurrentPlayer);return true;
        }
        private void NextPlayer()
        {
            while (queue.Count > 0)
            {
                CurrentPlayer = queue.Dequeue();
                RerollsRemaining=Mathf.RoundToInt(progression.EffectPower(MetaEffectKind.RelicReroll));RollOptions();
                if (Options.Length > 0) return;
            }
            Cancel(); Finished?.Invoke();
        }
        private void RollOptions()
        {
            var candidates = catalog.items.Where(x=>progression.Profile.unlockedRelics.Contains(x.id)).Where(CurrentPlayer.Inventory.CanAdd).ToList();
            for (int i = candidates.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var temp = candidates[i]; candidates[i] = candidates[j]; candidates[j] = temp; }
            Options = candidates.Take(3).ToArray();
        }
        public void Cancel() { queue.Clear(); CurrentPlayer = null; Options = Array.Empty<ItemDefinition>(); }
    }
}
