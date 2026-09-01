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
        private Combatant actor;
        private float elevation,attackTimer,attackDuration=.25f,skillTimer,hitTimer,dashTimer;
        private Vector3 baseScale;
        public void Build(Combatant actor, Color color, float scale, bool angular = false)
        {
            this.actor=actor;
            normal = color;
            silhouette = new GameObject("Silhouette").transform;
            silhouette.SetParent(transform, false); silhouette.localScale = baseScale=Vector3.one * scale;
            var torso = PrimitiveFactory.Shape("Body", angular ? PrimitiveType.Cube : PrimitiveType.Capsule, silhouette,
                new Vector3(0, .88f, 0), new Vector3(.7f, angular ? 1.3f : .75f, .7f), color);
            body = torso.GetComponent<Renderer>();
            PrimitiveFactory.Shape("Face", PrimitiveType.Cube, silhouette, new Vector3(0, 1.55f, .25f), new Vector3(.42f, .18f, .23f), Palette.Gold);
            if (actor.IsPlayer || actor.Faction == Faction.Corrupted)
                PrimitiveFactory.Shape("Blade", PrimitiveType.Cube, silhouette, new Vector3(.56f, .72f, .66f), new Vector3(.12f, .12f, 1.3f), Palette.Player);
            var ring = CombatVfx.Ring(transform.position, .64f * scale, color, float.MaxValue, .05f);
            ring.transform.SetParent(transform, true);
            actor.Attacks.BasicAttack+=()=>PlayAttack();actor.Attacks.AbilityUsed+=()=>skillTimer=.42f;actor.Motor.DashStarted+=()=>dashTimer=.24f;
        }
        public void PlayAttack(float duration=.25f){attackDuration=Mathf.Max(.01f,duration);attackTimer=Mathf.Max(attackTimer,attackDuration);}
        private void Update()
        {
            if(!silhouette||!actor||!actor.Alive)return;float dt=Time.unscaledDeltaTime;attackTimer=Mathf.Max(0,attackTimer-dt);skillTimer=Mathf.Max(0,skillTimer-dt);hitTimer=Mathf.Max(0,hitTimer-dt);dashTimer=Mathf.Max(0,dashTimer-dt);
            bool moving=actor.Motor!=null&&actor.Motor.IsMoving;float bob=Mathf.Sin(Time.unscaledTime*(moving?11f:2.2f))*(moving?.075f:.025f);silhouette.localPosition=Vector3.up*(elevation+bob);
            float attack=attackTimer>0?Mathf.Sin((1-attackTimer/attackDuration)*Mathf.PI)*24:0;float skill=skillTimer>0?Mathf.Sin((1-skillTimer/.42f)*Mathf.PI)*.12f:0;float recoil=hitTimer>0?Mathf.Sin(hitTimer/.11f*Mathf.PI)*-10:0;float lean=dashTimer>0?18:moving?Mathf.Sin(Time.unscaledTime*11)*3:0;
            silhouette.localRotation=Quaternion.Euler(lean+recoil,attack,0);silhouette.localScale=baseScale*(1+skill);
        }
        public void SetDead(bool dead)
        {
            if (silhouette) silhouette.localRotation = dead ? Quaternion.Euler(0, 0, 80) : Quaternion.identity;
            if (body) body.sharedMaterial = PrimitiveFactory.Material(dead ? new Color(.2f, .2f, .22f) : normal);
        }
        public void Flash(Color color, float seconds = .09f)
        {
            hitTimer=Mathf.Max(hitTimer,.11f);
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
        public void SetElement(ElementTag element)
        {
            normal=WeaponSkillExecutor.Tint(WeaponSkillExecutor.Element(element));if(body)body.sharedMaterial=PrimitiveFactory.Material(normal);
        }
        public void SetElevation(float height) { elevation=height;if (silhouette) silhouette.localPosition = Vector3.up * height; }
        public void SetVisible(bool visible) { if (silhouette) silhouette.gameObject.SetActive(visible); }
    }
}
