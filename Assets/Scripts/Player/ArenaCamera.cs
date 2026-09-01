using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public enum CameraFollowMode { Automatic, Solo, PartyCentroid }
    public enum CameraZoomOverride { Automatic, Minimum, Maximum }
    public enum CameraContext { StandardCombat, LargeCombat, Elite, Boss, Transition, Service, FinalPvP }

    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        private Camera view;
        private RunManager run;
        private RoomDirector rooms;
        private CombatSpaceDefinition space;
        private static ArenaCamera instance;
        private Vector3 focusPoint, focusVelocity, lastRawCenter;
        private float zoomVelocity, shakeTime, shakeStrength;
        private bool centerInitialized, exceededLastFrame;

        public static ArenaCamera Instance => instance;
        public bool FollowEnabled { get; set; } = true;
        public CameraFollowMode FollowMode { get; set; } = CameraFollowMode.Automatic;
        public CameraZoomOverride ZoomOverride { get; set; } = CameraZoomOverride.Automatic;
        public bool DisplayPartyCentroid { get; set; }
        public bool DisplayPartySpread { get; set; }
        public bool DisplayClampBounds { get; set; }
        public Vector3 PartyCentroid { get; private set; }
        public float PartySpread { get; private set; }
        public bool PartyBeyondSoftLimit { get; private set; }
        public Rect ClampBounds { get; private set; }
        public CameraContext Context { get; private set; }
        public int OffscreenIndicatorCount { get; private set; }
        public float CurrentZoom => view ? view.orthographicSize : 0;
        public float MinimumZoom => space ? space.cameraOrthographicSize : 11.5f;
        public float MaximumZoom => space ? Mathf.Max(MinimumZoom, space.cameraMaximumOrthographicSize) : 18;
        public Vector3 FocusPoint => focusPoint;
        public event Action<float> SoftSpreadLimitExceeded;

        private void Awake()
        {
            instance = this;
            view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = 11.5f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.025f, .032f, .045f);
            transform.rotation = Quaternion.Euler(53, 0, 0);
            transform.position = new Vector3(0, 24, -19.2f);
            view.nearClipPlane = .1f;
            view.farClipPlane = 140;
        }

        public void Configure(RunManager manager, RoomDirector director)
        {
            run = manager;
            rooms = director;
            ApplySpace(rooms ? rooms.ActiveCombatSpace : null);
            SnapToTargets();
        }

        public static void Shake(float strength, float duration)
        {
            if (!instance) return;
            instance.shakeStrength = Mathf.Max(instance.shakeStrength, strength);
            instance.shakeTime = Mathf.Max(instance.shakeTime, duration);
        }

        public static void UseSpace(CombatSpaceDefinition definition)
        {
            if (instance) instance.ApplySpace(definition);
        }

        private void ApplySpace(CombatSpaceDefinition definition)
        {
            space = definition;
            centerInitialized = false;
            focusVelocity = Vector3.zero;
            zoomVelocity = 0;
            UpdateClampBounds();
        }

        public void SnapToTargets()
        {
            UpdateContext();
            Vector3 target = DetermineFramingCenter(out _);
            focusPoint = ClampFocus(target);
            centerInitialized = true;
            lastRawCenter = target;
            if (view) view.orthographicSize = DesiredZoom(0);
            PlaceCamera(Vector3.zero);
        }

        private void LateUpdate()
        {
            float dt = Mathf.Max(.0001f, Time.unscaledDeltaTime);
            UpdateContext();
            Vector3 rawCenter = DetermineFramingCenter(out float framingSpread);
            Vector3 desiredFocus = rawCenter;
            if (!centerInitialized) { focusPoint = rawCenter; lastRawCenter = rawCenter; centerInitialized = true; }
            else
            {
                Vector3 centerVelocity = (rawCenter - lastRawCenter) / dt;
                lastRawCenter = rawCenter;
                float anticipation = space ? space.cameraAnticipationDistance : 1.2f;
                if (centerVelocity.sqrMagnitude > .04f)
                    desiredFocus += centerVelocity.normalized * Mathf.Min(anticipation, centerVelocity.magnitude * .1f);
            }
            desiredFocus = ClampFocus(desiredFocus);

            if (FollowEnabled)
            {
                float smooth = space ? space.cameraFollowSmoothTime : .28f;
                focusPoint = Vector3.SmoothDamp(focusPoint, desiredFocus, ref focusVelocity, smooth, Mathf.Infinity, dt);
            }

            float desiredZoom = DesiredZoom(framingSpread);
            float zoomSmooth = space ? space.cameraZoomSmoothTime : .32f;
            view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, desiredZoom, ref zoomVelocity, zoomSmooth, Mathf.Infinity, dt);
            PartyBeyondSoftLimit = PartySpread > (space ? space.multiplayerSeparationLimit : 18);
            if (PartyBeyondSoftLimit && !exceededLastFrame) SoftSpreadLimitExceeded?.Invoke(PartySpread);
            exceededLastFrame = PartyBeyondSoftLimit;

            Vector3 shake = Vector3.zero;
            if (shakeTime > 0)
            {
                shakeTime -= dt;
                shake = UnityEngine.Random.insideUnitSphere * shakeStrength;
            }
            else shakeStrength = 0;
            PlaceCamera(shake);
        }

        private Vector3 DetermineFramingCenter(out float framingSpread)
        {
            var living = run == null ? new List<Combatant>() : run.Players.Where(x => x && x.Alive).ToList();
            if (living.Count == 0 && run != null) living = run.Players.Where(x => x).ToList();
            PartyCentroid = living.Count == 0 ? Vector3.zero : living.Aggregate(Vector3.zero, (sum, actor) => sum + actor.transform.position) / living.Count;
            PartyCentroid = Ground(PartyCentroid);
            PartySpread = MaximumPairDistance(living);

            bool solo = FollowMode == CameraFollowMode.Solo || (FollowMode == CameraFollowMode.Automatic && living.Count <= 1);
            Vector3 center = solo && living.Count > 0 ? Ground(living[0].transform.position) : PartyCentroid;
            var framing = solo && living.Count > 0 ? new List<Combatant> { living[0] } : new List<Combatant>(living);

            if (Context == CameraContext.Boss && rooms && rooms.Boss && rooms.Boss.Alive) framing.Add(rooms.Boss);
            if (Context == CameraContext.FinalPvP && run != null && run.Corruption.Reflection && run.Corruption.Reflection.Alive)
                framing.Add(run.Corruption.Reflection);
            framingSpread = MaximumPairDistance(framing);
            if (framing.Count > living.Count && framing.Count > 0)
            {
                Vector3 boundsCenter = framing.Aggregate(Vector3.zero, (sum, actor) => sum + actor.transform.position) / framing.Count;
                center = Vector3.Lerp(center, Ground(boundsCenter), Context == CameraContext.Boss ? .42f : .5f);
            }
            return Ground(center);
        }

        private float DesiredZoom(float framingSpread)
        {
            float minimum = MinimumZoom;
            float maximum = MaximumZoom;
            float context = Context == CameraContext.Service ? -1.25f : Context == CameraContext.Transition ? .5f :
                Context == CameraContext.LargeCombat ? .8f : Context == CameraContext.Elite ? 1.25f :
                Context == CameraContext.Boss ? 1.8f : Context == CameraContext.FinalPvP ? 1.5f : 0;
            float target = minimum + context + Mathf.Max(0, framingSpread - 4) * .36f;
            target = Mathf.Max(target, minimum * 1.2f / Mathf.Max(.6f, view.aspect));
            target = ZoomOverride == CameraZoomOverride.Minimum ? minimum : ZoomOverride == CameraZoomOverride.Maximum ? maximum : target;
            return Mathf.Clamp(target, minimum, maximum);
        }

        private void UpdateContext()
        {
            if (run == null || run.Flow == null) { Context = CameraContext.StandardCombat; return; }
            if (run.Flow.State == RunState.FinalPvP) { Context = CameraContext.FinalPvP; return; }
            if (run.Flow.State == RunState.BossFight || run.CurrentNode?.Definition.nodeType == ExpeditionNodeType.Boss) { Context = CameraContext.Boss; return; }
            var type = run.CurrentNode?.Definition.nodeType;
            if (type == ExpeditionNodeType.Treasure || type == ExpeditionNodeType.Merchant || type == ExpeditionNodeType.Rest || type == ExpeditionNodeType.Event || type == ExpeditionNodeType.Relic)
            { Context = CameraContext.Service; return; }
            if (run.AwaitingRouteGate || run.RegionCompleteAwaitingFinalGate || run.RouteSelectionOpen) { Context = CameraContext.Transition; return; }
            if (type == ExpeditionNodeType.Elite || type == ExpeditionNodeType.Challenge) { Context = CameraContext.Elite; return; }
            Context = space && space.category == CombatSpaceCategory.Large ? CameraContext.LargeCombat : CameraContext.StandardCombat;
        }

        private Vector3 ClampFocus(Vector3 target)
        {
            UpdateClampBounds();
            target.x = Mathf.Clamp(target.x, ClampBounds.xMin, ClampBounds.xMax);
            target.z = Mathf.Clamp(target.z, ClampBounds.yMin, ClampBounds.yMax);
            target.y = 0;
            return target;
        }

        private void UpdateClampBounds()
        {
            Vector2 bounds = space ? space.ScaledTechnicalBounds : new Vector2(30, 24);
            float padding = space ? space.cameraClampPadding : 1.5f;
            float halfX = Mathf.Max(1, bounds.x * .5f - padding);
            float halfZ = Mathf.Max(1, bounds.y * .5f - padding);
            ClampBounds = Rect.MinMaxRect(-halfX, -halfZ, halfX, halfZ);
        }

        private void PlaceCamera(Vector3 shake)
        {
            float height = space && space.category == CombatSpaceCategory.Large ? 31 : space && space.category == CombatSpaceCategory.Small ? 23 : 27;
            transform.position = focusPoint + new Vector3(0, height, -height * .8f) + shake;
            transform.rotation = Quaternion.Euler(53, 0, 0);
        }

        private static Vector3 Ground(Vector3 value) { value.y = 0; return value; }
        private static float MaximumPairDistance(IReadOnlyList<Combatant> actors)
        {
            float result = 0;
            for (int i = 0; i < actors.Count; i++) for (int j = i + 1; j < actors.Count; j++)
            {
                Vector3 delta = actors[i].transform.position - actors[j].transform.position; delta.y = 0;
                result = Mathf.Max(result, delta.magnitude);
            }
            return result;
        }

        private void OnGUI()
        {
            if (!view || run == null || run.Flow.State == RunState.Lobby) return;
            GUI.depth = 12;
            OffscreenIndicatorCount = 0;
            foreach (var actor in IndicatorTargets()) DrawEdgeIndicator(actor);
            if (DisplayPartyCentroid)
            {
                Vector3 screen = view.WorldToScreenPoint(PartyCentroid + Vector3.up * .1f);
                if (screen.z > 0) GUI.Box(new Rect(screen.x - 7, Screen.height - screen.y - 7, 14, 14), "+");
            }
            if (DisplayPartySpread)
                GUI.Label(new Rect(18, Screen.height - 66, 420, 24), "Party spread: " + PartySpread.ToString("0.0") + " / " + (space ? space.multiplayerSeparationLimit : 18).ToString("0.0") + (PartyBeyondSoftLimit ? " · REGROUP HOOK" : ""));
            if (DisplayClampBounds) DrawClampBounds();
        }

        private IEnumerable<Combatant> IndicatorTargets()
        {
            foreach (var player in run.Players) if (player && player.Alive) yield return player;
            if (run.Flow.State == RunState.FinalPvP && run.Corruption.Reflection && run.Corruption.Reflection.Alive) yield return run.Corruption.Reflection;
        }

        private void DrawEdgeIndicator(Combatant actor)
        {
            Vector3 viewport = view.WorldToViewportPoint(actor.transform.position + Vector3.up);
            if (viewport.z > 0 && viewport.x >= .03f && viewport.x <= .97f && viewport.y >= .04f && viewport.y <= .96f) return;
            OffscreenIndicatorCount++;
            Vector2 point = new Vector2(Mathf.Clamp(viewport.x, .05f, .95f) * Screen.width, (1 - Mathf.Clamp(viewport.y, .07f, .93f)) * Screen.height);
            string arrow = viewport.x < .03f ? "◀" : viewport.x > .97f ? "▶" : viewport.y < .04f ? "▼" : "▲";
            Color previous = GUI.color;
            int playerIndex = -1;
            for (int i = 0; i < run.Players.Count; i++)
                if (run.Players[i] == actor) { playerIndex = i; break; }
            GUI.color = playerIndex >= 0 && playerIndex < Palette.Party.Length ? Palette.Party[playerIndex] : Palette.Corrupted;
            GUI.Box(new Rect(point.x - 35, point.y - 14, 70, 28), arrow + " " + actor.Id);
            GUI.color = previous;
        }

        private void DrawClampBounds()
        {
            Vector3[] world =
            {
                new Vector3(ClampBounds.xMin,0,ClampBounds.yMin), new Vector3(ClampBounds.xMax,0,ClampBounds.yMin),
                new Vector3(ClampBounds.xMax,0,ClampBounds.yMax), new Vector3(ClampBounds.xMin,0,ClampBounds.yMax)
            };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a3 = view.WorldToScreenPoint(world[i]); Vector3 b3 = view.WorldToScreenPoint(world[(i + 1) % 4]);
                if (a3.z <= 0 || b3.z <= 0) continue;
                DrawLine(new Vector2(a3.x, Screen.height - a3.y), new Vector2(b3.x, Screen.height - b3.y), new Color(.2f, .9f, 1, .85f), 2);
            }
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Matrix4x4 matrix = GUI.matrix; Color previous = GUI.color;
            float angle = Vector2.SignedAngle(Vector2.right, to - from);
            GUI.color = color; GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * .5f, Vector2.Distance(from, to), width), Texture2D.whiteTexture);
            GUI.matrix = matrix; GUI.color = previous;
        }

        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
