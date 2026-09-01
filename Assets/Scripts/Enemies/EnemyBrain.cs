using System.Collections;
using UnityEngine;

namespace Ashbound
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        public static bool AiEnabled { get; set; } = true;
        public static bool TelegraphsEnabled { get; set; } = true;
        private EnemyRoleBehaviour behavior;
        private Combatant target;
        private float nextAttack, nextTargetScan;
        private bool busy, aerial, burrowed;
        public Combatant Actor { get; private set; }
        public EnemyDefinition Definition { get; private set; }
        public bool Busy => busy;

        public void Configure(Combatant owner, EnemyDefinition definition)
        {
            Actor = owner; Definition = definition;
            behavior = CreateBehavior(definition.role);
            owner.Health.Died += OnDeath;
            owner.Combat.DamageResolved += OnDamageResolved;
        }

        private static EnemyRoleBehaviour CreateBehavior(EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Bruiser: return new BruiserEnemyBehaviour();
                case EnemyRole.Assassin: return new AssassinEnemyBehaviour();
                case EnemyRole.Ranger: return new RangerEnemyBehaviour();
                case EnemyRole.Mage: return new MageEnemyBehaviour();
                case EnemyRole.Flyer: return new FlyerEnemyBehaviour();
                case EnemyRole.Burrower: return new BurrowerEnemyBehaviour();
                case EnemyRole.Bomber: return new BomberEnemyBehaviour();
                case EnemyRole.Support: return new SupportEnemyBehaviour();
                case EnemyRole.Controller: return new ControllerEnemyBehaviour();
                default: return new WarriorEnemyBehaviour();
            }
        }

        private void Update()
        {
            if (!Actor || !Actor.Alive || !Actor.Combat.Active || !AiEnabled || busy) { if (Actor) Actor.Motor.SetMove(Vector3.zero); return; }
            if (!target || !Actor.Combat.AreEnemies(Actor, target) || Time.time >= nextTargetScan)
            {
                target = SelectTarget(); nextTargetScan = Time.time + .25f;
            }
            if (!target) { Actor.Motor.SetMove(Vector3.zero); return; }
            Vector3 offset = target.transform.position - transform.position; offset.y = 0;
            float distance = offset.magnitude; Vector3 direction = distance > .01f ? offset / distance : Actor.Motor.Facing;
            Actor.Motor.SetFacing(direction);
            behavior.Move(this, target, distance, direction);
            if (distance <= Mathf.Max(Definition.preferredDistance + 1.5f, Definition.role == EnemyRole.Ranger || Definition.role == EnemyRole.Mage || Definition.role == EnemyRole.Controller || Definition.role == EnemyRole.Support ? 11 : 3.6f)
                && Time.time >= nextAttack && !Actor.Motor.IsStunned) StartCoroutine(AttackRoutine(target));
        }

        private Combatant SelectTarget()
        {
            Combatant selected=null;float best=float.MaxValue;
            foreach(var candidate in Actor.Combat.Actors)
            {
                if(!Actor.Combat.AreEnemies(Actor,candidate))continue;float score;
                if(Definition.targeting==EnemyTargetingStyle.LowestHealth)score=candidate.Health.CurrentHealth/candidate.Health.MaxHealth;
                else if(Definition.targeting==EnemyTargetingStyle.Furthest)score=-(candidate.transform.position-transform.position).sqrMagnitude;
                else if(Definition.targeting==EnemyTargetingStyle.Isolated||Definition.targeting==EnemyTargetingStyle.Cluster)
                {
                    int nearby=0;foreach(var other in Actor.Combat.Actors)if(other!=candidate&&Actor.Combat.AreEnemies(Actor,other)&&(other.transform.position-candidate.transform.position).sqrMagnitude<16)nearby++;
                    score=Definition.targeting==EnemyTargetingStyle.Isolated?nearby:-nearby;
                }
                else score=(candidate.transform.position-transform.position).sqrMagnitude;
                if(score<best){best=score;selected=candidate;}
            }
            return selected;
        }

        private IEnumerator AttackRoutine(Combatant attackTarget)
        {
            busy = true; Actor.Motor.SetMove(Vector3.zero);
            if(Definition.attackVfxPrefab) Destroy(Instantiate(Definition.attackVfxPrefab,transform.position,transform.rotation),3);
            if(Definition.attackAudio) AudioSource.PlayClipAtPoint(Definition.attackAudio,transform.position);
            yield return behavior.Attack(this, attackTarget);
            nextAttack = Time.time + Definition.attackCooldown;
            busy = false;
        }

        public IEnumerator WarningRing(float radius, float seconds) => WarningAt(transform.position, radius, seconds);
        public IEnumerator WarningAt(Vector3 position, float radius, float seconds, Color? color = null)
        {
            if (TelegraphsEnabled) CombatVfx.Ring(position, radius, color ?? TelegraphColor(), seconds, .13f, true);
            if(TelegraphsEnabled&&Definition.telegraphPrefab)Destroy(Instantiate(Definition.telegraphPrefab,position,Quaternion.identity),seconds+.1f);
            yield return new WaitForSeconds(seconds);
        }
        public IEnumerator WarningDirection(Vector3 direction, float length, float seconds)
        {
            if (TelegraphsEnabled) CombatVfx.Direction(transform.position, direction, length, TelegraphColor(), seconds);
            if(TelegraphsEnabled&&Definition.telegraphPrefab)Destroy(Instantiate(Definition.telegraphPrefab,transform.position,Quaternion.LookRotation(direction)),seconds+.1f);
            yield return new WaitForSeconds(seconds);
        }
        private Color TelegraphColor() => Definition.element == ElementTag.None ? Palette.Danger : WeaponSkillExecutor.Tint(WeaponSkillExecutor.Element(Definition.element));
        public bool CanFinish(Combatant attackTarget) => Actor && Actor.Alive && Actor.Combat.Active && attackTarget && attackTarget.Alive;
        public Vector3 DirectionTo(Combatant other)
        {
            Vector3 direction = other.transform.position - transform.position; direction.y = 0;
            return direction.sqrMagnitude > .01f ? direction.normalized : Actor.Motor.Facing;
        }
        public void AreaHit(Vector3 center, float radius, float multiplier, float stun, float knockback)
        {
            Actor.Combat.DamageArea(Actor, center, radius, Definition.attackDamage * multiplier, DamageKind.Ability, stun, knockback);
            CombatVfx.Pulse(center, radius, TelegraphColor());
        }
        public void DirectHit(Combatant other, float multiplier, float stun, float knockback)
        {
            Actor.Combat.DealDamage(other, new DamageInfo(Actor, Definition.attackDamage * multiplier, DamageKind.Ability, DirectionTo(other), stun, knockback,
                element: WeaponSkillExecutor.Element(Definition.element), impact: ImpactTier.Heavy));
        }
        public void Projectile(Vector3 direction, float speed, float multiplier)
        {
            CombatProjectile.Spawn(Actor, direction, speed, Definition.attackDamage * multiplier, TelegraphColor(), 6,
                WeaponSkillExecutor.Element(Definition.element), ImpactTier.Light, false);
        }
        public Combatant FindWoundedAlly()
        {
            Combatant selected=null;float ratio=.82f;foreach(var other in Actor.Combat.Actors){if(!other||other==Actor||!other.Alive||other.Faction!=Actor.Faction)continue;float current=other.Health.CurrentHealth/other.Health.MaxHealth;if(current<ratio){ratio=current;selected=other;}}return selected;
        }
        public void SetAerial(bool value)
        {
            if (aerial == value) return; aerial = value; Actor.SetTargetable(!value);
            if (Actor.View) Actor.View.SetElevation(value ? 1.65f : .15f);
        }
        public void SetBurrowed(bool value)
        {
            burrowed = value; Actor.SetTargetable(!value);
            if (Actor.View) Actor.View.SetVisible(!value);
        }
        private void OnDeath()
        {
            if (Actor) { Actor.SetTargetable(false); if (aerial && Actor.View) Actor.View.SetElevation(0); }
            if (Definition && Definition.role == EnemyRole.Bomber && Actor && Actor.Combat.Active)
            {
                CombatVfx.Pulse(transform.position, 2.5f, TelegraphColor());
                Actor.Combat.DamageArea(Actor, transform.position, 2.5f, Definition.attackDamage * .7f, DamageKind.Hazard, .08f, 4);
            }
        }
        private void OnDamageResolved(DamageEvent hit)
        {
            if(hit.Info.Source!=Actor)return;if(hit.Info.Kind!=DamageKind.Ability&&hit.Info.Kind!=DamageKind.Projectile)return;EnemyElementRuntime.Resolve(this,hit.Target);
        }
        private void OnDisable() { if (Actor && burrowed) SetBurrowed(false); }
        private void OnDestroy(){if(Actor&&Actor.Combat)Actor.Combat.DamageResolved-=OnDamageResolved;}
    }
}
