using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public sealed class ActorMotor : MonoBehaviour
    {
        private Combatant actor;
        private CharacterController controller;
        private LargeBodyNavigationSafety navigationSafety;
        private RoomView room;
        private readonly List<Collider> ignoredInternalObstacles=new List<Collider>();
        private Vector3 desired, impulse, dashDirection;
        private Vector3 playerDashEndpoint;
        private Vector3 lastSafePosition;
        private float stunUntil, dashUntil, dashSpeed, dashReadyAt;
        private float attackCommitUntil, attackMoveMultiplier = 1;
        private bool playerDashActive;
        private const float FallRecoveryHeight = -2f;
        private static readonly Vector3[] SupportOffsets =
        {
            Vector3.zero,
            new Vector3(.28f, 0, 0),
            new Vector3(-.28f, 0, 0),
            new Vector3(0, 0, .28f),
            new Vector3(0, 0, -.28f)
        };
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public bool IsDashing => Time.time < dashUntil;
        public bool IsMoving => IsDashing || desired.sqrMagnitude>.02f || impulse.sqrMagnitude>.02f;
        public bool IsStunned => Time.time < stunUntil || actor.Statuses.Stunned;
        public float DashCooldown => Mathf.Max(0, dashReadyAt - Time.time);
        public float LastDashTime { get; private set; } = -99;
        public int FallRecoveries { get; private set; }
        public Vector3 LastDashStart { get; private set; }
        public Vector3 LastDashRequestedEndpoint { get; private set; }
        public Vector3 LastDashResolvedEndpoint { get; private set; }
        public bool HasDashDebugSample { get; private set; }
        public bool DashCollisionOverrideActive=>playerDashActive&&ignoredInternalObstacles.Count>0;
        public int IgnoredInternalObstacleCount=>ignoredInternalObstacles.Count;
        public event Action DashStarted;
        public void Configure(Combatant owner)
        {
            actor = owner;
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = .4f; controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .25f; controller.minMoveDistance = 0;
            lastSafePosition = transform.position;
        }
        public void SetMove(Vector3 direction) { direction.y = 0;if(navigationSafety)direction=navigationSafety.FilterDirection(direction);desired = Vector3.ClampMagnitude(direction, 1); }
        public void SetNavigationSafety(LargeBodyNavigationSafety safety,float bodyRadius)
        {
            navigationSafety=safety;if(!controller)return;controller.radius=Mathf.Max(.4f,bodyRadius);controller.height=Mathf.Max(1.7f,bodyRadius*2.15f);controller.center=new Vector3(0,controller.height*.5f+.05f,0);controller.stepOffset=Mathf.Min(.35f,controller.height*.2f);
        }
        public void SetFacing(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) return;
            Facing = direction.normalized;
            transform.rotation = Quaternion.LookRotation(Facing);
        }
        public bool TryDash()
        {
            if (!actor.Alive || !actor.Combat.CanMove || IsStunned || IsDashing || DashCooldown > 0) return false;
            dashReadyAt = Time.time + 1.15f;
            LastDashTime = Time.time;
            Vector3 direction=desired.sqrMagnitude>.01f?desired.normalized:Facing;const float speed=22,duration=.22f;room=room?room:FindAnyObjectByType<RoomView>();LastDashStart=Flat(transform.position);LastDashRequestedEndpoint=LastDashStart+direction*speed*duration;LastDashResolvedEndpoint=room?room.ResolveDashEndpoint(LastDashStart,direction,speed*duration,controller.radius):LastDashRequestedEndpoint;HasDashDebugSample=true;
            Vector3 travel=Flat(LastDashResolvedEndpoint-transform.position);playerDashEndpoint=LastDashResolvedEndpoint;dashDirection=travel.sqrMagnitude>.0001f?travel.normalized:Vector3.zero;dashSpeed=speed;dashUntil=travel.sqrMagnitude>.0001f?Time.time+travel.magnitude/speed+.02f:0;playerDashActive=travel.sqrMagnitude>.0001f;if(playerDashActive)SetInternalObstacleCollisions(true);
            actor.Health.InvulnerableUntil = Time.time + .15f;
            DashStarted?.Invoke();
            return true;
        }
        public void Lunge(Vector3 direction, float speed, float duration)
        { EndPlayerDash();if(navigationSafety)direction=navigationSafety.FilterBurstDirection(direction,speed*duration);if(direction.sqrMagnitude<.01f){dashUntil=0;return;}dashDirection = direction.normalized; dashSpeed = speed; dashUntil = Time.time + duration; }
        public void Impact(Vector3 velocity, float stun)
        {
            if (actor.IsBoss) velocity *= .15f;
            impulse += velocity;
            stunUntil = Mathf.Max(stunUntil, Time.time + Mathf.Min(stun, actor.IsPlayer ? .15f : .65f));
        }
        public void CommitAttack(float duration, float movementMultiplier)
        { attackCommitUntil = Time.time + Mathf.Max(0, duration); attackMoveMultiplier = Mathf.Clamp(movementMultiplier, .2f, 1.25f); }
        public void Stop() { EndPlayerDash();desired = impulse = Vector3.zero; dashUntil = dashReadyAt = stunUntil = 0;if(navigationSafety)navigationSafety.ClearIntent(); }
        public void Teleport(Vector3 position)
        {
            controller.enabled = false; transform.position = position; controller.enabled = true; Stop();
            if (HasStableSupport(position)) lastSafePosition = position;
        }
        private void Update()
        {
            if(RoomView.DashCollisionDebugVisible)DrawDashDebugWorld();
            if (!actor.Alive) return;
            if (transform.position.y < FallRecoveryHeight) { RecoverFromFall(); return; }
            if (!actor.Combat.CanMove){EndPlayerDash();return;}
            if(playerDashActive)
            {
                Vector3 remaining=Flat(playerDashEndpoint-transform.position);float step=dashSpeed*Time.deltaTime;if(remaining.magnitude<=step+.03f||Time.time>=dashUntil){if(remaining.sqrMagnitude>.0001f)controller.Move(Vector3.ClampMagnitude(remaining,step+0.1f)+Vector3.down*16*Time.deltaTime);EndPlayerDash();}else controller.Move(remaining.normalized*step+Vector3.down*16*Time.deltaTime);return;
            }
            float moveFactor = Time.time < attackCommitUntil ? attackMoveMultiplier : 1;
            Vector3 velocity = IsDashing ? dashDirection * dashSpeed : IsStunned ? Vector3.zero : desired * actor.Speed * moveFactor;
            controller.Move((velocity + impulse + Vector3.down * 16) * Time.deltaTime);
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, 24 * Time.deltaTime);
            if (controller.isGrounded && HasStableSupport(transform.position)) lastSafePosition = transform.position;
        }
        private void SetInternalObstacleCollisions(bool ignore)
        {
            if(!controller)return;if(ignore){ignoredInternalObstacles.Clear();if(room)foreach(var obstacle in room.InternalObstacleColliders)if(obstacle){Physics.IgnoreCollision(controller,obstacle,true);ignoredInternalObstacles.Add(obstacle);}}else{foreach(var obstacle in ignoredInternalObstacles)if(obstacle)Physics.IgnoreCollision(controller,obstacle,false);ignoredInternalObstacles.Clear();}
        }
        private void EndPlayerDash(){if(!playerDashActive&&ignoredInternalObstacles.Count==0)return;playerDashActive=false;dashUntil=0;SetInternalObstacleCollisions(false);}
        private bool HasStableSupport(Vector3 position)
        {
            int mask = Physics.DefaultRaycastLayers & ~(1 << gameObject.layer);
            Vector3 origin = position + Vector3.up * .35f;
            foreach (var offset in SupportOffsets)
                if (!Physics.Raycast(origin + offset, Vector3.down, .9f, mask, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }
        private void RecoverFromFall()
        {
            controller.enabled = false;
            transform.position = lastSafePosition + Vector3.up * .05f;
            controller.enabled = true;
            Stop();
            FallRecoveries++;
        }
        private void DrawDashDebugWorld(){if(!HasDashDebugSample)return;Debug.DrawLine(LastDashStart+Vector3.up*.3f,LastDashRequestedEndpoint+Vector3.up*.3f,Color.yellow);Debug.DrawLine(LastDashStart+Vector3.up*.38f,LastDashResolvedEndpoint+Vector3.up*.38f,Color.green);Debug.DrawLine(LastDashResolvedEndpoint,LastDashResolvedEndpoint+Vector3.up*1.5f,Color.magenta);}
        private void OnGUI()
        {
            if(!RoomView.DashCollisionDebugVisible||!HasDashDebugSample||!actor||!actor.IsPlayer||!Camera.main)return;Vector3 a=Camera.main.WorldToScreenPoint(LastDashStart+Vector3.up*.35f),requested=Camera.main.WorldToScreenPoint(LastDashRequestedEndpoint+Vector3.up*.35f),resolved=Camera.main.WorldToScreenPoint(LastDashResolvedEndpoint+Vector3.up*.35f);if(a.z<=0||requested.z<=0||resolved.z<=0)return;Vector2 from=new Vector2(a.x,Screen.height-a.y),toRequested=new Vector2(requested.x,Screen.height-requested.y),toResolved=new Vector2(resolved.x,Screen.height-resolved.y);DrawGuiLine(from,toRequested,Color.yellow,2);DrawGuiLine(from,toResolved,Color.green,3);GUI.Label(new Rect(toResolved.x-55,toResolved.y-24,120,24),"◆ RESOLVED");
        }
        private static void DrawGuiLine(Vector2 from,Vector2 to,Color color,float width){Matrix4x4 matrix=GUI.matrix;Color old=GUI.color;GUI.color=color;GUIUtility.RotateAroundPivot(Vector2.SignedAngle(Vector2.right,to-from),from);GUI.DrawTexture(new Rect(from.x,from.y-width*.5f,Vector2.Distance(from,to),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;}
        private static Vector3 Flat(Vector3 value){value.y=0;return value;}
        private void OnDisable(){EndPlayerDash();}
    }
}
