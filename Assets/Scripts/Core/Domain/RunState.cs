namespace Ashbound
{
    public enum RunState
    {
        Lobby, StartingRun, Exploration, Combat, Reward, BossFight,
        BossDefeated, CorruptionTransition, FinalPvP, RunComplete
    }

    public enum Faction { Wanderers, Hostiles, Corrupted }
    public enum BuildTag
    {
        Critical, Bleed, Lightning, Fire, Frost, Mobility, Shield, Sustain, Summon, Curse,
        Poison, Void, Heavy, Combo, DashPrecision, Utility, Control, Area, DamageOverTime
    }
    public enum Rarity { Common, Uncommon, Rare }
    public enum WeaponFamily { Sword, Spear, Greatsword, Katana, DualBlades, Bow, Staff, Spellblade }
    public enum ElementTag { None, Fire, Frost, Lightning, Poison, Void }
    public enum WeaponRarity { Common, Advanced, Rare, Epic, Legendary }
    public enum ArmorSlot { Head, Chest, Gloves, Boots }
    public enum SkillDelivery { MeleeDash, AreaBurst, ProjectileVolley, PersistentZone, GravityWell }
    public enum WeaponMechanic { None, FocusedThrust, HeavyCommitment, DashPrecision, Momentum, ChargedShot, ArcaneCharge, SpellWave }
    public enum DamageElement { Physical, Fire, Frost, Lightning, Poison, Void }
    public enum ImpactTier { Light, Critical, Proc, Heavy, Ability, Major }
    public enum DamageKind { Weapon, Ability, Projectile, Bleed, Burning, Lightning, Echo, Rupture, Hazard, Poison, Frost, Void, Shockwave }
    public enum StatusKind { Bleed, Burning, Slow, Stun, Chill, Freeze, Poison, VoidMark, Corrosion }
    public enum TriggerKind
    {
        CriticalEcho, CriticalCooldown, BleedingVulnerability, BleedRupture, ChainLightning, LightningConductor,
        StatusVulnerability, StatusThresholdBurst, StatusDeathBurst, ImmediateStatusTick, DashZone, PostDashDamage,
        PostDashCritical, HeavyShockwave, HeavyCooldown, Momentum, MomentumBurst, LowHealthDamage, DelayedEcho,
        AbilityEntropy, ConditionalElement, CriticalStatusTick, StatusSpread,
        PoisonRecovery, SetDashZone, SetShatterWave, SetOverloadChain, SetCloudPoison, SetRiftPull
    }
    public enum EnemyKind { Cinderling, Lantern, Hound, Bulwark, Elite, MiniBoss, Boss, Reflection }
}
