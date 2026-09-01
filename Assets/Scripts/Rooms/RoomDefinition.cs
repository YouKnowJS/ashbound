using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public sealed class EnemyWave
    {
        public EnemyKind[] enemies;
        public EncounterDefinition encounter;
    }

    [CreateAssetMenu(menuName = "Ashbound/Room")]
    public sealed class RoomDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public bool isBoss;
        public Vector3[] spawnPoints;
        public EnemyWave[] waves;
        public CombatSpaceDefinition combatSpace;
        public LoreEntry fragment;
        [Range(.5f, 3f)] public float enemyHealthMultiplier = 1;
        [Min(30)] public float targetEncounterSeconds = 150;
    }
}
