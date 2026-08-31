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
                "Assets/ScriptableObjects/Corruption", "Assets/ScriptableObjects/Rooms", "Assets/ScriptableObjects/Lore", "Assets/ScriptableObjects/Weapons",
                "Assets/ScriptableObjects/WeaponSkills", "Assets/ScriptableObjects/Armor", "Assets/ScriptableObjects/ArmorSets", "Assets/ScriptableObjects/Meta", "Assets/Scenes" };
            foreach (string folder in folders) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var items = CreateItems(); var weaponSkills=CreateWeaponSkills();
            var weapons = CreateWeapons(weaponSkills); var weapon = weapons[0];
            var armorSets=CreateArmorSets(); var armor=CreateArmor(armorSets);
            var facilities=CreateFacilities();var preparations=CreatePreparations();var progressionTuning=CreateProgressionTuning();
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
            { x.items = items; x.weapon = weapon; x.weapons = weapons; x.weaponSkills=weaponSkills; x.armorSets=armorSets; x.armor=armor;x.facilities=facilities;x.preparations=preparations;x.progressionTuning=progressionTuning; x.boss = boss; x.rooms = new[] { room1, room2, eliteRoom, room4, miniRoom, room6, bossRoom }; });
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
            Debug.Log("Ashbound v0.4 content ready: v0.3 combat plus 6 Hub facilities, 28 upgrades, 5 preparations, resources, and equipment rewards.");
        }

        private static HubFacilityDefinition[] CreateFacilities()
        {
            ResourceWallet C(int ash=0,int ember=0,int alloy=0,int corruption=0)=>new ResourceWallet{ash=ash,emberShards=ember,ancientAlloy=alloy,corruptionFragments=corruption};
            FacilityUpgradeTier T(string id,string name,string description,ResourceWallet cost,MetaEffectKind effect,float power=0,string prerequisite="",int prerequisiteLevel=0,string[] weapons=null,string[] skills=null,string[] relics=null,string[] sets=null)=>new FacilityUpgradeTier{id=id,displayName=name,description=description,cost=cost,effect=effect,power=power,prerequisiteFacilityId=prerequisite,prerequisiteLevel=prerequisiteLevel,unlockWeaponIds=weapons??Array.Empty<string>(),unlockWeaponSkillIds=skills??Array.Empty<string>(),unlockRelicIds=relics??Array.Empty<string>(),unlockArmorSetIds=sets??Array.Empty<string>(),unlockPreparationIds=Array.Empty<string>()};
            HubFacilityDefinition F(string id,string name,HubFacilityKind kind,string description,params FacilityUpgradeTier[] tiers)=>Asset<HubFacilityDefinition>("Assets/ScriptableObjects/Meta/"+id+".asset",x=>{x.id=id;x.displayName=name;x.kind=kind;x.description=description;x.initiallyUnlocked=true;x.tiers=tiers;});
            return new[]{
                F("expedition-table","Expedition Table",HubFacilityKind.ExpeditionTable,"Review progress, preparations, route intelligence, and launch the next expedition.",
                    T("table-survey","Survey Ledger","Record the highest region and encounter reached.",C(18),MetaEffectKind.RouteReveal,0),
                    T("table-routes","Route Annotations","Reveal one additional future node hook.",C(30,3),MetaEffectKind.RouteReveal,1),
                    T("table-boss-ledger","Boss Ledger","Expose defeated-boss records and milestone retention information.",C(45,6),MetaEffectKind.RouteReveal,0),
                    T("table-deep-chart","Deep Chart","Reveal another future node hook without selecting a route for the player.",C(65,8,1),MetaEffectKind.RouteReveal,1)),
                F("forge","Forge",HubFacilityKind.Forge,"Unlock weapons, elemental variants, skills, and modest rarity craftsmanship.",
                    T("forge-weapons","Weapon Research","Add five elemental weapons and Winterglass armor to future reward pools.",C(24,3),MetaEffectKind.None,0,weapons:new[]{"flamebreaker","moonfrost","storm-twins","venom-rain","gravity-ash"},sets:new[]{"winterglass"}),
                    T("forge-elements","Elemental Forging","Add five alternate combinations and Stormcaller armor.",C(38,7),MetaEffectKind.None,0,weapons:new[]{"cinder-crook","winter-pike","thunder-string","decay-brand","rift-edge"},sets:new[]{"stormcaller"}),
                    T("forge-skills","Weapon Skill Research","Allow researched weapons to appear with skills; add Venomblood and Riftwalker armor.",C(52,10,1),MetaEffectKind.None,0,skills:new[]{"flamebreaker","moonfrost-draw","storm-rush","venom-rain","gravity-well","cinder-volley","frost-lance","storm-volley","toxic-wave","rift-cleave"},sets:new[]{"venomblood","riftwalker"}),
                    T("forge-craftsmanship","Rarity Craftsmanship","Increase high-rarity reward weighting by a capped 5%.",C(70,14,2),MetaEffectKind.RareWeight,.05f),
                    T("forge-legendary","Legendary Research","Moon-Eater and Hearth-Sunder can enter eligible pools; they are not starting gear.",C(90,20,5,2),MetaEffectKind.None,0,"expedition-table",2,new[]{"moon-eater","hearth-sunder"},new[]{"moon-eater-cut","hearth-collapse"})),
                F("quartermaster","Quartermaster",HubFacilityKind.Quartermaster,"Improve merchant choice and expedition economy without large combat stats.",
                    T("quartermaster-stock","Merchant Stock I","Future merchants may offer one additional option.",C(20),MetaEffectKind.MerchantStock,1),
                    T("quartermaster-network","Merchant Network","Slightly improve future merchant-node weighting.",C(32,3),MetaEffectKind.MerchantChance,.05f),
                    T("quartermaster-negotiation","Negotiation","The first future merchant reroll receives a discount hook.",C(42,6),MetaEffectKind.RerollDiscount,.25f),
                    T("quartermaster-salvage","Salvage Training","Dismantling yields 10% more expedition materials.",C(55,8,1),MetaEffectKind.SalvageYield,.1f),
                    T("quartermaster-cache","Supply Cache","Begin each expedition with 8 run Ash.",C(68,10,2),MetaEffectKind.StartingAsh,8)),
                F("infirmary","Infirmary",HubFacilityKind.Infirmary,"Small capped survival and recovery improvements.",
                    T("infirmary-medicine","Field Medicine","Checkpoint and future Rest recovery improve by 10%.",C(20),MetaEffectKind.RestRecovery,.1f),
                    T("infirmary-recovery","Recovery Training","Failure retention improves by 3%, within the global cap.",C(32,3),MetaEffectKind.FailureRetention,.03f),
                    T("infirmary-emergency","Emergency Supply","Expose one extra future Rest recovery option.",C(42,5),MetaEffectKind.EmergencyRest,1),
                    T("infirmary-vitality-1","Vitality Training I","Increase base maximum HP by 2%.",C(50,7),MetaEffectKind.MaxHealth,.02f),
                    T("infirmary-vitality-2","Vitality Training II","Increase base maximum HP by another 2%.",C(65,9,1),MetaEffectKind.MaxHealth,.02f),
                    T("infirmary-vitality-3","Vitality Training III","Reach the prototype permanent HP cap of 6%.",C(80,12,2),MetaEffectKind.MaxHealth,.02f)),
                F("archive","Archive",HubFacilityKind.Archive,"Optional fragmentary lore, boss observations, and Legendary records.",
                    T("archive-shelves","Recovered Shelves","Display discovered expedition notes.",C(12),MetaEffectKind.ArchiveCapacity,4),
                    T("archive-insignia","Insignia Index","Expand optional region-record capacity.",C(22,2),MetaEffectKind.ArchiveCapacity,4),
                    T("archive-observations","Boss Observations","Display defeated-boss records without explaining the cycle.",C(34,4),MetaEffectKind.ArchiveCapacity,4),
                    T("archive-legends","Legendary Folio","Display lore for discovered Legendary research.",C(46,6,1),MetaEffectKind.ArchiveCapacity,4)),
                F("research-station","Research Station",HubFacilityKind.ResearchStation,"Manipulate possibilities, information, and randomness.",
                    T("research-scavenging","Scavenging","Increase Rare-or-higher weighting by 5% and add foundational elemental relics.",C(24,3),MetaEffectKind.RareWeight,.05f,relics:new[]{"ember-brand","kindling","wildfire","rime-edge","deep-winter","cold-snap","static-charge","overload","venom-edge","toxicity","void-mark","rift-step"}),
                    T("research-cartography","Cartography","Reveal one additional future node hook.",C(36,5),MetaEffectKind.RouteReveal,1),
                    T("research-relics","Relic Analysis","Gain one relic reroll and add advanced synergy relics.",C(48,8,1),MetaEffectKind.RelicReroll,1,relics:new[]{"flashpoint","ashen-wake","inferno-core","shatter","frozen-step","brittle-ice","stormstep","thunderhead","corrosion","catalyst","toxic-burst","collapse","entropy","echo-beyond","abyssal-pact","patient-force","fault-line","bell-ringer","open-wound","rising-tempo","crescendo","flowing-step","afterimage-edge","keen-step","warded-heel"}),
                    T("research-appraisal","Equipment Appraisal","Expose richer equipment comparison information.",C(58,10,1),MetaEffectKind.EquipmentAppraisal,1),
                    T("research-affinity","Elemental Affinity","Enable capped elemental-bias preparations.",C(72,14,2),MetaEffectKind.ElementBias,0))
            };
        }

        private static PreparationDefinition[] CreatePreparations()
        {
            PreparationDefinition P(string id,string name,string description,PreparationKind kind,string facility,int level,MetaEffectKind effect,float power,ElementTag element=ElementTag.None)=>Asset<PreparationDefinition>("Assets/ScriptableObjects/Meta/preparation-"+id+".asset",x=>{x.id=id;x.displayName=name;x.description=description;x.kind=kind;x.requiredFacilityId=facility;x.requiredFacilityLevel=level;x.effect=effect;x.power=power;x.element=element;});
            return new[]{
                P("hunters-preparation","Hunter's Preparation","Slightly improve Rare reward weighting for the next expedition.",PreparationKind.HuntersPreparation,"forge",1,MetaEffectKind.RareWeight,.05f),
                P("frost-research","Frost Research","Bias eligible equipment toward Frost by 15%; never guarantees it.",PreparationKind.FrostResearch,"research-station",5,MetaEffectKind.ElementBias,.15f,ElementTag.Frost),
                P("cartographers-notes","Cartographer's Notes","Reveal one extra future route node.",PreparationKind.CartographersNotes,"expedition-table",2,MetaEffectKind.RouteReveal,1),
                P("merchant-contract","Merchant Contract","Reserve a free-first-reroll hook for future merchants.",PreparationKind.MerchantContract,"quartermaster",3,MetaEffectKind.RerollDiscount,1),
                P("field-supplies","Field Supplies","Checkpoint and future Rest healing improve by 15% this expedition.",PreparationKind.FieldSupplies,"infirmary",1,MetaEffectKind.RestRecovery,.15f)
            };
        }

        private static ProgressionTuningDefinition CreateProgressionTuning()=>Asset<ProgressionTuningDefinition>("Assets/ScriptableObjects/Meta/progression-tuning.asset",x=>
        {
            x.retention=new RetentionRules{ashFailure=.7f,emberFailure=.5f,alloyFailure=.25f,corruptionFailure=0,bossMilestoneBonus=.15f,maxFailureBonus=.15f};x.permanentHealthCap=.08f;x.rarityWeightCap=.15f;x.elementalBiasCap=.2f;
            x.targetMajorRegions=5;x.targetFinalAreas=1;x.targetNodesPerRegionMin=8;x.targetNodesPerRegionMax=10;x.targetExperiencedRunMinutesMin=30;x.targetExperiencedRunMinutesMax=45;
            x.rewards=new[]{new EncounterResourceReward{nodeType=ExpeditionNodeType.NormalCombat,resources=new ResourceWallet{ash=10},minimumQuality=RewardQuality.Common,maximumQuality=RewardQuality.Rare},new EncounterResourceReward{nodeType=ExpeditionNodeType.Elite,resources=new ResourceWallet{ash=14,emberShards=3},minimumQuality=RewardQuality.Advanced,maximumQuality=RewardQuality.Epic},new EncounterResourceReward{nodeType=ExpeditionNodeType.Boss,resources=new ResourceWallet{ash=28,emberShards=7,ancientAlloy=2,corruptionFragments=1},minimumQuality=RewardQuality.Rare,maximumQuality=RewardQuality.Legendary}};
            x.restOptions=new[]{new RestOptionDefinition{kind=RestOptionKind.Rest,displayName="Rest",description="Recover health.",power=.3f},new RestOptionDefinition{kind=RestOptionKind.Temper,displayName="Temper",description="Skip healing and reserve a small equipment-improvement hook.",power=.05f}};
        });

        private static RoomDefinition Room(string assetName, string id, string name, string description, Vector3[] points, LoreEntry lore, params EnemyKind[] enemies) =>
            Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/" + assetName + ".asset", x =>
            { x.id = id; x.displayName = name; x.description = description; x.isBoss = false; x.spawnPoints = points; x.fragment = lore; x.waves = new[] { new EnemyWave { enemies = enemies } };
              x.enemyHealthMultiplier = id == "cracked-sanctum" ? 1.35f : id == "last-procession" ? 1.2f : 1; x.targetEncounterSeconds = id == "cracked-sanctum" ? 180 : 145; });

        private static WeaponSkillDefinition[] CreateWeaponSkills()
        {
            WeaponSkillDefinition S(string id,string name,string description,ElementTag element,SkillDelivery delivery,float damage,float radius,float cooldown,StatusPayload status,params BuildTag[] tags)=>
                Asset<WeaponSkillDefinition>("Assets/ScriptableObjects/WeaponSkills/"+id+".asset",x=>{x.id=id;x.displayName=name;x.description=description;x.elements=new[]{element};x.tags=tags;x.delivery=delivery;x.damage=damage;x.radius=radius;x.cooldown=cooldown;x.duration=2.5f;x.movementDistance=4;x.projectileCount=5;x.projectileSpeed=17;x.minimumRarity=WeaponRarity.Rare;x.statuses=status.maxStacks>0?new[]{status}:Array.Empty<StatusPayload>();x.damageKind=element==ElementTag.Fire?DamageKind.Burning:element==ElementTag.Frost?DamageKind.Frost:element==ElementTag.Lightning?DamageKind.Lightning:element==ElementTag.Poison?DamageKind.Poison:DamageKind.Void;});
            StatusPayload P(StatusKind kind,float duration,float power,int stacks)=>new StatusPayload{kind=kind,duration=duration,power=power,maxStacks=stacks};
            return new[]{
                S("flamebreaker","Flamebreaker","An overhead impact releases a burning shockwave.",ElementTag.Fire,SkillDelivery.AreaBurst,48,3.8f,8,P(StatusKind.Burning,5,3,4),BuildTag.Fire,BuildTag.Heavy,BuildTag.Area),
                S("moonfrost-draw","Moonfrost Draw","Dash-slash through chilled prey and leave them brittle.",ElementTag.Frost,SkillDelivery.MeleeDash,38,2.2f,6,P(StatusKind.Chill,5,.13f,5),BuildTag.Frost,BuildTag.Critical,BuildTag.DashPrecision),
                S("storm-rush","Storm Rush","A rapid lightning volley builds charge across a group.",ElementTag.Lightning,SkillDelivery.ProjectileVolley,12,2,7,default,BuildTag.Lightning,BuildTag.Combo,BuildTag.Area),
                S("venom-rain","Venom Rain","Create a toxic cloud that applies long Poison pressure.",ElementTag.Poison,SkillDelivery.PersistentZone,32,3.5f,9,P(StatusKind.Poison,8,2.5f,8),BuildTag.Poison,BuildTag.DamageOverTime,BuildTag.Area),
                S("gravity-well","Gravity Well","Create a Void field that pulls normal enemies inward.",ElementTag.Void,SkillDelivery.GravityWell,30,4,10,P(StatusKind.VoidMark,6,0,5),BuildTag.Void,BuildTag.Control,BuildTag.Area),
                S("cinder-volley","Cinder Volley","Fan burning spell projectiles through a group.",ElementTag.Fire,SkillDelivery.ProjectileVolley,15,2,7,P(StatusKind.Burning,4,2.5f,4),BuildTag.Fire,BuildTag.Area),
                S("frost-lance","Frost Lance","A precise lunge sharply builds Chill on one target.",ElementTag.Frost,SkillDelivery.MeleeDash,44,1.8f,7,P(StatusKind.Chill,6,.14f,5),BuildTag.Frost,BuildTag.Control,BuildTag.Critical),
                S("storm-volley","Storm Volley","Fire a wide reactive volley of lightning arrows.",ElementTag.Lightning,SkillDelivery.ProjectileVolley,14,2,7,default,BuildTag.Lightning,BuildTag.Critical,BuildTag.Area),
                S("toxic-wave","Toxic Wave","Release a close Poison wave that sustains attrition.",ElementTag.Poison,SkillDelivery.AreaBurst,34,3,8,P(StatusKind.Poison,8,2.2f,8),BuildTag.Poison,BuildTag.Sustain,BuildTag.Area),
                S("rift-cleave","Rift Cleave","Cut open a short control rift ahead.",ElementTag.Void,SkillDelivery.PersistentZone,28,3,8,P(StatusKind.VoidMark,6,0,5),BuildTag.Void,BuildTag.Control),
                S("moon-eater-cut","The Empty Meridian","Dash through marked enemies and collapse space behind you.",ElementTag.Void,SkillDelivery.MeleeDash,58,3,7,P(StatusKind.VoidMark,8,0,5),BuildTag.Void,BuildTag.Critical,BuildTag.DashPrecision),
                S("hearth-collapse","Keeper's Last Ember","Sunder the floor with a major burning blast.",ElementTag.Fire,SkillDelivery.AreaBurst,68,4.5f,9,P(StatusKind.Burning,6,4,5),BuildTag.Fire,BuildTag.Heavy,BuildTag.Area)
            };
        }

        private static ArmorSetDefinition[] CreateArmorSets()
        {
            TriggeredEffect E(TriggerKind kind,float power)=>new TriggeredEffect{kind=kind,power=power};
            ArmorSetDefinition S(string id,string name,ElementTag element,string two,string four,TriggeredEffect twoEffect,TriggeredEffect fourEffect,params BuildTag[] tags)=>
                Asset<ArmorSetDefinition>("Assets/ScriptableObjects/ArmorSets/"+id+".asset",x=>{x.id=id;x.displayName=name;x.element=element;x.twoPiece=new SetBonusTier{pieces=2,description=two,effects=new[]{twoEffect},tags=tags};x.fourPiece=new SetBonusTier{pieces=4,description=four,effects=new[]{fourEffect},tags=tags};});
            return new[]{
                S("ashwalker","Ashwalker",ElementTag.Fire,"Dashing leaves a small Fire wake.","Overlapping wakes gain explosive force.",E(TriggerKind.SetDashZone,14),E(TriggerKind.SetDashZone,28),BuildTag.Fire,BuildTag.DashPrecision,BuildTag.Area),
                S("winterglass","Winterglass",ElementTag.Frost,"Chill control gains a longer precision window.","Heavy or critical Frost hits release a Shatter wave.",E(TriggerKind.StatusVulnerability,.12f),E(TriggerKind.SetShatterWave,26),BuildTag.Frost,BuildTag.Critical,BuildTag.Control),
                S("stormcaller","Stormcaller",ElementTag.Lightning,"Critical pressure improves charge generation.","Overload sends a smaller additional chain.",E(TriggerKind.LightningConductor,1),E(TriggerKind.SetOverloadChain,12),BuildTag.Lightning,BuildTag.Critical,BuildTag.Area),
                S("venomblood","Venomblood",ElementTag.Poison,"Poison kills restore a throttled amount of health.","Toxic death clouds gain damage and infection pressure.",E(TriggerKind.PoisonRecovery,5),E(TriggerKind.SetCloudPoison,18),BuildTag.Poison,BuildTag.Sustain,BuildTag.DamageOverTime),
                S("riftwalker","Riftwalker",ElementTag.Void,"Dash-created rifts gain force.","Rift effects briefly pull normal enemies.",E(TriggerKind.SetRiftPull,12),E(TriggerKind.SetRiftPull,24),BuildTag.Void,BuildTag.Control,BuildTag.DashPrecision)
            };
        }

        private static ArmorDefinition[] CreateArmor(ArmorSetDefinition[] sets)
        {
            var list=new System.Collections.Generic.List<ArmorDefinition>();
            string[] nouns={"Crown","Mantle","Grasp","Treads"};
            foreach(var set in sets)for(int i=0;i<4;i++)
            {
                ArmorSlot slot=(ArmorSlot)i;string id=set.id+"-"+slot.ToString().ToLowerInvariant();
                list.Add(Asset<ArmorDefinition>("Assets/ScriptableObjects/Armor/"+id+".asset",x=>{x.id=id;x.displayName=set.displayName+" "+nouns[i];x.description="A "+slot+" piece that carries the "+set.element+" set mechanic.";x.rarity=i>=2?WeaponRarity.Rare:WeaponRarity.Advanced;x.slot=slot;x.set=set;x.elements=new[]{set.element};x.tags=set.twoPiece.tags;x.statModifiers=i==3?new StatModifiers{movementSpeed=.04f}:i==1?new StatModifiers{maxHealth=.06f}:default;x.passive=new ArmorPassive{kind=set.element==ElementTag.Poison?ArmorPassiveKind.Recovery:set.element==ElementTag.Frost?ArmorPassiveKind.ControlDuration:set.element==ElementTag.Lightning?ArmorPassiveKind.ProcCharge:set.element==ElementTag.Fire?ArmorPassiveKind.ElementAmplify:ArmorPassiveKind.Cooldown,power=.08f,element=set.element};}));
            }
            return list.ToArray();
        }

        private static WeaponDefinition[] CreateWeapons(WeaponSkillDefinition[] skills)
        {
            WeaponDefinition W(string id, string name, WeaponFamily family, WeaponMechanic mechanic, float damage, float rate, float reach, float arc, float knockback,
                float move, float crit, float critDamage, float projectile, int threshold, float power, params BuildTag[] tags) =>
                Asset<WeaponDefinition>(id == "wayfarer-edge" ? "Assets/ScriptableObjects/Weapons/WayfarersEdge.asset" : "Assets/ScriptableObjects/Weapons/" + id + ".asset", x =>
                { x.id=id; x.displayName=name; x.family=family; x.mechanic=mechanic; x.damage=damage; x.attackInterval=rate; x.reach=reach; x.arcDegrees=arc; x.knockback=knockback;
                  x.attackMoveMultiplier=move; x.criticalChanceModifier=crit; x.criticalDamageModifier=critDamage; x.projectileSpeed=projectile; x.projectileLifetime=1.5f; x.comboThreshold=threshold; x.mechanicPower=power; x.tags=tags;
                  x.rarity=WeaponRarity.Common;x.elements=Array.Empty<ElementTag>();x.skill=null;x.basicAttackDescription="";x.passiveDescription="";x.lore="";x.onHitStatuses=Array.Empty<StatusPayload>(); });
            var result = new[]
            {
                W("wayfarer-edge","Wayfarer's Edge",WeaponFamily.Sword,WeaponMechanic.None,24,.34f,2.7f,115,3,.9f,0,0,0,4,.15f),
                W("long-reach","Long Reach",WeaponFamily.Spear,WeaponMechanic.FocusedThrust,26,.43f,4.2f,42,4,.82f,0,0,0,3,.08f,BuildTag.Heavy),
                W("bell-cleaver","Bell Cleaver",WeaponFamily.Greatsword,WeaponMechanic.HeavyCommitment,48,.78f,3.25f,145,8,.55f,-.02f,.18f,0,3,.2f,BuildTag.Heavy),
                W("moon-shear","Moon Shear",WeaponFamily.Katana,WeaponMechanic.DashPrecision,27,.3f,2.9f,88,3.5f,.92f,.04f,.1f,0,3,.28f,BuildTag.Critical,BuildTag.DashPrecision),
                W("twin-embers","Twin Embers",WeaponFamily.DualBlades,WeaponMechanic.Momentum,13,.16f,1.85f,92,1.8f,1.05f,0,0,0,6,.03f,BuildTag.Combo),
                W("vault-bow","Vault Bow",WeaponFamily.Bow,WeaponMechanic.ChargedShot,23,.4f,0,0,2,.78f,.02f,.1f,22,3,.85f,BuildTag.DashPrecision),
                W("ashen-staff","Ashen Staff",WeaponFamily.Staff,WeaponMechanic.ArcaneCharge,19,.32f,0,0,2,.88f,0,0,16,4,.6f,BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void),
                W("rift-brand","Rift Brand",WeaponFamily.Spellblade,WeaponMechanic.SpellWave,22,.29f,2.45f,105,2.5f,.9f,0,0,0,4,.7f,BuildTag.Void),
                EW("flamebreaker","Flamebreaker",WeaponFamily.Greatsword,WeaponMechanic.HeavyCommitment,44,.72f,3.3f,145,8,WeaponRarity.Rare,ElementTag.Fire,"flamebreaker",StatusKind.Burning,4,3,4,BuildTag.Fire,BuildTag.Heavy,BuildTag.Area),
                EW("moonfrost","Moonfrost",WeaponFamily.Katana,WeaponMechanic.DashPrecision,26,.29f,2.9f,88,3.5f,WeaponRarity.Epic,ElementTag.Frost,"moonfrost-draw",StatusKind.Chill,4,.1f,5,BuildTag.Frost,BuildTag.Critical,BuildTag.DashPrecision),
                EW("storm-twins","Storm Twins",WeaponFamily.DualBlades,WeaponMechanic.Momentum,13,.16f,1.9f,92,2,WeaponRarity.Rare,ElementTag.Lightning,"storm-rush",StatusKind.Stun,.12f,0,1,BuildTag.Lightning,BuildTag.Combo,BuildTag.Area),
                EW("venom-rain","Venom Rain",WeaponFamily.Bow,WeaponMechanic.ChargedShot,21,.39f,0,0,2,WeaponRarity.Epic,ElementTag.Poison,"venom-rain",StatusKind.Poison,7,2.4f,7,BuildTag.Poison,BuildTag.DamageOverTime,BuildTag.Sustain),
                EW("gravity-ash","Gravity Ash",WeaponFamily.Staff,WeaponMechanic.ArcaneCharge,18,.34f,0,0,2,WeaponRarity.Epic,ElementTag.Void,"gravity-well",StatusKind.VoidMark,5,0,5,BuildTag.Void,BuildTag.Control,BuildTag.Area),
                EW("cinder-crook","Cinder Crook",WeaponFamily.Staff,WeaponMechanic.ArcaneCharge,19,.31f,0,0,2,WeaponRarity.Rare,ElementTag.Fire,"cinder-volley",StatusKind.Burning,4,2.5f,4,BuildTag.Fire,BuildTag.Area),
                EW("winter-pike","Winter Pike",WeaponFamily.Spear,WeaponMechanic.FocusedThrust,25,.42f,4.2f,42,4,WeaponRarity.Rare,ElementTag.Frost,"frost-lance",StatusKind.Chill,5,.09f,5,BuildTag.Frost,BuildTag.Control,BuildTag.Heavy),
                EW("thunder-string","Thunder String",WeaponFamily.Bow,WeaponMechanic.ChargedShot,22,.38f,0,0,2,WeaponRarity.Rare,ElementTag.Lightning,"storm-volley",StatusKind.Stun,.1f,0,1,BuildTag.Lightning,BuildTag.Critical,BuildTag.Area),
                EW("decay-brand","Decay Brand",WeaponFamily.Spellblade,WeaponMechanic.SpellWave,21,.3f,2.5f,105,2.5f,WeaponRarity.Rare,ElementTag.Poison,"toxic-wave",StatusKind.Poison,7,2.2f,7,BuildTag.Poison,BuildTag.Sustain,BuildTag.Combo),
                EW("rift-edge","Rift Edge",WeaponFamily.Sword,WeaponMechanic.None,23,.34f,2.7f,115,3,WeaponRarity.Rare,ElementTag.Void,"rift-cleave",StatusKind.VoidMark,6,0,5,BuildTag.Void,BuildTag.Control,BuildTag.DashPrecision),
                EW("moon-eater","Moon-Eater",WeaponFamily.Katana,WeaponMechanic.DashPrecision,29,.27f,3f,92,4,WeaponRarity.Legendary,ElementTag.Void,"moon-eater-cut",StatusKind.VoidMark,7,0,5,BuildTag.Void,BuildTag.Critical,BuildTag.DashPrecision),
                EW("hearth-sunder","Hearth-Sunder",WeaponFamily.Greatsword,WeaponMechanic.HeavyCommitment,51,.76f,3.4f,150,9,WeaponRarity.Legendary,ElementTag.Fire,"hearth-collapse",StatusKind.Burning,5,3.5f,5,BuildTag.Fire,BuildTag.Heavy,BuildTag.Area)
            };
            result[1].rarity=WeaponRarity.Advanced;result[1].passiveDescription="Focused thrusts reward exact spacing.";EditorUtility.SetDirty(result[1]);
            result[3].rarity=WeaponRarity.Advanced;result[3].passiveDescription="Dash timing improves precision pressure.";EditorUtility.SetDirty(result[3]);
            return result;
            WeaponDefinition EW(string id,string name,WeaponFamily family,WeaponMechanic mechanic,float damage,float rate,float reach,float arc,float knockback,WeaponRarity rarity,ElementTag element,string skillId,StatusKind status,float duration,float statusPower,int stacks,params BuildTag[] tags)
            {
                var value=W(id,name,family,mechanic,damage,rate,reach,arc,knockback,.82f,0,0,family==WeaponFamily.Bow?22:family==WeaponFamily.Staff?16:0,4,.25f,tags);
                value.rarity=rarity;value.elements=new[]{element};value.skill=skills.First(x=>x.id==skillId);value.onHitStatuses=new[]{new StatusPayload{kind=status,duration=duration,power=statusPower,maxStacks=stacks}};
                value.basicAttackDescription=family+" attacks carrying "+element+" identity.";value.passiveDescription=rarity>=WeaponRarity.Epic?"Its element alters both basic pressure and its weapon skill.":"A focused elemental variant.";
                value.lore=rarity==WeaponRarity.Legendary?"A named weapon recovered from a keeper whose title was carved away.":"";EditorUtility.SetDirty(value);return value;
            }
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
