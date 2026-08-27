namespace Ashbound
{
    public static class CombatRules
    {
        public static bool IsCombatState(RunState state) => state == RunState.Combat || state == RunState.BossFight || state == RunState.FinalPvP;

        public static bool CanDamage(string attackerId, string targetId, Faction attacker, Faction target,
            bool attackerIsPlayer, bool targetIsPlayer, RunState state, bool friendlyFire = false)
        {
            if (!IsCombatState(state) || string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId) || attackerId == targetId) return false;
            if ((attacker == Faction.Corrupted || target == Faction.Corrupted) && state != RunState.FinalPvP) return false;
            if (attackerIsPlayer && targetIsPlayer && state != RunState.FinalPvP) return false;
            if (attacker == target) return state == RunState.FinalPvP && friendlyFire && attackerIsPlayer && targetIsPlayer;
            return true;
        }
    }
}
