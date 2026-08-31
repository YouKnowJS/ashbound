using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class CombatService : MonoBehaviour
    {
        private readonly List<Combatant> actors = new List<Combatant>();
        private System.Random random = new System.Random(1);
        public IReadOnlyList<Combatant> Actors => actors;
        public RunState State { get; set; }
        public bool Paused { get; set; }
        public bool FriendlyFire { get; set; }
        public bool Active => !Paused && CombatRules.IsCombatState(State);
        public bool CanMove => !Paused && (CombatRules.IsCombatState(State) || State == RunState.Exploration);
        public bool PvPEnabled => State == RunState.FinalPvP;
        public CombatFeedback Feedback { get; private set; }
        public event Action<DamageEvent> DamageResolved;
        public event Action<Combatant, string> BuildProc;
        public event Action<Combatant, float> ControlApplied;
        public void RecordProc(Combatant actor, string id) { BuildProc?.Invoke(actor, id); }
        public void RecordControl(Combatant actor,float duration){ControlApplied?.Invoke(actor,duration);}
        private void Awake() { Feedback = gameObject.AddComponent<CombatFeedback>(); Feedback.Configure(this); }
        public void Seed(int seed) { random = new System.Random(seed); }
        public void Register(Combatant actor) { if (!actors.Contains(actor)) actors.Add(actor); }
        public void Unregister(Combatant actor) { actors.Remove(actor); }
        public bool AreEnemies(Combatant source, Combatant target) => source && target && target.Alive &&
            CombatRules.CanDamage(source.Id, target.Id, source.Faction, target.Faction, source.IsPlayer, target.IsPlayer, State, FriendlyFire);
        public Combatant NearestEnemy(Combatant source) => actors.Where(x => AreEnemies(source, x))
            .OrderBy(x => (x.transform.position - source.transform.position).sqrMagnitude).FirstOrDefault();

        public bool DealDamage(Combatant target, DamageInfo info)
        {
            if (!Active || !AreEnemies(info.Source, target)) return false;
            bool critical = info.CanCrit && random.NextDouble() < Mathf.Clamp01(info.Source.CriticalChance + info.CriticalChanceBonus);
            float amount = info.Amount * info.Source.DamageMultiplier * (critical ? info.Source.CriticalMultiplier : 1);
            if (info.Element != DamageElement.Physical)
                amount *= 1 + info.Source.Equipment.PassivePower(ArmorPassiveKind.ElementAmplify, (ElementTag)info.Element);
            if (info.Source.Statuses.Corroded) amount *= .9f;
            if (target.Statuses.FrostVulnerable && (critical || info.Impact >= ImpactTier.Heavy)) amount *= 1.25f;
            if (info.Source.Inventory.HasEffect(TriggerKind.LowHealthDamage) && info.Source.Health.CurrentHealth / info.Source.Health.MaxHealth <= .35f)
                amount *= 1 + info.Source.Inventory.EffectPower(TriggerKind.LowHealthDamage);
            if (info.Kind == DamageKind.Weapon && target.Statuses.StackCount(StatusKind.Bleed) > 0)
                amount *= 1 + info.Source.Inventory.EffectPower(TriggerKind.BleedingVulnerability);
            var loss = target.Health.TakeDamage(amount);
            if (loss.Total <= 0) return false;
            if (target.Alive) target.Motor.Impact(info.Direction * info.Knockback, target.IsBoss ? 0 : info.Stun);
            DamageResolved?.Invoke(new DamageEvent(info, target, loss, critical));
            return true;
        }

        public void DamageArea(Combatant owner, Vector3 center, float radius, float amount, DamageKind kind, float stun = 0, float knockback = 0)
        {
            foreach (var target in actors.ToArray())
                if (target && (target.transform.position - center).sqrMagnitude <= radius * radius)
                    DealDamage(target, new DamageInfo(owner, amount, kind, (target.transform.position - center).normalized, stun, knockback));
        }
    }
}
