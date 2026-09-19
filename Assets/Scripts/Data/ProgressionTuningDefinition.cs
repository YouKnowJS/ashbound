using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public struct RarityWeights
    {
        [Min(0)] public float common, advanced, rare, epic, legendary;
        public float Get(WeaponRarity rarity)=>rarity==WeaponRarity.Common?common:rarity==WeaponRarity.Advanced?advanced:rarity==WeaponRarity.Rare?rare:rarity==WeaponRarity.Epic?epic:legendary;
    }

    [Serializable]
    public sealed class RewardRarityTuning
    {
        public RarityWeights early = new RarityWeights{common=62,advanced=30,rare=8,epic=0,legendary=0};
        public RarityWeights mid = new RarityWeights{common=25,advanced=45,rare=32,epic=8,legendary=0};
        public RarityWeights late = new RarityWeights{common=5,advanced=18,rare=55,epic=21,legendary=1};
        public RarityWeights boss = new RarityWeights{common=0,advanced=5,rare=57,epic=35,legendary=3};
        [Range(0,.5f)] public float regionRareStep=.08f;
        [Range(0,.5f)] public float riskRareStep=.12f;
        [Range(1,3)] public float eliteRareMultiplier=1.45f;
        [Range(1,3)] public float qualityStepMultiplier=1.18f;
        [Range(0,3)] public float metaRareMultiplier=1;
        [Range(0,3)] public float preparationRareMultiplier=1.25f;
    }

    [Serializable]
    public sealed class ThreatBudgetTuning
    {
        [Range(1,2)] public float baseDensityMultiplier=1.2f;
        [Range(0,1)] public float depthMultiplier=.35f;
        [Range(0,1)] public float riskStep=.12f;
        [Range(0,1)] public float eliteBonus=.28f;
        [Range(0,1)] public float playerBonusPerExtraPlayer=.48f;
        [Min(.1f)] public float reinforcementDelay=.8f;
        [Min(1)] public int maximumEnemies=18;
    }

    [Serializable]
    public struct EncounterResourceReward
    {
        public ExpeditionNodeType nodeType;
        public ResourceWallet resources;
        public RewardQuality minimumQuality;
        public RewardQuality maximumQuality;
    }

    [Serializable]
    public struct RestOptionDefinition
    {
        public RestOptionKind kind;
        public string displayName;
        [TextArea] public string description;
        public float power;
    }

    [CreateAssetMenu(menuName = "Ashbound/Meta/Progression Tuning")]
    public sealed class ProgressionTuningDefinition : ScriptableObject
    {
        public RetentionRules retention = new RetentionRules();
        [Range(0, .1f)] public float permanentHealthCap = .08f;
        [Range(0, .25f)] public float rarityWeightCap = .15f;
        [Range(0, .35f)] public float elementalBiasCap = .2f;
        public int targetMajorRegions = 5;
        public int targetFinalAreas = 1;
        public int targetNodesPerRegionMin = 8;
        public int targetNodesPerRegionMax = 10;
        public float targetExperiencedRunMinutesMin = 30;
        public float targetExperiencedRunMinutesMax = 45;
        public EncounterResourceReward[] rewards = Array.Empty<EncounterResourceReward>();
        public RestOptionDefinition[] restOptions = Array.Empty<RestOptionDefinition>();
        public RewardRarityTuning rarity = new RewardRarityTuning();
        public ThreatBudgetTuning threatBudget = new ThreatBudgetTuning();
    }
}
