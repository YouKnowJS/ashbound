using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class ExpeditionEventChoice
    {
        public string id;
        public string displayName;
        [TextArea] public string outcomeText;
        public bool outcomeInitiallyHidden;
        public EventOutcomeKind outcome;
        public ResourceWallet cost=new ResourceWallet();
        public ResourceWallet reward=new ResourceWallet();
        [Range(0,.8f)] public float currentHealthCost;
        [Range(0,1)] public float recoveryFraction;
        public EncounterDefinition escalationEncounter;
        public RewardQuality equipmentQuality=RewardQuality.Advanced;
    }

    [CreateAssetMenu(menuName="Ashbound/Routes/Event definition")]
    public sealed class EventDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public ExpeditionEventChoice[] choices=Array.Empty<ExpeditionEventChoice>();
        public string[] loreFragments=Array.Empty<string>();
    }
}
