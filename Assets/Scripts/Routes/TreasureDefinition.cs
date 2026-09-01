using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class TreasureVariantDefinition
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public TreasureVariantKind kind;
        [Min(0)] public float weight=1;
        public RewardQuality rewardQuality=RewardQuality.Advanced;
        public EquipmentOfferKind offerKind=EquipmentOfferKind.Mixed;
        public ResourceWallet openCost=new ResourceWallet();
        [Range(0,.8f)] public float currentHealthCost;
        [Range(1,3)] public int maximumGreedRewards=1;
        public ResourceWallet bonusResources=new ResourceWallet();
        public EncounterDefinition mimicEncounter;
        public bool addsEliteToNextCombat;
        public bool addsVoidPressureToNextCombat;
    }

    [CreateAssetMenu(menuName="Ashbound/Routes/Treasure definition")]
    public sealed class TreasureDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        public TreasureVariantDefinition[] variants=Array.Empty<TreasureVariantDefinition>();
        [TextArea] public string informationRule;
    }
}
