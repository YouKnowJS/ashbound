using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public sealed class EnemyController : MonoBehaviour
    {
        private Combatant actor;
        private EnemyKind kind;
        private float nextAttack;
        private bool winding;
        public void Configure(Combatant owner, EnemyKind type) { actor = owner; kind = type; }
        private void Update()
        {
            if (!actor.Alive || !actor.Combat.Active || winding) return;
            var target = actor.Combat.NearestEnemy(actor);
            if (!target) { actor.Motor.SetMove(Vector3.zero); return; }
            Vector3 offset = target.transform.position - transform.position;
            actor.Motor.SetFacing(offset);
            float distance = offset.magnitude;
            if (kind == EnemyKind.Lantern)
                actor.Motor.SetMove(distance > 7 ? offset.normalized : distance < 4 ? -offset.normalized : Vector3.zero);
            else actor.Motor.SetMove(distance > (kind == EnemyKind.Elite || kind == EnemyKind.MiniBoss || kind == EnemyKind.Bulwark ? 2.1f : 1.2f) ? offset.normalized : Vector3.zero);
            float range = kind == EnemyKind.Lantern || kind == EnemyKind.Hound ? 10 : kind == EnemyKind.Elite || kind == EnemyKind.MiniBoss || kind == EnemyKind.Bulwark ? 3 : 1.7f;
            if (distance < range && Time.time >= nextAttack && !actor.Motor.IsStunned) StartCoroutine(Attack(target));
        }
        private IEnumerator Attack(Combatant target)
        {
            winding = true; actor.Motor.SetMove(Vector3.zero);
            Vector3 direction = (target.transform.position - transform.position).normalized;
            if (kind == EnemyKind.Hound)
            {
                var marker = CombatVfx.Direction(transform.position, direction, 9, Palette.Danger, .75f);
                yield return new WaitForSeconds(.75f);
                if (marker) Destroy(marker);
                if (CanFinish())
                {
                    actor.Motor.Lunge(direction, 14, .55f);
                    var hit = new HashSet<string>();
                    float time = .55f;
                    while (time > 0 && CanFinish())
                    {
                        foreach (var other in actor.Combat.Actors)
                            if (actor.Combat.AreEnemies(actor, other) && Vector3.Distance(transform.position, other.transform.position) < 1.4f && hit.Add(other.Id))
                                actor.Combat.DealDamage(other, new DamageInfo(actor, 16, DamageKind.Ability, direction, .13f, 5));
                        time -= Time.deltaTime; yield return null;
                    }
                }
            }
            else if (kind == EnemyKind.Lantern)
            {
                CombatVfx.Ring(transform.position, .9f, Palette.Danger, .65f);
                yield return new WaitForSeconds(.65f);
                if (CanFinish()) CombatProjectile.Spawn(actor, direction, 8, 9, Palette.Danger);
            }
            else if (kind == EnemyKind.MiniBoss && Random.value > .5f)
            {
                CombatVfx.Direction(transform.position, direction, 11, Palette.Gold, .8f);
                yield return new WaitForSeconds(.8f);
                if (CanFinish()) { CombatProjectile.Spawn(actor, direction, 10, 22, Palette.Gold); CombatProjectile.Spawn(actor, Quaternion.Euler(0,18,0)*direction, 10, 18, Palette.Gold); CombatProjectile.Spawn(actor, Quaternion.Euler(0,-18,0)*direction, 10, 18, Palette.Gold); }
            }
            else
            {
                float radius = kind == EnemyKind.MiniBoss ? 3.5f : kind == EnemyKind.Elite || kind == EnemyKind.Bulwark ? 2.9f : 1.65f;
                AreaAttack.Spawn(actor, transform.position + direction * .4f, radius, kind == EnemyKind.MiniBoss ? 27 : kind == EnemyKind.Elite || kind == EnemyKind.Bulwark ? 21 : 10, kind == EnemyKind.MiniBoss ? 1f : kind == EnemyKind.Elite || kind == EnemyKind.Bulwark ? .85f : .45f);
                yield return new WaitForSeconds(kind == EnemyKind.MiniBoss ? 1.1f : kind == EnemyKind.Elite || kind == EnemyKind.Bulwark ? .95f : .55f);
            }
            nextAttack = Time.time + (kind == EnemyKind.Hound ? 1.7f : 1.1f); winding = false;
        }
        private bool CanFinish() => actor && actor.Alive && actor.Combat.Active;
    }
}
