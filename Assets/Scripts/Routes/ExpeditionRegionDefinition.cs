using System;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName="Ashbound/Routes/Expedition region")]
    public sealed class ExpeditionRegionDefinition:ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string regionIntent;
        public ExpeditionRouteGraphDefinition[] graphVariants=Array.Empty<ExpeditionRouteGraphDefinition>();
        [Min(1)] public int eventualRegionCount=5;
        [Min(1)] public int eventualFinalAreaCount=1;
        [Min(1)] public int targetNodesPerRegionMin=8;
        [Min(1)] public int targetNodesPerRegionMax=10;
    }
}
