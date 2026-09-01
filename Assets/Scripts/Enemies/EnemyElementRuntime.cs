using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public static class EnemyElementRuntime
    {
        public static void Resolve(EnemyBrain brain, Combatant primary)
        {
            if (!brain || !primary || !primary.Alive) return;
            var owner = brain.Actor;
            switch (brain.Definition.element)
            {
                case ElementTag.Fire:
                    primary.Statuses.Apply(owner, new StatusPayload { kind = StatusKind.Burning, duration = 4, power = 1.8f, maxStacks = 3 });
                    AreaAttack.Spawn(owner, primary.transform.position, 1.8f, brain.Definition.attackDamage * .18f, .35f, 2.2f);
                    break;
                case ElementTag.Frost:
                    primary.Statuses.Apply(owner, new StatusPayload { kind = StatusKind.Chill, duration = 3, power = .12f, maxStacks = 4 });
                    break;
                case ElementTag.Lightning:
                    var chained = owner.Combat.Actors.Where(x => owner.Combat.AreEnemies(owner, x) && x != primary)
                        .OrderBy(x => (x.transform.position - primary.transform.position).sqrMagnitude).FirstOrDefault();
                    if (chained && Vector3.Distance(chained.transform.position, primary.transform.position) <= 4.5f)
                    {
                        CombatVfx.Bolt(primary.transform.position + Vector3.up, chained.transform.position + Vector3.up, WeaponSkillExecutor.Tint(DamageElement.Lightning));
                        owner.Combat.DealDamage(chained, new DamageInfo(owner, brain.Definition.attackDamage * .38f, DamageKind.Lightning, element: DamageElement.Lightning, impact: ImpactTier.Proc));
                    }
                    break;
                case ElementTag.Poison:
                    primary.Statuses.Apply(owner, new StatusPayload { kind = StatusKind.Poison, duration = 6, power = 1.35f, maxStacks = 5 });
                    AreaAttack.Spawn(owner, owner.transform.position, 1.5f, brain.Definition.attackDamage * .12f, .25f, 2.8f);
                    break;
                case ElementTag.Void:
                    primary.Statuses.Apply(owner, new StatusPayload { kind = StatusKind.VoidMark, duration = 3, power = 0, maxStacks = 3 });
                    Vector3 pull = owner.transform.position - primary.transform.position; pull.y = 0;
                    primary.Motor.Impact(pull.normalized * 2.4f, .04f);
                    break;
            }
        }
    }
}
