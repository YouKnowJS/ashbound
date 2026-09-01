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
        [Min(1)] public float layoutScale = 1;
        public CombatSpaceSection[] sections = Array.Empty<CombatSpaceSection>();
        public CombatSpaceObstacle[] obstacles = Array.Empty<CombatSpaceObstacle>();
        public Vector2[] boundaryPoints = Array.Empty<Vector2>();
        public Vector3 entrancePosition = new Vector3(0, 0, -7);
        public Vector3 exitPosition = new Vector3(0, 0, 9);
        [Min(0)] public float transitionLength = 5;
        [Min(8)] public float cameraOrthographicSize = 14;
        [Min(8)] public float cameraMaximumOrthographicSize = 20;
        [Min(.05f)] public float cameraFollowSmoothTime = .28f;
        [Min(.05f)] public float cameraZoomSmoothTime = .32f;
        [Min(0)] public float cameraAnticipationDistance = 1.2f;
        [Min(0)] public float cameraClampPadding = 1.5f;
        [Min(6)] public float multiplayerSeparationLimit = 18;
        [Min(8)] public float nonPlayableWorldDepth = 28;
        public string[] distantLandmarkHooks = Array.Empty<string>();
        public GameObject environmentPrefab;
        public GameObject boundaryPrefab;
        public GameObject distantBackgroundPrefab;

        public Vector2 ScaledTechnicalBounds => technicalBounds * Mathf.Max(1, layoutScale);
        public Vector3 ScalePoint(Vector3 point) => new Vector3(point.x * layoutScale, point.y, point.z * layoutScale);
        public Vector2 ScalePoint(Vector2 point) => point * layoutScale;
    }
}
