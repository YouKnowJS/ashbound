using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Boss reward")]
    public sealed class BossRewardDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        public ResourceWallet resources=new ResourceWallet();
        public RewardQuality equipmentQuality=RewardQuality.Epic;
        public bool grantEquipment=true;
        public bool grantRelic;
        [TextArea] public string futureRegionTransitionHook;
    }
}
