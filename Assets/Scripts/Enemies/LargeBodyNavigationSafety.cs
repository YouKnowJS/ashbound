using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class LargeBodyNavigationSafety:MonoBehaviour
    {
        private static readonly List<LargeBodyNavigationSafety> Live=new List<LargeBodyNavigationSafety>();
        private static readonly float[] SteeringAngles={0,35,-35,70,-70,110,-110,180};
        private Combatant actor,target;
        private RoomView room;
        private RunManager run;
        private string[] allowedSections=Array.Empty<string>();
        private Vector3 intendedDirection,appliedDirection,lastSamplePosition,recoveryDirection;
        private float radius,clearance,preferredZone=.8f,stuckThreshold=1.5f,minimumDisplacement=.12f,nextSample,recoveryUntil;
        private int attemptsBeforeReposition=3,recoveryAttempts;
        private readonly List<Vector3> recoveryCandidates=new List<Vector3>();

        public static bool DebugVisible { get; set; }
        public static IReadOnlyList<LargeBodyNavigationSafety> Instances=>Live;
        public static LargeBodyNavigationSafety ActiveBoss=>Live.FirstOrDefault(x=>x&&x.actor&&x.actor.IsBoss&&x.actor.Alive);
        public float NavigationRadius=>radius;
        public float Clearance=>clearance;
        public float StuckSeconds { get; private set; }
        public float TotalStuckSeconds { get; private set; }
        public int RecoveryAttempts=>recoveryAttempts;
        public int EmergencyRepositions { get; private set; }
        public Vector3 IntendedDirection=>intendedDirection;
        public Vector3 AppliedDirection=>appliedDirection;
        public IReadOnlyList<Vector3> RecoveryCandidates=>recoveryCandidates;
        public IReadOnlyList<Vector3> SafeAnchors=>room?room.NavigationAnchors(radius+clearance,allowedSections):Array.Empty<Vector3>();

        public void Configure(Combatant owner,float bodyRadius,float obstacleClearance,float movementZone,string[] sections,float detectionSeconds,float stuckDisplacement,int attempts)
        {
            actor=owner;radius=Mathf.Max(.4f,bodyRadius);clearance=Mathf.Max(0,obstacleClearance);preferredZone=Mathf.Clamp(movementZone,.25f,1);allowedSections=sections??Array.Empty<string>();stuckThreshold=Mathf.Max(.5f,detectionSeconds);minimumDisplacement=Mathf.Max(.01f,stuckDisplacement);attemptsBeforeReposition=Mathf.Max(1,attempts);
            room=FindAnyObjectByType<RoomView>();run=FindAnyObjectByType<PrototypeBootstrap>()?.Run;lastSamplePosition=transform.position;nextSample=Time.time+.25f;owner.Motor.SetNavigationSafety(this,radius);
        }

        public void SetTarget(Combatant value){target=value;}
        public void ClearIntent(){intendedDirection=appliedDirection=Vector3.zero;}
        public Vector3 FilterDirection(Vector3 requested)
        {
            requested.y=0;intendedDirection=Vector3.ClampMagnitude(requested,1);
            if(intendedDirection.sqrMagnitude<.01f){appliedDirection=Vector3.zero;return Vector3.zero;}
            if(Time.time<recoveryUntil&&recoveryDirection.sqrMagnitude>.01f){appliedDirection=recoveryDirection;return recoveryDirection*requested.magnitude;}
            appliedDirection=BestDirection(intendedDirection,Mathf.Max(1.1f,radius+clearance+.55f));return appliedDirection*requested.magnitude;
        }
        public Vector3 FilterBurstDirection(Vector3 requested,float distance)
        {
            requested=Flat(requested);if(requested.sqrMagnitude<.01f)return Vector3.zero;intendedDirection=requested.normalized;appliedDirection=BestDirection(intendedDirection,Mathf.Max(radius+clearance+1,distance));return appliedDirection;
        }

        private Vector3 BestDirection(Vector3 desired,float probeDistance)
        {
            recoveryCandidates.Clear();float best=float.NegativeInfinity;Vector3 selected=Vector3.zero;Vector3 targetDirection=target?Flat(target.transform.position-transform.position).normalized:desired.normalized;
            foreach(float angle in SteeringAngles)
            {
                Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*desired.normalized;Vector3 point=transform.position+direction*probeDistance;recoveryCandidates.Add(point);
                if(!ProbeClear(direction,probeDistance)||room&&!room.IsNavigationPointValid(point,radius+clearance,allowedSections))continue;
                float score=Vector3.Dot(direction,desired.normalized)*2+Vector3.Dot(direction,targetDirection)-Mathf.Abs(angle)/180f;
                if(score>best){best=score;selected=direction;}
            }
            return selected;
        }

        private bool ProbeClear(Vector3 direction,float distance)
        {
            int mask=Physics.DefaultRaycastLayers&~(1<<8);float probeRadius=radius+clearance;Vector3 origin=new Vector3(transform.position.x,probeRadius+.25f,transform.position.z);
            return !Physics.SphereCast(origin,probeRadius,direction,out _,distance,mask,QueryTriggerInteraction.Ignore);
        }

        private void Update()
        {
            if(!actor||!actor.Alive)return;if(!room)room=FindAnyObjectByType<RoomView>();if(!run)run=FindAnyObjectByType<PrototypeBootstrap>()?.Run;
            if(DebugVisible)DrawDebugWorld();
            if(Time.time<nextSample)return;float elapsed=Mathf.Max(.01f,Time.time-(nextSample-.25f));nextSample=Time.time+.25f;float displacement=Flat(transform.position-lastSamplePosition).magnitude;lastSamplePosition=transform.position;
            bool wantsMovement=intendedDirection.sqrMagnitude>.04f&&actor.Combat.CanMove&&!actor.Motor.IsStunned&&(target==null||Flat(target.transform.position-transform.position).magnitude>radius+1.5f);
            if(wantsMovement&&displacement<minimumDisplacement){StuckSeconds+=elapsed;TotalStuckSeconds+=elapsed;}else if(displacement>=minimumDisplacement){StuckSeconds=0;recoveryAttempts=0;}
            else StuckSeconds=Mathf.Max(0,StuckSeconds-elapsed);
            if(StuckSeconds>=stuckThreshold)AttemptRecovery(false);
        }

        public void ForceStuckSimulation(){StuckSeconds=stuckThreshold;intendedDirection=actor?actor.Motor.Facing:Vector3.forward;AttemptRecovery(false);}
        public void ForceRecovery(){AttemptRecovery(true);}
        private void AttemptRecovery(bool forced)
        {
            if(!actor||!room)return;recoveryAttempts++;string arena=room.Definition?room.Definition.id:"legacy";run?.Telemetry.NavigationStuck(actor,arena,Mathf.Max(StuckSeconds,forced?stuckThreshold:0));
            StuckSeconds=0;Vector3 desired=target?Flat(target.transform.position-transform.position).normalized:(intendedDirection.sqrMagnitude>.01f?intendedDirection:actor.Motor.Facing);float probe=Mathf.Max(2.2f,(radius+clearance)*2);
            if(recoveryAttempts<attemptsBeforeReposition)
            {
                recoveryDirection=BestDirection(Quaternion.AngleAxis(recoveryAttempts%2==0?-55:55,Vector3.up)*desired,probe);
                if(recoveryDirection.sqrMagnitude<.01f)recoveryDirection=BestDirection(-desired,probe);
                if(recoveryDirection.sqrMagnitude>.01f){recoveryUntil=Time.time+.8f;run?.Telemetry.NavigationRecovery(actor,arena,false);return;}
            }
            Vector3? anchor=BestSafeAnchor();
            if(anchor.HasValue&&recoveryAttempts<attemptsBeforeReposition+1)
            {
                recoveryDirection=Flat(anchor.Value-transform.position).normalized;recoveryUntil=Time.time+1.15f;run?.Telemetry.NavigationRecovery(actor,arena,false);return;
            }
            if(anchor.HasValue)
            {
                CombatVfx.Ring(transform.position,radius+clearance,Palette.Gold,.22f,.12f,true);actor.Motor.Teleport(anchor.Value+Vector3.up*.05f);CombatVfx.Ring(anchor.Value,radius+clearance,Palette.Gold,.45f,.12f,true);EmergencyRepositions++;recoveryAttempts=0;recoveryUntil=0;run?.Telemetry.NavigationRecovery(actor,arena,true);
            }
        }

        private Vector3? BestSafeAnchor()
        {
            if(!room)return null;var anchors=room.NavigationAnchors(radius+clearance,allowedSections);Vector3? best=null;float bestScore=float.MaxValue;
            foreach(var anchor in anchors)
            {
                if(!room.IsNavigationPointValid(anchor,radius+clearance,allowedSections)||Flat(anchor-transform.position).sqrMagnitude<4)continue;bool overlapsPlayer=actor.Combat.Actors.Any(x=>x&&x.IsPlayer&&x.Alive&&Flat(x.transform.position-anchor).magnitude<radius+2.2f);if(overlapsPlayer)continue;
                float targetScore=target?Flat(anchor-target.transform.position).sqrMagnitude*.15f:0;float zonePenalty=room.Definition?Mathf.Max(0,Mathf.Abs(anchor.x)-room.Definition.ScaledTechnicalBounds.x*.5f*preferredZone)+Mathf.Max(0,Mathf.Abs(anchor.z)-room.Definition.ScaledTechnicalBounds.y*.5f*preferredZone):0;float score=Flat(anchor-transform.position).sqrMagnitude+targetScore+zonePenalty*20;if(score<bestScore){bestScore=score;best=anchor;}
            }
            return best;
        }

        private void DrawDebugWorld()
        {
            Color color=StuckSeconds>0?Color.red:Color.cyan;Debug.DrawLine(transform.position+Vector3.up*.2f,transform.position+Vector3.up*.2f+intendedDirection*3,Color.yellow);Debug.DrawLine(transform.position+Vector3.up*.35f,transform.position+Vector3.up*.35f+appliedDirection*3,color);
            foreach(var point in recoveryCandidates)Debug.DrawLine(transform.position+Vector3.up*.1f,point+Vector3.up*.1f,new Color(.3f,.8f,1));
        }

        private void OnGUI()
        {
            if(!DebugVisible||!actor||!Camera.main)return;Vector3 screen=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*3);if(screen.z>0)GUI.Label(new Rect(screen.x-100,Screen.height-screen.y-42,200,42),actor.DisplayName+"  r="+radius.ToString("0.00")+"\nStuck "+StuckSeconds.ToString("0.00")+"s · recovery "+recoveryAttempts);
            foreach(var anchor in SafeAnchors){Vector3 p=Camera.main.WorldToScreenPoint(anchor+Vector3.up*.15f);if(p.z>0)GUI.Label(new Rect(p.x-8,Screen.height-p.y-8,20,20),"◇");}
            foreach(var point in recoveryCandidates){Vector3 p=Camera.main.WorldToScreenPoint(point+Vector3.up*.2f);if(p.z>0)GUI.Label(new Rect(p.x-8,Screen.height-p.y-8,20,20),"·");}
        }
        private static Vector3 Flat(Vector3 value){value.y=0;return value;}
        private void OnEnable(){if(!Live.Contains(this))Live.Add(this);}
        private void OnDisable(){Live.Remove(this);}
    }
}
