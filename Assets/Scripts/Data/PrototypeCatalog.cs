using System.Linq;
using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Prototype catalog")]
    public sealed class PrototypeCatalog : ScriptableObject
    {
        public WeaponDefinition weapon;
        public BossDefinition boss;
        public ItemDefinition[] items;
        public RoomDefinition[] rooms;
        [Range(1, 2)] public int corruptedAtFourPlayers = 2;
        public ItemDefinition FindItem(string id) => items.FirstOrDefault(x => x.id == id);
    }
}
