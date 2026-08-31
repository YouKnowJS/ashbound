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
            var weapons = CreateWeapons(); var weapon = weapons[0];
            var corruption = Asset<BossCorruptionProfile>("Assets/ScriptableObjects/Corruption/Ash.asset", x => { });
            var boss = Asset<BossDefinition>("Assets/ScriptableObjects/Bosses/CinderRegent.asset", x => x.corruption = corruption);
            var firstLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/WatchkeepersNote.asset", x =>
            { x.id = "watchkeepers-note"; x.title = "A watchkeeper's note"; x.text = "We carried no fuel below. Still, every morning, the braziers were warm."; });
            var secondLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/EmptyThrone.asset", x =>
            { x.id = "empty-throne"; x.title = "Inscription beneath a bell"; x.text = "The keeper is a title, not a name. The stone has been carved over many times."; });
            var points = new[] { new Vector3(-7, 0, 4), new Vector3(6, 0, 5), new Vector3(0, 0, 7), new Vector3(8, 0, -1), new Vector3(-8, 0, -1), new Vector3(3, 0, 3), new Vector3(-4, 0, 6), new Vector3(0, 0, 4) };
            var room1 = Room("Threshold", "threshold", "01 / The Threshold", "Old bells ring beneath the stone.", points, firstLore,
                EnemyKind.Cinderling, EnemyKind.Cinderling, EnemyKind.Lantern);
            var room2 = Room("BellChamber", "bell-chamber", "02 / The Bell Chamber", "Fast shapes circle the old bell.", points, secondLore,
                EnemyKind.Hound, EnemyKind.Lantern, EnemyKind.Cinderling, EnemyKind.Hound);
            var eliteRoom = Room("WardenCrossing", "warden-crossing", "03 / Warden Crossing", "A shielded keeper bars the crossing.", points, null,
                EnemyKind.Bulwark, EnemyKind.Elite, EnemyKind.Lantern);
            var room4 = Room("AshGallery", "ash-gallery", "04 / The Ash Gallery", "The vault gathers its remaining guard.", points, null,
                EnemyKind.Cinderling, EnemyKind.Hound, EnemyKind.Bulwark, EnemyKind.Lantern);
            var miniRoom = Room("CrackedSanctum", "cracked-sanctum", "05 / The Cracked Sanctum", "A lesser keeper tests what your build has become.", points, null,
                EnemyKind.MiniBoss);
            var room6 = Room("LastProcession", "last-procession", "06 / The Last Procession", "One final seal stands before the throne.", points, null,
                EnemyKind.Elite, EnemyKind.Hound, EnemyKind.Lantern, EnemyKind.Bulwark);
            var bossRoom = Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/CinderThrone.asset", x =>
            {
                x.id = "cinder-throne"; x.displayName = "07 / The Cinder Throne"; x.description = "The last keeper waits."; x.isBoss = true; x.spawnPoints = points; x.waves = Array.Empty<EnemyWave>();
            });
            var catalog = Asset<PrototypeCatalog>("Assets/Resources/PrototypeCatalog.asset", x =>
            { x.items = items; x.weapon = weapon; x.weapons = weapons; x.boss = boss; x.rooms = new[] { room1, room2, eliteRoom, room4, miniRoom, room6, bossRoom }; });
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
            Debug.Log("Ashbound v0.2 content ready: 45 relics, 8 weapons, 7 encounters, mini-boss, final boss, and 3 scenes.");
        }

        private static RoomDefinition Room(string assetName, string id, string name, string description, Vector3[] points, LoreEntry lore, params EnemyKind[] enemies) =>
            Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/" + assetName + ".asset", x =>
            { x.id = id; x.displayName = name; x.description = description; x.isBoss = false; x.spawnPoints = points; x.fragment = lore; x.waves = new[] { new EnemyWave { enemies = enemies } };
              x.enemyHealthMultiplier = id == "cracked-sanctum" ? 1.35f : id == "last-procession" ? 1.2f : 1; x.targetEncounterSeconds = id == "cracked-sanctum" ? 180 : 145; });

        private static WeaponDefinition[] CreateWeapons()
        {
            WeaponDefinition W(string id, string name, WeaponFamily family, WeaponMechanic mechanic, float damage, float rate, float reach, float arc, float knockback,
                float move, float crit, float critDamage, float projectile, int threshold, float power, params BuildTag[] tags) =>
                Asset<WeaponDefinition>(id == "wayfarer-edge" ? "Assets/ScriptableObjects/Weapons/WayfarersEdge.asset" : "Assets/ScriptableObjects/Weapons/" + id + ".asset", x =>
                { x.id=id; x.displayName=name; x.family=family; x.mechanic=mechanic; x.damage=damage; x.attackInterval=rate; x.reach=reach; x.arcDegrees=arc; x.knockback=knockback;
                  x.attackMoveMultiplier=move; x.criticalChanceModifier=crit; x.criticalDamageModifier=critDamage; x.projectileSpeed=projectile; x.projectileLifetime=1.5f; x.comboThreshold=threshold; x.mechanicPower=power; x.tags=tags; });
            return new[]
            {
                W("wayfarer-edge","Wayfarer's Edge",WeaponFamily.Sword,WeaponMechanic.None,24,.34f,2.7f,115,3,.9f,0,0,0,4,.15f),
                W("long-reach","Long Reach",WeaponFamily.Spear,WeaponMechanic.FocusedThrust,26,.43f,4.2f,42,4,.82f,0,0,0,3,.08f,BuildTag.Heavy),
                W("bell-cleaver","Bell Cleaver",WeaponFamily.Greatsword,WeaponMechanic.HeavyCommitment,48,.78f,3.25f,145,8,.55f,-.02f,.18f,0,3,.2f,BuildTag.Heavy),
                W("moon-shear","Moon Shear",WeaponFamily.Katana,WeaponMechanic.DashPrecision,27,.3f,2.9f,88,3.5f,.92f,.04f,.1f,0,3,.28f,BuildTag.Critical,BuildTag.DashPrecision),
                W("twin-embers","Twin Embers",WeaponFamily.DualBlades,WeaponMechanic.Momentum,13,.16f,1.85f,92,1.8f,1.05f,0,0,0,6,.03f,BuildTag.Combo),
                W("vault-bow","Vault Bow",WeaponFamily.Bow,WeaponMechanic.ChargedShot,23,.4f,0,0,2,.78f,.02f,.1f,22,3,.85f,BuildTag.DashPrecision),
                W("ashen-staff","Ashen Staff",WeaponFamily.Staff,WeaponMechanic.ArcaneCharge,19,.32f,0,0,2,.88f,0,0,16,4,.6f,BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void),
                W("rift-brand","Rift Brand",WeaponFamily.Spellblade,WeaponMechanic.SpellWave,22,.29f,2.45f,105,2.5f,.9f,0,0,0,4,.7f,BuildTag.Void)
            };
        }

        private static ItemDefinition[] CreateItems()
        {
            StatusPayload S(StatusKind kind, float duration, float power, int stacks) => new StatusPayload { kind=kind, duration=duration, power=power, maxStacks=stacks };
            TriggeredEffect E(TriggerKind kind, float power, int threshold=0) => new TriggeredEffect { kind=kind, power=power, threshold=threshold };
            return new[]
            {
                Item("glass-sigil", "Glass Sigil", "Gain 17% critical chance. Critical strikes deal 170% damage.", new[] { BuildTag.Critical },
                    new StatModifiers { criticalChance = .17f }),
                Item("echo-edge", "Echo Edge", "Critical strikes release an echo, dealing 35% weapon damage in a small area.", new[] { BuildTag.Critical }, default,
                    new[] { E(TriggerKind.CriticalEcho, .35f) }),
                Item("quicksilver", "Quicksilver Oath", "Every critical strike reduces your active ability cooldown by 0.7 seconds.", new[] { BuildTag.Critical, BuildTag.Mobility }, default,
                    new[] { E(TriggerKind.CriticalCooldown, .7f) }),
                Item("thorn-rune", "Thorn Rune", "Weapon hits apply bleed: 3 damage per second per stack for 4 seconds. Up to 5 stacks.", new[] { BuildTag.Bleed }, default, null,
                    new[] { S(StatusKind.Bleed,4,3,5) }),
                Item("bloodglass", "Bloodglass", "Weapon hits deal 30% more damage to bleeding enemies. Gain 5% critical chance.", new[] { BuildTag.Bleed, BuildTag.Critical },
                    new StatModifiers { criticalChance = .05f }, new[] { E(TriggerKind.BleedingVulnerability,.3f) }),
                Item("rupture", "Red Benediction", "At 4 bleed stacks, consume bleed in a 40-damage explosion. Heal 4 health.", new[] { BuildTag.Bleed, BuildTag.Sustain }, default,
                    new[] { E(TriggerKind.BleedRupture,40,4) }),
                Item("storm-coil", "Storm Coil", "Every fourth landed weapon hit arcs lightning through up to 3 enemies for 16 damage each.", new[] { BuildTag.Lightning }, default,
                    new[] { E(TriggerKind.ChainLightning,16,4) }),
                Item("forked-heart", "Forked Heart", "Lightning jumps to 2 more targets. Critical strikes supply 2 additional lightning charges.", new[] { BuildTag.Lightning, BuildTag.Critical }, default,
                    new[] { E(TriggerKind.LightningConductor,2) }, null, "storm-coil"),

                Item("ember-brand","Ember Brand","Weapon hits apply stacking Burn.",new[]{BuildTag.Fire},default,null,new[]{S(StatusKind.Burning,4,3,4)}),
                Item("kindling","Kindling","Burning targets yield stronger attacks.",new[]{BuildTag.Fire,CriticalTag()},new StatModifiers{damage=.08f}),
                Item("wildfire","Wildfire","Defeated status targets burst flame around them.",new[]{BuildTag.Fire,BuildTag.Poison},default,new[]{E(TriggerKind.StatusDeathBurst,18)}),
                Item("flashpoint","Flashpoint","Four Burn stacks erupt for 34 Fire damage.",new[]{BuildTag.Fire},default,new[]{E(TriggerKind.StatusThresholdBurst,34,4)},null,"ember-brand"),
                Item("ashen-wake","Ashen Wake","Dash leaves a short damaging Fire wake.",new[]{BuildTag.Fire,BuildTag.DashPrecision},default,new[]{E(TriggerKind.DashZone,16)}),
                Item("inferno-core","Inferno Core","Repeated Burn reaches an explosive capstone.",new[]{BuildTag.Fire,BuildTag.Heavy},new StatModifiers{damage=.12f},new[]{E(TriggerKind.StatusThresholdBurst,55,5)},null,"flashpoint"),

                Item("rime-edge","Rime Edge","Hits apply Chill; maximum Chill freezes normal enemies.",new[]{BuildTag.Frost},default,null,new[]{S(StatusKind.Chill,4,.08f,5)}),
                Item("deep-winter","Deep Winter","Chill slows more strongly and lasts longer.",new[]{BuildTag.Frost},default,null,new[]{S(StatusKind.Chill,5,.12f,5)},"rime-edge"),
                Item("cold-snap","Cold Snap","Reach maximum Chill faster.",new[]{BuildTag.Frost,BuildTag.Combo},new StatModifiers{attackSpeed=.07f},null,new[]{S(StatusKind.Chill,4,.1f,4)},"rime-edge"),
                Item("shatter","Shatter","Heavy and critical attacks punish frozen or frost-vulnerable targets.",new[]{BuildTag.Frost,BuildTag.Heavy,CriticalTag()},new StatModifiers{criticalMultiplier=.18f}),
                Item("frozen-step","Frozen Step","Dash releases a Frost shock around you.",new[]{BuildTag.Frost,BuildTag.DashPrecision},default,new[]{E(TriggerKind.DashZone,14)}),
                Item("brittle-ice","Brittle Ice","Gain critical chance against controlled prey.",new[]{BuildTag.Frost,BuildTag.Critical},new StatModifiers{criticalChance=.08f}),

                Item("static-charge","Static Charge","Critical hits add extra Lightning charge.",new[]{BuildTag.Lightning,BuildTag.Critical},default,new[]{E(TriggerKind.LightningConductor,2)},null,"storm-coil"),
                Item("overload","Overload","Lightning charge threshold is reduced.",new[]{BuildTag.Lightning,BuildTag.Combo},default,new[]{E(TriggerKind.ChainLightning,12,3)},null,"storm-coil"),
                Item("stormstep","Stormstep","Dash discharges a close Lightning pulse.",new[]{BuildTag.Lightning,BuildTag.DashPrecision},default,new[]{E(TriggerKind.DashZone,17)}),
                Item("thunderhead","Thunderhead","A rare storm capstone: larger proc damage.",new[]{BuildTag.Lightning,BuildTag.Heavy},new StatModifiers{damage=.14f},new[]{E(TriggerKind.ChainLightning,25,4)},null,"storm-coil"),

                Item("venom-edge","Venom Edge","Weapon hits apply stacking Poison.",new[]{BuildTag.Poison},default,null,new[]{S(StatusKind.Poison,6,2.5f,6)}),
                Item("toxicity","Toxicity","Apply an additional long Poison profile.",new[]{BuildTag.Poison,BuildTag.Combo},default,null,new[]{S(StatusKind.Poison,8,2,8)},"venom-edge"),
                Item("corrosion","Corrosion","Hits corrode targets, reducing their outgoing damage.",new[]{BuildTag.Poison,BuildTag.Utility},default,null,new[]{S(StatusKind.Corrosion,4,.1f,1)}),
                Item("catalyst","Catalyst","Critical hits immediately tick Bleed and Poison.",new[]{BuildTag.Poison,BuildTag.Critical},default,new[]{E(TriggerKind.CriticalStatusTick,1)}),
                Item("toxic-burst","Toxic Burst","Defeated enemies release a damaging poison cloud.",new[]{BuildTag.Poison,BuildTag.Fire},default,new[]{E(TriggerKind.StatusDeathBurst,24)},null,"venom-edge"),

                Item("void-mark","Void Mark","Hits apply stacking Void Marks.",new[]{BuildTag.Void},default,null,new[]{S(StatusKind.VoidMark,6,0,5)}),
                Item("collapse","Collapse","Five Void Marks collapse for a burst.",new[]{BuildTag.Void,BuildTag.Heavy},default,new[]{E(TriggerKind.StatusThresholdBurst,42,5)},null,"void-mark"),
                Item("rift-step","Rift Step","Dash tears open a damaging Void rift.",new[]{BuildTag.Void,BuildTag.DashPrecision},default,new[]{E(TriggerKind.DashZone,20)}),
                Item("entropy","Entropy","Trade a little health for attack speed and damage.",new[]{BuildTag.Void,BuildTag.Combo},new StatModifiers{attackSpeed=.18f,damage=.1f,maxHealth=-.08f}),
                Item("echo-beyond","Echo Beyond","Weapon hits repeat at reduced power after a delay.",new[]{BuildTag.Void,CriticalTag()},default,new[]{E(TriggerKind.DelayedEcho,.22f)}),
                Item("abyssal-pact","Abyssal Pact","Below 35% health, deal substantially more damage.",new[]{BuildTag.Void,BuildTag.Heavy},default,new[]{E(TriggerKind.LowHealthDamage,.32f)},null,"void-mark"),

                Item("patient-force","Patient Force","Increase damage and prepare deliberate heavy strikes.",new[]{BuildTag.Heavy},new StatModifiers{damage=.12f,attackSpeed=-.06f}),
                Item("fault-line","Fault Line","Heavy impacts release a physical shockwave.",new[]{BuildTag.Heavy},default,new[]{E(TriggerKind.HeavyShockwave,24)}),
                Item("bell-ringer","Bell Ringer","Heavy hits reduce active cooldown.",new[]{BuildTag.Heavy,BuildTag.Utility},default,new[]{E(TriggerKind.HeavyCooldown,.8f)}),
                Item("open-wound","Open Wound","Heavy builds apply stronger Bleed.",new[]{BuildTag.Heavy,BuildTag.Bleed},default,null,new[]{S(StatusKind.Bleed,5,4,6)}),

                Item("rising-tempo","Rising Tempo","Continuous hits build Momentum and attack speed.",new[]{BuildTag.Combo},new StatModifiers{attackSpeed=.12f},new[]{E(TriggerKind.Momentum,1,6)}),
                Item("crescendo","Crescendo","High combo accelerates Lightning charge.",new[]{BuildTag.Combo,BuildTag.Lightning},default,new[]{E(TriggerKind.Momentum,1,5)}),
                Item("flowing-step","Flowing Step","Momentum increases movement and proc access.",new[]{BuildTag.Combo,BuildTag.DashPrecision},new StatModifiers{movementSpeed=.1f,attackSpeed=.06f}),

                Item("afterimage-edge","Afterimage Edge","First hit shortly after dash repeats for bonus damage.",new[]{BuildTag.DashPrecision},default,new[]{E(TriggerKind.PostDashDamage,.35f)}),
                Item("keen-step","Keen Step","Gain critical chance and speed for forgiving dash follow-ups.",new[]{BuildTag.DashPrecision,BuildTag.Critical},new StatModifiers{criticalChance=.1f,movementSpeed=.06f}),
                Item("warded-heel","Warded Heel","Mobility and a larger health reserve for risky builds.",new[]{BuildTag.DashPrecision,BuildTag.Shield,BuildTag.Utility},new StatModifiers{movementSpeed=.08f,maxHealth=.12f})
            };
        }
        private static BuildTag CriticalTag() => BuildTag.Critical;
        private static ItemDefinition Item(string id, string name, string description, BuildTag[] tags, StatModifiers modifiers,
            TriggeredEffect[] effects = null, StatusPayload[] statuses = null, string requirement = "") =>
            Asset<ItemDefinition>("Assets/ScriptableObjects/Items/" + id + ".asset", x =>
            {
                x.id = id; x.displayName = name; x.description = description; x.tags = tags; x.statModifiers = modifiers;
                x.triggeredEffects = effects ?? Array.Empty<TriggeredEffect>(); x.statusEffects = statuses ?? Array.Empty<StatusPayload>();
                x.requiredItemId = requirement; x.rarity = !string.IsNullOrEmpty(requirement) || id == "rupture" || id == "forked-heart" ? Rarity.Rare : Rarity.Uncommon;
            });
        private static T Asset<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            initialize(asset); EditorUtility.SetDirty(asset); return asset;
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
