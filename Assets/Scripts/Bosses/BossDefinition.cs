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
        [Header("Large-body navigation")]
        [Min(.4f)] public float navigationRadius = 1.2f;
        [Min(0)] public float minimumObstacleClearance = .35f;
        [Range(.25f,1)] public float preferredMovementZone = .78f;
        public string[] allowedArenaSections = System.Array.Empty<string>();
        [Min(.5f)] public float stuckDetectionSeconds = 1.5f;
        [Min(.01f)] public float minimumStuckDisplacement = .12f;
        [Min(1)] public int recoveryAttemptsBeforeReposition = 3;
        public BossCorruptionProfile corruption;
    }
}
