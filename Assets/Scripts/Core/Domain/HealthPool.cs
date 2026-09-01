using System;

namespace Ashbound
{
    public readonly struct HealthLoss
    {
        public readonly float Health, Shield;
        public float Total => Health + Shield;
        public HealthLoss(float health, float shield) { Health = health; Shield = shield; }
    }

    public sealed class HealthPool
    {
        public float Maximum { get; private set; }
        public float Current { get; private set; }
        public float Shield { get; private set; }
        public bool Alive => Current > 0;
        public HealthPool(float maximum) { Reset(maximum); }

        public void Reset(float maximum)
        {
            Maximum = Math.Max(1, maximum);
            Current = Maximum;
            Shield = 0;
        }

        public void Resize(float maximum)
        {
            if (float.IsNaN(maximum) || float.IsInfinity(maximum)) return;
            float fraction = Current / Maximum;
            Maximum = Math.Max(1, maximum);
            Current = Maximum * fraction;
            Shield = Math.Min(Shield, Maximum * .5f);
        }

        public HealthLoss Damage(float amount, bool invulnerable = false)
        {
            if (!Alive || invulnerable || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return default;
            float shieldLoss = Math.Min(Shield, amount);
            Shield -= shieldLoss;
            float healthLoss = Math.Min(Current, amount - shieldLoss);
            Current -= healthLoss;
            return new HealthLoss(healthLoss, shieldLoss);
        }

        public float Heal(float amount)
        {
            if (!Alive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return 0;
            float actual = Math.Min(amount, Maximum - Current);
            Current += actual;
            return actual;
        }
        public float SpendCurrentHealth(float amount)
        {
            if(!Alive||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return 0;float paid=Math.Min(Math.Max(0,Current-1),amount);Current-=paid;return paid;
        }

        public void AddShield(float amount)
        {
            if (Alive && amount > 0 && !float.IsNaN(amount) && !float.IsInfinity(amount)) Shield = Math.Min(Maximum * .5f, Shield + amount);
        }
    }
}
