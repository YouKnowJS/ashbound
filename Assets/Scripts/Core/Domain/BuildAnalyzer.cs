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
        private static readonly HashSet<BuildTag> BuildThemes = new HashSet<BuildTag>
        {
            BuildTag.Fire, BuildTag.Frost, BuildTag.Lightning, BuildTag.Poison, BuildTag.Void,
            BuildTag.Critical, BuildTag.Bleed, BuildTag.Heavy, BuildTag.Combo, BuildTag.DashPrecision,
            BuildTag.Sustain, BuildTag.Control, BuildTag.Area, BuildTag.DamageOverTime
        };
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
            CountTags(itemTags).Where(x => BuildThemes.Contains(x.Tag))
                .Take(limit).Select(x => x.Tag).ToArray();
        public static BuildTag[] Analyze(IEnumerable<BuildTag> relicTags, IEnumerable<BuildTag> weaponTags,
            IEnumerable<BuildTag> skillTags, IEnumerable<BuildTag> armorTags, IEnumerable<BuildTag> setTags, int limit=3) =>
            Dominant(new[]{relicTags??Enumerable.Empty<BuildTag>(),weaponTags??Enumerable.Empty<BuildTag>(),skillTags??Enumerable.Empty<BuildTag>(),armorTags??Enumerable.Empty<BuildTag>(),setTags??Enumerable.Empty<BuildTag>()},limit);

        public static string[] ReflectionItems(IEnumerable<BuildTag> dominant)
        {
            var items = new List<string>();
            foreach (var tag in dominant.Distinct().Take(3))
            {
                switch (tag)
                {
                    case BuildTag.Critical: items.AddRange(new[] { "glass-sigil", "echo-edge" }); break;
                    case BuildTag.Bleed: items.AddRange(new[] { "thorn-rune", "rupture" }); break;
                    case BuildTag.Lightning: items.AddRange(new[] { "storm-coil", "forked-heart" }); break;
                    case BuildTag.Fire: items.AddRange(new[] { "ember-brand", "flashpoint" }); break;
                    case BuildTag.Frost: items.AddRange(new[] { "rime-edge", "shatter" }); break;
                    case BuildTag.Poison: items.AddRange(new[] { "venom-edge", "execution-toxin" }); break;
                    case BuildTag.Void: items.AddRange(new[] { "void-mark", "collapse" }); break;
                    case BuildTag.Heavy: items.AddRange(new[] { "patient-force", "fault-line" }); break;
                    case BuildTag.Combo: items.AddRange(new[] { "rising-tempo", "crescendo" }); break;
                    case BuildTag.DashPrecision: items.AddRange(new[] { "afterimage-edge", "keen-step" }); break;
                    case BuildTag.Sustain: items.AddRange(new[] { "warded-heel", "rupture" }); break;
                    case BuildTag.Control: items.AddRange(new[] { "rime-edge", "rift-step" }); break;
                    case BuildTag.Area: items.AddRange(new[] { "storm-coil", "flashpoint" }); break;
                    case BuildTag.DamageOverTime: items.AddRange(new[] { "venom-edge", "ember-brand" }); break;
                }
            }
            return items.Distinct().Take(4).ToArray();
        }
    }
}
