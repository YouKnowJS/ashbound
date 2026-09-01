using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Enemies/Enemy definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string combatRead;
        public EnemyKind legacyKind;
        public bool legacyFallback;
        public EnemyRole role;
        [Min(1)] public float maxHealth = 60;
        [Min(.1f)] public float movementSpeed = 3.5f;
        [Min(1)] public float attackDamage = 10;
        [Min(.2f)] public float attackCooldown = 1.5f;
        [Min(.5f)] public float preferredDistance = 2;
        public ElementTag element;
        [Range(0, .9f)] public float statusResistance;
        [Range(0, .9f)] public float staggerResistance;
        public EnemyTargetingStyle targeting = EnemyTargetingStyle.Nearest;
        public EnemyMovementStyle movement = EnemyMovementStyle.Advance;
        public EnemyAttackBehavior attack = EnemyAttackBehavior.Melee;
        public SpawnPresentation spawnPresentation = SpawnPresentation.Edge;
        public EnemyRewardTier rewardTier;
        public bool elite;
        public float shield;
        public float visualScale = 1;
        public Color baseTint = new Color(.68f, .24f, .18f);
        [TextArea] public string telegraphLanguage;
        [TextArea] public string rewardHook;
        public GameObject prefab;
        public GameObject telegraphPrefab;
        public GameObject attackVfxPrefab;
        public AudioClip attackAudio;
    }
}
