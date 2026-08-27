using UnityEngine;

namespace Ashbound
{
    public enum PrototypeSceneMode { Lobby, Run, TestArena }
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        public PrototypeSceneMode mode;
        public PrototypeCatalog catalog;
        public RunManager Run { get; private set; }
        private void Awake()
        {
            if (!catalog) catalog = Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            if (!catalog) { Debug.LogError("Missing PrototypeCatalog. Use Ashbound > Create prototype content in the Editor."); enabled = false; return; }
            Application.targetFrameRate = 120;
            Physics.IgnoreLayerCollision(8, 8, true);
            RenderSettings.ambientLight = new Color(.48f, .49f, .55f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            var sun = new GameObject("Vault light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.1f;
            sun.transform.SetParent(transform);
            sun.transform.rotation = Quaternion.Euler(48, -25, 0); sun.shadows = LightShadows.Soft;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.SetParent(transform);
            camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<ArenaCamera>();
            var actorRoot = new GameObject("Actors").transform;
            actorRoot.SetParent(transform);
            var combat = gameObject.AddComponent<CombatService>();
            var factory = new EntityFactory(combat, catalog, actorRoot);
            var rooms = new GameObject("Rooms").AddComponent<RoomDirector>(); rooms.Configure(factory, catalog, combat);
            rooms.transform.SetParent(transform);
            Run = gameObject.AddComponent<RunManager>(); Run.Configure(catalog, combat, rooms, factory, camera);
            var audio = gameObject.AddComponent<AudioDirector>(); Run.StateChanged += audio.OnState;
            gameObject.AddComponent<PrototypeHud>().Configure(Run, camera);
            gameObject.AddComponent<DebugMenu>().Configure(Run);
        }
        private void Start()
        {
            if (!Run) return;
            if (mode == PrototypeSceneMode.Run) Run.StartRun();
            if (mode == PrototypeSceneMode.TestArena) { Run.StartRun(42); Run.DebugSkipToBoss(); Run.DebugOpen = true; }
        }
        private void OnDestroy() { PrimitiveFactory.DisposeMaterials(); }
    }
}
