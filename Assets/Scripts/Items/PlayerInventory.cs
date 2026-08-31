using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        private readonly List<ItemDefinition> items = new List<ItemDefinition>();
        public IReadOnlyList<ItemDefinition> Items => items;
        public event Action<ItemDefinition> Added;
        public bool Has(string id) => items.Any(x => x.id == id);
        public int CountTag(BuildTag tag) => items.Count(x => x.tags.Contains(tag));
        public bool CanAdd(ItemDefinition item) => item && !Has(item.id) && (string.IsNullOrEmpty(item.requiredItemId) || Has(item.requiredItemId));
        public bool TryAdd(ItemDefinition item)
        {
            if (!CanAdd(item)) return false;
            items.Add(item);
            Added?.Invoke(item);
            return true;
        }
        public BuildTag[] DominantTags() => BuildAnalyzer.Dominant(items.Select(x => x.tags));
        public StatModifiers SumModifiers()
        {
            var sum = new StatModifiers();
            foreach (var item in items)
            {
                var m = item.statModifiers;
                sum.damage += m.damage; sum.criticalChance += m.criticalChance;
                sum.criticalMultiplier += m.criticalMultiplier; sum.attackSpeed += m.attackSpeed;
                sum.movementSpeed += m.movementSpeed; sum.maxHealth += m.maxHealth;
            }
            return sum;
        }
        public bool HasEffect(TriggerKind kind) => items.Any(x => x.triggeredEffects.Any(e => e.kind == kind));
        public float EffectPower(TriggerKind kind) => items.Sum(x => x.triggeredEffects.Where(e => e.kind == kind).Sum(e => e.power));
        public int EffectThreshold(TriggerKind kind, int fallback)
        {
            foreach (var effect in items.SelectMany(x => x.triggeredEffects))
                if (effect.kind == kind && effect.threshold > 0) return effect.threshold;
            return fallback;
        }
        public void Clear() { items.Clear(); }
    }
}
