using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public readonly struct ActiveSetBonus
    {
        public readonly ArmorSetDefinition Set;
        public readonly int Pieces;
        public readonly SetBonusTier Tier;
        public ActiveSetBonus(ArmorSetDefinition set, int pieces, SetBonusTier tier) { Set=set; Pieces=pieces; Tier=tier; }
        public string Key => Set.id + ":" + Tier.pieces;
    }

    public sealed class PlayerEquipment : MonoBehaviour
    {
        private readonly Dictionary<ArmorSlot, ArmorDefinition> equipped = new Dictionary<ArmorSlot, ArmorDefinition>();
        public IReadOnlyDictionary<ArmorSlot, ArmorDefinition> Equipped => equipped;
        public event Action Changed;
        public bool Equip(ArmorDefinition armor)
        {
            if (!armor) return false; equipped[armor.slot]=armor; Changed?.Invoke(); return true;
        }
        public void Clear() { equipped.Clear(); Changed?.Invoke(); }
        public ArmorDefinition InSlot(ArmorSlot slot) => equipped.TryGetValue(slot,out var armor)?armor:null;
        public IEnumerable<ActiveSetBonus> ActiveBonuses()
        {
            foreach(var group in equipped.Values.Where(x=>x&&x.set).GroupBy(x=>x.set))
            {
                int count=group.Count();
                if(count>=2) yield return new ActiveSetBonus(group.Key,count,group.Key.twoPiece);
                if(count>=4) yield return new ActiveSetBonus(group.Key,count,group.Key.fourPiece);
            }
        }
        public StatModifiers SumModifiers()
        {
            var sum=new StatModifiers();
            foreach(var armor in equipped.Values) Add(ref sum,armor.statModifiers);
            foreach(var bonus in ActiveBonuses()) Add(ref sum,bonus.Tier.statModifiers);
            return sum;
        }
        public IEnumerable<BuildTag> BuildTags()
        {
            foreach(var armor in equipped.Values){foreach(var tag in armor.tags)yield return tag;foreach(var element in armor.elements)yield return ElementBuildTag(element);}
            foreach(var bonus in ActiveBonuses())if(bonus.Tier.tags!=null)foreach(var tag in bonus.Tier.tags)yield return tag;
        }
        public bool HasEffect(TriggerKind kind)=>ActiveBonuses().Any(x=>x.Tier.effects!=null&&x.Tier.effects.Any(e=>e.kind==kind));
        public float EffectPower(TriggerKind kind)=>ActiveBonuses().Sum(x=>x.Tier.effects==null?0:x.Tier.effects.Where(e=>e.kind==kind).Sum(e=>e.power));
        public int EffectThreshold(TriggerKind kind,int fallback)
        {
            foreach(var e in ActiveBonuses().SelectMany(x=>x.Tier.effects??Array.Empty<TriggeredEffect>()))if(e.kind==kind&&e.threshold>0)return e.threshold;
            return fallback;
        }
        public float PassivePower(ArmorPassiveKind kind,ElementTag element=ElementTag.None)=>equipped.Values.Where(x=>x.passive.kind==kind&&(element==ElementTag.None||x.passive.element==element)).Sum(x=>x.passive.power);
        private static void Add(ref StatModifiers sum,StatModifiers value){sum.damage+=value.damage;sum.criticalChance+=value.criticalChance;sum.criticalMultiplier+=value.criticalMultiplier;sum.attackSpeed+=value.attackSpeed;sum.movementSpeed+=value.movementSpeed;sum.maxHealth+=value.maxHealth;}
        public static BuildTag ElementBuildTag(ElementTag element)=>element==ElementTag.Fire?BuildTag.Fire:element==ElementTag.Frost?BuildTag.Frost:element==ElementTag.Lightning?BuildTag.Lightning:element==ElementTag.Poison?BuildTag.Poison:element==ElementTag.Void?BuildTag.Void:BuildTag.Utility;
    }
}
