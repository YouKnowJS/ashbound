using UnityEngine;

namespace Ashbound
{
    public sealed class CombatVfx : MonoBehaviour
    {
        private LineRenderer line;
        private float duration, elapsed;
        private Color tint;
        private bool expand;
        private Vector3 originalScale;

        public static GameObject Ring(Vector3 position, float radius, Color color, float seconds, float width = .07f, bool pulse = false)
        {
            var obj = new GameObject("Ring"); obj.transform.position = position + Vector3.up * .06f;
            var points = new Vector3[65];
            for (int i = 0; i < points.Length; i++) { float a = i / 64f * Mathf.PI * 2; points[i] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius); }
            Setup(obj, points, color, seconds, width, pulse);
            return obj;
        }
        public static void Pulse(Vector3 position, float radius, Color color) => Ring(position, radius, color, .32f, .16f, true);
        public static void Arc(Vector3 position, Vector3 facing, float radius, float degrees, Color color)
        {
            var obj = new GameObject("Blade arc"); obj.transform.position = position + Vector3.up * .8f;
            var points = new Vector3[25];
            for (int i = 0; i < points.Length; i++) points[i] = Quaternion.AngleAxis(-degrees / 2 + degrees * i / (points.Length - 1), Vector3.up) * facing * radius;
            Setup(obj, points, color, .14f, .18f, false);
        }
        public static void Bolt(Vector3 from, Vector3 to, Color color)
        {
            var obj = new GameObject("Lightning"); obj.transform.position = from;
            Vector3 offset = to - from, side = Vector3.Cross(offset.normalized, Vector3.up) * .25f;
            Setup(obj, new[] { Vector3.zero, offset * .3f + side, offset * .6f - side, offset }, color, .18f, .09f, false);
        }
        public static GameObject Direction(Vector3 origin, Vector3 direction, float length, Color color, float seconds)
        {
            var obj = new GameObject("Charge warning"); obj.transform.position = origin + Vector3.up * .12f;
            Vector3 end = direction.normalized * length, side = Vector3.Cross(direction.normalized, Vector3.up);
            Setup(obj, new[] { Vector3.zero, end, end - direction * 1.5f + side, end, end - direction * 1.5f - side }, color, seconds, .14f, false);
            return obj;
        }
        private static void Setup(GameObject obj, Vector3[] points, Color color, float duration, float width, bool pulse)
        {
            var effect = obj.AddComponent<CombatVfx>();
            effect.line = obj.AddComponent<LineRenderer>(); effect.line.useWorldSpace = false;
            effect.line.sharedMaterial = PrimitiveFactory.LineMaterial; effect.line.positionCount = points.Length; effect.line.SetPositions(points);
            effect.line.startWidth = effect.line.endWidth = width; effect.line.startColor = effect.line.endColor = color;
            effect.duration = duration; effect.tint = color; effect.expand = pulse; effect.originalScale = obj.transform.localScale;
        }
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            float fraction = elapsed / duration;
            line.startColor = line.endColor = new Color(tint.r, tint.g, tint.b, 1 - fraction * .7f);
            if (expand) transform.localScale = originalScale * Mathf.Lerp(.6f, 1, fraction);
        }
    }
}
