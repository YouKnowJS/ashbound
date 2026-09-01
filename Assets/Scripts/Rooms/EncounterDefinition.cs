using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class EnemySpawnGroup
    {
        public string id;
        public EnemyDefinition enemy;
        [Min(1)] public int count = 1;
        [Min(0)] public float startDelay;
        [Min(0)] public float spawnInterval;
        public SpawnPresentation presentation = SpawnPresentation.Edge;
        public bool reinforcement;
    }

    [CreateAssetMenu(menuName = "Ashbound/Rooms/Encounter definition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string intent;
        public EnemySpawnGroup[] groups = Array.Empty<EnemySpawnGroup>();
        public EncounterDifficulty difficulty;
        public EncounterRiskTier riskTier;
        public ElementTag[] elementalPressure = Array.Empty<ElementTag>();
        public EnemyRewardTier rewardTier;
        public CombatSpaceCategory requiredArenaSize = CombatSpaceCategory.Medium;
        [Min(8)] public float preferredSpaceDiameter = 24;
        [Min(15)] public float targetDurationSeconds = 120;
        public bool allowsElite;
        [TextArea] public string compositionNotes;
    }
}
