using UnityEngine;

namespace Ashbound
{
    public sealed class RoomView : MonoBehaviour
    {
        private Transform geometry;
        private Renderer gate;
        public readonly Vector3 ExitPosition = new Vector3(0, 0, 8.9f);
        public void Build(int index)
        {
            if (geometry) { geometry.gameObject.SetActive(false); Destroy(geometry.gameObject); }
            geometry = new GameObject("Room geometry").transform; geometry.SetParent(transform);
            Color floor = index == 6 ? new Color(.19f, .17f, .17f) : new Color(.17f, .20f, .22f);
            PrimitiveFactory.Shape("Floor", PrimitiveType.Cube, geometry, new Vector3(0, -.35f, 0), new Vector3(28, .7f, 22), floor, true);
            Color stone = new Color(.25f, .28f, .31f);
            for (int x = -14; x <= 14; x += 28)
                PrimitiveFactory.Shape("Wall", PrimitiveType.Cube, geometry, new Vector3(x, .9f, 0), new Vector3(.7f, 1.8f, 22), stone, true);
            for (int z = -11; z <= 11; z += 22)
                PrimitiveFactory.Shape("Wall", PrimitiveType.Cube, geometry, new Vector3(0, .65f, z), new Vector3(28, 1.3f, .7f), stone, true);
            for (int x = -10; x <= 10; x += 20)
                for (int z = -7; z <= 7; z += 14)
                {
                    PrimitiveFactory.Shape("Pillar", PrimitiveType.Cube, geometry, new Vector3(x, 1.2f, z), new Vector3(1.1f, 2.4f, 1.1f), stone, true);
                    PrimitiveFactory.Shape("Brazier", PrimitiveType.Sphere, geometry, new Vector3(x, 2.65f, z), Vector3.one * .5f, Palette.Gold);
                }
            // Floor seams are deliberately sparse so attack markers remain readable.
            for (int z = -8; z <= 8; z += 4)
                PrimitiveFactory.Shape("Floor seam", PrimitiveType.Cube, geometry, new Vector3(0, .012f, z), new Vector3(25, .016f, .025f), new Color(.26f, .28f, .29f));
            var gateObject = PrimitiveFactory.Shape("Exit seal", PrimitiveType.Cube, geometry, ExitPosition + Vector3.up * .04f,
                new Vector3(3.3f, .08f, 1.3f), Palette.Danger);
            gate = gateObject.GetComponent<Renderer>();
            CombatVfx.Ring(Vector3.zero, index == 6 ? 6 : 4, new Color(.4f, .36f, .26f), float.MaxValue, .04f).transform.SetParent(geometry);
        }
        public void SetGate(bool open) { if (gate) gate.sharedMaterial = PrimitiveFactory.Material(open ? Palette.Player : Palette.Danger); }
    }
}
