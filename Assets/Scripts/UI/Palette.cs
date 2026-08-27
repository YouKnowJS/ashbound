using UnityEngine;

namespace Ashbound
{
    public static class Palette
    {
        public static readonly Color Player = new Color(.25f, .85f, .87f);
        public static readonly Color Gold = new Color(.95f, .74f, .4f);
        public static readonly Color Danger = new Color(1f, .28f, .16f);
        public static readonly Color Corrupted = new Color(.85f, .28f, 1f);
        public static readonly Color Lightning = new Color(.4f, .65f, 1f);
        public static readonly Color Bleed = new Color(.85f, .13f, .3f);
        public static readonly Color[] Party = { Player, new Color(.5f, .72f, 1), new Color(.72f, .95f, .5f), Gold };
    }
}
