using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public struct StatModifiers
    {
        public float damage, criticalChance, criticalMultiplier, attackSpeed, movementSpeed, maxHealth;
    }

    [Serializable]
    public struct TriggeredEffect
    {
        public TriggerKind kind;
        public float power;
        public int threshold;
    }

    [Serializable]
    public struct StatusPayload
    {
        public StatusKind kind;
        public float duration;
        public float power;
        public int maxStacks;
    }

    [CreateAssetMenu(menuName = "Ashbound/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Rarity rarity;
        public BuildTag[] tags = Array.Empty<BuildTag>();
        public StatModifiers statModifiers;
        public TriggeredEffect[] triggeredEffects = Array.Empty<TriggeredEffect>();
        public StatusPayload[] statusEffects = Array.Empty<StatusPayload>();
        public string requiredItemId;
    }
}
