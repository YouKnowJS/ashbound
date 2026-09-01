using System;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Expedition node")]
    public sealed class ExpeditionNodeDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ExpeditionNodeType nodeType;
        public NodeRiskRating risk;
        public NodeRewardCategory rewardCategory;
        public RewardQuality rewardQuality;
        public string[] outgoingConnections=Array.Empty<string>();
        public EncounterDefinition encounter;
        public CombatSpaceDefinition combatSpace;
        public TreasureDefinition treasure;
        public MerchantDefinition merchant;
        public RestNodeDefinition rest;
        public EventDefinition eventDefinition;
        public ChallengeDefinition challenge;
        public BossDefinition finalBoss;
        public BossRewardDefinition bossReward;
        public bool isTrueFinalBoss;
        public bool grantEquipment;
        public bool grantRelic;
        public ResourceWallet resourceReward=new ResourceWallet();
        public Vector3[] spawnPoints=Array.Empty<Vector3>();
        public string[] telemetryTags=Array.Empty<string>();
    }
}
