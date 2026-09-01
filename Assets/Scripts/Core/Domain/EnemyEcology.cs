namespace Ashbound
{
    public enum EnemyRole { Warrior, Bruiser, Assassin, Ranger, Mage, Flyer, Burrower, Bomber, Support, Controller }
    public enum EnemyAttackBehavior { Melee, WideMelee, Lunge, Projectile, Area, Dive, Eruption, Detonation, AllyAid, ControlField }
    public enum EnemyMovementStyle { Advance, HoldGround, Flank, Kite, Orbit, AerialOrbit, Burrow, Pursue, SupportLine, ZoneKeeper }
    public enum EnemyTargetingStyle { Nearest, LowestHealth, Furthest, Isolated, Cluster }
    public enum SpawnPresentation { Gate, Edge, DropIn, Flight, Burrow, Rift, Reinforcement }
    public enum EnemyRewardTier { Standard, Dangerous, Elite }
    public enum EncounterDifficulty { Introductory, Standard, Dangerous, Elite }
    public enum EncounterRiskTier { Low, Medium, High }
    public enum CombatSpaceCategory { Small, Medium, Large }
    public enum ArenaLayoutKind { IrregularCourtyard, ConnectedCourtyards, BranchingRuins, BrokenRing, DividedHall }
}
