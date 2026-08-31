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
        private void LateUpdate()
        {
            view.orthographicSize = Mathf.Max(13.7f, 17.5f / view.aspect);
            if (shakeTime > 0) { shakeTime -= Time.unscaledDeltaTime; transform.position = basePosition + Random.insideUnitSphere * shakeStrength; }
            else { transform.position = basePosition; shakeStrength = 0; }
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
