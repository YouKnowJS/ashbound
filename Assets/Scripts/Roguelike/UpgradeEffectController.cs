using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class UpgradeEffectController : MonoBehaviour
    {
        private Combatant actor;
        private int lightningCharges;
        public void Configure(Combatant owner) { actor = owner; actor.Combat.DamageResolved += OnDamage; }
        private void OnDestroy() { if (actor && actor.Combat) actor.Combat.DamageResolved -= OnDamage; }

        private void OnDamage(DamageEvent hit)
        {
            // Secondary damage never triggers item effects again: this prevents recursive proc loops.
            if (hit.Info.Source != actor || !hit.Info.TriggerEffects) return;
            var build = actor.Inventory;
            if (hit.Target.Alive)
                foreach (var item in build.Items)
                    foreach (var status in item.statusEffects) hit.Target.Statuses.Apply(actor, status);
            if (hit.Critical)
            {
                if (build.HasEffect(TriggerKind.CriticalCooldown)) actor.Attacks.ReduceCooldown(build.EffectPower(TriggerKind.CriticalCooldown));
                if (build.HasEffect(TriggerKind.CriticalEcho))
                {
                    CombatVfx.Pulse(hit.Target.transform.position, 1.8f, Palette.Gold);
                    actor.Combat.DamageArea(actor, hit.Target.transform.position, 1.8f,
                        hit.Info.Amount * build.EffectPower(TriggerKind.CriticalEcho), DamageKind.Echo);
                }
            }
            if (hit.Target.Alive && build.HasEffect(TriggerKind.BleedRupture) &&
                hit.Target.Statuses.StackCount(StatusKind.Bleed) >= build.EffectThreshold(TriggerKind.BleedRupture, 4))
            {
                hit.Target.Statuses.Consume(StatusKind.Bleed);
                CombatVfx.Pulse(hit.Target.transform.position, 2.8f, Palette.Bleed);
                actor.Combat.DamageArea(actor, hit.Target.transform.position, 2.8f, build.EffectPower(TriggerKind.BleedRupture), DamageKind.Rupture);
                actor.Health.Heal(4);
            }
            if (!build.HasEffect(TriggerKind.ChainLightning)) return;
            lightningCharges += 1 + (hit.Critical && build.HasEffect(TriggerKind.LightningConductor) ? 2 : 0);
            int threshold = build.EffectThreshold(TriggerKind.ChainLightning, 4);
            if (lightningCharges < threshold) return;
            lightningCharges -= threshold;
            Chain(hit.Target, 3 + (build.HasEffect(TriggerKind.LightningConductor) ? 2 : 0), build.EffectPower(TriggerKind.ChainLightning));
        }

        private void Chain(Combatant first, int count, float damage)
        {
            Vector3 position = actor.transform.position;
            var visited = new HashSet<Combatant>();
            Combatant target = first;
            for (int i = 0; i < count; i++)
            {
                if (!target || !target.Alive)
                    target = actor.Combat.Actors.Where(x => !visited.Contains(x) && actor.Combat.AreEnemies(actor, x) &&
                        Vector3.Distance(position, x.transform.position) < 6).OrderBy(x => (x.transform.position - position).sqrMagnitude).FirstOrDefault();
                if (!target) break;
                visited.Add(target);
                Vector3 next = target.transform.position;
                CombatVfx.Bolt(position + Vector3.up, next + Vector3.up, Palette.Lightning);
                actor.Combat.DealDamage(target, new DamageInfo(actor, damage, DamageKind.Lightning));
                position = next;
                target = null;
            }
        }
    }
}
