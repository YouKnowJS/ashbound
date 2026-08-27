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
    }
}
