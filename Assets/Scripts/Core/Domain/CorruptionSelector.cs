using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public static class CorruptionSelector
    {
        public static string[] Select(IReadOnlyList<string> playerIds, int fourPlayerCount, Random random, IReadOnlyList<string> forced = null)
        {
            if (playerIds.Count < 2) return Array.Empty<string>();
            if (playerIds.Count > 4 || playerIds.Distinct().Count() != playerIds.Count) throw new ArgumentException("A roster must contain 1–4 unique IDs.");
            int count = playerIds.Count == 4 ? Math.Max(1, Math.Min(2, fourPlayerCount)) : 1;
            var selected = new List<string>();
            if (forced != null)
                foreach (string id in forced)
                    if (playerIds.Contains(id) && !selected.Contains(id) && selected.Count < count) selected.Add(id);
            var candidates = playerIds.Where(id => !selected.Contains(id)).ToList();
            while (selected.Count < count)
            {
                int index = random.Next(candidates.Count);
                selected.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return selected.ToArray();
        }
    }
}
