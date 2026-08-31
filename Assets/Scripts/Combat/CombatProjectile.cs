using UnityEngine;

namespace Ashbound
{
    public sealed class CombatProjectile : MonoBehaviour
    {
        private Combatant owner;
        private Vector3 direction;
        private float speed, damage, lifetime = 5;
        private DamageElement element;
        private ImpactTier impact;
        private bool triggerEffects;
        public static void Spawn(Combatant source, Vector3 direction, float speed, float damage, Color color)
            => Spawn(source, direction, speed, damage, color, 5, DamageElement.Physical, ImpactTier.Light, false);
        public static void Spawn(Combatant source, Vector3 direction, float speed, float damage, Color color,
            float lifetime, DamageElement element, ImpactTier impact, bool triggerEffects)
        {
            var obj = PrimitiveFactory.Shape("Ember projectile", PrimitiveType.Sphere, null,
                source.transform.position + Vector3.up * .9f + direction.normalized * .75f, Vector3.one * .36f, color);
            var projectile = obj.AddComponent<CombatProjectile>();
            projectile.owner = source; projectile.direction = direction.normalized; projectile.speed = speed; projectile.damage = damage;
            projectile.lifetime = lifetime; projectile.element = element; projectile.impact = impact; projectile.triggerEffects = triggerEffects;
        }
        private void Update()
        {
            if (!owner || !owner.Alive || !CombatRules.IsCombatState(owner.Combat.State)) { Destroy(gameObject); return; }
            if (!owner.Combat.Active) return;
            lifetime -= Time.deltaTime;
            if (lifetime <= 0) { Destroy(gameObject); return; }
            Vector3 from = transform.position, to = from + direction * speed * Time.deltaTime;
            if (Physics.Linecast(from, to, 1 << 0, QueryTriggerInteraction.Ignore)) { Destroy(gameObject); return; }
            foreach (var actor in owner.Combat.Actors)
            {
                if (!owner.Combat.AreEnemies(owner, actor)) continue;
                Vector3 center = actor.transform.position + Vector3.up * .9f;
                Vector3 segment = to - from;
                float t = Mathf.Clamp01(Vector3.Dot(center - from, segment) / Mathf.Max(.0001f, segment.sqrMagnitude));
                if ((center - (from + segment * t)).sqrMagnitude > .6f * .6f) continue;
                owner.Combat.DealDamage(actor, new DamageInfo(owner, damage, DamageKind.Projectile, direction, .08f, 2,
                    true, triggerEffects, element, impact));
                CombatVfx.Pulse(actor.transform.position, .5f, Palette.Danger); Destroy(gameObject); return;
            }
            transform.position = to;
        }
    }
}
