using UnityEngine;

namespace Ashbound
{
    [CreateAssetMenu(menuName = "Ashbound/Lore entry")]
    public sealed class LoreEntry : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 8)] public string text;
    }
}
