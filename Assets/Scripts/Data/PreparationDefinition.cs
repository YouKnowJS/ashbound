using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Meta/Preparation")]
    public sealed class PreparationDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public PreparationKind kind;
        public string requiredFacilityId;
        public int requiredFacilityLevel;
        public MetaEffectKind effect;
        public float power;
        public ElementTag element;
    }
}
