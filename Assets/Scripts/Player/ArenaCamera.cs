using UnityEngine;

namespace Ashbound
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        private Camera view;
        private static ArenaCamera instance;
        private float shakeTime, shakeStrength;
        private Vector3 basePosition;
        private float targetSize = 13.7f;
        private void Awake()
        {
            instance = this; view = GetComponent<Camera>(); view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.045f, .055f, .075f);
            transform.position = basePosition = new Vector3(0, 24, -19); transform.rotation = Quaternion.Euler(53, 0, 0);
            view.nearClipPlane = .1f; view.farClipPlane = 80;
        }
        public static void Shake(float strength, float duration)
        {
            if (!instance) return;
            instance.shakeStrength = Mathf.Max(instance.shakeStrength, strength);
            instance.shakeTime = Mathf.Max(instance.shakeTime, duration);
        }
        public static void UseSpace(CombatSpaceDefinition space)
        {
            if (!instance) return;
            instance.targetSize = space ? space.cameraOrthographicSize : 13.7f;
            float height = space && space.category == CombatSpaceCategory.Large ? 27 : space && space.category == CombatSpaceCategory.Small ? 22 : 24;
            instance.basePosition = new Vector3(0, height, -height * .8f);
        }
        private void LateUpdate()
        {
            view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, Mathf.Max(targetSize, targetSize * 1.28f / view.aspect), Time.unscaledDeltaTime * 10);
            if (shakeTime > 0) { shakeTime -= Time.unscaledDeltaTime; transform.position = basePosition + Random.insideUnitSphere * shakeStrength; }
            else { transform.position = basePosition; shakeStrength = 0; }
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
