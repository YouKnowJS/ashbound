using System;
using UnityEngine;

namespace Ashbound
{
    public sealed class ActorMotor : MonoBehaviour
    {
        private Combatant actor;
        private CharacterController controller;
        private Vector3 desired, impulse, dashDirection;
        private float stunUntil, dashUntil, dashSpeed, dashReadyAt;
        private float attackCommitUntil, attackMoveMultiplier = 1;
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public bool IsDashing => Time.time < dashUntil;
        public bool IsStunned => Time.time < stunUntil || actor.Statuses.Stunned;
        public float DashCooldown => Mathf.Max(0, dashReadyAt - Time.time);
        public float LastDashTime { get; private set; } = -99;
        public event Action DashStarted;
        public void Configure(Combatant owner)
        {
            actor = owner;
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.7f; controller.radius = .4f; controller.center = new Vector3(0, .9f, 0);
            controller.stepOffset = .25f; controller.minMoveDistance = 0;
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
        }
        private void Update()
        {
            if (!actor.Alive || !actor.Combat.CanMove) return;
            float moveFactor = Time.time < attackCommitUntil ? attackMoveMultiplier : 1;
            Vector3 velocity = IsDashing ? dashDirection * dashSpeed : IsStunned ? Vector3.zero : desired * actor.Speed * moveFactor;
            controller.Move((velocity + impulse + Vector3.down * 16) * Time.deltaTime);
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, 24 * Time.deltaTime);
        }
    }
}
