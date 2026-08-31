using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public enum ExpeditionResource { Ash, EmberShards, AncientAlloy, CorruptionFragments }
    public enum HubFacilityKind { ExpeditionTable, Forge, Quartermaster, Infirmary, Archive, ResearchStation }
    public enum PreparationKind { None, HuntersPreparation, FrostResearch, CartographersNotes, MerchantContract, FieldSupplies }
    public enum MetaEffectKind
    {
        None, RouteReveal, RareWeight, MerchantStock, MerchantChance, RerollDiscount, SalvageYield,
        StartingAsh, RestRecovery, FailureRetention, EmergencyRest, MaxHealth, RelicReroll,
        EquipmentAppraisal, ElementBias, ArchiveCapacity
    }
    public enum ExpeditionNodeType { NormalCombat, HardCombat, Elite, Challenge, Merchant, Event, Rest, Treasure, Secret, Boss }
    public enum RewardQuality { Common, Advanced, Rare, Epic, Legendary }
    public enum RestOptionKind { Rest, Temper, Salvage, Prepare }

    [Serializable]
    public sealed class ResourceWallet
    {
        public int ash, emberShards, ancientAlloy, corruptionFragments;
        public int Get(ExpeditionResource resource) => resource == ExpeditionResource.Ash ? ash : resource == ExpeditionResource.EmberShards ? emberShards : resource == ExpeditionResource.AncientAlloy ? ancientAlloy : corruptionFragments;
        public void Set(ExpeditionResource resource, int value)
        {
            value = Math.Max(0, value);
            if (resource == ExpeditionResource.Ash) ash = value;
            else if (resource == ExpeditionResource.EmberShards) emberShards = value;
            else if (resource == ExpeditionResource.AncientAlloy) ancientAlloy = value;
            else corruptionFragments = value;
        }
        public void Add(ExpeditionResource resource, int amount) => Set(resource, Get(resource) + Math.Max(0, amount));
        public void Add(ResourceWallet other) { if (other == null) return; ash += Math.Max(0, other.ash); emberShards += Math.Max(0, other.emberShards); ancientAlloy += Math.Max(0, other.ancientAlloy); corruptionFragments += Math.Max(0, other.corruptionFragments); }
        public bool CanAfford(ResourceWallet cost) => cost != null && ash >= cost.ash && emberShards >= cost.emberShards && ancientAlloy >= cost.ancientAlloy && corruptionFragments >= cost.corruptionFragments;
        public bool TrySpend(ResourceWallet cost)
        {
            if (!CanAfford(cost)) return false;
            ash -= cost.ash; emberShards -= cost.emberShards; ancientAlloy -= cost.ancientAlloy; corruptionFragments -= cost.corruptionFragments; return true;
        }
        public ResourceWallet Copy() => new ResourceWallet { ash=ash, emberShards=emberShards, ancientAlloy=ancientAlloy, corruptionFragments=corruptionFragments };
        public ResourceWallet Minus(ResourceWallet other) => new ResourceWallet { ash=Math.Max(0,ash-(other?.ash??0)), emberShards=Math.Max(0,emberShards-(other?.emberShards??0)), ancientAlloy=Math.Max(0,ancientAlloy-(other?.ancientAlloy??0)), corruptionFragments=Math.Max(0,corruptionFragments-(other?.corruptionFragments??0)) };
        public bool Empty => ash == 0 && emberShards == 0 && ancientAlloy == 0 && corruptionFragments == 0;
        public override string ToString() => $"Ash {ash}  ·  Ember {emberShards}  ·  Alloy {ancientAlloy}  ·  Corruption {corruptionFragments}";
    }

    [Serializable]
    public sealed class FacilityProgress
    {
        public string facilityId;
        public bool unlocked = true;
        public int level;
    }

    [Serializable]
    public sealed class LifetimeExpeditionStatistics
    {
        public int expeditionsStarted, expeditionsCompleted, expeditionsFailed, bossesDefeated, highestRegionReached, highestEncounterReached;
        public int equipmentAcquired, equipmentDismantled, resourcesRecovered;
    }

    [Serializable]
    public sealed class MetaProgressionProfile
    {
        public const int CurrentVersion = 2;
        public int schemaVersion = CurrentVersion;
        public string profileId;
        public ResourceWallet currencies = new ResourceWallet();
        public List<FacilityProgress> facilities = new List<FacilityProgress>();
        public List<string> unlockedWeapons = new List<string>();
        public List<string> unlockedWeaponSkills = new List<string>();
        public List<string> unlockedRelics = new List<string>();
        public List<string> unlockedArmor = new List<string>();
        public List<string> unlockedArmorSets = new List<string>();
        public List<string> unlockedExpeditionUpgrades = new List<string>();
        public List<string> discoveredLore = new List<string>();
        public List<string> defeatedBosses = new List<string>();
        public LifetimeExpeditionStatistics lifetime = new LifetimeExpeditionStatistics();
        public string selectedPreparation = PreparationKind.None.ToString();

        public static MetaProgressionProfile CreateDefault(string profileId = null)
        {
            var profile = new MetaProgressionProfile { profileId = string.IsNullOrWhiteSpace(profileId) ? Guid.NewGuid().ToString("N") : profileId };
            profile.unlockedWeapons.AddRange(new[] { "wayfarer-edge", "long-reach", "bell-cleaver", "moon-shear", "twin-embers", "vault-bow", "ashen-staff", "rift-brand" });
            profile.unlockedRelics.AddRange(new[] { "glass-sigil", "echo-edge", "quicksilver", "thorn-rune", "bloodglass", "rupture", "storm-coil", "forked-heart" });
            profile.unlockedArmorSet("ashwalker", new[] { "head", "chest", "gloves", "boots" });
            foreach (HubFacilityKind facility in Enum.GetValues(typeof(HubFacilityKind))) profile.facilities.Add(new FacilityProgress { facilityId=FacilityId(facility), unlocked=true });
            return profile;
        }
        public void Normalize()
        {
            schemaVersion = CurrentVersion;
            if (string.IsNullOrWhiteSpace(profileId)) profileId = Guid.NewGuid().ToString("N");
            currencies ??= new ResourceWallet(); facilities ??= new List<FacilityProgress>(); unlockedWeapons ??= new List<string>(); unlockedWeaponSkills ??= new List<string>();
            unlockedRelics ??= new List<string>(); unlockedArmor ??= new List<string>(); unlockedArmorSets ??= new List<string>(); unlockedExpeditionUpgrades ??= new List<string>();
            discoveredLore ??= new List<string>(); defeatedBosses ??= new List<string>(); lifetime ??= new LifetimeExpeditionStatistics();
            foreach (HubFacilityKind facility in Enum.GetValues(typeof(HubFacilityKind))) if (!facilities.Any(x => x.facilityId == FacilityId(facility))) facilities.Add(new FacilityProgress { facilityId=FacilityId(facility), unlocked=true });
            if (string.IsNullOrWhiteSpace(selectedPreparation)) selectedPreparation = PreparationKind.None.ToString();
            Deduplicate(unlockedWeapons); Deduplicate(unlockedWeaponSkills); Deduplicate(unlockedRelics); Deduplicate(unlockedArmor); Deduplicate(unlockedArmorSets); Deduplicate(unlockedExpeditionUpgrades); Deduplicate(discoveredLore); Deduplicate(defeatedBosses);
        }
        public FacilityProgress Facility(string id)
        {
            var value = facilities.FirstOrDefault(x => x.facilityId == id);
            if (value != null) return value;
            value = new FacilityProgress { facilityId=id }; facilities.Add(value); return value;
        }
        public void unlockedArmorSet(string setId, IEnumerable<string> slots)
        {
            if (!unlockedArmorSets.Contains(setId)) unlockedArmorSets.Add(setId);
            foreach (string slot in slots) if (!unlockedArmor.Contains(setId + "-" + slot)) unlockedArmor.Add(setId + "-" + slot);
        }
        public static string FacilityId(HubFacilityKind kind) => kind == HubFacilityKind.ResearchStation ? "research-station" : string.Concat(kind.ToString().Select((c,i) => char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        private static void Deduplicate(List<string> values) { var clean=values.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().ToArray();values.Clear();values.AddRange(clean); }
    }

    [Serializable]
    public sealed class RetentionRules
    {
        public float ashFailure = .7f, emberFailure = .5f, alloyFailure = .25f, corruptionFailure;
        public float bossMilestoneBonus = .15f;
        public float maxFailureBonus = .15f;
    }

    public static class ProgressionEconomy
    {
        public static ResourceWallet Retained(ResourceWallet collected, bool completed, bool bossMilestone, RetentionRules rules, float upgradeBonus = 0)
        {
            if (collected == null) return new ResourceWallet();
            if (completed) return collected.Copy();
            rules ??= new RetentionRules();
            float bonus = Math.Min(rules.maxFailureBonus, Math.Max(0, upgradeBonus)) + (bossMilestone ? rules.bossMilestoneBonus : 0);
            int Keep(int amount, float rate) => Math.Max(0, (int)Math.Floor(amount * Math.Min(1, Math.Max(0, rate + bonus)) + .0001));
            return new ResourceWallet { ash=Keep(collected.ash,rules.ashFailure), emberShards=Keep(collected.emberShards,rules.emberFailure), ancientAlloy=Keep(collected.ancientAlloy,rules.alloyFailure), corruptionFragments=Keep(collected.corruptionFragments,rules.corruptionFailure) };
        }
        public static ResourceWallet Salvage(WeaponRarity rarity, bool weapon, float yieldBonus = 0)
        {
            float multiplier = Math.Max(1, 1 + yieldBonus);
            int ash = rarity == WeaponRarity.Common ? 4 : rarity == WeaponRarity.Advanced ? 7 : rarity == WeaponRarity.Rare ? 10 : rarity == WeaponRarity.Epic ? 12 : 16;
            int ember = rarity >= WeaponRarity.Rare ? (rarity == WeaponRarity.Rare ? 2 : rarity == WeaponRarity.Epic ? 5 : 8) : 0;
            int alloy = rarity >= WeaponRarity.Epic ? (rarity == WeaponRarity.Epic ? 1 : 3) : 0;
            return new ResourceWallet { ash=(int)Math.Floor(ash*multiplier), emberShards=(int)Math.Floor(ember*multiplier), ancientAlloy=(int)Math.Floor(alloy*multiplier)+(weapon&&rarity==WeaponRarity.Legendary?1:0) };
        }
        public static ResourceWallet MerchantPrice(WeaponRarity rarity, bool weapon)
        {
            var salvage=Salvage(rarity,weapon); return new ResourceWallet { ash=salvage.ash*3+8, emberShards=salvage.emberShards*2+1, ancientAlloy=salvage.ancientAlloy*2 };
        }
        public static bool MerchantAlwaysExceedsSalvage(WeaponRarity rarity, bool weapon)
        {
            var salvage=Salvage(rarity,weapon);var price=MerchantPrice(rarity,weapon);
            return price.ash>salvage.ash && price.emberShards>=salvage.emberShards && price.ancientAlloy>=salvage.ancientAlloy;
        }
    }
}
