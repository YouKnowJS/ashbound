using System.Collections;
using UnityEngine;

namespace Ashbound
{
    public abstract class EnemyRoleBehaviour
    {
        public abstract void Move(EnemyBrain brain, Combatant target, float distance, Vector3 direction);
        public abstract IEnumerator Attack(EnemyBrain brain, Combatant target);
        protected static void KeepDistance(EnemyBrain brain, float distance, float preferred, Vector3 direction, float orbit = 0)
        {
            Vector3 movement = distance > preferred + .7f ? direction : distance < preferred - .7f ? -direction : Vector3.zero;
            if (orbit != 0) movement += Vector3.Cross(Vector3.up, direction) * orbit;
            brain.Actor.Motor.SetMove(Vector3.ClampMagnitude(movement, 1));
        }
    }

    public sealed class WarriorEnemyBehaviour : EnemyRoleBehaviour
    {
        private int attacks;
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, b.Definition.preferredDistance, v);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            attacks++;bool heavy=attacks%3==0;float radius=heavy?2.25f:1.7f;
            yield return b.WarningRing(radius, heavy?.72f:.42f);
            if (b.CanFinish(t)) b.AreaHit(b.Actor.transform.position + b.DirectionTo(t) * .45f, radius, heavy?1.55f:1, heavy?.2f:.12f, heavy?7:4);
        }
    }

    public sealed class BruiserEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, 2.5f, v);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            yield return b.WarningRing(3.2f, .85f);
            if (b.CanFinish(t)) b.AreaHit(b.Actor.transform.position, 3.2f, 1.15f, .22f, 8);
        }
    }

    public sealed class AssassinEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, 4, v, .72f);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Vector3 direction = b.DirectionTo(t);
            yield return b.WarningDirection(direction, 8, .58f);
            if (!b.CanFinish(t)) yield break;
            b.Actor.Motor.Lunge(direction, 15, .48f);
            yield return new WaitForSeconds(.2f);
            if (b.CanFinish(t) && Vector3.Distance(b.Actor.transform.position, t.transform.position) < 2.2f) b.DirectHit(t, 1.15f, .12f, 5);
        }
    }

    public sealed class RangerEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, b.Definition.preferredDistance, v, .25f);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            yield return b.WarningDirection(b.DirectionTo(t), 10, .38f);
            if (b.CanFinish(t)) b.Projectile(b.DirectionTo(t), 9.5f, 1);
        }
    }

    public sealed class MageEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, b.Definition.preferredDistance, v, -.2f);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Vector3 predicted = t.transform.position + t.Motor.Facing * 1.2f;
            yield return b.WarningAt(predicted, 2.8f, .8f);
            if (b.CanFinish(t)) b.AreaHit(predicted, 2.8f, 1.05f, .12f, 3);
        }
    }

    public sealed class FlyerEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v)
        {
            b.SetAerial(true);
            KeepDistance(b, d, b.Definition.preferredDistance, v, .8f);
        }
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Vector3 direction = b.DirectionTo(t);
            b.SetAerial(false);
            yield return b.WarningDirection(direction, 7, .65f);
            if (b.CanFinish(t))
            {
                b.Actor.Motor.Lunge(direction, 13, .45f);
                yield return new WaitForSeconds(.28f);
                if (b.CanFinish(t)) b.AreaHit(b.Actor.transform.position, 1.8f, 1.1f, .1f, 4);
            }
        }
    }

    public sealed class BurrowerEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, 3.5f, v);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Vector3 eruption = t.transform.position + t.Motor.Facing * 1.1f;
            b.SetBurrowed(true);
            yield return b.WarningAt(eruption, 2.25f, .9f);
            if (!b.Actor || !b.Actor.Alive) yield break;
            b.Actor.Motor.Teleport(eruption);
            b.SetBurrowed(false);
            b.AreaHit(eruption, 2.25f, 1.2f, .16f, 6);
        }
    }

    public sealed class BomberEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, 1.8f, v);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            yield return b.WarningRing(3.1f, 1.05f);
            if (b.CanFinish(t)) b.AreaHit(b.Actor.transform.position, 3.1f, 1.35f, .1f, 9);
        }
    }

    public sealed class SupportEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v)
        {
            var ally = b.FindWoundedAlly();
            if (ally) KeepDistance(b, Vector3.Distance(b.Actor.transform.position, ally.transform.position), 4.5f, (ally.transform.position - b.Actor.transform.position).normalized, .2f);
            else KeepDistance(b, d, 7, v, .2f);
        }
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Combatant ally = b.FindWoundedAlly();
            if (!ally) { yield return b.WarningDirection(b.DirectionTo(t), 8, .45f); if (b.CanFinish(t)) b.Projectile(b.DirectionTo(t), 8, .7f); yield break; }
            yield return b.WarningAt(ally.transform.position, 1.25f, .65f, Palette.Player);
            if (ally && ally.Alive)
            {
                ally.Health.Heal(b.Definition.attackDamage * 1.4f);
                ally.Health.Shield(b.Definition.attackDamage * .6f);
                CombatVfx.Pulse(ally.transform.position, 1.25f, Palette.Player);
            }
        }
    }

    public sealed class ControllerEnemyBehaviour : EnemyRoleBehaviour
    {
        public override void Move(EnemyBrain b, Combatant t, float d, Vector3 v) => KeepDistance(b, d, b.Definition.preferredDistance, v, -.35f);
        public override IEnumerator Attack(EnemyBrain b, Combatant t)
        {
            Vector3 center = t.transform.position;
            yield return b.WarningAt(center, 3.3f, .75f, WeaponSkillExecutor.Tint(DamageElement.Void));
            if (!b.CanFinish(t)) yield break;
            foreach (var other in b.Actor.Combat.Actors)
            {
                if (!b.Actor.Combat.AreEnemies(b.Actor, other) || (other.transform.position - center).sqrMagnitude > 10.9f) continue;
                other.Statuses.Apply(b.Actor, new StatusPayload { kind = StatusKind.Slow, duration = 1.25f, power = .32f, maxStacks = 1 });
                Vector3 pull = center - other.transform.position; pull.y = 0;
                other.Motor.Impact(pull.normalized * 2.2f, .08f);
                b.DirectHit(other, .45f, .04f, 0);
            }
        }
    }
}
