using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public sealed class CinderRegentController : MonoBehaviour
    {
        private Combatant actor;
        private BossDefinition definition;
        private bool busy;
        private int pattern;
        private float nextAttack;
        public bool SecondPhase { get; private set; }
        public event Action PhaseChanged;
        public void Configure(Combatant owner, BossDefinition data) { actor = owner; definition = data; }
        private void Update()
        {
            if (!actor.Alive || !actor.Combat.Active) return;
            if (!SecondPhase && actor.Health.CurrentHealth / actor.Health.MaxHealth <= definition.secondPhaseThreshold)
            { SecondPhase = true; CombatVfx.Pulse(transform.position, 3, Palette.Gold); PhaseChanged?.Invoke(); }
            if (busy) return;
            var target = actor.Combat.NearestEnemy(actor);
            if (!target) return;
            Vector3 offset = target.transform.position - transform.position;
            actor.Motor.SetFacing(offset);
            actor.Motor.SetMove(offset.magnitude > 5 ? offset.normalized * .5f : Vector3.zero);
            if (Time.time >= nextAttack) StartCoroutine(Pattern(target));
        }
        private IEnumerator Pattern(Combatant target)
        {
            busy = true; actor.Motor.SetMove(Vector3.zero);
            Vector3 direction = (target.transform.position - transform.position).normalized;
            float warning = definition.telegraphDuration * (SecondPhase ? .8f : 1);
            switch (pattern++ % 3)
            {
                case 0:
                    CombatVfx.Direction(transform.position, direction, 5, Palette.Danger, .65f);
                    yield return new WaitForSeconds(.65f);
                    if (Valid())
                        for (int i = -2; i <= 2; i++) CombatProjectile.Spawn(actor, Quaternion.AngleAxis(i * (SecondPhase ? 16 : 22), Vector3.up) * direction, SecondPhase ? 10 : 8, 13, Palette.Danger);
                    break;
                case 1:
                    AreaAttack.Spawn(actor, target.transform.position, definition.areaRadius, definition.areaDamage, warning);
                    if (SecondPhase)
                    {
                        AreaAttack.Spawn(actor, transform.position, 2.5f, definition.areaDamage, warning + .35f);
                        foreach (var player in actor.Combat.Actors)
                            if (player != target && actor.Combat.AreEnemies(actor, player)) AreaAttack.Spawn(actor, player.transform.position, 2.3f, 20, warning + .2f);
                    }
                    yield return new WaitForSeconds(warning + .4f);
                    break;
                case 2:
                    CombatVfx.Direction(transform.position, direction, 11, Palette.Danger, warning);
                    yield return new WaitForSeconds(warning);
                    if (Valid())
                    {
                        actor.Motor.Lunge(direction, 15, .65f);
                        var hit = new HashSet<string>();
                        float remaining = .65f;
                        while (remaining > 0 && Valid())
                        {
                            foreach (var other in actor.Combat.Actors)
                                if (actor.Combat.AreEnemies(actor, other) && Vector3.Distance(transform.position, other.transform.position) < 2.1f && hit.Add(other.Id))
                                    actor.Combat.DealDamage(other, new DamageInfo(actor, 24, DamageKind.Ability, direction, .15f, 8));
                            remaining -= Time.deltaTime; yield return null;
                        }
                    }
                    break;
            }
            nextAttack = Time.time + (SecondPhase ? .75f : 1.3f); busy = false;
        }
        private bool Valid() => actor && actor.Alive && actor.Combat.State == RunState.BossFight;
    }
}
