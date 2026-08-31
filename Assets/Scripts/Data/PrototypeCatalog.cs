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
        [Range(1, 2)] public int corruptedAtFourPlayers = 2;
        public ItemDefinition FindItem(string id) => items.FirstOrDefault(x => x.id == id);
        public WeaponDefinition FindWeapon(WeaponFamily family) => weapons?.FirstOrDefault(x => x && x.family == family) ?? weapon;
        public WeaponDefinition FindWeapon(string id) => weapons?.FirstOrDefault(x=>x&&x.id==id) ?? weapon;
        public ArmorDefinition FindArmor(string id)=>armor?.FirstOrDefault(x=>x&&x.id==id);
    }
}
