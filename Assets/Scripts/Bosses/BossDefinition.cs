using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Boss")]
    public sealed class BossDefinition : ScriptableObject
    {
        public string id = "cinder-regent";
        public string displayName = "The Cinder Regent";
        [TextArea] public string description = "The last keeper still tends a fire with no fuel.";
        public float health = 900;
        public float secondPhaseThreshold = .4f;
        public float areaDamage = 25;
        public float areaRadius = 3.1f;
        public float telegraphDuration = 1.1f;
        public BossCorruptionProfile corruption;
    }
}
