using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public sealed class RoomView : MonoBehaviour
    {
        private Transform geometry;
        private Renderer gate;
        public Vector3 EntrancePosition { get; private set; } = new Vector3(0, 0, -7);
        public Vector3 ExitPosition { get; private set; } = new Vector3(0, 0, 8.9f);
        public CombatSpaceDefinition Definition { get; private set; }

        public void Build(CombatSpaceDefinition definition, int roomIndex)
        {
            Definition = definition;
            if (geometry) { geometry.gameObject.SetActive(false); Destroy(geometry.gameObject); }
            geometry = new GameObject("Combat space · " + (definition ? definition.displayName : "legacy")).transform;
            geometry.SetParent(transform);
            if (!definition) { BuildLegacy(); return; }
            EntrancePosition = definition.ScalePoint(definition.entrancePosition);
            ExitPosition = definition.ScalePoint(definition.exitPosition);
            Color floor = roomIndex == 6 ? new Color(.19f, .17f, .17f) : new Color(.17f, .20f, .22f);
            Color path = new Color(.205f, .22f, .225f);
            float scale = definition.layoutScale;
            foreach (var section in definition.sections)
            {
                var piece = PrimitiveFactory.Shape(section.transitionPath ? "Transition path" : "Playable section · " + section.id, PrimitiveType.Cube, geometry,
                    new Vector3(section.center.x * scale, -.3f, section.center.y * scale), new Vector3(section.size.x * scale, .6f, section.size.y * scale), section.transitionPath ? path : floor, true);
                piece.transform.localRotation = Quaternion.Euler(0, section.rotation, 0);
            }
            BuildWorldExtension(definition);
            BuildBoundary(definition);
            foreach (var obstacle in definition.obstacles)
            {
                float obstacleScale = Mathf.Lerp(1, scale, .55f);
                var obj = PrimitiveFactory.Shape("Navigation obstacle", PrimitiveType.Cube, geometry,
                    new Vector3(obstacle.position.x * scale, obstacle.height * .5f, obstacle.position.y * scale), new Vector3(obstacle.size.x * obstacleScale, obstacle.height, obstacle.size.y * obstacleScale), new Color(.25f,.28f,.31f), true);
                obj.transform.localRotation = Quaternion.Euler(0, obstacle.rotation, 0);
            }
            for (int i = 0; i < definition.distantLandmarkHooks.Length; i++)
            {
                float x = (i - (definition.distantLandmarkHooks.Length - 1) * .5f) * Mathf.Max(8, definition.ScaledTechnicalBounds.x / (definition.distantLandmarkHooks.Length + 1));
                PrimitiveFactory.Shape("Background hook · " + definition.distantLandmarkHooks[i], PrimitiveType.Cube, geometry,
                    new Vector3(x, 4 + i, definition.ScaledTechnicalBounds.y * .5f + 10 + i * 3), new Vector3(3, 8 + i * 3, 3), new Color(.07f,.085f,.105f));
            }
            if (definition.environmentPrefab) Instantiate(definition.environmentPrefab, geometry);
            if (definition.distantBackgroundPrefab) Instantiate(definition.distantBackgroundPrefab, geometry);
            BuildGate(); ArenaCamera.UseSpace(definition);
        }

        private void BuildBoundary(CombatSpaceDefinition definition)
        {
            Color stone = new Color(.25f, .28f, .31f); var points = definition.boundaryPoints; float scale = definition.layoutScale;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = points[i] * scale, b = points[(i + 1) % points.Length] * scale, middle = (a + b) * .5f;
                if (middle.y > ExitPosition.z - 1.8f * scale && Mathf.Abs(middle.x - ExitPosition.x) < 2.2f * scale) continue;
                Vector2 delta = b - a; float length = delta.magnitude;
                var wall = PrimitiveFactory.Shape("Irregular boundary", PrimitiveType.Cube, geometry, new Vector3(middle.x, .8f, middle.y), new Vector3(length, 1.6f, .65f), stone, true);
                wall.transform.localRotation = Quaternion.Euler(0, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 0);
            }
            if (definition.boundaryPrefab) Instantiate(definition.boundaryPrefab, geometry);
        }
        private void BuildWorldExtension(CombatSpaceDefinition definition)
        {
            Vector2 bounds = definition.ScaledTechnicalBounds;
            float depth = definition.nonPlayableWorldDepth;
            Color near = new Color(.075f, .085f, .095f);
            PrimitiveFactory.Shape("World apron · west", PrimitiveType.Cube, geometry, new Vector3(-(bounds.x + depth) * .5f, -.75f, 0), new Vector3(depth, .7f, bounds.y + depth * 2), near);
            PrimitiveFactory.Shape("World apron · east", PrimitiveType.Cube, geometry, new Vector3((bounds.x + depth) * .5f, -.75f, 0), new Vector3(depth, .7f, bounds.y + depth * 2), near);
            PrimitiveFactory.Shape("World apron · south", PrimitiveType.Cube, geometry, new Vector3(0, -.75f, -(bounds.y + depth) * .5f), new Vector3(bounds.x, .7f, depth), near);
            PrimitiveFactory.Shape("World apron · north", PrimitiveType.Cube, geometry, new Vector3(0, -.75f, (bounds.y + depth) * .5f), new Vector3(bounds.x, .7f, depth), near);
            float halfX = bounds.x * .5f + depth * .65f, halfZ = bounds.y * .5f + depth * .55f;
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * (halfX + (i % 3) * 3);
                float z = -halfZ + i * (halfZ * 2 / 7f);
                PrimitiveFactory.Shape("Distant world silhouette", PrimitiveType.Cube, geometry, new Vector3(x, 2.5f + i % 3, z), new Vector3(3 + i % 2, 5 + (i % 3) * 2, 3), new Color(.055f,.065f,.08f));
            }
        }
        private void BuildGate()
        {
            var gateObject = PrimitiveFactory.Shape("Exit seal", PrimitiveType.Cube, geometry, ExitPosition + Vector3.up * .04f, new Vector3(3.3f, .08f, 1.3f), Palette.Danger);
            gate = gateObject.GetComponent<Renderer>();
        }
        private void BuildLegacy()
        {
            EntrancePosition = new Vector3(0, 0, -7);
            ExitPosition = new Vector3(0, 0, 8.9f);
            PrimitiveFactory.Shape("Legacy floor", PrimitiveType.Cube, geometry, new Vector3(0,-.35f,0), new Vector3(28,.7f,22), new Color(.17f,.20f,.22f), true);
            BuildGate(); ArenaCamera.UseSpace(null);
        }
        public void SetGate(bool open) { if (gate) gate.sharedMaterial = PrimitiveFactory.Material(open ? Palette.Player : Palette.Danger); }

        public IReadOnlyList<Vector3> NavigationAnchors(float clearance,string[] allowedSections=null)
        {
            var anchors=new List<Vector3>();
            if(!Definition){anchors.Add(Vector3.zero);anchors.Add(new Vector3(-6,0,0));anchors.Add(new Vector3(6,0,0));return anchors;}
            var allowed=allowedSections??Array.Empty<string>();
            foreach(var section in Definition.sections)
            {
                if(section.transitionPath||(allowed.Length>0&&Array.IndexOf(allowed,section.id)<0))continue;
                Vector2 center=section.center*Definition.layoutScale;Vector2 half=section.size*Definition.layoutScale*.5f-Vector2.one*clearance;if(half.x<=.25f||half.y<=.25f)continue;
                Vector2[] local={Vector2.zero,new Vector2(half.x*.55f,0),new Vector2(-half.x*.55f,0),new Vector2(0,half.y*.55f),new Vector2(0,-half.y*.55f)};
                Quaternion rotation=Quaternion.Euler(0,section.rotation,0);
                foreach(var offset in local){Vector3 rotated=rotation*new Vector3(offset.x,0,offset.y);var point=new Vector3(center.x+rotated.x,0,center.y+rotated.z);if(IsNavigationPointValid(point,clearance,allowedSections))anchors.Add(point);}
            }
            return anchors;
        }

        public bool IsNavigationPointValid(Vector3 point,float clearance,string[] allowedSections=null)
        {
            bool inside=false;
            if(!Definition)inside=Mathf.Abs(point.x)<=13-clearance&&Mathf.Abs(point.z)<=10-clearance;
            else
            {
                var allowed=allowedSections??Array.Empty<string>();
                foreach(var section in Definition.sections)
                {
                    if(section.transitionPath||(allowed.Length>0&&Array.IndexOf(allowed,section.id)<0))continue;
                    Vector2 center=section.center*Definition.layoutScale,size=section.size*Definition.layoutScale;Vector3 delta=new Vector3(point.x-center.x,0,point.z-center.y);Vector3 local=Quaternion.Inverse(Quaternion.Euler(0,section.rotation,0))*delta;
                    if(Mathf.Abs(local.x)<=size.x*.5f-clearance&&Mathf.Abs(local.z)<=size.y*.5f-clearance){inside=true;break;}
                }
            }
            if(!inside)return false;
            int mask=Physics.DefaultRaycastLayers&~(1<<8);float radius=Mathf.Max(.2f,clearance);Vector3 bottom=new Vector3(point.x,radius+.25f,point.z),top=new Vector3(point.x,Mathf.Max(radius+.3f,2.3f),point.z);
            return !Physics.CheckCapsule(bottom,top,radius,mask,QueryTriggerInteraction.Ignore);
        }

        public Vector3 FindNearestNavigationPoint(Vector3 desired,float clearance,string[] allowedSections=null)
        {
            desired.y=0;if(IsNavigationPointValid(desired,clearance,allowedSections))return desired;var anchors=NavigationAnchors(clearance,allowedSections);float best=float.MaxValue;Vector3 selected=EntrancePosition;
            foreach(var anchor in anchors){float score=(anchor-desired).sqrMagnitude;if(score<best){best=score;selected=anchor;}}return selected;
        }
    }
}
