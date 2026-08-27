using System;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class AttackController : MonoBehaviour
    {
        private Combatant actor;
        private float nextAttack, nextAbility;
        public float AbilityCooldown => Mathf.Max(0, nextAbility - Time.time);
        public float AbilityDuration => actor.Corruption && actor.Corruption.overrideAbilityWithFireBurst ? actor.Corruption.burstCooldown : 7;
        public event Action BasicAttack;
        public void Configure(Combatant owner) { actor = owner; }
        public void ResetCooldowns() { nextAttack = nextAbility = 0; }
        public void ReduceCooldown(float seconds) { nextAbility -= seconds; }
        public bool TryAttack()
        {
            if (!CanAct() || Time.time < nextAttack || actor.Motor.IsDashing) return false;
            nextAttack = Time.time + actor.AttackInterval;
            CombatVfx.Arc(actor.transform.position, actor.Motor.Facing, actor.Weapon.reach, actor.Weapon.arcDegrees,
                actor.Corruption ? Palette.Corrupted : Palette.Player);
            float minimumDot = Mathf.Cos(actor.Weapon.arcDegrees * .5f * Mathf.Deg2Rad);
            foreach (var target in actor.Combat.Actors.ToArray())
            {
                if (!actor.Combat.AreEnemies(actor, target)) continue;
                Vector3 offset = target.transform.position - actor.transform.position; offset.y = 0;
                if (offset.sqrMagnitude <= actor.Weapon.reach * actor.Weapon.reach &&
                    (offset.sqrMagnitude < .3f || Vector3.Dot(offset.normalized, actor.Motor.Facing) >= minimumDot))
                    actor.Combat.DealDamage(target, new DamageInfo(actor, actor.Weapon.damage, DamageKind.Weapon,
                        offset.normalized, .12f, actor.Weapon.knockback, true, true));
            }
            BasicAttack?.Invoke();
            return true;
        }
        public bool TryAbility()
        {
            if (!CanAct() || AbilityCooldown > 0) return false;
            nextAbility = Time.time + AbilityDuration;
            if (actor.Corruption && actor.Corruption.overrideAbilityWithFireBurst)
            {
                CombatVfx.Pulse(transform.position, actor.Corruption.burstRadius, Palette.Corrupted);
                actor.Combat.DamageArea(actor, transform.position, actor.Corruption.burstRadius, actor.Corruption.burstDamage, DamageKind.Ability, .3f, 7);
            }
            else
            {
                actor.Health.Shield(20);
                CombatVfx.Pulse(transform.position, 3.5f, Palette.Player);
                actor.Combat.DamageArea(actor, transform.position, 3.5f, 36, DamageKind.Ability, .55f, 7);
            }
            return true;
        }
        private bool CanAct() => actor.Alive && actor.Combat.Active && !actor.Motor.IsStunned;
    }
}
