using System.Linq;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Prototype catalog")]
    public sealed class PrototypeCatalog : ScriptableObject
    {
        public WeaponDefinition weapon;
        public WeaponDefinition[] weapons;
        public WeaponSkillDefinition[] weaponSkills;
        public ArmorDefinition[] armor;
        public ArmorSetDefinition[] armorSets;
        public HubFacilityDefinition[] facilities;
        public PreparationDefinition[] preparations;
        public ProgressionTuningDefinition progressionTuning;
        public BossDefinition boss;
        public ItemDefinition[] items;
        public RoomDefinition[] rooms;
        public EnemyDefinition[] enemies;
        public EncounterDefinition[] encounters;
        public CombatSpaceDefinition[] combatSpaces;
        public RegionEnemyPoolDefinition[] regionEcologies;
        public ExpeditionRegionDefinition prototypeRegion;
        public TreasureDefinition[] treasures;
        public MerchantDefinition[] merchants;
        public RestNodeDefinition[] rests;
        public EventDefinition[] events;
        public ChallengeDefinition[] challenges;
        public BossRewardDefinition[] bossRewards;
        [Range(1, 2)] public int corruptedAtFourPlayers = 2;
        public ItemDefinition FindItem(string id) => items.FirstOrDefault(x => x.id == id);
        public WeaponDefinition FindWeapon(WeaponFamily family) => weapons?.FirstOrDefault(x => x && x.family == family) ?? weapon;
        public WeaponDefinition FindWeapon(string id) => weapons?.FirstOrDefault(x=>x&&x.id==id) ?? weapon;
        public ArmorDefinition FindArmor(string id)=>armor?.FirstOrDefault(x=>x&&x.id==id);
        public EnemyDefinition FindEnemy(string id)=>enemies?.FirstOrDefault(x=>x&&x.id==id);
        public EnemyDefinition FindEnemy(EnemyKind kind)=>enemies?.FirstOrDefault(x=>x&&x.legacyFallback&&x.legacyKind==kind);
    }
}
