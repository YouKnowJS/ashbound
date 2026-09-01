using System;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Route graph")]
    public sealed class ExpeditionRouteGraphDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        public string startNodeId;
        public string bossNodeId;
        public ExpeditionNodeDefinition[] nodes=Array.Empty<ExpeditionNodeDefinition>();
        public VoteTieBehavior tieBehavior=VoteTieBehavior.HostBreaksTie;
        [Min(1)] public int minimumCombatNodes=3;
        [Min(0)] public int maximumRestNodes=2;
        [Min(0)] public int maximumMerchantNodes=2;
        [Min(1)] public int maximumRepeatedType=2;
    }
}
