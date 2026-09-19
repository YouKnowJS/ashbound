using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public enum CinderRegentAttack { WideSweep,GroundEruption,ChargeRush,ExpandingFlameRing,SummonHazards }

    public sealed class CinderRegentController : MonoBehaviour
    {
        private Combatant actor;private BossDefinition definition;private bool busy;private int pattern;private float nextAttack;private LargeBodyNavigationSafety navigationSafety;private RoomView room;private CinderRegentAttack? previousAttack;
        public bool SecondPhase { get; private set; }
        public CinderRegentAttack? LastAttack { get; private set; }
        public int DistinctAttackCount=>Enum.GetValues(typeof(CinderRegentAttack)).Length;
        public int PhaseTransitionCount { get; private set; }
        public float RecoveryRemaining=>Mathf.Max(0,nextAttack-Time.time);
        public event Action PhaseChanged;
        public event Action<CinderRegentAttack> AttackStarted;
        public void Configure(Combatant owner,BossDefinition data){actor=owner;definition=data;navigationSafety=GetComponent<LargeBodyNavigationSafety>();room=FindAnyObjectByType<RoomView>();}
        private void Update()
        {
            if(!actor.Alive||!actor.Combat.Active)return;
            if(!SecondPhase&&actor.Health.CurrentHealth/actor.Health.MaxHealth<=definition.secondPhaseThreshold){SecondPhase=true;PhaseTransitionCount++;CombatVfx.Ring(transform.position,5,Palette.Gold,1.1f,.22f,true);PlayHook(definition.phaseTransitionVfx,definition.phaseTransitionAudio);PhaseChanged?.Invoke();}
            if(busy)return;var target=actor.Combat.NearestEnemy(actor);if(!target)return;if(navigationSafety)navigationSafety.SetTarget(target);Vector3 offset=target.transform.position-transform.position;actor.Motor.SetFacing(offset);actor.Motor.SetMove(offset.magnitude>definition.preferredAttackDistance?offset.normalized*.45f:Vector3.zero);if(Time.time>=nextAttack)StartCoroutine(Execute(SelectAttack(offset.magnitude),target));
        }
        private CinderRegentAttack SelectAttack(float distance)
        {
            var candidates=new List<CinderRegentAttack>{CinderRegentAttack.WideSweep,CinderRegentAttack.GroundEruption,CinderRegentAttack.ChargeRush,CinderRegentAttack.ExpandingFlameRing};if(SecondPhase)candidates.Add(CinderRegentAttack.SummonHazards);
            if(distance<3.2f)candidates.Remove(CinderRegentAttack.ChargeRush);if(distance>6)candidates.Remove(CinderRegentAttack.WideSweep);if(previousAttack.HasValue&&candidates.Count>1)candidates.Remove(previousAttack.Value);var selected=candidates[pattern++%candidates.Count];previousAttack=selected;return selected;
        }
        private IEnumerator Execute(CinderRegentAttack attack,Combatant target)
        {
            busy=true;LastAttack=attack;AttackStarted?.Invoke(attack);actor.Motor.SetMove(Vector3.zero);if(actor.View)actor.View.PlayAttack(.55f);Vector3 direction=Flat(target.transform.position-transform.position).normalized;float warning=definition.telegraphDuration*(SecondPhase?.72f:1);PlayHook(Vfx(attack),Audio(attack));
            switch(attack)
            {
                case CinderRegentAttack.WideSweep:yield return WideSweep(direction,warning);break;
                case CinderRegentAttack.GroundEruption:yield return GroundEruption(target,warning);break;
                case CinderRegentAttack.ChargeRush:yield return Charge(direction,warning);break;
                case CinderRegentAttack.ExpandingFlameRing:yield return FlameRing(warning);break;
                case CinderRegentAttack.SummonHazards:yield return SummonHazards(target,warning);break;
            }
            nextAttack=Time.time+(SecondPhase?definition.phaseTwoRecovery:definition.recoveryDuration);busy=false;
        }
        private IEnumerator WideSweep(Vector3 direction,float warning)
        {
            float arc=SecondPhase?300:220;CombatVfx.Arc(transform.position,direction,definition.sweepRadius,arc,Palette.Gold);yield return new WaitForSeconds(warning*.7f);if(!Valid())yield break;foreach(var other in actor.Combat.Actors.ToArray()){if(!actor.Combat.AreEnemies(actor,other))continue;Vector3 offset=Flat(other.transform.position-transform.position);if(offset.magnitude>definition.sweepRadius)continue;if(SecondPhase||Vector3.Angle(direction,offset)<=arc*.5f)actor.Combat.DealDamage(other,new DamageInfo(actor,definition.sweepDamage,DamageKind.Ability,offset.normalized,.18f,7));}
        }
        private IEnumerator GroundEruption(Combatant target,float warning)
        {
            Vector3 center=ResolveArenaPoint(target.transform.position);AreaAttack.Spawn(actor,center,definition.areaRadius,definition.areaDamage,warning);if(SecondPhase){Vector3 side=Vector3.Cross(Flat(target.transform.position-transform.position).normalized,Vector3.up)*3;AreaAttack.Spawn(actor,ResolveArenaPoint(center+side),definition.areaRadius*.72f,definition.areaDamage*.8f,warning+.22f);AreaAttack.Spawn(actor,ResolveArenaPoint(center-side),definition.areaRadius*.72f,definition.areaDamage*.8f,warning+.38f);}yield return new WaitForSeconds(warning+.45f);
        }
        private IEnumerator Charge(Vector3 direction,float warning)
        {
            CombatVfx.Direction(transform.position,direction,definition.chargeDistance,Palette.Danger,warning);yield return new WaitForSeconds(warning);if(!Valid())yield break;actor.Motor.Lunge(direction,definition.chargeSpeed,definition.chargeDuration);var hit=new HashSet<string>();float remaining=definition.chargeDuration;while(remaining>0&&Valid()){foreach(var other in actor.Combat.Actors)if(actor.Combat.AreEnemies(actor,other)&&Vector3.Distance(transform.position,other.transform.position)<2.2f&&hit.Add(other.Id))actor.Combat.DealDamage(other,new DamageInfo(actor,definition.chargeDamage,DamageKind.Ability,direction,.15f,9));remaining-=Time.deltaTime;yield return null;}
        }
        private IEnumerator FlameRing(float warning)
        {
            int rings=SecondPhase?4:3;for(int i=1;i<=rings;i++)CombatVfx.Ring(transform.position,definition.flameRingRadius*i/rings,Palette.Danger,warning+i*.16f,.14f);yield return new WaitForSeconds(warning);for(int i=1;i<=rings&&Valid();i++){float outer=definition.flameRingRadius*i/rings,inner=Mathf.Max(0,outer-definition.flameRingWidth);foreach(var other in actor.Combat.Actors.ToArray()){float distance=Flat(other.transform.position-transform.position).magnitude;if(actor.Combat.AreEnemies(actor,other)&&distance>=inner&&distance<=outer)actor.Combat.DealDamage(other,new DamageInfo(actor,definition.flameRingDamage,DamageKind.Burning,(other.transform.position-transform.position).normalized,.08f,3));}yield return new WaitForSeconds(.16f);}
        }
        private IEnumerator SummonHazards(Combatant target,float warning)
        {
            Vector3 axis=Flat(target.transform.position-transform.position).normalized;for(int i=-1;i<=1;i++){Vector3 point=ResolveArenaPoint(transform.position+axis*(2.5f+i*2.5f)+Vector3.Cross(axis,Vector3.up)*i*2);AreaAttack.Spawn(actor,point,1.8f,definition.hazardDamage,warning+i*.08f,definition.hazardDuration);}yield return new WaitForSeconds(warning+.3f);
        }
        private Vector3 ResolveArenaPoint(Vector3 point)=>room?room.FindNearestNavigationPoint(Flat(point),.55f,definition.allowedArenaSections):Flat(point);
        private void PlayHook(GameObject prefab,AudioClip clip){if(prefab)Instantiate(prefab,transform.position,Quaternion.identity);if(clip)AudioSource.PlayClipAtPoint(clip,transform.position);}
        private GameObject Vfx(CinderRegentAttack attack)=>attack==CinderRegentAttack.WideSweep?definition.sweepVfx:attack==CinderRegentAttack.GroundEruption?definition.eruptionVfx:attack==CinderRegentAttack.ChargeRush?definition.chargeVfx:attack==CinderRegentAttack.ExpandingFlameRing?definition.flameRingVfx:definition.summonVfx;
        private AudioClip Audio(CinderRegentAttack attack)=>attack==CinderRegentAttack.WideSweep?definition.sweepAudio:attack==CinderRegentAttack.GroundEruption?definition.eruptionAudio:attack==CinderRegentAttack.ChargeRush?definition.chargeAudio:attack==CinderRegentAttack.ExpandingFlameRing?definition.flameRingAudio:definition.summonAudio;
        private bool Valid()=>actor&&actor.Alive&&actor.Combat.State==RunState.BossFight;
        private static Vector3 Flat(Vector3 value){value.y=0;return value;}
    }
}
