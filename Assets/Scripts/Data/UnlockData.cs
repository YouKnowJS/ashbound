using System;
using System.Collections.Generic;

namespace Ashbound
{
    [Serializable]
    public sealed class UnlockData
    {
        public int version = 1;
        public List<string> weapons = new List<string> { "wayfarer-edge" };
        public List<string> items = new List<string>();
        public List<string> maps = new List<string> { "cinder-vault" };
        public List<string> bosses = new List<string> { "cinder-regent" };
        public List<string> cosmetics = new List<string>();
    }
}
