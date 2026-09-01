using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class EquipmentRewardOption
    {
        public WeaponDefinition Weapon { get; }
        public ArmorDefinition Armor { get; }
        public bool IsWeapon => Weapon;
        public WeaponRarity Rarity => IsWeapon ? Weapon.rarity : Armor.rarity;
        public string DisplayName => IsWeapon ? Weapon.displayName : Armor.displayName;
        public ElementTag Element => IsWeapon ? Weapon.PrimaryElement : Armor.elements.FirstOrDefault();
        public EquipmentRewardOption(WeaponDefinition weapon){Weapon=weapon;}
        public EquipmentRewardOption(ArmorDefinition armor){Armor=armor;}
    }

    public sealed class EquipmentRewardDraft
    {
        private readonly PrototypeCatalog catalog;
        private readonly MetaProgressionService progression;
        private readonly Queue<Combatant> queue=new Queue<Combatant>();
        private readonly System.Random random;
        private int depth;
        private RewardQuality quality;
        private EquipmentOfferKind offerKind;
        private ArmorSlot? forcedSlot;
        public Combatant CurrentPlayer { get; private set; }
        public EquipmentRewardOption[] Options { get; private set; }=Array.Empty<EquipmentRewardOption>();
        public bool Active=>CurrentPlayer;
        public WeaponRarity? ForcedRarity { get; set; }
        public event Action Finished;
        public event Action<Combatant,EquipmentRewardOption> Equipped;
        public event Action<Combatant,EquipmentRewardOption,ResourceWallet> Dismantled;
        public EquipmentRewardDraft(PrototypeCatalog catalog,MetaProgressionService progression,int seed){this.catalog=catalog;this.progression=progression;random=new System.Random(seed);}
        public void Begin(IEnumerable<Combatant> players,int encounterDepth)=>Begin(players,encounterDepth,RewardQuality.Common,EquipmentOfferKind.Mixed);
        public void Begin(IEnumerable<Combatant> players,int encounterDepth,RewardQuality rewardQuality,EquipmentOfferKind kind=EquipmentOfferKind.Mixed,ArmorSlot? slot=null){queue.Clear();foreach(var player in players)queue.Enqueue(player);depth=encounterDepth;quality=rewardQuality;offerKind=kind;forcedSlot=slot;NextPlayer();}
        public bool Equip(int index)
        {
            if(!Valid(index))return false;var option=Options[index];if(option.IsWeapon)CurrentPlayer.Attacks.SetWeapon(option.Weapon);else CurrentPlayer.Equipment.Equip(option.Armor);
            progression.RecordEquipment(false);Equipped?.Invoke(CurrentPlayer,option);NextPlayer();return true;
        }
        public bool Dismantle(int index)
        {
            if(!Valid(index))return false;var option=Options[index];var value=ProgressionEconomy.Salvage(option.Rarity,option.IsWeapon,progression.EffectPower(MetaEffectKind.SalvageYield));progression.RunResources.Add(value);progression.RecordEquipment(true);Dismantled?.Invoke(CurrentPlayer,option,value);NextPlayer();return true;
        }
        public bool Leave(){if(!Active)return false;NextPlayer();return true;}
        public void Cancel(){queue.Clear();CurrentPlayer=null;Options=Array.Empty<EquipmentRewardOption>();ForcedRarity=null;}
        private bool Valid(int index)=>Active&&index>=0&&index<Options.Length;
        private void NextPlayer()
        {
            if(queue.Count==0){Cancel();Finished?.Invoke();return;}CurrentPlayer=queue.Dequeue();var choices=new List<EquipmentRewardOption>();int target=quality>=RewardQuality.Rare?3:2;
            var weapons=progression.UnlockedWeapons().Where(x=>!ForcedRarity.HasValue||x.rarity==ForcedRarity.Value).ToList();
            var armor=progression.UnlockedArmor().Where(x=>(!ForcedRarity.HasValue||x.rarity==ForcedRarity.Value)&&(!forcedSlot.HasValue||x.slot==forcedSlot.Value)).ToList();
            while(choices.Count<target&&(weapons.Count>0||armor.Count>0))
            {
                bool chooseWeapon=offerKind==EquipmentOfferKind.Weapon||(offerKind==EquipmentOfferKind.Mixed&&((choices.Count%2==0&&weapons.Count>0)||armor.Count==0));if(offerKind==EquipmentOfferKind.Armor||offerKind==EquipmentOfferKind.ArmorSlot)chooseWeapon=false;
                if(chooseWeapon&&weapons.Count>0){var selected=Weighted(weapons,x=>x.rarity,x=>x.PrimaryElement);weapons.Remove(selected);choices.Add(new EquipmentRewardOption(PrepareWeapon(selected)));}
                else if(armor.Count>0){var selected=Weighted(armor,x=>x.rarity,x=>x.elements.FirstOrDefault());armor.Remove(selected);choices.Add(new EquipmentRewardOption(selected));}
                else break;
            }
            if(choices.Count==0)choices.Add(new EquipmentRewardOption(catalog.weapon));Options=choices.ToArray();
        }
        private WeaponDefinition PrepareWeapon(WeaponDefinition source)
        {
            if(!source.skill||progression.Profile.unlockedWeaponSkills.Contains(source.skill.id))return source;
            var copy=UnityEngine.Object.Instantiate(source);copy.name=source.name+" (unskilled)";copy.skill=null;copy.passiveDescription=(source.passiveDescription+" Weapon Skill research is still locked.").Trim();return copy;
        }
        private T Weighted<T>(IList<T> values,Func<T,WeaponRarity> rarity,Func<T,ElementTag> element)
        {
            var weights=new float[values.Count];float total=0;for(int i=0;i<values.Count;i++){float w=BaseWeight(rarity(values[i]));if(rarity(values[i])>=WeaponRarity.Rare)w*=1+progression.EffectPower(MetaEffectKind.RareWeight);if(element(values[i])==progression.PreferredElement)w*=1+progression.EffectPower(MetaEffectKind.ElementBias);weights[i]=w;total+=w;}
            double roll=random.NextDouble()*total;for(int i=0;i<values.Count;i++){roll-=weights[i];if(roll<=0)return values[i];}return values[values.Count-1];
        }
        private float BaseWeight(WeaponRarity rarity)
        {
            float depthBonus=Mathf.Clamp01(depth/7f);float qualityBonus=(int)quality*.18f;return rarity==WeaponRarity.Common?Mathf.Max(.15f,1.2f-depthBonus*.45f-qualityBonus):rarity==WeaponRarity.Advanced?1.05f+qualityBonus*.35f:rarity==WeaponRarity.Rare?.55f+depthBonus*.25f+qualityBonus:rarity==WeaponRarity.Epic?.16f+depthBonus*.16f+qualityBonus*.65f:.025f+depthBonus*.04f+qualityBonus*.22f;
        }
    }
}
