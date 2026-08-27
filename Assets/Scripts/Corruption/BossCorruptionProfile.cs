using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Boss corruption profile")]
    public sealed class BossCorruptionProfile : ScriptableObject
    {
        public string id = "ash";
        public string displayName = "Ashbound";
        public float healthMultiplier = 1.25f;
        public float damageMultiplier = 1.10f;
        public float movementMultiplier = 1.13f;
        public bool overrideAbilityWithFireBurst = true;
        public bool attacksBurn = true;
        public bool dashLeavesFire = true;
        public float burnDamagePerSecond = 4;
        public float burnDuration = 3;
        public float burstDamage = 34;
        public float burstRadius = 3.5f;
        public float burstCooldown = 6;
        public GameObject vfxPrefab;
        public BuildTag[] corruptionTags = { BuildTag.Fire, BuildTag.Curse };
        public Color tint = new Color(.85f, .28f, 1f);
    }
}
