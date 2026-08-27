using UnityEngine;

namespace Ashbound
{
    public sealed class CorruptionAbilities : MonoBehaviour
    {
        private Combatant actor;
        private float nextTrail;
        public void Apply(Combatant owner, BossCorruptionProfile profile)
        {
            actor = owner; actor.Corruption = profile; actor.Faction = Faction.Corrupted;
            actor.Restore(); actor.View.SetCorruption(profile);
            actor.Combat.DamageResolved += OnDamage;
        }
        private void OnDamage(DamageEvent damage)
        {
            if (damage.Info.Source != actor || !actor.Corruption.attacksBurn || !damage.Info.TriggerEffects) return;
            damage.Target.Statuses.Apply(actor, new StatusPayload { kind = StatusKind.Burning, duration = actor.Corruption.burnDuration, power = actor.Corruption.burnDamagePerSecond, maxStacks = 1 });
        }
        private void Update()
        {
            if (!actor || !actor.Alive || !actor.Combat.Active || !actor.Corruption.dashLeavesFire || !actor.Motor.IsDashing || Time.time < nextTrail) return;
            nextTrail = Time.time + .1f;
            AreaAttack.Spawn(actor, transform.position, .8f, 8, 0, 2);
        }
        private void OnDestroy() { if (actor && actor.Combat) actor.Combat.DamageResolved -= OnDamage; }
    }
}
