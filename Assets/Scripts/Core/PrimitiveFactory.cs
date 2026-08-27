using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public static class PrimitiveFactory
    {
        private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private static Material lineMaterial;
        public static Material LineMaterial => lineMaterial ? lineMaterial : lineMaterial = new Material(Shader.Find("Sprites/Default"));
        public static Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var existing) && existing) return existing;
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", .12f);
            materials[color] = material;
            return material;
        }
        public static GameObject Shape(string name, PrimitiveType kind, Transform parent, Vector3 position, Vector3 scale, Color color, bool solid = false)
        {
            var obj = GameObject.CreatePrimitive(kind);
            obj.name = name; obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!solid) { var collider = obj.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider); }
            return obj;
        }
        public static void DisposeMaterials()
        {
            foreach (var material in materials.Values) if (material) Object.Destroy(material);
            materials.Clear();
            if (lineMaterial) Object.Destroy(lineMaterial);
            lineMaterial = null;
        }
    }
}
