using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public readonly struct TagWeight
    {
        public readonly BuildTag Tag;
        public readonly int Count;
        public TagWeight(BuildTag tag, int count) { Tag = tag; Count = count; }
    }

    public static class BuildAnalyzer
    {
        public static List<TagWeight> CountTags(IEnumerable<IEnumerable<BuildTag>> itemTags)
        {
            var counts = new Dictionary<BuildTag, int>();
            foreach (var tags in itemTags)
                foreach (var tag in tags.Distinct())
                    counts[tag] = counts.TryGetValue(tag, out int count) ? count + 1 : 1;
            return counts.Select(pair => new TagWeight(pair.Key, pair.Value))
                .OrderByDescending(pair => pair.Count).ThenBy(pair => pair.Tag).ToList();
        }

        public static BuildTag[] Dominant(IEnumerable<IEnumerable<BuildTag>> itemTags, int limit = 2) =>
            CountTags(itemTags).Where(x => x.Tag == BuildTag.Critical || x.Tag == BuildTag.Bleed || x.Tag == BuildTag.Lightning)
                .Take(limit).Select(x => x.Tag).ToArray();

        public static string[] ReflectionItems(IEnumerable<BuildTag> dominant)
        {
            var items = new List<string>();
            foreach (var tag in dominant.Distinct().Take(2))
            {
                if (tag == BuildTag.Critical) items.AddRange(new[] { "glass-sigil", "echo-edge" });
                if (tag == BuildTag.Bleed) items.AddRange(new[] { "thorn-rune", "rupture" });
                if (tag == BuildTag.Lightning) items.AddRange(new[] { "storm-coil", "forked-heart" });
            }
            return items.ToArray();
        }
    }
}
