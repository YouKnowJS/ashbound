using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class RouteRunModifiers
    {
        public bool EliteNextCombat { get; set; }
        public bool VoidPressureNextCombat { get; set; }
        public bool HealingReducedUntilRest { get; set; }
        public void ClearCombatModifiers(){EliteNextCombat=false;VoidPressureNextCombat=false;}
        public void ClearAtRest(){HealingReducedUntilRest=false;}
    }

    public enum TreasureAction { None, EquipmentReward, MimicCombat, Complete }
    public static class TreasureSelector
    {
        public static TreasureVariantDefinition Select(TreasureDefinition definition,System.Random random,TreasureVariantKind? forced=null)
        {
            if(!definition||definition.variants==null||definition.variants.Length==0)return null;if(forced.HasValue)return definition.variants.FirstOrDefault(x=>x.kind==forced.Value);
            float total=definition.variants.Sum(x=>Mathf.Max(0,x.weight));if(total<=0)return definition.variants[0];double roll=random.NextDouble()*total;foreach(var variant in definition.variants){roll-=Mathf.Max(0,variant.weight);if(roll<=0)return variant;}return definition.variants[definition.variants.Length-1];
        }
    }

    public sealed class TreasureSession
    {
        public TreasureVariantDefinition Variant { get; }
        public int RewardsTaken { get; private set; }
        public bool Opened { get; private set; }
        public bool MimicActive { get; private set; }
        public bool MimicDefeated { get; private set; }
        public bool Completed { get; private set; }
        public string Message { get; private set; }
        public bool CanContinueGreed=>Variant!=null&&Variant.kind==TreasureVariantKind.GreedyCache&&RewardsTaken<Variant.maximumGreedRewards&&!Completed;
        public TreasureSession(TreasureDefinition definition,int seed,TreasureVariantKind? forced=null){Variant=TreasureSelector.Select(definition,new System.Random(seed),forced);Message=Variant?.description??"The cache is empty.";if(Variant==null)Completed=true;}
        public TreasureAction Open(MetaProgressionService progression,IEnumerable<Combatant> players,RouteRunModifiers modifiers)
        {
            if(Completed||Variant==null)return TreasureAction.None;if(!Opened)
            {
                if(!progression.TrySpendRun(Variant.openCost)){Message="The seal demands "+Variant.openCost+".";return TreasureAction.None;}
                PayHealth(players,Variant.currentHealthCost);Opened=true;
                if(Variant.addsVoidPressureToNextCombat){modifiers.VoidPressureNextCombat=true;modifiers.HealingReducedUntilRest=true;foreach(var player in players)if(player)player.Health.HealingMultiplier=.65f;}
                if(Variant.kind==TreasureVariantKind.Mimic){MimicActive=true;Message="The cache unfolds into a hunting shape.";return TreasureAction.MimicCombat;}
            }
            return BeginReward(progression,players,modifiers);
        }
        public TreasureAction ContinueGreed(MetaProgressionService progression,IEnumerable<Combatant> players,RouteRunModifiers modifiers)
        {
            if(!CanContinueGreed)return TreasureAction.None;if(RewardsTaken==1)PayHealth(players,.15f);else if(RewardsTaken==2)modifiers.EliteNextCombat=true;return BeginReward(progression,players,modifiers);
        }
        private TreasureAction BeginReward(MetaProgressionService progression,IEnumerable<Combatant> players,RouteRunModifiers modifiers)
        {
            RewardsTaken++;progression.Award(Variant.bonusResources);Message="Reward "+RewardsTaken+" claimed.";if(Variant.kind!=TreasureVariantKind.GreedyCache)Completed=true;return TreasureAction.EquipmentReward;
        }
        public void Stop(){if(Completed)return;Completed=true;Message="The party leaves the remaining temptation sealed.";}
        public void MarkMimicDefeated(){MimicActive=false;MimicDefeated=true;Opened=true;RewardsTaken=1;Completed=true;Message="The false cache breaks. Its hoard remains.";}
        private static void PayHealth(IEnumerable<Combatant> players,float fraction){if(fraction<=0)return;foreach(var player in players)if(player&&player.Alive)player.Health.SpendCurrentHealth(fraction);}
    }

    public sealed class MerchantOffer
    {
        public MerchantOfferKind Kind;
        public WeaponDefinition Weapon;
        public ArmorDefinition Armor;
        public ItemDefinition Relic;
        public ResourceWallet Price;
        public bool Sold;
        public string DisplayName=>Kind==MerchantOfferKind.Weapon?Weapon.displayName:Kind==MerchantOfferKind.Armor?Armor.displayName:Kind==MerchantOfferKind.Relic?Relic.displayName:"Field recovery";
        public WeaponRarity Rarity=>Kind==MerchantOfferKind.Weapon?Weapon.rarity:Kind==MerchantOfferKind.Armor?Armor.rarity:WeaponRarity.Common;
    }

    public sealed class MerchantSession
    {
        private readonly PrototypeCatalog catalog;private readonly MetaProgressionService progression;private readonly MerchantDefinition definition;private readonly System.Random random;
        public MerchantOffer[] Offers { get; private set; }=Array.Empty<MerchantOffer>();
        public int RerollsUsed { get; private set; }
        public int MaximumRerolls=>definition.maximumRerolls;
        public bool Closed { get; private set; }
        public MerchantSession(PrototypeCatalog catalog,MetaProgressionService progression,MerchantDefinition definition,int seed){this.catalog=catalog;this.progression=progression;this.definition=definition;random=new System.Random(seed);Roll();}
        public bool Buy(int index,Combatant player,out MerchantOffer bought)
        {
            bought=null;if(Closed||player==null||index<0||index>=Offers.Length)return false;var offer=Offers[index];if(offer.Sold||!CanReceive(offer,player)||!progression.RunResources.CanAfford(offer.Price))return false;if(!progression.TrySpendRun(offer.Price))return false;
            if(offer.Kind==MerchantOfferKind.Weapon)player.Attacks.SetWeapon(offer.Weapon);else if(offer.Kind==MerchantOfferKind.Armor)player.Equipment.Equip(offer.Armor);else if(offer.Kind==MerchantOfferKind.Relic)player.Inventory.TryAdd(offer.Relic);else player.Health.Heal(player.Health.MaxHealth*definition.recoveryFraction);
            offer.Sold=true;bought=offer;return true;
        }
        public bool Reroll(out ResourceWallet paid)
        {
            paid=RerollPrice();if(Closed||RerollsUsed>=MaximumRerolls||!progression.TrySpendRun(paid))return false;RerollsUsed++;Roll();return true;
        }
        public ResourceWallet RerollPrice(){float discount=Mathf.Clamp01(progression.EffectPower(MetaEffectKind.RerollDiscount));return Scale(definition.rerollCost,1-discount);}
        public void Close(){Closed=true;}
        private void Roll()
        {
            int count=Mathf.Clamp(definition.baseStock+Mathf.RoundToInt(progression.EffectPower(MetaEffectKind.MerchantStock)),2,6);var offers=new List<MerchantOffer>();
            var weapons=progression.UnlockedWeapons().ToArray();var armor=progression.UnlockedArmor().ToArray();var relics=catalog.items.Where(x=>x&&progression.Profile.unlockedRelics.Contains(x.id)).ToArray();
            for(int i=0;i<count;i++)
            {
                int kind=i%4;if(kind==0&&weapons.Length>0){var item=weapons[random.Next(weapons.Length)];offers.Add(new MerchantOffer{Kind=MerchantOfferKind.Weapon,Weapon=item,Price=Scale(ProgressionEconomy.MerchantPrice(item.rarity,true),definition.priceMultiplier)});}
                else if(kind==1&&armor.Length>0){var item=armor[random.Next(armor.Length)];offers.Add(new MerchantOffer{Kind=MerchantOfferKind.Armor,Armor=item,Price=Scale(ProgressionEconomy.MerchantPrice(item.rarity,false),definition.priceMultiplier)});}
                else if(kind==2&&relics.Length>0){var item=relics[random.Next(relics.Length)];offers.Add(new MerchantOffer{Kind=MerchantOfferKind.Relic,Relic=item,Price=Scale(new ResourceWallet{ash=20,emberShards=2},definition.priceMultiplier)});}
                else offers.Add(new MerchantOffer{Kind=MerchantOfferKind.Recovery,Price=Scale(definition.recoveryPrice,definition.priceMultiplier)});
            }
            Offers=offers.ToArray();
        }
        private static bool CanReceive(MerchantOffer offer,Combatant player)=>offer.Kind!=MerchantOfferKind.Relic||player.Inventory.CanAdd(offer.Relic);
        private static ResourceWallet Scale(ResourceWallet value,float multiplier)=>new ResourceWallet{ash=Mathf.CeilToInt(value.ash*multiplier),emberShards=Mathf.CeilToInt(value.emberShards*multiplier),ancientAlloy=Mathf.CeilToInt(value.ancientAlloy*multiplier),corruptionFragments=Mathf.CeilToInt(value.corruptionFragments*multiplier)};
    }

    public sealed class RestSession
    {
        private readonly RestNodeDefinition definition;private readonly MetaProgressionService progression;
        public bool Completed { get; private set; }
        public RestNodeChoice? Choice { get; private set; }
        public string Message { get; private set; }
        public RestSession(RestNodeDefinition definition,MetaProgressionService progression){this.definition=definition;this.progression=progression;}
        public bool Rest(IEnumerable<Combatant> players,RouteRunModifiers modifiers)
        {
            if(Completed)return false;float recovery=definition.restRecovery*(1+progression.EffectPower(MetaEffectKind.RestRecovery));foreach(var player in players){if(!player)continue;player.Health.HealingMultiplier=1;if(!player.Alive)player.Restore(Mathf.Min(.5f,recovery));else player.Health.Heal(player.Health.MaxHealth*recovery);}modifiers.ClearAtRest();return Finish(RestNodeChoice.Rest,"The party rests without returning to full strength.");
        }
        public bool TemperWeapon(Combatant player)
        {
            if(Completed||!player||!player.Weapon)return false;WeaponRarity next=Next(player.Weapon.rarity,definition.temperMaximum);if(next==player.Weapon.rarity){Message="This weapon cannot be tempered further here.";return false;}var copy=UnityEngine.Object.Instantiate(player.Weapon);copy.name=player.Weapon.name+" (Tempered)";copy.displayName=player.Weapon.displayName+" +";copy.rarity=next;copy.damage*=1.06f;player.Attacks.SetWeapon(copy);return Finish(RestNodeChoice.TemperWeapon,"Weapon tempered once for this node.");
        }
        public bool TemperArmor(Combatant player,ArmorSlot slot)
        {
            if(Completed||!player)return false;var armor=player.Equipment.InSlot(slot);if(!armor)return false;WeaponRarity next=Next(armor.rarity,definition.temperMaximum);if(next==armor.rarity){Message="This armor cannot be tempered further here.";return false;}var copy=UnityEngine.Object.Instantiate(armor);copy.name=armor.name+" (Tempered)";copy.displayName=armor.displayName+" +";copy.rarity=next;var stats=copy.statModifiers;stats.maxHealth+=.02f;copy.statModifiers=stats;player.Equipment.Equip(copy);return Finish(RestNodeChoice.TemperArmor,"Armor tempered once for this node.");
        }
        public bool Salvage(IEnumerable<Combatant> players)
        {
            if(Completed)return false;progression.Award(definition.salvageResources);foreach(var player in players)if(player&&player.Alive)player.Health.Heal(player.Health.MaxHealth*definition.salvageRecovery);return Finish(RestNodeChoice.Salvage,"Supplies salvaged; recovery remains modest.");
        }
        private bool Finish(RestNodeChoice choice,string message){Choice=choice;Message=message;Completed=true;return true;}
        private static WeaponRarity Next(WeaponRarity current,WeaponRarity maximum)=>(WeaponRarity)Mathf.Min((int)maximum,(int)current+1);
    }

    public sealed class EventSession
    {
        public EventDefinition Definition { get; }
        public ExpeditionEventChoice Choice { get; private set; }
        public bool Completed { get; private set; }
        public bool CombatEscalation=>Choice!=null&&Choice.escalationEncounter;
        public string Message { get; private set; }
        public EventSession(EventDefinition definition){Definition=definition;}
        public bool Choose(int index,MetaProgressionService progression,IEnumerable<Combatant> players)
        {
            if(Completed||!Definition||index<0||index>=Definition.choices.Length)return false;var choice=Definition.choices[index];if(!progression.TrySpendRun(choice.cost))return false;foreach(var player in players)if(player&&player.Alive){player.Health.SpendCurrentHealth(choice.currentHealthCost);player.Health.Heal(player.Health.MaxHealth*choice.recoveryFraction);}progression.Award(choice.reward);Choice=choice;Message=choice.outcomeText;if(!choice.escalationEncounter)Completed=true;return true;
        }
        public void CompleteCombat(){Completed=true;}
    }
}
