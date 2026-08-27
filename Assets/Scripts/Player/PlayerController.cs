using System;
using UnityEngine;

namespace Ashbound
{
    public sealed class PlayerController : MonoBehaviour
    {
        private Combatant actor;
        public IPlayerInput InputSource { get; private set; }
        public event Action<Combatant> Interacted;
        public void Configure(Combatant owner, IPlayerInput input) { actor = owner; InputSource = input; }
        private void Update()
        {
            if (!actor || !actor.Alive || !actor.Combat.CanMove) return;
            var command = InputSource.Read(transform.position);
            actor.Motor.SetMove(command.Move);
            actor.Motor.SetFacing(command.Aim);
            if (command.Dash) actor.Motor.TryDash();
            if (command.Attack) actor.Attacks.TryAttack();
            if (command.Ability) actor.Attacks.TryAbility();
            if (command.Interact) Interacted?.Invoke(actor);
        }
    }
}
