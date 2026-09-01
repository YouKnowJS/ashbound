using UnityEngine;

namespace Ashbound
{
    public sealed class RoomView : MonoBehaviour
    {
        private Transform geometry;
        private Renderer gate;
        public Vector3 ExitPosition { get; private set; } = new Vector3(0, 0, 8.9f);
        public CombatSpaceDefinition Definition { get; private set; }

        public void Build(CombatSpaceDefinition definition, int roomIndex)
        {
            Definition = definition;
            if (geometry) { geometry.gameObject.SetActive(false); Destroy(geometry.gameObject); }
            geometry = new GameObject("Combat space · " + (definition ? definition.displayName : "legacy")).transform;
            geometry.SetParent(transform);
            if (!definition) { BuildLegacy(); return; }
            ExitPosition = definition.exitPosition;
            Color floor = roomIndex == 6 ? new Color(.19f, .17f, .17f) : new Color(.17f, .20f, .22f);
            Color path = new Color(.205f, .22f, .225f);
            foreach (var section in definition.sections)
            {
                var piece = PrimitiveFactory.Shape(section.transitionPath ? "Transition path" : "Playable section · " + section.id, PrimitiveType.Cube, geometry,
                    new Vector3(section.center.x, -.3f, section.center.y), new Vector3(section.size.x, .6f, section.size.y), section.transitionPath ? path : floor, true);
                piece.transform.localRotation = Quaternion.Euler(0, section.rotation, 0);
            }
            BuildBoundary(definition);
            foreach (var obstacle in definition.obstacles)
            {
                var obj = PrimitiveFactory.Shape("Navigation obstacle", PrimitiveType.Cube, geometry,
                    new Vector3(obstacle.position.x, obstacle.height * .5f, obstacle.position.y), new Vector3(obstacle.size.x, obstacle.height, obstacle.size.y), new Color(.25f,.28f,.31f), true);
                obj.transform.localRotation = Quaternion.Euler(0, obstacle.rotation, 0);
            }
            for (int i = 0; i < definition.distantLandmarkHooks.Length; i++)
            {
                float x = (i - (definition.distantLandmarkHooks.Length - 1) * .5f) * 7;
                PrimitiveFactory.Shape("Background hook · " + definition.distantLandmarkHooks[i], PrimitiveType.Cube, geometry,
                    new Vector3(x, 3 + i, 19 + i * 2), new Vector3(2.2f, 6 + i * 2, 2.2f), new Color(.09f,.11f,.13f));
            }
            if (definition.environmentPrefab) Instantiate(definition.environmentPrefab, geometry);
            if (definition.distantBackgroundPrefab) Instantiate(definition.distantBackgroundPrefab, geometry);
            BuildGate(); ArenaCamera.UseSpace(definition);
        }

        private void BuildBoundary(CombatSpaceDefinition definition)
        {
            Color stone = new Color(.25f, .28f, .31f); var points = definition.boundaryPoints;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i], b = points[(i + 1) % points.Length], middle = (a + b) * .5f;
                if (middle.y > definition.exitPosition.z - 1.8f && Mathf.Abs(middle.x - definition.exitPosition.x) < 2.2f) continue;
                Vector2 delta = b - a; float length = delta.magnitude;
                var wall = PrimitiveFactory.Shape("Irregular boundary", PrimitiveType.Cube, geometry, new Vector3(middle.x, .8f, middle.y), new Vector3(length, 1.6f, .65f), stone, true);
                wall.transform.localRotation = Quaternion.Euler(0, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 0);
            }
            if (definition.boundaryPrefab) Instantiate(definition.boundaryPrefab, geometry);
        }
        private void BuildGate()
        {
            var gateObject = PrimitiveFactory.Shape("Exit seal", PrimitiveType.Cube, geometry, ExitPosition + Vector3.up * .04f, new Vector3(3.3f, .08f, 1.3f), Palette.Danger);
            gate = gateObject.GetComponent<Renderer>();
        }
        private void BuildLegacy()
        {
            ExitPosition = new Vector3(0, 0, 8.9f);
            PrimitiveFactory.Shape("Legacy floor", PrimitiveType.Cube, geometry, new Vector3(0,-.35f,0), new Vector3(28,.7f,22), new Color(.17f,.20f,.22f), true);
            BuildGate(); ArenaCamera.UseSpace(null);
        }
        public void SetGate(bool open) { if (gate) gate.sharedMaterial = PrimitiveFactory.Material(open ? Palette.Player : Palette.Danger); }
    }
}
