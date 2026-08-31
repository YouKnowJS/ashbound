using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public struct FacilityUpgradeTier
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ResourceWallet cost;
        public string prerequisiteFacilityId;
        public int prerequisiteLevel;
        public MetaEffectKind effect;
        public float power;
        public string[] unlockWeaponIds;
        public string[] unlockWeaponSkillIds;
        public string[] unlockRelicIds;
        public string[] unlockArmorSetIds;
        public string[] unlockPreparationIds;
    }

    [CreateAssetMenu(menuName = "Ashbound/Meta/Hub Facility")]
    public sealed class HubFacilityDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public HubFacilityKind kind;
        public bool initiallyUnlocked = true;
        public FacilityUpgradeTier[] tiers = Array.Empty<FacilityUpgradeTier>();
        public int MaxLevel => tiers?.Length ?? 0;
    }
}
