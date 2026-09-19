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
        private RewardRarityContext rarityContext;
        public Combatant CurrentPlayer { get; private set; }
        public ItemDefinition[] Options { get; private set; } = Array.Empty<ItemDefinition>();
        public bool Active => CurrentPlayer;
        public int RerollsRemaining { get; private set; }
        public event Action Finished;
        public event Action<Combatant, ItemDefinition> Selected;
        public event Action<Combatant> Rerolled;
        public UpgradeDraft(PrototypeCatalog catalog, MetaProgressionService progression, int seed) { this.catalog = catalog; this.progression=progression; random = new System.Random(seed); }
        public RewardRarityContext RarityContext=>rarityContext;
        public void Begin(IEnumerable<Combatant> players)=>Begin(players,new RewardRarityContext(1,1,8,NodeRiskRating.Low,RewardQuality.Common,false,false,false));
        public void Begin(IEnumerable<Combatant> players,RewardRarityContext context)
        {
            rarityContext=context;queue.Clear(); foreach (var player in players) queue.Enqueue(player); NextPlayer();
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
            var candidates = catalog.items.Where(x=>progression.Profile.unlockedRelics.Contains(x.id)).Where(CurrentPlayer.Inventory.CanAdd).ToList();var selected=new List<ItemDefinition>();
            while(selected.Count<3&&candidates.Count>0){var item=Weighted(candidates);candidates.Remove(item);selected.Add(item);}Options=selected.ToArray();
        }
        private ItemDefinition Weighted(IList<ItemDefinition> values){float total=values.Sum(x=>RewardRarityPolicy.Weight(catalog.progressionTuning,x.rarity,rarityContext));if(total<=0)return values[random.Next(values.Count)];double roll=random.NextDouble()*total;foreach(var value in values){roll-=RewardRarityPolicy.Weight(catalog.progressionTuning,value.rarity,rarityContext);if(roll<=0)return value;}return values[values.Count-1];}
        public void Cancel() { queue.Clear(); CurrentPlayer = null; Options = Array.Empty<ItemDefinition>(); }
    }
}
