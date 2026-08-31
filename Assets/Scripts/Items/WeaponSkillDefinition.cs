using System;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Weapon Skill")]
    public sealed class WeaponSkillDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public float cooldown = 7;
        public WeaponRarity minimumRarity = WeaponRarity.Rare;
        public ElementTag[] elements = Array.Empty<ElementTag>();
        public BuildTag[] tags = Array.Empty<BuildTag>();
        public SkillDelivery delivery;
        public float damage = 35;
        public DamageKind damageKind = DamageKind.Ability;
        public float radius = 3;
        public float duration = 2;
        public float movementDistance = 5;
        public int projectileCount = 1;
        public float projectileSpeed = 15;
        public StatusPayload[] statuses = Array.Empty<StatusPayload>();
        public TriggeredEffect[] followUpEffects = Array.Empty<TriggeredEffect>();
        public GameObject vfxPrefab;
        public AudioClip audioClip;
    }
}
