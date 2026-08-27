namespace Ashbound
{
    public enum RunState
    {
        Lobby, StartingRun, Exploration, Combat, Reward, BossFight,
        BossDefeated, CorruptionTransition, FinalPvP, RunComplete
    }

    public enum Faction { Wanderers, Hostiles, Corrupted }
    public enum BuildTag { Critical, Bleed, Lightning, Fire, Frost, Mobility, Shield, Sustain, Summon, Curse }
    public enum Rarity { Common, Uncommon, Rare }
    public enum DamageKind { Weapon, Ability, Projectile, Bleed, Burning, Lightning, Echo, Rupture, Hazard }
    public enum StatusKind { Bleed, Burning, Slow, Stun }
    public enum TriggerKind { CriticalEcho, CriticalCooldown, BleedingVulnerability, BleedRupture, ChainLightning, LightningConductor }
    public enum EnemyKind { Cinderling, Lantern, Hound, Elite, Boss, Reflection }
}
