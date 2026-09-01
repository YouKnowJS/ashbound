using System;

namespace Ashbound
{
    public sealed class RunStateMachine
    {
        public RunState State { get; private set; } = RunState.Lobby;
        public bool BossWasDefeated { get; private set; }
        public event Action<RunState> Changed;

        public bool TryAdvance(RunState next)
        {
            bool allowed = next == RunState.RunComplete && State != RunState.Lobby && State != RunState.RunComplete;
            switch (State)
            {
                case RunState.Lobby: allowed |= next == RunState.StartingRun; break;
                case RunState.StartingRun: allowed |= next == RunState.Exploration; break;
                case RunState.Exploration: allowed |= next == RunState.Combat || next == RunState.BossFight || next == RunState.Reward; break;
                case RunState.Combat: allowed |= next == RunState.Reward; break;
                case RunState.Reward: allowed |= next == RunState.Exploration; break;
                case RunState.BossFight: allowed |= next == RunState.BossDefeated; break;
                case RunState.BossDefeated: allowed |= next == RunState.CorruptionTransition; break;
                case RunState.CorruptionTransition: allowed |= next == RunState.FinalPvP && BossWasDefeated; break;
            }
            if (!allowed) return false;
            if (next == RunState.BossDefeated) BossWasDefeated = true;
            SetState(next);
            return true;
        }

        public bool DebugSkipToBoss()
        {
            if (State != RunState.Exploration && State != RunState.Combat && State != RunState.Reward) return false;
            SetState(RunState.BossFight);
            return true;
        }
        public bool DebugJumpToCombat()
        {
            if (State == RunState.Lobby || State == RunState.FinalPvP || State == RunState.RunComplete || BossWasDefeated) return false;
            SetState(RunState.Combat); return true;
        }

        public void Reset()
        {
            BossWasDefeated = false;
            SetState(RunState.Lobby);
        }

        private void SetState(RunState next) { State = next; Changed?.Invoke(next); }
    }
}
