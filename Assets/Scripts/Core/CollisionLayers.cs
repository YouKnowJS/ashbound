using UnityEngine;

namespace Ashbound
{
    public static class CollisionLayers
    {
        public const int Actor=8;
        public const int InternalObstacle=9;
        public const int WorldBoundary=10;
        public const string InternalObstacleName="InternalObstacle";
        public const string WorldBoundaryName="WorldBoundary";
        public static int InternalObstacleMask=>1<<InternalObstacle;
        public static int WorldBoundaryMask=>1<<WorldBoundary;
        public static int DashEndpointMask=>InternalObstacleMask|WorldBoundaryMask;
    }
}
