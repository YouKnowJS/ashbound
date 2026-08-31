using System;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class AttackController : MonoBehaviour
    {
        private Combatant actor;
        private float nextAttack, nextAbility, lastAttack = -99;
        private int combo;
        private Combatant focusedTarget;
        private int focusedHits;
        public float AbilityCooldown => Mathf.Max(0, nextAbility - Time.time);
        public float AbilityDuration => Mathf.Max(1, (actor.Corruption && actor.Corruption.overrideAbilityWithFireBurst ? actor.Corruption.burstCooldown : actor.Weapon&&actor.Weapon.skill ? actor.Weapon.skill.cooldown : 7)
            * (1 - Mathf.Clamp(actor.Equipment.PassivePower(ArmorPassiveKind.Cooldown), 0, .75f)));
        public int Combo => combo;
        public event Action BasicAttack;
        public void Configure(Combatant owner) { actor = owner; }
        public void ResetCooldowns() { nextAttack = nextAbility = 0; combo = focusedHits = 0; focusedTarget = null; }
        public void ReduceCooldown(float seconds) { nextAbility -= seconds; }
        public void SetWeapon(WeaponDefinition weapon) { if (weapon) { actor.Weapon = weapon; ResetCooldowns(); } }

        public bool TryAttack()
        {
            if (!CanAct() || Time.time < nextAttack || actor.Motor.IsDashing || !actor.Weapon) return false;
            if (Time.time - lastAttack > .85f) combo = focusedHits = 0;
            combo++; lastAttack = Time.time;
            float interval = actor.AttackInterval;
            if (actor.Weapon.mechanic == WeaponMechanic.Momentum) interval /= 1 + Mathf.Min(.3f, combo * .025f);
            nextAttack = Time.time + interval;
            actor.Motor.CommitAttack(interval * .7f, actor.Weapon.attackMoveMultiplier);
            if (actor.Weapon.IsRanged) FireProjectile(); else Swing();
            BasicAttack?.Invoke(); return true;
        }

        private void FireProjectile()
        {
            bool empowered = combo % Mathf.Max(2, actor.Weapon.comboThreshold) == 0;
            float multiplier = empowered ? 1 + actor.Weapon.mechanicPower : 1;
            DamageElement element = actor.Weapon.PrimaryElement!=ElementTag.None ? WeaponSkillExecutor.Element(actor.Weapon.PrimaryElement) : actor.Weapon.family == WeaponFamily.Staff ? DominantElement() : DamageElement.Physical;
            Color color = element == DamageElement.Frost ? new Color(.3f, .85f, 1) : element == DamageElement.Fire ? new Color(1, .3f, .05f) :
                element == DamageElement.Poison ? Color.green : element == DamageElement.Void ? new Color(.7f, .2f, 1) : Palette.Player;
            CombatProjectile.Spawn(actor, actor.Motor.Facing, actor.Weapon.projectileSpeed, actor.Weapon.damage * multiplier,
                color, actor.Weapon.projectileLifetime, element, empowered ? ImpactTier.Heavy : ImpactTier.Light, true);
        }

        private void Swing()
        {
            bool heavy = actor.Weapon.IsHeavy;
            CombatVfx.Arc(actor.transform.position, actor.Motor.Facing, actor.Weapon.reach, actor.Weapon.arcDegrees,
                actor.Corruption ? Palette.Corrupted : heavy ? Palette.Gold : Palette.Player);
            if (actor.Weapon.trailPrefab) Instantiate(actor.Weapon.trailPrefab, actor.transform.position, actor.transform.rotation);
            float minimumDot = Mathf.Cos(actor.Weapon.arcDegrees * .5f * Mathf.Deg2Rad);
            foreach (var target in actor.Combat.Actors.ToArray())
            {
                if (!actor.Combat.AreEnemies(actor, target)) continue;
                Vector3 offset = target.transform.position - actor.transform.position; offset.y = 0;
                if (offset.sqrMagnitude > actor.Weapon.reach * actor.Weapon.reach ||
                    (offset.sqrMagnitude >= .3f && Vector3.Dot(offset.normalized, actor.Motor.Facing) < minimumDot)) continue;
                float damage = actor.Weapon.damage;
                if (actor.Weapon.mechanic == WeaponMechanic.FocusedThrust)
                {
                    if (focusedTarget == target) focusedHits++; else { focusedTarget = target; focusedHits = 1; }
                    damage *= 1 + Mathf.Min(.3f, (focusedHits - 1) * actor.Weapon.mechanicPower);
                }
                bool afterDash = Time.time - actor.Motor.LastDashTime <= .65f;
                float critBonus = actor.Weapon.mechanic == WeaponMechanic.DashPrecision && afterDash ? actor.Weapon.mechanicPower : 0;
                DamageElement element=WeaponSkillExecutor.Element(actor.Weapon.PrimaryElement);
                actor.Combat.DealDamage(target, new DamageInfo(actor, damage, DamageKind.Weapon, offset.normalized,
                    heavy ? .2f : .12f, actor.Weapon.knockback, true, true, element,
                    heavy ? ImpactTier.Heavy : ImpactTier.Light, critBonus));
            }
            if (actor.Weapon.mechanic == WeaponMechanic.SpellWave && combo % Mathf.Max(2, actor.Weapon.comboThreshold) == 0)
            {
                CombatVfx.Pulse(transform.position + actor.Motor.Facing * 1.4f, 2.2f, new Color(.5f, .5f, 1));
                actor.Combat.DamageArea(actor, transform.position + actor.Motor.Facing * 1.4f, 2.2f,
                    actor.Weapon.damage * actor.Weapon.mechanicPower, DamageKind.Void, .08f, 2);
            }
        }

        private DamageElement DominantElement()
        {
            var tags = actor.Inventory.DominantTags();
            if (tags.Contains(BuildTag.Fire)) return DamageElement.Fire;
            if (tags.Contains(BuildTag.Frost)) return DamageElement.Frost;
            if (tags.Contains(BuildTag.Lightning)) return DamageElement.Lightning;
            if (tags.Contains(BuildTag.Poison)) return DamageElement.Poison;
            if (tags.Contains(BuildTag.Void)) return DamageElement.Void;
            return DamageElement.Physical;
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
            else if(actor.Weapon&&actor.Weapon.skill&&actor.Skills.Execute(actor.Weapon.skill)) { }
            else
            {
                actor.Health.Shield(20); CombatVfx.Pulse(transform.position, 3.5f, Palette.Player);
                actor.Combat.DamageArea(actor, transform.position, 3.5f, 36, DamageKind.Ability, .55f, 7);
            }
            return true;
        }
        private bool CanAct() => actor.Alive && actor.Combat.Active && !actor.Motor.IsStunned;
    }
}
