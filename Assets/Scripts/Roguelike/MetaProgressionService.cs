using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public readonly struct ResourceSettlement
    {
        public readonly ResourceWallet Collected, Retained, Lost;
        public readonly bool Completed;
        public ResourceSettlement(ResourceWallet collected, ResourceWallet retained, bool completed) { Collected=collected;Retained=retained;Lost=collected.Minus(retained);Completed=completed; }
    }

    public sealed class MetaProgressionService
    {
        private readonly PrototypeCatalog catalog;
        private readonly MetaProgressionStore store;
        public MetaProgressionProfile Profile { get; private set; }
        public ResourceWallet RunResources { get; private set; } = new ResourceWallet();
        public ResourceWallet MaterialsSpentSinceRun { get; private set; } = new ResourceWallet();
        public ResourceSettlement? LastSettlement { get; private set; }
        public PreparationDefinition ActivePreparation { get; private set; }
        public bool RunOpen { get; private set; }
        public string SavePath => store.SavePath;
        public string LastSaveError => store.LastError;
        public event Action ProfileChanged;
        public event Action<ExpeditionResource,int> RunResourceAdded;
        public event Action<HubFacilityDefinition,int> FacilityLevelChanged;

        public MetaProgressionService(PrototypeCatalog catalog, string savePath = null)
        {
            this.catalog=catalog;store=new MetaProgressionStore(savePath);Profile=store.LoadOrCreate();EnsureInitialUnlocks();store.Save(Profile);
        }
        public void BeginExpedition()
        {
            RunResources=new ResourceWallet();LastSettlement=null;RunOpen=true;Profile.lifetime.expeditionsStarted++;
            ActivePreparation=catalog.preparations?.FirstOrDefault(x=>x&&x.id==Profile.selectedPreparation&&PreparationAvailable(x));
            int startingAsh=Mathf.RoundToInt(EffectPower(MetaEffectKind.StartingAsh));if(startingAsh>0)Award(ExpeditionResource.Ash,startingAsh);
            store.Save(Profile);
        }
        public void Award(ExpeditionResource resource,int amount)
        {
            if(!RunOpen||amount<=0)return;RunResources.Add(resource,amount);RunResourceAdded?.Invoke(resource,amount);
        }
        public void Award(ResourceWallet value){if(!RunOpen||value==null)return;foreach(ExpeditionResource resource in Enum.GetValues(typeof(ExpeditionResource))){int amount=value.Get(resource);if(amount>0)Award(resource,amount);}}
        public bool TrySpendRun(ResourceWallet cost){if(!RunOpen||cost==null)return false;return RunResources.TrySpend(cost);}
        public ResourceSettlement ResolveRun(bool completed,bool bossMilestone=false,bool abandoned=false)
        {
            if(!RunOpen)return LastSettlement??new ResourceSettlement(new ResourceWallet(),new ResourceWallet(),completed);
            var collected=RunResources.Copy();var retained=abandoned?new ResourceWallet():ProgressionEconomy.Retained(collected,completed,bossMilestone,catalog.progressionTuning.retention,EffectPower(MetaEffectKind.FailureRetention));
            Profile.currencies.Add(retained);Profile.lifetime.resourcesRecovered+=retained.ash+retained.emberShards+retained.ancientAlloy+retained.corruptionFragments;
            if(completed)Profile.lifetime.expeditionsCompleted++;else Profile.lifetime.expeditionsFailed++;
            LastSettlement=new ResourceSettlement(collected,retained,completed);RunOpen=false;store.Save(Profile);ProfileChanged?.Invoke();return LastSettlement.Value;
        }
        public bool TryUpgrade(HubFacilityDefinition definition,out string reason)
        {
            reason="";if(!definition){reason="Missing facility definition.";return false;}var progress=Profile.Facility(definition.id);
            if(!progress.unlocked){reason="Facility is locked.";return false;}if(progress.level>=definition.MaxLevel){reason="Maximum level reached.";return false;}
            var tier=definition.tiers[progress.level];
            if(!string.IsNullOrEmpty(tier.prerequisiteFacilityId)&&Profile.Facility(tier.prerequisiteFacilityId).level<tier.prerequisiteLevel){reason="Prerequisite not met.";return false;}
            if(!Profile.currencies.TrySpend(tier.cost)){reason="Insufficient resources.";return false;}
            MaterialsSpentSinceRun.Add(tier.cost);progress.level++;ApplyUnlocks(tier);Profile.unlockedExpeditionUpgrades.Add(tier.id);Profile.Normalize();store.Save(Profile);FacilityLevelChanged?.Invoke(definition,progress.level);ProfileChanged?.Invoke();return true;
        }
        public float EffectPower(MetaEffectKind effect)
        {
            float total=0;foreach(var definition in catalog.facilities??Array.Empty<HubFacilityDefinition>()){if(!definition)continue;int level=Math.Min(Profile.Facility(definition.id).level,definition.MaxLevel);for(int i=0;i<level;i++)if(definition.tiers[i].effect==effect)total+=definition.tiers[i].power;}
            if(ActivePreparation&&ActivePreparation.effect==effect)total+=ActivePreparation.power;
            if(effect==MetaEffectKind.MaxHealth)return Mathf.Min(total,catalog.progressionTuning.permanentHealthCap);
            if(effect==MetaEffectKind.RareWeight)return Mathf.Min(total,catalog.progressionTuning.rarityWeightCap);
            if(effect==MetaEffectKind.ElementBias)return Mathf.Min(total,catalog.progressionTuning.elementalBiasCap);
            return total;
        }
        public bool PreparationAvailable(PreparationDefinition definition)=>definition&&Profile.Facility(definition.requiredFacilityId).level>=definition.requiredFacilityLevel;
        public bool SelectPreparation(PreparationDefinition definition)
        {
            if(!PreparationAvailable(definition))return false;Profile.selectedPreparation=definition.id;store.Save(Profile);ProfileChanged?.Invoke();return true;
        }
        public void ApplyRunStart(Combatant player){player.SetMetaHealthBonus(EffectPower(MetaEffectKind.MaxHealth));}
        public IEnumerable<WeaponDefinition> UnlockedWeapons()=>catalog.weapons.Where(x=>x&&Profile.unlockedWeapons.Contains(x.id));
        public IEnumerable<ArmorDefinition> UnlockedArmor()=>catalog.armor.Where(x=>x&&Profile.unlockedArmor.Contains(x.id));
        public ElementTag PreferredElement=>ActivePreparation&&ActivePreparation.element!=ElementTag.None?ActivePreparation.element:ElementTag.None;
        public void RecordLore(string id){if(string.IsNullOrEmpty(id)||Profile.discoveredLore.Contains(id))return;Profile.discoveredLore.Add(id);store.Save(Profile);ProfileChanged?.Invoke();}
        public void RecordBoss(string id){if(!Profile.defeatedBosses.Contains(id))Profile.defeatedBosses.Add(id);Profile.lifetime.bossesDefeated++;store.Save(Profile);}
        public void RecordProgress(int region,int encounter){Profile.lifetime.highestRegionReached=Math.Max(Profile.lifetime.highestRegionReached,region);Profile.lifetime.highestEncounterReached=Math.Max(Profile.lifetime.highestEncounterReached,encounter);}
        public void RecordEquipment(bool dismantled){if(dismantled)Profile.lifetime.equipmentDismantled++;else Profile.lifetime.equipmentAcquired++;}
        public void Save()=>store.Save(Profile);
        public void ResetProfile(){Profile=store.Reset();RunResources=new ResourceWallet();RunOpen=false;LastSettlement=null;EnsureInitialUnlocks();store.Save(Profile);ProfileChanged?.Invoke();}
        public void DebugAdd(ExpeditionResource resource,int amount){Profile.currencies.Add(resource,amount);store.Save(Profile);ProfileChanged?.Invoke();}
        public void DebugSet(ExpeditionResource resource,int amount){Profile.currencies.Set(resource,amount);store.Save(Profile);ProfileChanged?.Invoke();}
        public void DebugZeroCurrencies(){Profile.currencies=new ResourceWallet();store.Save(Profile);ProfileChanged?.Invoke();}
        public void DebugSetFacility(HubFacilityDefinition definition,int level){var p=Profile.Facility(definition.id);p.unlocked=true;p.level=Mathf.Clamp(level,0,definition.MaxLevel);for(int i=0;i<p.level;i++)ApplyUnlocks(definition.tiers[i]);Profile.Normalize();store.Save(Profile);FacilityLevelChanged?.Invoke(definition,p.level);ProfileChanged?.Invoke();}
        public void DebugUnlockAll()
        {
            Profile.unlockedWeapons=catalog.weapons.Where(x=>x).Select(x=>x.id).ToList();Profile.unlockedWeaponSkills=catalog.weaponSkills.Where(x=>x).Select(x=>x.id).ToList();Profile.unlockedRelics=catalog.items.Where(x=>x).Select(x=>x.id).ToList();Profile.unlockedArmor=catalog.armor.Where(x=>x).Select(x=>x.id).ToList();Profile.unlockedArmorSets=catalog.armorSets.Where(x=>x).Select(x=>x.id).ToList();store.Save(Profile);ProfileChanged?.Invoke();
        }
        public ResourceSettlement DebugSimulateOutcome(bool completed)
        {
            bool previous=RunOpen;var previousResources=RunResources;RunOpen=true;RunResources=new ResourceWallet{ash=40,emberShards=10,ancientAlloy=3,corruptionFragments=1};var result=ResolveRun(completed,completed);if(previous){RunOpen=true;RunResources=previousResources;}return result;
        }
        public ResourceWallet ConsumeMaterialsSpent(){var value=MaterialsSpentSinceRun.Copy();MaterialsSpentSinceRun=new ResourceWallet();return value;}
        private void ApplyUnlocks(FacilityUpgradeTier tier)
        {
            AddUnique(Profile.unlockedWeapons,tier.unlockWeaponIds);AddUnique(Profile.unlockedWeaponSkills,tier.unlockWeaponSkillIds);AddUnique(Profile.unlockedRelics,tier.unlockRelicIds);
            foreach(string setId in tier.unlockArmorSetIds??Array.Empty<string>()){if(!Profile.unlockedArmorSets.Contains(setId))Profile.unlockedArmorSets.Add(setId);AddUnique(Profile.unlockedArmor,catalog.armor.Where(x=>x&&x.set&&x.set.id==setId).Select(x=>x.id));}
        }
        private void EnsureInitialUnlocks(){var defaults=MetaProgressionProfile.CreateDefault(Profile.profileId);AddUnique(Profile.unlockedWeapons,defaults.unlockedWeapons);AddUnique(Profile.unlockedRelics,defaults.unlockedRelics);AddUnique(Profile.unlockedArmor,defaults.unlockedArmor);AddUnique(Profile.unlockedArmorSets,defaults.unlockedArmorSets);Profile.Normalize();}
        private static void AddUnique(List<string> target,IEnumerable<string> values){if(values==null)return;foreach(string value in values)if(!string.IsNullOrEmpty(value)&&!target.Contains(value))target.Add(value);}
    }
}
