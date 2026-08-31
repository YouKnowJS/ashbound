using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id = "wayfarer-edge";
        public string displayName = "Wayfarer's Edge";
        public float damage = 24;
        public float attackInterval = .34f;
        public float reach = 2.7f;
        public float arcDegrees = 115;
        public float knockback = 3;
        public WeaponFamily family = WeaponFamily.Sword;
        public WeaponMechanic mechanic;
        [Range(.35f, 1.25f)] public float attackMoveMultiplier = .9f;
        public float criticalChanceModifier;
        public float criticalDamageModifier;
        public float projectileSpeed = 18;
        public float projectileLifetime = 1.2f;
        public int comboThreshold = 4;
        public float mechanicPower = .15f;
        public BuildTag[] tags = System.Array.Empty<BuildTag>();
        public GameObject trailPrefab;
        public GameObject impactPrefab;
        public AudioClip lightAttackSound;
        public AudioClip heavyAttackSound;
        public bool IsRanged => family == WeaponFamily.Bow || family == WeaponFamily.Staff;
        public bool IsHeavy => family == WeaponFamily.Greatsword;
    }
}
