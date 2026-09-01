namespace Ashbound
{
    public enum NodeRiskRating { Low, Moderate, High, Severe }
    public enum NodeRewardCategory { Resources, Equipment, Relic, Merchant, Recovery, Variable, Challenge, Boss }
    public enum RouteVisibilityState { Hidden, Obscured, Visible }
    public enum VoteTieBehavior { HostBreaksTie, SeededRandom }
    public enum TreasureVariantKind { StandardCache, SealedVault, CursedChest, GreedyCache, Mimic, CorruptedCache }
    public enum MerchantOfferKind { Weapon, Armor, Relic, Recovery }
    public enum RestNodeChoice { Rest, TemperWeapon, TemperArmor, Salvage }
    public enum ChallengeKind { TimedElimination, Survive, PriorityTargets, NoHealing, DefendPoint }
    public enum EventOutcomeKind { Resources, Recovery, Equipment, Relic, Combat, RouteInformation }
    public enum EquipmentOfferKind { Mixed, Weapon, Armor, ArmorSlot }
}
