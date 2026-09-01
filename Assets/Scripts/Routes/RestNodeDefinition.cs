using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Rest definition")]
    public sealed class RestNodeDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Range(.05f,1)] public float restRecovery=.35f;
        [Range(.05f,1)] public float salvageRecovery=.1f;
        public ResourceWallet salvageResources=new ResourceWallet{ash=8};
        public WeaponRarity temperMaximum=WeaponRarity.Epic;
    }
}
