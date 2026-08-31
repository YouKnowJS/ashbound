using System;
using UnityEngine;

namespace Ashbound
{
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
    }
}
