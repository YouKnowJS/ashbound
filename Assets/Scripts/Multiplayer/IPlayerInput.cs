using UnityEngine;

namespace Ashbound
{
    public struct PlayerCommand
    {
        public Vector3 Move, Aim;
        public bool Attack, Dash, Ability, Interact;
    }
    public interface IPlayerInput
    {
        bool Connected { get; }
        PlayerCommand Read(Vector3 worldPosition);
    }
}
