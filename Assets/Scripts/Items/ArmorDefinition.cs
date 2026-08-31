using System;
using UnityEngine;

namespace Ashbound
{
    public enum ArmorPassiveKind { None, Cooldown, ShieldOnDash, ElementAmplify, Recovery, ControlDuration, StatusResistance, ProcCharge }

    [Serializable]
    public struct ArmorPassive
    {
        public ArmorPassiveKind kind;
        public float power;
        public ElementTag element;
    }

    [Serializable]
    public struct SetBonusTier
    {
        public int pieces;
        [TextArea] public string description;
        public StatModifiers statModifiers;
        public TriggeredEffect[] effects;
        public BuildTag[] tags;
    }

    [CreateAssetMenu(menuName = "Ashbound/Armor")]
    public sealed class ArmorDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public WeaponRarity rarity;
        public ArmorSlot slot;
        public BuildTag[] tags = Array.Empty<BuildTag>();
        public ElementTag[] elements = Array.Empty<ElementTag>();
        public StatModifiers statModifiers;
        public ArmorPassive passive;
        public ArmorSetDefinition set;
        public GameObject vfxPrefab;
    }
}
