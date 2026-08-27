using UnityEngine;

namespace Ashbound
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        private Camera view;
        private void Awake()
        {
            view = GetComponent<Camera>(); view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.045f, .055f, .075f);
            transform.position = new Vector3(0, 24, -19); transform.rotation = Quaternion.Euler(53, 0, 0);
            view.nearClipPlane = .1f; view.farClipPlane = 80;
        }
        private void LateUpdate() { view.orthographicSize = Mathf.Max(13.7f, 17.5f / view.aspect); }
    }
}
