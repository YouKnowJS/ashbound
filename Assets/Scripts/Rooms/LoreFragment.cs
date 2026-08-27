using UnityEngine;

namespace Ashbound
{
    public sealed class LoreFragment : MonoBehaviour
    {
        public LoreEntry Entry { get; private set; }
        public void Configure(LoreEntry entry) { Entry = entry; }
        public static LoreFragment Spawn(LoreEntry entry, Vector3 position)
        {
            var obj = PrimitiveFactory.Shape("Text fragment", PrimitiveType.Cube, null, position + Vector3.up * .25f,
                new Vector3(.6f, .15f, .8f), Palette.Gold);
            var fragment = obj.AddComponent<LoreFragment>(); fragment.Configure(entry); return fragment;
        }
    }
}
