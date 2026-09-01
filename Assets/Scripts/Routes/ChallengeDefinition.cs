using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Challenge definition")]
    public sealed class ChallengeDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ChallengeKind kind=ChallengeKind.TimedElimination;
        [Min(5)] public float duration=45;
        public EncounterDefinition encounter;
        public ResourceWallet successReward=new ResourceWallet();
        public ResourceWallet consolationReward=new ResourceWallet();
        public RewardQuality successQuality=RewardQuality.Rare;
        public bool noHealing;
        public bool failureEndsRun;
    }
}
