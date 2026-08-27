using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ashbound.Editor
{
    public sealed class PrototypeContentBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AssetDatabase.LoadAssetAtPath<PrototypeCatalog>("Assets/Resources/PrototypeCatalog.asset"))
                throw new BuildFailedException("Create the prototype content from the Ashbound menu before building.");
        }
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!Application.isPlaying && !File.Exists("Assets/Resources/PrototypeCatalog.asset")) CreateContent();
            };
        }

        [MenuItem("Ashbound/Create prototype content")]
        public static void CreateContent()
        {
            bool missingScene = new[] { "MainMenu", "PrototypeRun", "TestArena" }.Any(name => !File.Exists("Assets/Scenes/" + name + ".unity"));
            if (missingScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string[] folders = { "Assets/Resources", "Assets/ScriptableObjects/Items", "Assets/ScriptableObjects/Bosses",
                "Assets/ScriptableObjects/Corruption", "Assets/ScriptableObjects/Rooms", "Assets/ScriptableObjects/Lore", "Assets/ScriptableObjects/Weapons", "Assets/Scenes" };
            foreach (string folder in folders) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var items = CreateItems();
            var weapon = Asset<WeaponDefinition>("Assets/ScriptableObjects/Weapons/WayfarersEdge.asset", x => { });
            var corruption = Asset<BossCorruptionProfile>("Assets/ScriptableObjects/Corruption/Ash.asset", x => { });
            var boss = Asset<BossDefinition>("Assets/ScriptableObjects/Bosses/CinderRegent.asset", x => x.corruption = corruption);
            var firstLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/WatchkeepersNote.asset", x =>
            { x.id = "watchkeepers-note"; x.title = "A watchkeeper's note"; x.text = "We carried no fuel below. Still, every morning, the braziers were warm."; });
            var secondLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/EmptyThrone.asset", x =>
            { x.id = "empty-throne"; x.title = "Inscription beneath a bell"; x.text = "The keeper is a title, not a name. The stone has been carved over many times."; });
            var points = new[] { new Vector3(-7, 0, 4), new Vector3(6, 0, 5), new Vector3(0, 0, 7), new Vector3(8, 0, -1), new Vector3(-8, 0, -1), new Vector3(3, 0, 3), new Vector3(-4, 0, 6), new Vector3(0, 0, 4) };
            var room1 = Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/Threshold.asset", x =>
            {
                x.id = "threshold"; x.displayName = "01 / The Threshold"; x.description = "Old bells ring beneath the stone.";
                x.spawnPoints = points; x.fragment = firstLore;
                x.waves = new[] { new EnemyWave { enemies = new[] { EnemyKind.Cinderling, EnemyKind.Cinderling, EnemyKind.Lantern } },
                    new EnemyWave { enemies = new[] { EnemyKind.Cinderling, EnemyKind.Lantern, EnemyKind.Hound, EnemyKind.Cinderling } } };
            });
            var room2 = Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/BellChamber.asset", x =>
            {
                x.id = "bell-chamber"; x.displayName = "02 / The Bell Chamber"; x.description = "Something moves behind the last seal.";
                x.spawnPoints = points; x.fragment = secondLore;
                x.waves = new[] { new EnemyWave { enemies = new[] { EnemyKind.Cinderling, EnemyKind.Hound, EnemyKind.Lantern, EnemyKind.Hound } },
                    new EnemyWave { enemies = new[] { EnemyKind.Elite, EnemyKind.Lantern, EnemyKind.Hound, EnemyKind.Lantern } } };
            });
            var bossRoom = Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/CinderThrone.asset", x =>
            { x.id = "cinder-throne"; x.displayName = "03 / The Cinder Throne"; x.description = "The last keeper waits."; x.isBoss = true; x.spawnPoints = points; x.waves = Array.Empty<EnemyWave>(); });
            var catalog = Asset<PrototypeCatalog>("Assets/Resources/PrototypeCatalog.asset", x =>
            { x.items = items; x.weapon = weapon; x.boss = boss; x.rooms = new[] { room1, room2, bossRoom }; });
            if (!File.Exists("Assets/Resources/PrototypeLit.mat")) AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), "Assets/Resources/PrototypeLit.mat");
            if (!File.Exists("Assets/Resources/PrototypeLine.mat")) AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")), "Assets/Resources/PrototypeLine.mat");
            ConfigureProject(); AssetDatabase.SaveAssets();
            string previous = SceneManager.GetActiveScene().path;
            bool createdScene = CreateScene("MainMenu", PrototypeSceneMode.Lobby, catalog);
            createdScene |= CreateScene("PrototypeRun", PrototypeSceneMode.Run, catalog);
            createdScene |= CreateScene("TestArena", PrototypeSceneMode.TestArena, catalog);
            EditorBuildSettings.scenes = new[] { "MainMenu", "PrototypeRun", "TestArena" }
                .Select(name => new EditorBuildSettingsScene("Assets/Scenes/" + name + ".unity", true)).ToArray();
            if (createdScene)
            {
                if (!string.IsNullOrEmpty(previous) && File.Exists(previous)) EditorSceneManager.OpenScene(previous);
                else EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Ashbound content ready: 8 items, 3 rooms, boss, corruption profile, and 3 scenes.");
        }

        private static ItemDefinition[] CreateItems()
        {
            return new[]
            {
                Item("glass-sigil", "Glass Sigil", "Gain 17% critical chance. Critical strikes deal 170% damage.", new[] { BuildTag.Critical },
                    new StatModifiers { criticalChance = .17f }),
                Item("echo-edge", "Echo Edge", "Critical strikes release an echo, dealing 35% weapon damage in a small area.", new[] { BuildTag.Critical }, default,
                    new[] { new TriggeredEffect { kind = TriggerKind.CriticalEcho, power = .35f } }),
                Item("quicksilver", "Quicksilver Oath", "Every critical strike reduces your active ability cooldown by 0.7 seconds.", new[] { BuildTag.Critical, BuildTag.Mobility }, default,
                    new[] { new TriggeredEffect { kind = TriggerKind.CriticalCooldown, power = .7f } }),
                Item("thorn-rune", "Thorn Rune", "Weapon hits apply bleed: 3 damage per second per stack for 4 seconds. Up to 5 stacks.", new[] { BuildTag.Bleed }, default, null,
                    new[] { new StatusPayload { kind = StatusKind.Bleed, duration = 4, power = 3, maxStacks = 5 } }),
                Item("bloodglass", "Bloodglass", "Weapon hits deal 30% more damage to bleeding enemies. Gain 5% critical chance.", new[] { BuildTag.Bleed, BuildTag.Critical },
                    new StatModifiers { criticalChance = .05f }, new[] { new TriggeredEffect { kind = TriggerKind.BleedingVulnerability, power = .3f } }),
                Item("rupture", "Red Benediction", "At 4 bleed stacks, consume bleed in a 40-damage explosion. Heal 4 health.", new[] { BuildTag.Bleed, BuildTag.Sustain }, default,
                    new[] { new TriggeredEffect { kind = TriggerKind.BleedRupture, power = 40, threshold = 4 } }),
                Item("storm-coil", "Storm Coil", "Every fourth landed weapon hit arcs lightning through up to 3 enemies for 16 damage each.", new[] { BuildTag.Lightning }, default,
                    new[] { new TriggeredEffect { kind = TriggerKind.ChainLightning, power = 16, threshold = 4 } }),
                Item("forked-heart", "Forked Heart", "Lightning jumps to 2 more targets. Critical strikes supply 2 additional lightning charges.", new[] { BuildTag.Lightning, BuildTag.Critical }, default,
                    new[] { new TriggeredEffect { kind = TriggerKind.LightningConductor, power = 2 } }, null, "storm-coil")
            };
        }
        private static ItemDefinition Item(string id, string name, string description, BuildTag[] tags, StatModifiers modifiers,
            TriggeredEffect[] effects = null, StatusPayload[] statuses = null, string requirement = "") =>
            Asset<ItemDefinition>("Assets/ScriptableObjects/Items/" + id + ".asset", x =>
            {
                x.id = id; x.displayName = name; x.description = description; x.tags = tags; x.statModifiers = modifiers;
                x.triggeredEffects = effects ?? Array.Empty<TriggeredEffect>(); x.statusEffects = statuses ?? Array.Empty<StatusPayload>();
                x.requiredItemId = requirement; x.rarity = id == "rupture" || id == "forked-heart" ? Rarity.Rare : Rarity.Uncommon;
            });
        private static T Asset<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>(); initialize(asset); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static bool CreateScene(string name, PrototypeSceneMode mode, PrototypeCatalog catalog)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (File.Exists(path)) return false;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Ashbound prototype").AddComponent<PrototypeBootstrap>();
            bootstrap.mode = mode; bootstrap.catalog = catalog;
            EditorSceneManager.SaveScene(scene, path);
            return true;
        }
        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "AshboundPrototype"; PlayerSettings.productName = "Ashbound";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler"); if (input != null) input.intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }
        [MenuItem("Ashbound/Build Windows prototype")]
        public static void BuildWindows()
        {
            CreateContent();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(),
                locationPathName = "Builds/Windows/Ashbound.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Windows build failed: " + report.summary.result);
            Debug.Log("Windows prototype built: Builds/Windows/Ashbound.exe");
        }
    }
}
