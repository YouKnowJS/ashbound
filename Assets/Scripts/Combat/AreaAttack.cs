using UnityEngine;

namespace Ashbound
{
    public sealed class AreaAttack : MonoBehaviour
    {
        private Combatant owner;
        private float radius, damage, warning, lifetime, nextTick;
        private GameObject indicator;
        private bool fired, persistent;
        public static void Spawn(Combatant source, Vector3 center, float radius, float damage, float warning, float duration = 0)
        {
            var obj = new GameObject(duration > 0 ? "Burning ground" : "Telegraphed area attack");
            obj.transform.position = center;
            var area = obj.AddComponent<AreaAttack>(); area.owner = source; area.radius = radius; area.damage = damage;
            area.warning = warning; area.lifetime = duration; area.persistent = duration > 0;
            area.indicator = CombatVfx.Ring(center, radius, source.Corruption ? Palette.Corrupted : Palette.Danger, warning + duration + .3f, .12f);
            area.indicator.transform.SetParent(obj.transform, true);
        }
        private void Update()
        {
            if (!owner || !owner.Alive || !CombatRules.IsCombatState(owner.Combat.State)) { Destroy(gameObject); return; }
            if (!owner.Combat.Active) return;
            if (!fired)
            {
                warning -= Time.deltaTime;
                if (warning > 0) return;
                fired = true;
                CombatVfx.Pulse(transform.position, radius, owner.Corruption ? Palette.Corrupted : Palette.Danger);
                if (!persistent)
                {
                    owner.Combat.DamageArea(owner, transform.position, radius, damage, DamageKind.Ability, .15f, 4);
                    Destroy(gameObject); return;
                }
            }
            lifetime -= Time.deltaTime; nextTick -= Time.deltaTime;
            if (nextTick <= 0) { nextTick = .5f; owner.Combat.DamageArea(owner, transform.position, radius, damage * .5f, DamageKind.Hazard); }
            if (lifetime <= 0) Destroy(gameObject);
        }
    }
}
