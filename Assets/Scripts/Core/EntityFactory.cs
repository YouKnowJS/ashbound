using UnityEngine;

namespace Ashbound
{
    public sealed class EntityFactory
    {
        private readonly CombatService combat;
        private readonly PrototypeCatalog catalog;
        private readonly Transform root;
        private int sequence;
        public EntityFactory(CombatService combat, PrototypeCatalog catalog, Transform root)
        { this.combat = combat; this.catalog = catalog; this.root = root; }

        public Combatant Player(LobbySlot slot, int colorIndex, Vector3 position, Camera camera)
        {
            var actor = Create(slot.PlayerId, slot.PlayerId + " · Wanderer", true, Faction.Wanderers, 120, position, Palette.Party[colorIndex], 1);
            actor.gameObject.AddComponent<PlayerController>().Configure(actor, new LocalPlayerInput(slot, camera));
            return actor;
        }

        public Combatant Enemy(EnemyKind kind, Vector3 position, int partySize)
        {
            float health = kind == EnemyKind.Cinderling ? 52 : kind == EnemyKind.Lantern ? 42 : kind == EnemyKind.Hound ? 68 :
                kind == EnemyKind.Bulwark ? 120 : kind == EnemyKind.MiniBoss ? 430 : 180;
            string name = kind == EnemyKind.Cinderling ? "Cinderling" : kind == EnemyKind.Lantern ? "Lantern acolyte" : kind == EnemyKind.Hound ? "Ash hound" :
                kind == EnemyKind.Bulwark ? "Cinder Bulwark" : kind == EnemyKind.MiniBoss ? "The Cracked Warden" : "Bell Warden";
            var actor = Create("E" + ++sequence, name, false, Faction.Hostiles, health * (1 + .15f * (partySize - 1)), position,
                kind == EnemyKind.Lantern ? new Color(.75f, .55f, .35f) : kind == EnemyKind.Elite || kind == EnemyKind.MiniBoss ? Palette.Gold : Palette.Danger,
                kind == EnemyKind.MiniBoss ? 1.7f : kind == EnemyKind.Elite ? 1.4f : kind == EnemyKind.Hound ? .8f : 1, kind == EnemyKind.Elite || kind == EnemyKind.MiniBoss || kind == EnemyKind.Lantern);
            actor.BaseSpeed = kind == EnemyKind.Hound ? 4.3f : kind == EnemyKind.Lantern ? 2.8f : kind == EnemyKind.Elite || kind == EnemyKind.MiniBoss || kind == EnemyKind.Bulwark ? 2.6f : 3.5f;
            if (kind == EnemyKind.Elite || kind == EnemyKind.Bulwark) actor.Health.Shield(kind == EnemyKind.Bulwark ? 65 : 45);
            if (kind == EnemyKind.MiniBoss) actor.IsBoss = true;
            actor.gameObject.AddComponent<EnemyController>().Configure(actor, kind);
            return actor;
        }

        public Combatant Boss(Vector3 position, int partySize)
        {
            var actor = Create("B" + ++sequence, catalog.boss.displayName, false, Faction.Hostiles,
                catalog.boss.health * (1 + .65f * (partySize - 1)), position, new Color(.65f, .32f, .23f), 2.1f, true);
            actor.BaseSpeed = 3; actor.IsBoss = true;
            actor.gameObject.AddComponent<CinderRegentController>().Configure(actor, catalog.boss);
            return actor;
        }

        public Combatant Reflection(Combatant player, Vector3 position)
        {
            var actor = Create("R" + ++sequence, "Corrupted reflection", false, Faction.Corrupted, player.BaseMaxHealth, position, Palette.Corrupted, 1);
            actor.Weapon = player.Weapon;
            return actor;
        }

        private Combatant Create(string id, string name, bool player, Faction faction, float hp, Vector3 position, Color tint, float scale, bool angular = false)
        {
            var obj = new GameObject(name) { layer = 8 };
            obj.transform.SetParent(root); obj.transform.position = position;
            var actor = obj.AddComponent<Combatant>(); actor.Initialize(id, name, player, faction, hp, combat, catalog.weapon);
            actor.View = obj.AddComponent<ActorView>(); actor.View.Build(actor, tint, scale, angular);
            return actor;
        }
    }
}
