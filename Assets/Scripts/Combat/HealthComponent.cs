using System;
using UnityEngine;

namespace Ashbound
{
    public sealed class HealthComponent : MonoBehaviour, IDamageable
    {
        public HealthPool Pool { get; private set; } = new HealthPool(120);
        public bool IsAlive => Pool.Alive;
        public float CurrentHealth => Pool.Current;
        public float MaxHealth => Pool.Maximum;
        public bool DebugInvulnerable { get; set; }
        public bool HealingBlocked { get; set; }
        public float HealingMultiplier { get; set; }=1;
        public float InvulnerableUntil { get; set; }
        public event Action Died;
        public event Action Changed;
        public void Initialize(float maximum) { Pool.Reset(maximum); InvulnerableUntil = 0;HealingBlocked=false;HealingMultiplier=1;Changed?.Invoke(); }
        public HealthLoss TakeDamage(float amount)
        {
            bool wasAlive = IsAlive;
            var loss = Pool.Damage(amount, DebugInvulnerable || Time.time < InvulnerableUntil);
            if (loss.Total > 0) Changed?.Invoke();
            if (wasAlive && !IsAlive) Died?.Invoke();
            return loss;
        }
        public void Heal(float amount) { if (!HealingBlocked&&Pool.Heal(amount*Mathf.Clamp(HealingMultiplier,0,2)) > 0) Changed?.Invoke(); }
        public float SpendCurrentHealth(float fraction){float paid=Pool.SpendCurrentHealth(Pool.Current*Mathf.Clamp01(fraction));if(paid>0)Changed?.Invoke();return paid;}
        public void Resize(float maximum) { Pool.Resize(maximum); Changed?.Invoke(); }
        public void Shield(float amount) { Pool.AddShield(amount); Changed?.Invoke(); }
        public void DebugKill()
        {
            if (!IsAlive) return;
            Pool.Damage(Pool.Current + Pool.Shield);
            Changed?.Invoke(); Died?.Invoke();
        }
    }
}
