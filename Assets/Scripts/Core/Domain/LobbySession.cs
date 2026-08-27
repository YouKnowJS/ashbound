using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public enum InputKind { MouseKeyboard, SecondKeyboard, Gamepad }

    public sealed class LobbySlot
    {
        public string PlayerId { get; }
        public InputKind InputKind { get; }
        public int DeviceId { get; }
        public string DeviceLabel { get; }
        public LobbySlot(string id, InputKind kind, int deviceId, string label)
        { PlayerId = id; InputKind = kind; DeviceId = deviceId; DeviceLabel = label; }
    }

    public sealed class LobbySession
    {
        private readonly List<LobbySlot> slots = new List<LobbySlot>();
        public IReadOnlyList<LobbySlot> Slots => slots;
        public bool Locked { get; private set; }
        public LobbySession() { TryJoin(InputKind.MouseKeyboard, -1, "Keyboard + mouse"); }

        public bool TryJoin(InputKind kind, int deviceId, string label)
        {
            if (Locked || slots.Count >= 4 || slots.Any(x => x.InputKind == kind && (kind != InputKind.Gamepad || x.DeviceId == deviceId))) return false;
            string id = Enumerable.Range(1, 4).Select(x => "P" + x).First(x => slots.All(s => s.PlayerId != x));
            slots.Add(new LobbySlot(id, kind, deviceId, label));
            return true;
        }

        public bool RemoveLast()
        {
            if (Locked || slots.Count <= 1) return false;
            slots.RemoveAt(slots.Count - 1);
            return true;
        }
        public void Lock() { Locked = true; }
        public void Unlock() { Locked = false; }
    }
}
