using System.Collections;
using UnityEngine;

namespace Ashbound
{
    public sealed class ActorView : MonoBehaviour
    {
        private Transform silhouette;
        private Renderer body;
        private GameObject corruptionCrown;
        private Color normal;
        public void Build(Combatant actor, Color color, float scale, bool angular = false)
        {
            normal = color;
            silhouette = new GameObject("Silhouette").transform;
            silhouette.SetParent(transform, false); silhouette.localScale = Vector3.one * scale;
            var torso = PrimitiveFactory.Shape("Body", angular ? PrimitiveType.Cube : PrimitiveType.Capsule, silhouette,
                new Vector3(0, .88f, 0), new Vector3(.7f, angular ? 1.3f : .75f, .7f), color);
            body = torso.GetComponent<Renderer>();
            PrimitiveFactory.Shape("Face", PrimitiveType.Cube, silhouette, new Vector3(0, 1.55f, .25f), new Vector3(.42f, .18f, .23f), Palette.Gold);
            if (actor.IsPlayer || actor.Faction == Faction.Corrupted)
                PrimitiveFactory.Shape("Blade", PrimitiveType.Cube, silhouette, new Vector3(.56f, .72f, .66f), new Vector3(.12f, .12f, 1.3f), Palette.Player);
            var ring = CombatVfx.Ring(transform.position, .64f * scale, color, float.MaxValue, .05f);
            ring.transform.SetParent(transform, true);
        }
        public void SetDead(bool dead)
        {
            if (silhouette) silhouette.localRotation = dead ? Quaternion.Euler(0, 0, 80) : Quaternion.identity;
            if (body) body.sharedMaterial = PrimitiveFactory.Material(dead ? new Color(.2f, .2f, .22f) : normal);
        }
        public void Flash(Color color, float seconds = .09f)
        {
            if (body && isActiveAndEnabled) StartCoroutine(FlashRoutine(color, seconds));
        }
        private IEnumerator FlashRoutine(Color color, float seconds)
        {
            body.sharedMaterial = PrimitiveFactory.Material(color);
            yield return new WaitForSecondsRealtime(seconds);
            if (body) body.sharedMaterial = PrimitiveFactory.Material(normal);
        }
        public void SetCorruption(BossCorruptionProfile profile)
        {
            normal = profile.tint;
            body.sharedMaterial = PrimitiveFactory.Material(normal);
            corruptionCrown = PrimitiveFactory.Shape("Ash crown", PrimitiveType.Cube, silhouette,
                new Vector3(0, 1.9f, 0), new Vector3(.6f, .15f, .6f), normal);
            corruptionCrown.transform.localRotation = Quaternion.Euler(0, 45, 0);
            CombatVfx.Ring(transform.position, .95f, normal, float.MaxValue, .13f).transform.SetParent(transform, true);
            if (profile.vfxPrefab) Instantiate(profile.vfxPrefab, transform);
        }
    }
}
