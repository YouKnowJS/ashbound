using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class StatusEffectController : MonoBehaviour
    {
        private sealed class Entry
        {
            public StatusKind Kind;
            public Combatant Source;
            public float Remaining, Power, Tick;
            public int Stacks;
        }
        private readonly List<Entry> entries = new List<Entry>();
        private Combatant actor;
        public bool Stunned => entries.Any(x => x.Kind == StatusKind.Stun || x.Kind == StatusKind.Freeze);
        public bool FrostVulnerable => Time.time < frostVulnerableUntil;
        public bool Corroded => entries.Any(x => x.Kind == StatusKind.Corrosion);
        private float frostVulnerableUntil;
        public float MovementFactor => Stunned ? 0 : entries.Where(x => x.Kind == StatusKind.Slow || x.Kind == StatusKind.Chill)
            .Select(x => Mathf.Clamp(1 - x.Power * x.Stacks, .2f, 1)).DefaultIfEmpty(1).Min();
        public void Configure(Combatant owner) { actor = owner; }
        public int StackCount(StatusKind kind) => entries.Where(x => x.Kind == kind).Sum(x => x.Stacks);
        public void Clear() { entries.Clear(); frostVulnerableUntil = 0; }
        public void Consume(StatusKind kind) { entries.RemoveAll(x => x.Kind == kind); }

        public void Apply(Combatant source, StatusPayload effect)
        {
            if (!actor.Alive || !actor.Combat.Active || !actor.Combat.AreEnemies(source, actor)) return;
            if (actor.IsBoss && effect.kind == StatusKind.Stun) return;
            var entry = entries.FirstOrDefault(x => x.Kind == effect.kind && x.Source == source);
            if (entry == null)
            {
                entry = new Entry { Kind = effect.kind, Source = source, Tick = 1 };
                entries.Add(entry);
            }
            entry.Remaining = Mathf.Max(.1f, effect.duration);
            entry.Power = effect.power;
            entry.Stacks = Mathf.Min(entry.Stacks + 1, Mathf.Max(1, effect.maxStacks));
            if (effect.kind == StatusKind.Chill && entry.Stacks >= Mathf.Max(1, effect.maxStacks))
            {
                if (actor.IsBoss) frostVulnerableUntil = Mathf.Max(frostVulnerableUntil, Time.time + 1.5f);
                else
                {
                    entries.Remove(entry);
                    entries.Add(new Entry { Kind = StatusKind.Freeze, Source = source, Remaining = .8f, Stacks = 1 });
                }
            }
        }

        public void TickNow(StatusKind kind)
        {
            foreach (var entry in entries.Where(x => x.Kind == kind).ToArray()) DealTick(entry);
        }

        private void Update()
        {
            if (!actor || !actor.Alive || !actor.Combat.Active) return;
            foreach (var entry in entries.ToArray())
            {
                entry.Remaining -= Time.deltaTime;
                entry.Tick -= Time.deltaTime;
                if (entry.Tick <= 0 && (entry.Kind == StatusKind.Bleed || entry.Kind == StatusKind.Burning || entry.Kind == StatusKind.Poison))
                {
                    entry.Tick += 1; DealTick(entry);
                }
                if (entry.Remaining <= 0 || !entry.Source) entries.Remove(entry);
            }
        }
        private void DealTick(Entry entry)
        {
            DamageKind kind = entry.Kind == StatusKind.Bleed ? DamageKind.Bleed : entry.Kind == StatusKind.Poison ? DamageKind.Poison : DamageKind.Burning;
            DamageElement element = entry.Kind == StatusKind.Poison ? DamageElement.Poison : entry.Kind == StatusKind.Burning ? DamageElement.Fire : DamageElement.Physical;
            actor.Combat.DealDamage(actor, new DamageInfo(entry.Source, entry.Power * entry.Stacks, kind,
                element: element, impact: ImpactTier.Proc));
        }
    }
}
