using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Armor Set")]
    public sealed class ArmorSetDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public ElementTag element;
        public SetBonusTier twoPiece;
        public SetBonusTier fourPiece;
    }
}
