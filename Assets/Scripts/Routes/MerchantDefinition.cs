using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Merchant definition")]
    public sealed class MerchantDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Range(2,6)] public int baseStock=3;
        [Range(0,4)] public int maximumRerolls=2;
        public ResourceWallet rerollCost=new ResourceWallet{ash=8};
        [Range(.5f,2)] public float priceMultiplier=1;
        [Range(.05f,1)] public float recoveryFraction=.3f;
        public ResourceWallet recoveryPrice=new ResourceWallet{ash=10};
    }
}
