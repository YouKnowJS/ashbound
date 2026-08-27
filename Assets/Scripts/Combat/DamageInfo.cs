using UnityEngine;

namespace Ashbound
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }

    public readonly struct DamageInfo
    {
        public readonly Combatant Source;
        public readonly float Amount, Stun, Knockback;
        public readonly DamageKind Kind;
        public readonly Vector3 Direction;
        public readonly bool CanCrit, TriggerEffects;
        public DamageInfo(Combatant source, float amount, DamageKind kind, Vector3 direction = default,
            float stun = 0, float knockback = 0, bool canCrit = false, bool triggerEffects = false)
        {
            Source = source; Amount = amount; Kind = kind; Direction = direction;
            Stun = stun; Knockback = knockback; CanCrit = canCrit; TriggerEffects = triggerEffects;
        }
    }

    public readonly struct DamageEvent
    {
        public readonly DamageInfo Info;
        public readonly Combatant Target;
        public readonly HealthLoss Loss;
        public readonly bool Critical;
        public DamageEvent(DamageInfo info, Combatant target, HealthLoss loss, bool critical)
        { Info = info; Target = target; Loss = loss; Critical = critical; }
    }
}
