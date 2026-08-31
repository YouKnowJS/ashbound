using UnityEngine;

namespace Ashbound
{
    public sealed class Combatant : MonoBehaviour
    {
        public string Id { get; private set; }
        public string DisplayName { get; set; }
        public bool IsPlayer { get; private set; }
        public bool IsBoss { get; set; }
        public Faction Faction { get; set; }
        public CombatService Combat { get; private set; }
        public HealthComponent Health { get; private set; }
        public StatusEffectController Statuses { get; private set; }
        public PlayerInventory Inventory { get; private set; }
        public ActorMotor Motor { get; private set; }
        public AttackController Attacks { get; private set; }
        public ActorView View { get; set; }
        public WeaponDefinition Weapon { get; set; }
        public BossCorruptionProfile Corruption { get; set; }
        public float BaseSpeed { get; set; } = 7;
        public float BaseMaxHealth { get; private set; } = 120;
        public bool Alive => Health && Health.IsAlive;
        public float DamageMultiplier => (1 + Inventory.SumModifiers().damage) * (Corruption ? Corruption.damageMultiplier : 1);
        public float CriticalChance => Mathf.Clamp01(.08f + (Weapon ? Weapon.criticalChanceModifier : 0) + Inventory.SumModifiers().criticalChance);
        public float CriticalMultiplier => 1.7f + (Weapon ? Weapon.criticalDamageModifier : 0) + Inventory.SumModifiers().criticalMultiplier;
        public float Speed => BaseSpeed * (1 + Inventory.SumModifiers().movementSpeed) * (Corruption ? Corruption.movementMultiplier : 1) * Statuses.MovementFactor;
        public float AttackInterval => Weapon.attackInterval / Mathf.Max(.25f, 1 + Inventory.SumModifiers().attackSpeed);
        public float MaximumHealth => BaseMaxHealth * (1 + Inventory.SumModifiers().maxHealth) * (Corruption ? Corruption.healthMultiplier : 1);

        public void Initialize(string id, string displayName, bool isPlayer, Faction faction, float health, CombatService combat, WeaponDefinition weapon)
        {
            Id = id; DisplayName = displayName; IsPlayer = isPlayer; Faction = faction; Combat = combat; Weapon = weapon;
            BaseMaxHealth = health;
            Health = gameObject.AddComponent<HealthComponent>();
            Inventory = gameObject.AddComponent<PlayerInventory>();
            Inventory.Added += _ => Health.Resize(MaximumHealth);
            Statuses = gameObject.AddComponent<StatusEffectController>(); Statuses.Configure(this);
            Motor = gameObject.AddComponent<ActorMotor>(); Motor.Configure(this);
            Attacks = gameObject.AddComponent<AttackController>(); Attacks.Configure(this);
            gameObject.AddComponent<UpgradeEffectController>().Configure(this);
            Health.Initialize(health);
            Health.Died += OnDeath;
            combat.Register(this);
        }
        private void OnDeath() { Motor.Stop(); Statuses.Clear(); if (View) View.SetDead(true); }
        public void Restore(float healthFraction = 1)
        {
            Statuses.Clear(); Motor.Stop(); Attacks.ResetCooldowns();
            Health.Initialize(MaximumHealth);
            if (healthFraction < 1) Health.Pool.Damage(MaximumHealth * (1 - healthFraction));
            if (View) View.SetDead(false);
        }
        public void ScaleHealth(float multiplier)
        {
            BaseMaxHealth *= Mathf.Max(.1f, multiplier); Health.Resize(MaximumHealth); Health.Heal(Health.MaxHealth);
        }
        private void OnDestroy() { if (Combat) Combat.Unregister(this); }
    }
}
