using System;
using UnityEngine;

namespace Ashbound
{
    [Serializable]
    public struct CombatSpaceSection
    {
        public string id;
        public Vector2 center;
        public Vector2 size;
        public float rotation;
        public bool transitionPath;
    }

    [Serializable]
    public struct CombatSpaceObstacle
    {
        public Vector2 position;
        public Vector2 size;
        public float height;
        public float rotation;
    }

    [CreateAssetMenu(menuName = "Ashbound/Rooms/Combat space")]
    public sealed class CombatSpaceDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public CombatSpaceCategory category;
        public ArenaLayoutKind layout;
        [TextArea] public string spatialIntent;
        public Vector2 technicalBounds = new Vector2(28, 22);
        public CombatSpaceSection[] sections = Array.Empty<CombatSpaceSection>();
        public CombatSpaceObstacle[] obstacles = Array.Empty<CombatSpaceObstacle>();
        public Vector2[] boundaryPoints = Array.Empty<Vector2>();
        public Vector3 entrancePosition = new Vector3(0, 0, -7);
        public Vector3 exitPosition = new Vector3(0, 0, 9);
        [Min(0)] public float transitionLength = 5;
        [Min(8)] public float cameraOrthographicSize = 14;
        [Min(6)] public float multiplayerSeparationLimit = 18;
        public string[] distantLandmarkHooks = Array.Empty<string>();
        public GameObject environmentPrefab;
        public GameObject boundaryPrefab;
        public GameObject distantBackgroundPrefab;
    }
}
