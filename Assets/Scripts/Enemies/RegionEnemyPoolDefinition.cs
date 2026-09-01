using System;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Enemies/Region ecology pool")]
    public sealed class RegionEnemyPoolDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string ecologyIntent;
        public ElementTag[] favoredElements=Array.Empty<ElementTag>();
        public EnemyDefinition[] commonEnemies=Array.Empty<EnemyDefinition>();
        public EnemyDefinition[] hardEnemies=Array.Empty<EnemyDefinition>();
        public EnemyDefinition[] eliteCandidates=Array.Empty<EnemyDefinition>();
        public EncounterDefinition[] encounterPool=Array.Empty<EncounterDefinition>();
    }
}
