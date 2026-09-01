using System;
using UnityEngine;

namespace Ashbound
{
    public sealed class ActorMotor : MonoBehaviour
    {
        private Combatant actor;
        private CharacterController controller;
        private Vector3 desired, impulse, dashDirection;
        private Vector3 lastSafePosition;
        private float stunUntil, dashUntil, dashSpeed, dashReadyAt;
        private float attackCommitUntil, attackMoveMultiplier = 1;
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
        public event Action DashStarted;
        public void Configure(Combatant owner)
        {
            actor = owner;
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = .4f; controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .25f; controller.minMoveDistance = 0;
            lastSafePosition = transform.position;
        }
        public void SetMove(Vector3 direction) { direction.y = 0; desired = Vector3.ClampMagnitude(direction, 1); }
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
            Lunge(desired.sqrMagnitude > .01f ? desired : Facing, 22, .22f);
            actor.Health.InvulnerableUntil = Time.time + .15f;
            DashStarted?.Invoke();
            return true;
        }
        public void Lunge(Vector3 direction, float speed, float duration)
        { dashDirection = direction.normalized; dashSpeed = speed; dashUntil = Time.time + duration; }
        public void Impact(Vector3 velocity, float stun)
        {
            if (actor.IsBoss) velocity *= .15f;
            impulse += velocity;
            stunUntil = Mathf.Max(stunUntil, Time.time + Mathf.Min(stun, actor.IsPlayer ? .15f : .65f));
        }
        public void CommitAttack(float duration, float movementMultiplier)
        { attackCommitUntil = Time.time + Mathf.Max(0, duration); attackMoveMultiplier = Mathf.Clamp(movementMultiplier, .2f, 1.25f); }
        public void Stop() { desired = impulse = Vector3.zero; dashUntil = dashReadyAt = stunUntil = 0; }
        public void Teleport(Vector3 position)
        {
            controller.enabled = false; transform.position = position; controller.enabled = true; Stop();
            if (HasStableSupport(position)) lastSafePosition = position;
        }
        private void Update()
        {
            if (!actor.Alive) return;
            if (transform.position.y < FallRecoveryHeight) { RecoverFromFall(); return; }
            if (!actor.Combat.CanMove) return;
            float moveFactor = Time.time < attackCommitUntil ? attackMoveMultiplier : 1;
            Vector3 velocity = IsDashing ? dashDirection * dashSpeed : IsStunned ? Vector3.zero : desired * actor.Speed * moveFactor;
            controller.Move((velocity + impulse + Vector3.down * 16) * Time.deltaTime);
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, 24 * Time.deltaTime);
            if (controller.isGrounded && HasStableSupport(transform.position)) lastSafePosition = transform.position;
        }
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
    }
}
