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
            var definition = catalog.FindEnemy(kind);
            if (definition) return Enemy(definition, position, partySize);
            throw new System.InvalidOperationException("Missing enemy definition for legacy kind " + kind + ". Rebuild prototype content.");
        }

        public Combatant Enemy(EnemyDefinition definition, Vector3 position, int partySize)
        {
            float partyHealth = definition.maxHealth * (1 + .15f * (partySize - 1));
            var actor = Create("E" + ++sequence, definition.displayName, false, Faction.Hostiles, partyHealth, position,
                definition.element == ElementTag.None ? definition.baseTint : WeaponSkillExecutor.Tint(WeaponSkillExecutor.Element(definition.element)),
                definition.visualScale, definition.elite || definition.role == EnemyRole.Bruiser || definition.role == EnemyRole.Mage);
            actor.BaseSpeed = definition.movementSpeed;
            actor.IsBoss = definition.legacyKind == EnemyKind.MiniBoss;
            actor.ConfigureEnemy(definition);
            if (definition.shield > 0) actor.Health.Shield(definition.shield);
            if (definition.prefab) Object.Instantiate(definition.prefab, actor.transform);
            actor.gameObject.AddComponent<EnemyBrain>().Configure(actor, definition);
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
