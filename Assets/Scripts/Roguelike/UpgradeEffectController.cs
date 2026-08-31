using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class UpgradeEffectController : MonoBehaviour
    {
        private Combatant actor;
        private int lightningCharges;
        private float dashBuffUntil;
        public void Configure(Combatant owner)
        {
            actor = owner; actor.Combat.DamageResolved += OnDamage; actor.Motor.DashStarted += OnDash;
        }
        private void OnDestroy()
        {
            if (actor && actor.Combat) actor.Combat.DamageResolved -= OnDamage;
            if (actor && actor.Motor) actor.Motor.DashStarted -= OnDash;
        }

        private void OnDash()
        {
            var build = actor.Inventory;
            dashBuffUntil = Time.time + .8f;
            bool setDash=actor.HasEffect(TriggerKind.SetDashZone)||actor.HasEffect(TriggerKind.SetRiftPull);
            if (!build.HasEffect(TriggerKind.DashZone)&&!setDash) return;
            DamageElement element = ElementFor(build.Items.FirstOrDefault(x => x.triggeredEffects.Any(e => e.kind == TriggerKind.DashZone)));
            if(actor.HasEffect(TriggerKind.SetDashZone))element=DamageElement.Fire;
            if(actor.HasEffect(TriggerKind.SetRiftPull))element=DamageElement.Void;
            Color color = element == DamageElement.Fire ? new Color(1, .25f, .05f) : element == DamageElement.Frost ? Color.cyan : new Color(.65f, .2f, 1);
            CombatVfx.Pulse(actor.transform.position, 1.8f, color);
            float power=build.EffectPower(TriggerKind.DashZone)+actor.EffectPower(TriggerKind.SetDashZone)+actor.EffectPower(TriggerKind.SetRiftPull);
            actor.Combat.DamageArea(actor, actor.transform.position, 1.8f, power,
                KindFor(element), .05f, 1);
            Proc("dash-zone");
        }

        private void OnDamage(DamageEvent hit)
        {
            if (hit.Info.Source != actor || !hit.Info.TriggerEffects) return;
            var build = actor.Inventory;
            if (hit.Target.Alive)
            {
                if(actor.Weapon&&actor.Weapon.onHitStatuses!=null)foreach(var status in actor.Weapon.onHitStatuses)hit.Target.Statuses.Apply(actor,status);
                foreach (var item in build.Items)
                    foreach (var status in item.statusEffects) hit.Target.Statuses.Apply(actor, status);
            }

            if (hit.Critical)
            {
                if (build.HasEffect(TriggerKind.CriticalCooldown)) { actor.Attacks.ReduceCooldown(build.EffectPower(TriggerKind.CriticalCooldown)); Proc("critical-cooldown"); }
                if (build.HasEffect(TriggerKind.CriticalEcho))
                {
                    ProcArea(hit.Target.transform.position, 1.8f, hit.Info.Amount * build.EffectPower(TriggerKind.CriticalEcho), DamageKind.Echo, Palette.Gold, "critical-echo");
                }
                if (build.HasEffect(TriggerKind.CriticalStatusTick))
                {
                    hit.Target.Statuses.TickNow(StatusKind.Bleed); hit.Target.Statuses.TickNow(StatusKind.Poison); Proc("critical-status-tick");
                }
            }

            if (Time.time <= dashBuffUntil && actor.HasEffect(TriggerKind.PostDashDamage))
            {
                actor.Combat.DealDamage(hit.Target, new DamageInfo(actor, hit.Info.Amount * actor.EffectPower(TriggerKind.PostDashDamage),
                    DamageKind.Echo, impact: ImpactTier.Proc)); Proc("post-dash"); dashBuffUntil = 0;
            }
            if (hit.Target.Alive && build.HasEffect(TriggerKind.BleedRupture) &&
                hit.Target.Statuses.StackCount(StatusKind.Bleed) >= build.EffectThreshold(TriggerKind.BleedRupture, 4))
            {
                hit.Target.Statuses.Consume(StatusKind.Bleed);
                ProcArea(hit.Target.transform.position, 2.8f, build.EffectPower(TriggerKind.BleedRupture), DamageKind.Rupture, Palette.Bleed, "bleed-rupture");
                actor.Health.Heal(4);
            }
            TryThreshold(hit.Target, StatusKind.Burning, DamageElement.Fire);
            TryThreshold(hit.Target, StatusKind.Poison, DamageElement.Poison);
            TryThreshold(hit.Target, StatusKind.VoidMark, DamageElement.Void);
            if(actor.HasEffect(TriggerKind.SetShatterWave)&&hit.Info.Element==DamageElement.Frost&&(hit.Critical||hit.Info.Impact>=ImpactTier.Heavy))
                ProcArea(hit.Target.transform.position,2.2f,actor.EffectPower(TriggerKind.SetShatterWave),DamageKind.Frost,Color.cyan,"winterglass-shatter");

            if (actor.HasEffect(TriggerKind.HeavyShockwave) && hit.Info.Impact >= ImpactTier.Heavy)
                ProcArea(hit.Target.transform.position, 2.4f, actor.EffectPower(TriggerKind.HeavyShockwave), DamageKind.Shockwave, Palette.Gold, "heavy-shockwave");
            if (actor.HasEffect(TriggerKind.HeavyCooldown) && hit.Info.Impact >= ImpactTier.Heavy)
                actor.Attacks.ReduceCooldown(actor.EffectPower(TriggerKind.HeavyCooldown));
            if (build.HasEffect(TriggerKind.DelayedEcho)) StartCoroutine(Delayed(hit.Target.transform.position,
                hit.Info.Amount * build.EffectPower(TriggerKind.DelayedEcho)));
            if (!hit.Target.Alive && (build.HasEffect(TriggerKind.StatusDeathBurst)||actor.HasEffect(TriggerKind.SetCloudPoison)))
                ProcArea(hit.Target.transform.position, 2.5f, Mathf.Max(build.EffectPower(TriggerKind.StatusDeathBurst),actor.EffectPower(TriggerKind.SetCloudPoison)), DamageKind.Poison,
                    new Color(.3f, .9f, .2f), "death-burst");
            if(!hit.Target.Alive&&actor.HasEffect(TriggerKind.PoisonRecovery)&&Time.time>=nextRecovery)
            {nextRecovery=Time.time+1.25f;float before=actor.Health.CurrentHealth;float recovery=actor.EffectPower(TriggerKind.PoisonRecovery)+actor.Equipment.PassivePower(ArmorPassiveKind.Recovery,ElementTag.Poison)*10;actor.Health.Heal(Mathf.Min(8,recovery));float healed=actor.Health.CurrentHealth-before;var run=FindAnyObjectByType<RunManager>();if(run)run.Telemetry.Recovery(actor,healed);Proc("poison-recovery");}

            if (!build.HasEffect(TriggerKind.ChainLightning)) return;
            lightningCharges += 1 + (hit.Critical && build.HasEffect(TriggerKind.LightningConductor) ? 2 : 0);
            if(hit.Info.Element==DamageElement.Lightning)lightningCharges+=Mathf.FloorToInt(actor.Equipment.PassivePower(ArmorPassiveKind.ProcCharge,ElementTag.Lightning)*4);
            if (build.HasEffect(TriggerKind.Momentum) && actor.Attacks.Combo >= build.EffectThreshold(TriggerKind.Momentum, 6)) lightningCharges++;
            int threshold = build.EffectThreshold(TriggerKind.ChainLightning, 4);
            if (lightningCharges < threshold) return;
            lightningCharges -= threshold;
            Chain(hit.Target, 3 + (build.HasEffect(TriggerKind.LightningConductor) ? 2 : 0)+(actor.HasEffect(TriggerKind.SetOverloadChain)?1:0), build.EffectPower(TriggerKind.ChainLightning)+actor.EffectPower(TriggerKind.SetOverloadChain));
            Proc("chain-lightning");
        }
        private float nextRecovery;

        private void TryThreshold(Combatant target, StatusKind status, DamageElement element)
        {
            var item = actor.Inventory.Items.FirstOrDefault(x => x.tags.Contains(TagFor(element)) && x.triggeredEffects.Any(e => e.kind == TriggerKind.StatusThresholdBurst));
            if (!item || !target.Alive) return;
            var effect = item.triggeredEffects.First(x => x.kind == TriggerKind.StatusThresholdBurst);
            if (target.Statuses.StackCount(status) < Mathf.Max(2, effect.threshold)) return;
            target.Statuses.Consume(status);
            ProcArea(target.transform.position, 2.3f, effect.power, KindFor(element), ElementColor(element), item.id);
        }

        private IEnumerator Delayed(Vector3 position, float damage)
        {
            yield return new WaitForSeconds(.35f);
            if (actor && actor.Alive && actor.Combat.Active)
                ProcArea(position, 1.4f, damage, DamageKind.Void, new Color(.65f, .2f, 1), "echo-beyond");
        }

        private void ProcArea(Vector3 position, float radius, float damage, DamageKind kind, Color color, string id)
        {
            CombatVfx.Pulse(position, radius, color); actor.Combat.DamageArea(actor, position, radius, damage, kind); Proc(id);
        }
        private void Proc(string id) { actor.Combat.RecordProc(actor, id); }

        private void Chain(Combatant first, int count, float damage)
        {
            Vector3 position = actor.transform.position; var visited = new HashSet<Combatant>(); Combatant target = first;
            for (int i = 0; i < count; i++)
            {
                if (!target || !target.Alive) target = actor.Combat.Actors.Where(x => !visited.Contains(x) && actor.Combat.AreEnemies(actor, x) &&
                    Vector3.Distance(position, x.transform.position) < 6).OrderBy(x => (x.transform.position - position).sqrMagnitude).FirstOrDefault();
                if (!target) break;
                visited.Add(target); Vector3 next = target.transform.position;
                CombatVfx.Bolt(position + Vector3.up, next + Vector3.up, Palette.Lightning);
                actor.Combat.DealDamage(target, new DamageInfo(actor, damage, DamageKind.Lightning,
                    element: DamageElement.Lightning, impact: ImpactTier.Proc));
                position = next; target = null;
            }
        }

        private static DamageElement ElementFor(ItemDefinition item)
        {
            if (!item) return DamageElement.Physical;
            if (item.tags.Contains(BuildTag.Fire)) return DamageElement.Fire;
            if (item.tags.Contains(BuildTag.Frost)) return DamageElement.Frost;
            if (item.tags.Contains(BuildTag.Lightning)) return DamageElement.Lightning;
            if (item.tags.Contains(BuildTag.Poison)) return DamageElement.Poison;
            if (item.tags.Contains(BuildTag.Void)) return DamageElement.Void;
            return DamageElement.Physical;
        }
        private static BuildTag TagFor(DamageElement element) => element == DamageElement.Fire ? BuildTag.Fire :
            element == DamageElement.Poison ? BuildTag.Poison : element == DamageElement.Void ? BuildTag.Void :
            element == DamageElement.Frost ? BuildTag.Frost : element == DamageElement.Lightning ? BuildTag.Lightning : BuildTag.Utility;
        private static DamageKind KindFor(DamageElement element) => element == DamageElement.Fire ? DamageKind.Burning :
            element == DamageElement.Poison ? DamageKind.Poison : element == DamageElement.Void ? DamageKind.Void :
            element == DamageElement.Frost ? DamageKind.Frost : element == DamageElement.Lightning ? DamageKind.Lightning : DamageKind.Echo;
        private static Color ElementColor(DamageElement element) => element == DamageElement.Fire ? new Color(1, .25f, .05f) :
            element == DamageElement.Poison ? new Color(.3f, .9f, .2f) : element == DamageElement.Void ? new Color(.65f, .2f, 1) :
            element == DamageElement.Frost ? Color.cyan : Palette.Lightning;
    }
}
