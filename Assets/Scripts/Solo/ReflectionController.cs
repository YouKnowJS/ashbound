using UnityEngine;

namespace Ashbound
{
    public sealed class ReflectionController : MonoBehaviour
    {
        private Combatant actor;
        private float nextDash;
        public void Configure(Combatant owner) { actor = owner; }
        private void Update()
        {
            if (!actor.Alive || !actor.Combat.Active) return;
            var target = actor.Combat.NearestEnemy(actor);
            if (!target) return;
            Vector3 offset = target.transform.position - transform.position;
            actor.Motor.SetFacing(offset);
            float distance = offset.magnitude;
            Vector3 movement = distance > 2.2f ? offset.normalized : Vector3.Cross(offset.normalized, Vector3.up) * .4f;
            actor.Motor.SetMove(movement);
            if (distance < actor.Weapon.reach) actor.Attacks.TryAttack();
            if (distance < 3.3f) actor.Attacks.TryAbility();
            if (distance > 4 && Time.time > nextDash)
            { actor.Motor.TryDash(); nextDash = Time.time + 3.2f; }
        }
    }
}
