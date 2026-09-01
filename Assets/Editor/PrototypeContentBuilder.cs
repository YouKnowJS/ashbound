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
            folders = folders.Concat(new[] { "Assets/ScriptableObjects/Enemies", "Assets/ScriptableObjects/Encounters", "Assets/ScriptableObjects/CombatSpaces", "Assets/ScriptableObjects/RegionEcologies",
                "Assets/ScriptableObjects/Routes", "Assets/ScriptableObjects/Routes/Nodes", "Assets/ScriptableObjects/Treasures", "Assets/ScriptableObjects/Merchants", "Assets/ScriptableObjects/Rests", "Assets/ScriptableObjects/Events", "Assets/ScriptableObjects/Challenges", "Assets/ScriptableObjects/BossRewards" }).ToArray();
            foreach (string folder in folders) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var items = CreateItems(); var weaponSkills=CreateWeaponSkills();
            var weapons = CreateWeapons(weaponSkills); var weapon = weapons[0];
            var armorSets=CreateArmorSets(); var armor=CreateArmor(armorSets);
            var facilities=CreateFacilities();var preparations=CreatePreparations();var progressionTuning=CreateProgressionTuning();
            var enemies=CreateEnemies();var encounters=CreateEncounters(enemies);var combatSpaces=CreateCombatSpaces();var regionEcologies=CreateRegionEcologies(enemies,encounters);
            var treasures=CreateTreasures(encounters);var merchants=CreateMerchants();var rests=CreateRests();var events=CreateEvents(encounters);var challenges=CreateChallenges(encounters);var bossRewards=CreateBossRewards();
            var corruption = Asset<BossCorruptionProfile>("Assets/ScriptableObjects/Corruption/Ash.asset", x => { });
            var boss = Asset<BossDefinition>("Assets/ScriptableObjects/Bosses/CinderRegent.asset", x => x.corruption = corruption);
            var firstLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/WatchkeepersNote.asset", x =>
            { x.id = "watchkeepers-note"; x.title = "A watchkeeper's note"; x.text = "We carried no fuel below. Still, every morning, the braziers were warm."; });
            var secondLore = Asset<LoreEntry>("Assets/ScriptableObjects/Lore/EmptyThrone.asset", x =>
            { x.id = "empty-throne"; x.title = "Inscription beneath a bell"; x.text = "The keeper is a title, not a name. The stone has been carved over many times."; });
            var points = new[] { new Vector3(-7, 0, 4), new Vector3(6, 0, 5), new Vector3(0, 0, 7), new Vector3(8, 0, -1), new Vector3(-8, 0, -1), new Vector3(3, 0, 3), new Vector3(-4, 0, 6), new Vector3(0, 0, 4) };
            var room1 = Room("Threshold", "threshold", "01 / The Threshold", "Old bells ring beneath the stone.", points, firstLore,encounters[0],combatSpaces[0]);
            var room2 = Room("BellChamber", "bell-chamber", "02 / The Bell Chamber", "Fast shapes circle the old bell.", points, secondLore,encounters[1],combatSpaces[1]);
            var eliteRoom = Room("WardenCrossing", "warden-crossing", "03 / Warden Crossing", "A shielded keeper bars the crossing.", points, null,encounters[4],combatSpaces[2]);
            var room4 = Room("AshGallery", "ash-gallery", "04 / The Ash Gallery", "The vault gathers its remaining guard.", points, null,encounters[3],combatSpaces[3]);
            var miniRoom = Room("CrackedSanctum", "cracked-sanctum", "05 / The Cracked Sanctum", "A lesser keeper tests what your build has become.", points, null,encounters[5],combatSpaces[4]);
            var room6 = Room("LastProcession", "last-procession", "06 / The Last Procession", "One final seal stands before the throne.", points, null,encounters[2],combatSpaces[3]);
            var bossRoom = Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/CinderThrone.asset", x =>
            {
                x.id = "cinder-throne"; x.displayName = "07 / The Cinder Throne"; x.description = "The last keeper waits."; x.isBoss = true; x.spawnPoints = points; x.waves = Array.Empty<EnemyWave>();x.combatSpace=combatSpaces[4];
            });
            var prototypeRegion=CreatePrototypeRegion(encounters,combatSpaces,treasures,merchants,rests,events,challenges,bossRewards,points);
            var catalog = Asset<PrototypeCatalog>("Assets/Resources/PrototypeCatalog.asset", x =>
            { x.items = items; x.weapon = weapon; x.weapons = weapons; x.weaponSkills=weaponSkills; x.armorSets=armorSets; x.armor=armor;x.facilities=facilities;x.preparations=preparations;x.progressionTuning=progressionTuning; x.boss = boss; x.rooms = new[] { room1, room2, eliteRoom, room4, miniRoom, room6, bossRoom };x.enemies=enemies;x.encounters=encounters;x.combatSpaces=combatSpaces;x.regionEcologies=regionEcologies;x.prototypeRegion=prototypeRegion;x.treasures=treasures;x.merchants=merchants;x.rests=rests;x.events=events;x.challenges=challenges;x.bossRewards=bossRewards; });
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
            Debug.Log("Ashbound v0.6 content ready: three validated branching route variants, ten node identities, node-driven rewards, local voting, and a regional-to-final-boss gate.");
        }

        private static EnemyDefinition[] CreateEnemies()
        {
            EnemyDefinition D(string id,string name,EnemyRole role,float hp,float speed,float damage,float cooldown,float distance,EnemyMovementStyle movement,EnemyAttackBehavior attack,EnemyTargetingStyle targeting,Color tint,float scale=1,bool elite=false,float resistance=.05f,float stagger=.05f,ElementTag element=ElementTag.None,SpawnPresentation spawn=SpawnPresentation.Edge,EnemyKind legacy=EnemyKind.Cinderling,bool fallback=false,float shield=0)=>
                Asset<EnemyDefinition>("Assets/ScriptableObjects/Enemies/"+id+".asset",x=>{x.id=id;x.displayName=name;x.role=role;x.maxHealth=hp;x.movementSpeed=speed;x.attackDamage=damage;x.attackCooldown=cooldown;x.preferredDistance=distance;x.movement=movement;x.attack=attack;x.targeting=targeting;x.baseTint=tint;x.visualScale=scale;x.elite=elite;x.statusResistance=resistance;x.staggerResistance=stagger;x.element=element;x.spawnPresentation=spawn;x.legacyKind=legacy;x.legacyFallback=fallback;x.shield=shield;x.rewardTier=elite?EnemyRewardTier.Elite:hp>=100?EnemyRewardTier.Dangerous:EnemyRewardTier.Standard;x.combatRead=role+" pressure using "+attack+" with "+movement+" movement.";x.telegraphLanguage=attack+" warning uses a persistent ground ring or directional line before commitment.";x.rewardHook=elite?"Eligible for elite equipment weighting.":"Standard encounter reward contribution.";});
            Color ash=new Color(.68f,.24f,.18f),gold=new Color(.75f,.55f,.3f),pale=new Color(.55f,.64f,.7f);
            var list=new System.Collections.Generic.List<EnemyDefinition>
            {
                D("base-cinder-warrior","Cinder Warrior",EnemyRole.Warrior,54,3.5f,11,1.25f,1.7f,EnemyMovementStyle.Advance,EnemyAttackBehavior.Melee,EnemyTargetingStyle.Nearest,ash,legacy:EnemyKind.Cinderling,fallback:true),
                D("base-ash-bruiser","Ash Bruiser",EnemyRole.Bruiser,125,2.55f,20,2.15f,2.5f,EnemyMovementStyle.HoldGround,EnemyAttackBehavior.WideMelee,EnemyTargetingStyle.Cluster,gold,1.25f,false,.2f,.45f,legacy:EnemyKind.Bulwark,fallback:true,shield:35),
                D("base-veil-assassin","Veil Assassin",EnemyRole.Assassin,67,4.35f,16,1.85f,4,EnemyMovementStyle.Flank,EnemyAttackBehavior.Lunge,EnemyTargetingStyle.Isolated,pale,.82f,false,.08f,.05f,spawn:SpawnPresentation.Rift,legacy:EnemyKind.Hound,fallback:true),
                D("base-lantern-ranger","Lantern Ranger",EnemyRole.Ranger,45,2.85f,10,1.15f,7,EnemyMovementStyle.Kite,EnemyAttackBehavior.Projectile,EnemyTargetingStyle.LowestHealth,gold,.95f,false,.05f,.05f,legacy:EnemyKind.Lantern,fallback:true),
                D("base-bell-mage","Bell Mage",EnemyRole.Mage,82,2.7f,15,2.1f,6,EnemyMovementStyle.Orbit,EnemyAttackBehavior.Area,EnemyTargetingStyle.Cluster,gold,1.1f,true,.2f,.18f,spawn:SpawnPresentation.Rift,legacy:EnemyKind.Elite,fallback:true,shield:25),
                D("base-vault-flyer","Vault Flyer",EnemyRole.Flyer,48,4.15f,13,2,4.5f,EnemyMovementStyle.AerialOrbit,EnemyAttackBehavior.Dive,EnemyTargetingStyle.Furthest,pale,.75f,false,.05f,.02f,spawn:SpawnPresentation.Flight),
                D("base-grave-burrower","Grave Burrower",EnemyRole.Burrower,76,3.15f,17,2.6f,3.5f,EnemyMovementStyle.Burrow,EnemyAttackBehavior.Eruption,EnemyTargetingStyle.Isolated,ash,1,false,.12f,.18f,spawn:SpawnPresentation.Burrow),
                D("base-ember-bomber","Ember Bomber",EnemyRole.Bomber,42,3.7f,22,2.7f,1.8f,EnemyMovementStyle.Pursue,EnemyAttackBehavior.Detonation,EnemyTargetingStyle.Cluster,ash,.78f,false,.02f,.02f,spawn:SpawnPresentation.DropIn),
                D("base-choir-support","Ash Choir",EnemyRole.Support,63,3,12,3.4f,7,EnemyMovementStyle.SupportLine,EnemyAttackBehavior.AllyAid,EnemyTargetingStyle.LowestHealth,gold,.9f,false,.18f,.1f,spawn:SpawnPresentation.Gate),
                D("base-chain-controller","Chain Keeper",EnemyRole.Controller,92,2.6f,12,2.8f,6,EnemyMovementStyle.ZoneKeeper,EnemyAttackBehavior.ControlField,EnemyTargetingStyle.Cluster,pale,1.1f,false,.28f,.25f,spawn:SpawnPresentation.Rift),
                D("elite-cracked-warden","The Cracked Warden",EnemyRole.Bruiser,430,2.6f,27,2.15f,2.6f,EnemyMovementStyle.HoldGround,EnemyAttackBehavior.WideMelee,EnemyTargetingStyle.Cluster,gold,1.7f,true,.45f,.65f,spawn:SpawnPresentation.Gate,legacy:EnemyKind.MiniBoss,fallback:true,shield:65)
            };
            list.Add(D("variant-fire-warrior","Kindled Warrior",EnemyRole.Warrior,66,3.55f,13,1.35f,1.8f,EnemyMovementStyle.Advance,EnemyAttackBehavior.Melee,EnemyTargetingStyle.Nearest,ash,1.02f,false,.08f,.08f,ElementTag.Fire));
            list.Add(D("variant-fire-bomber","Furnace Bomber",EnemyRole.Bomber,51,3.6f,24,2.8f,1.8f,EnemyMovementStyle.Pursue,EnemyAttackBehavior.Detonation,EnemyTargetingStyle.Cluster,ash,.82f,false,.04f,.03f,ElementTag.Fire,SpawnPresentation.DropIn));
            list.Add(D("variant-frost-ranger","Rime Ranger",EnemyRole.Ranger,53,2.8f,11,1.3f,7.5f,EnemyMovementStyle.Kite,EnemyAttackBehavior.Projectile,EnemyTargetingStyle.LowestHealth,pale,.95f,false,.22f,.08f,ElementTag.Frost));
            list.Add(D("variant-frost-controller","Winter Chain Keeper",EnemyRole.Controller,105,2.5f,13,3,6,EnemyMovementStyle.ZoneKeeper,EnemyAttackBehavior.ControlField,EnemyTargetingStyle.Cluster,pale,1.12f,true,.45f,.3f,ElementTag.Frost,SpawnPresentation.Rift));
            list.Add(D("variant-lightning-flyer","Stormwing",EnemyRole.Flyer,55,4.4f,14,1.9f,4.7f,EnemyMovementStyle.AerialOrbit,EnemyAttackBehavior.Dive,EnemyTargetingStyle.Furthest,pale,.78f,false,.1f,.03f,ElementTag.Lightning,SpawnPresentation.Flight));
            list.Add(D("variant-lightning-assassin","Flash Assassin",EnemyRole.Assassin,70,4.55f,17,1.8f,4.2f,EnemyMovementStyle.Flank,EnemyAttackBehavior.Lunge,EnemyTargetingStyle.Isolated,pale,.84f,false,.08f,.04f,ElementTag.Lightning,SpawnPresentation.Rift));
            list.Add(D("variant-poison-burrower","Rot Burrower",EnemyRole.Burrower,84,3.05f,18,2.7f,3.5f,EnemyMovementStyle.Burrow,EnemyAttackBehavior.Eruption,EnemyTargetingStyle.Isolated,ash,1.02f,false,.3f,.2f,ElementTag.Poison,SpawnPresentation.Burrow));
            list.Add(D("variant-poison-support","Blight Choir",EnemyRole.Support,70,2.9f,13,3.5f,7,EnemyMovementStyle.SupportLine,EnemyAttackBehavior.AllyAid,EnemyTargetingStyle.LowestHealth,gold,.92f,false,.28f,.12f,ElementTag.Poison,SpawnPresentation.Gate));
            list.Add(D("variant-void-mage","Rift Mage",EnemyRole.Mage,90,2.65f,17,2.2f,6.4f,EnemyMovementStyle.Orbit,EnemyAttackBehavior.Area,EnemyTargetingStyle.Cluster,pale,1.12f,true,.35f,.2f,ElementTag.Void,SpawnPresentation.Rift));
            list.Add(D("variant-void-bruiser","Hollow Bruiser",EnemyRole.Bruiser,140,2.45f,22,2.25f,2.7f,EnemyMovementStyle.HoldGround,EnemyAttackBehavior.WideMelee,EnemyTargetingStyle.Cluster,gold,1.3f,true,.38f,.52f,ElementTag.Void,SpawnPresentation.Rift,shield:40));
            list.Add(D("common-vault-mimic","Vault Mimic",EnemyRole.Assassin,118,3.65f,19,1.7f,3.2f,EnemyMovementStyle.Flank,EnemyAttackBehavior.Lunge,EnemyTargetingStyle.Isolated,gold,1.22f,false,.22f,.2f,ElementTag.Void,SpawnPresentation.DropIn));
            return list.ToArray();
        }

        private static EncounterDefinition[] CreateEncounters(EnemyDefinition[] enemies)
        {
            EnemyDefinition E(string id)=>enemies.First(x=>x.id==id);
            EnemySpawnGroup G(string id,string enemy,int count,SpawnPresentation presentation,bool reinforcement=false)=>new EnemySpawnGroup{id=id,enemy=E(enemy),count=count,presentation=presentation,reinforcement=reinforcement,startDelay=0,spawnInterval=0};
            EncounterDefinition C(string id,string name,string intent,EncounterDifficulty difficulty,EncounterRiskTier risk,CombatSpaceCategory size,float diameter,float duration,EnemyRewardTier reward,string notes,params EnemySpawnGroup[] groups)=>Asset<EncounterDefinition>("Assets/ScriptableObjects/Encounters/"+id+".asset",x=>{x.id=id;x.displayName=name;x.intent=intent;x.difficulty=difficulty;x.riskTier=risk;x.requiredArenaSize=size;x.preferredSpaceDiameter=diameter;x.targetDurationSeconds=duration;x.rewardTier=reward;x.compositionNotes=notes;x.groups=groups;x.elementalPressure=groups.Where(g=>g.enemy&&g.enemy.element!=ElementTag.None).Select(g=>g.enemy.element).Distinct().ToArray();x.allowsElite=groups.Any(g=>g.enemy&&g.enemy.elite);});
            return new[]
            {
                C("frontline-pressure","Frontline Pressure","A durable line advances while ranged pressure punishes passive retreat.",EncounterDifficulty.Introductory,EncounterRiskTier.Low,CombatSpaceCategory.Small,20,105,EnemyRewardTier.Standard,"Frontline and backline have separate reads; reinforcement timing is exposed per group.",G("line","base-cinder-warrior",2,SpawnPresentation.Edge),G("anchor","base-ash-bruiser",1,SpawnPresentation.Gate),G("backline","variant-frost-ranger",1,SpawnPresentation.Edge)),
                C("collapse","Collapse","Flankers split the party before a readable bomber closes the safe pocket.",EncounterDifficulty.Standard,EncounterRiskTier.Medium,CombatSpaceCategory.Medium,25,125,EnemyRewardTier.Dangerous,"Assassins choose isolated targets; bomber chain reactions create player-directed risk.",G("flank","variant-lightning-assassin",2,SpawnPresentation.Rift),G("collapse","variant-fire-bomber",1,SpawnPresentation.DropIn,true),G("line","base-lantern-ranger",1,SpawnPresentation.Edge)),
                C("vertical-threat","Vertical Threat","Aerial orbit and dives demand open sightlines while support sustains the formation.",EncounterDifficulty.Dangerous,EncounterRiskTier.High,CombatSpaceCategory.Large,31,145,EnemyRewardTier.Dangerous,"Flyers expose landing windows. Ranged and support units remain answerable from connected subspaces.",G("air","variant-lightning-flyer",2,SpawnPresentation.Flight),G("cover","base-lantern-ranger",1,SpawnPresentation.Edge),G("choir","base-choir-support",1,SpawnPresentation.Gate)),
                C("ground-ambush","Ground Ambush","Burrow warnings displace players into controller zones without long hard control.",EncounterDifficulty.Standard,EncounterRiskTier.Medium,CombatSpaceCategory.Medium,27,130,EnemyRewardTier.Dangerous,"Eruption markers remain useful information while burrowers are untargetable.",G("burrow","variant-poison-burrower",2,SpawnPresentation.Burrow),G("keeper","base-chain-controller",1,SpawnPresentation.Rift),G("pressure","base-cinder-warrior",1,SpawnPresentation.Edge)),
                C("control-group","Control Group","Short pull and slow effects create attack windows for a mage and guarded support.",EncounterDifficulty.Dangerous,EncounterRiskTier.High,CombatSpaceCategory.Medium,28,150,EnemyRewardTier.Elite,"Control durations are short. Support has a cooldown and cannot create recursive support loops.",G("control","variant-frost-controller",1,SpawnPresentation.Rift),G("artillery","variant-void-mage",1,SpawnPresentation.Rift),G("support","variant-poison-support",1,SpawnPresentation.Gate),G("guard","base-cinder-warrior",1,SpawnPresentation.Edge)),
                C("cracked-trial","Cracked Trial","A single high-resistance warden checks developed builds before the throne.",EncounterDifficulty.Elite,EncounterRiskTier.High,CombatSpaceCategory.Large,30,180,EnemyRewardTier.Elite,"Mini-boss compatibility encounter; v0.4 reward and corruption sequence remains unchanged.",G("warden","elite-cracked-warden",1,SpawnPresentation.Gate)),
                C("mimic-reveal","False Cache","A common mimic reveals itself at close range; the compact duel pays one quality step above the false cache.",EncounterDifficulty.Standard,EncounterRiskTier.Medium,CombatSpaceCategory.Small,20,75,EnemyRewardTier.Dangerous,"Uses the shared assassin movement, telegraph, target selection, damage, and death pipeline.",G("mimic","common-vault-mimic",1,SpawnPresentation.DropIn))
            };
        }

        private static TreasureDefinition[] CreateTreasures(EncounterDefinition[] encounters)
        {
            ResourceWallet C(int ash=0,int ember=0,int alloy=0,int corruption=0)=>new ResourceWallet{ash=ash,emberShards=ember,ancientAlloy=alloy,corruptionFragments=corruption};
            var mimic=encounters.First(x=>x.id=="mimic-reveal");
            TreasureVariantDefinition V(string id,string name,string description,TreasureVariantKind kind,float weight,RewardQuality quality,EquipmentOfferKind offer=EquipmentOfferKind.Mixed,ResourceWallet cost=null,float health=0,int greed=1,ResourceWallet bonus=null,bool elite=false,bool pressure=false)=>new TreasureVariantDefinition{id=id,displayName=name,description=description,kind=kind,weight=weight,rewardQuality=quality,offerKind=offer,openCost=cost??C(),currentHealthCost=health,maximumGreedRewards=greed,bonusResources=bonus??C(),mimicEncounter=kind==TreasureVariantKind.Mimic?mimic:null,addsEliteToNextCombat=elite,addsVoidPressureToNextCombat=pressure};
            return new[]{Asset<TreasureDefinition>("Assets/ScriptableObjects/Treasures/cinder-cache-table.asset",x=>{x.id="cinder-cache-table";x.displayName="Cinder Cache";x.informationRule="The route reveals Treasure identity, but the weighted cache variant stays concealed until arrival.";x.variants=new[]{
                V("standard-cache","Standard Cache","A stable field cache with one equipment choice.",TreasureVariantKind.StandardCache,52,RewardQuality.Advanced),
                V("sealed-vault","Sealed Vault","A paid seal containing focused armor choices.",TreasureVariantKind.SealedVault,14,RewardQuality.Rare,EquipmentOfferKind.Armor,C(8,1),bonus:C(3)),
                V("cursed-chest","Cursed Chest","Spend current health without downing the party; the next Rest can clear its pressure.",TreasureVariantKind.CursedChest,9,RewardQuality.Rare,health:.18f,bonus:C(5,1),pressure:true),
                V("greedy-cache","Greedy Cache","Take up to three rewards. Later claims spend health and empower the next combat.",TreasureVariantKind.GreedyCache,10,RewardQuality.Advanced,greed:3),
                V("mimic","Mimic","The common false cache becomes a combat encounter, then improves the equipment quality.",TreasureVariantKind.Mimic,8,RewardQuality.Advanced),
                V("corrupted-cache","Corrupted Cache","Void pressure follows the party into its next combat.",TreasureVariantKind.CorruptedCache,7,RewardQuality.Epic,EquipmentOfferKind.Weapon,C(corruption:1),bonus:C(4,2),pressure:true)
            };})};
        }

        private static MerchantDefinition[] CreateMerchants()=>new[]{Asset<MerchantDefinition>("Assets/ScriptableObjects/Merchants/ember-quartermaster.asset",x=>{x.id="ember-quartermaster";x.displayName="Ember Quartermaster";x.description="Finite run-only stock. Bought gear cannot be dismantled for more than its purchase price.";x.baseStock=4;x.maximumRerolls=2;x.rerollCost=new ResourceWallet{ash=7};x.priceMultiplier=1;x.recoveryFraction=.3f;x.recoveryPrice=new ResourceWallet{ash=10};})};
        private static RestNodeDefinition[] CreateRests()=>new[]{Asset<RestNodeDefinition>("Assets/ScriptableObjects/Rests/quiet-brazier.asset",x=>{x.id="quiet-brazier";x.displayName="Quiet Brazier";x.description="Choose once: recover, temper one equipped item, or salvage modest supplies.";x.restRecovery=.34f;x.salvageRecovery=.1f;x.salvageResources=new ResourceWallet{ash=8};x.temperMaximum=WeaponRarity.Epic;})};

        private static EventDefinition[] CreateEvents(EncounterDefinition[] encounters)
        {
            ResourceWallet C(int ash=0,int ember=0,int alloy=0,int corruption=0)=>new ResourceWallet{ash=ash,emberShards=ember,ancientAlloy=alloy,corruptionFragments=corruption};
            ExpeditionEventChoice O(string id,string name,string text,EventOutcomeKind outcome,ResourceWallet cost=null,ResourceWallet reward=null,float health=0,float recovery=0,EncounterDefinition combat=null,RewardQuality quality=RewardQuality.Advanced,bool hidden=false)=>new ExpeditionEventChoice{id=id,displayName=name,outcomeText=text,outcome=outcome,cost=cost??C(),reward=reward??C(),currentHealthCost=health,recoveryFraction=recovery,escalationEncounter=combat,equipmentQuality=quality,outcomeInitiallyHidden=hidden};
            EventDefinition E(string id,string name,string description,string lore,params ExpeditionEventChoice[] choices)=>Asset<EventDefinition>("Assets/ScriptableObjects/Events/"+id+".asset",x=>{x.id=id;x.displayName=name;x.description=description;x.choices=choices;x.loreFragments=new[]{lore};});
            return new[]{
                E("abandoned-expedition-cache","Abandoned Expedition Cache","A prior party marked a lockbox and never returned.","The chalk arrows point both toward and away from the throne.",O("take-supplies","Take supplies","Loose Ash joins the run wallet.",EventOutcomeKind.Resources,reward:C(12,1)),O("open-case","Open the weapon case","A focused equipment draft begins.",EventOutcomeKind.Equipment,quality:RewardQuality.Rare),O("leave-marked","Read the marks","The current graph becomes fully visible.",EventOutcomeKind.RouteInformation)),
                E("broken-shrine","Broken Shrine","A cracked flame answers only in fragments.","The shrine remembers hands, not names.",O("offer-ash","Offer 8 Ash","The party recovers a measured amount.",EventOutcomeKind.Recovery,cost:C(8),recovery:.25f),O("offer-blood","Offer current health","A relic choice surfaces.",EventOutcomeKind.Relic,health:.12f),O("search-base","Search the base","Ember remains beneath the stone.",EventOutcomeKind.Resources,reward:C(4,2))),
                E("wounded-survivor-trace","Wounded Survivor Trace","A warm trail ends beside an abandoned pack.","Someone left before the ash could choose them.",O("use-medicine","Use supplies","The party recovers, but the pack stays untouched.",EventOutcomeKind.Recovery,cost:C(4),recovery:.32f),O("follow-trail","Follow the trail","The route ahead is annotated.",EventOutcomeKind.RouteInformation),O("take-pack","Take the pack","Gain materials and accept an ambush.",EventOutcomeKind.Combat,reward:C(9,1),combat:encounters.First(x=>x.id=="ground-ambush"),hidden:true)),
                E("unstable-rift","Unstable Rift","A narrow void fold distorts the next few steps.","The fold repeats one distant bell out of sequence.",O("stabilize","Stabilize","Spend fragments for alloy.",EventOutcomeKind.Resources,cost:C(corruption:1),reward:C(alloy:2)),O("cross","Cross the fold","Take equipment at a current-health cost.",EventOutcomeKind.Equipment,health:.15f,quality:RewardQuality.Epic),O("map-edge","Map its edge","Reveal all remaining node identities.",EventOutcomeKind.RouteInformation)),
                E("collapsed-armory","Collapsed Armory","Bent racks hold one usable frame and many broken plates.","The armory inventory ends one line before the keeper's seal.",O("recover-frame","Recover the frame","Choose advanced equipment.",EventOutcomeKind.Equipment,quality:RewardQuality.Advanced),O("salvage-plates","Salvage plates","Alloy and Ash enter the run wallet.",EventOutcomeKind.Resources,reward:C(7,0,1)),O("move-rubble","Move the rubble","A guard wakes before a richer cache.",EventOutcomeKind.Combat,reward:C(13,2),combat:encounters.First(x=>x.id=="frontline-pressure"),hidden:true))
            };
        }

        private static ChallengeDefinition[] CreateChallenges(EncounterDefinition[] encounters)
        {
            ChallengeDefinition C(string id,string name,string description,ChallengeKind kind,float duration,string encounter)=>Asset<ChallengeDefinition>("Assets/ScriptableObjects/Challenges/"+id+".asset",x=>{x.id=id;x.displayName=name;x.description=description;x.kind=kind;x.duration=duration;x.encounter=encounters.First(e=>e.id==encounter);x.successReward=new ResourceWallet{ash=16,emberShards=3,ancientAlloy=1};x.consolationReward=new ResourceWallet{ash=5};x.successQuality=RewardQuality.Rare;x.noHealing=kind==ChallengeKind.NoHealing;x.failureEndsRun=false;});
            return new[]{C("timed-collapse","Timed Collapse","Eliminate the formation before the seal closes. Failure grants consolation and continues the route.",ChallengeKind.TimedElimination,48,"collapse"),C("no-healing-ambush","Unmended Trial","Clear the ambush without healing; failure is nonfatal.",ChallengeKind.NoHealing,58,"ground-ambush"),C("priority-control","Priority Signal","Break the control group under a short timer.",ChallengeKind.PriorityTargets,55,"control-group")};
        }

        private static BossRewardDefinition[] CreateBossRewards()=>new[]{Asset<BossRewardDefinition>("Assets/ScriptableObjects/BossRewards/regional-warden-hoard.asset",x=>{x.id="regional-warden-hoard";x.displayName="Regional Warden Hoard";x.resources=new ResourceWallet{ash=24,emberShards=6,ancientAlloy=2};x.equipmentQuality=RewardQuality.Epic;x.grantEquipment=true;x.grantRelic=false;x.futureRegionTransitionHook="Advance to a future region selector; v0.6 routes this prototype through the explicit final-area gate.";})};

        private static ExpeditionRegionDefinition CreatePrototypeRegion(EncounterDefinition[] encounters,CombatSpaceDefinition[] spaces,TreasureDefinition[] treasures,MerchantDefinition[] merchants,RestNodeDefinition[] rests,EventDefinition[] events,ChallengeDefinition[] challenges,BossRewardDefinition[] bossRewards,Vector3[] points)
        {
            EncounterDefinition Enc(string id)=>encounters.First(x=>x.id==id);CombatSpaceDefinition Space(int i)=>spaces[Mathf.Clamp(i,0,spaces.Length-1)];
            ExpeditionRouteGraphDefinition Graph(string suffix,string title,VoteTieBehavior tie,int eventOffset)
            {
                string Id(string key)=>"route-"+suffix+"-"+key;
                ExpeditionNodeDefinition N(string key,string name,string description,ExpeditionNodeType type,NodeRiskRating risk,NodeRewardCategory reward,string[] next,EncounterDefinition encounter=null,CombatSpaceDefinition space=null)=>Asset<ExpeditionNodeDefinition>("Assets/ScriptableObjects/Routes/Nodes/"+Id(key)+".asset",x=>{x.id=Id(key);x.displayName=name;x.description=description;x.nodeType=type;x.risk=risk;x.rewardCategory=reward;x.rewardQuality=RewardQuality.Advanced;x.outgoingConnections=next.Select(Id).ToArray();x.encounter=encounter;x.combatSpace=space;x.spawnPoints=points;x.resourceReward=new ResourceWallet();x.grantEquipment=false;x.grantRelic=false;x.treasure=null;x.merchant=null;x.rest=null;x.eventDefinition=null;x.challenge=null;x.finalBoss=null;x.bossReward=null;x.isTrueFinalBoss=false;x.telemetryTags=new[]{"region-1",suffix,key};});
                var start=N("start","Threshold Patrol","A readable opening combat anchors the route.",ExpeditionNodeType.NormalCombat,NodeRiskRating.Low,NodeRewardCategory.Resources,new[]{"hard","treasure"},Enc("frontline-pressure"),Space(0));start.resourceReward=new ResourceWallet{ash=10};
                string[] hardEdges=suffix=="b"?new[]{"elite","merchant"}:suffix=="c"?new[]{"relic","merchant"}:new[]{"relic","elite"};string[] treasureEdges=suffix=="b"?new[]{"relic","rest"}:suffix=="c"?new[]{"elite","merchant"}:new[]{"relic","merchant"};string[] relicEdges=suffix=="c"?new[]{"event","challenge"}:new[]{"rest","event"};string[] eliteEdges=suffix=="b"?new[]{"rest","challenge"}:new[]{"rest","event"};
                var hard=N("hard","Broken Formation","A more demanding formation pays resources and one equipment draft.",ExpeditionNodeType.HardCombat,NodeRiskRating.High,NodeRewardCategory.Equipment,hardEdges,Enc(suffix=="b"?"ground-ambush":"collapse"),Space(1));hard.resourceReward=new ResourceWallet{ash=15,emberShards=2};hard.grantEquipment=true;hard.rewardQuality=RewardQuality.Advanced;
                var treasure=N("treasure","Veiled Cache","The node identity is known; its weighted cache behavior is not.",ExpeditionNodeType.Treasure,NodeRiskRating.Moderate,NodeRewardCategory.Variable,treasureEdges);treasure.treasure=treasures[0];
                var relic=N("relic","Ashbound Reliquary","A dedicated relic cadence choice without a preceding combat.",ExpeditionNodeType.Relic,NodeRiskRating.Low,NodeRewardCategory.Relic,relicEdges);relic.grantRelic=true;
                var elite=N("elite","Warden Escort","An elite composition offers a stronger equipment reward.",ExpeditionNodeType.Elite,NodeRiskRating.Severe,NodeRewardCategory.Equipment,eliteEdges,Enc("control-group"),Space(2));elite.resourceReward=new ResourceWallet{ash=20,emberShards=5};elite.grantEquipment=true;elite.rewardQuality=RewardQuality.Rare;
                var merchant=N("merchant","Ember Quartermaster","Spend expedition resources on finite stock.",ExpeditionNodeType.Merchant,NodeRiskRating.Low,NodeRewardCategory.Merchant,new[]{"rest","event"});merchant.merchant=merchants[0];
                var rest=N("rest","Quiet Brazier","Choose one recovery or tempering action.",ExpeditionNodeType.Rest,NodeRiskRating.Low,NodeRewardCategory.Recovery,new[]{"challenge"});rest.rest=rests[0];
                var eventNode=N("event",events[eventOffset%events.Length].displayName,"A two-to-three choice authored event.",ExpeditionNodeType.Event,NodeRiskRating.Moderate,NodeRewardCategory.Variable,new[]{"challenge"});eventNode.eventDefinition=events[eventOffset%events.Length];
                var challenge=N("challenge",challenges[eventOffset%challenges.Length].displayName,"An optional-rule combat with success and consolation outcomes.",ExpeditionNodeType.Challenge,NodeRiskRating.High,NodeRewardCategory.Challenge,new[]{"boss"},challenges[eventOffset%challenges.Length].encounter,Space(3));challenge.challenge=challenges[eventOffset%challenges.Length];
                var boss=N("boss","Regional Warden","This regional boss pays a region reward, then opens the separate final-area gate.",ExpeditionNodeType.Boss,NodeRiskRating.Severe,NodeRewardCategory.Boss,Array.Empty<string>(),Enc("cracked-trial"),Space(4));boss.bossReward=bossRewards[0];
                var nodes=new[]{start,hard,treasure,relic,elite,merchant,rest,eventNode,challenge,boss};
                return Asset<ExpeditionRouteGraphDefinition>("Assets/ScriptableObjects/Routes/route-"+suffix+".asset",x=>{x.id="route-"+suffix;x.displayName=title;x.startNodeId=start.id;x.bossNodeId=boss.id;x.nodes=nodes;x.tieBehavior=tie;x.minimumCombatNodes=3;x.maximumRestNodes=1;x.maximumMerchantNodes=1;x.maximumRepeatedType=2;});
            }
            var graphs=new[]{Graph("a","The Split Procession",VoteTieBehavior.HostBreaksTie,0),Graph("b","The Bell's Fork",VoteTieBehavior.SeededRandom,3),Graph("c","The Broken Ledger",VoteTieBehavior.HostBreaksTie,4)};
            return Asset<ExpeditionRegionDefinition>("Assets/ScriptableObjects/Routes/cinder-vault-region.asset",x=>{x.id="cinder-vault-region";x.displayName="The Cinder Vault";x.regionIntent="One complete branching 10-node region proving route identity, limited information, local voting, and regional-boss gating while reusing v0.5 combat spaces.";x.graphVariants=graphs;x.eventualRegionCount=5;x.eventualFinalAreaCount=1;x.targetNodesPerRegionMin=8;x.targetNodesPerRegionMax=10;});
        }

        private static RegionEnemyPoolDefinition[] CreateRegionEcologies(EnemyDefinition[] enemies,EncounterDefinition[] encounters)
        {
            EnemyDefinition[] Pick(params string[] ids)=>ids.Select(id=>enemies.First(x=>x.id==id)).ToArray();EncounterDefinition[] Enc(params string[] ids)=>ids.Select(id=>encounters.First(x=>x.id==id)).ToArray();
            RegionEnemyPoolDefinition R(string id,string name,string intent,ElementTag[] elements,EnemyDefinition[] common,EnemyDefinition[] hard,EnemyDefinition[] elite,EncounterDefinition[] pool)=>Asset<RegionEnemyPoolDefinition>("Assets/ScriptableObjects/RegionEcologies/"+id+".asset",x=>{x.id=id;x.displayName=name;x.ecologyIntent=intent;x.favoredElements=elements;x.commonEnemies=common;x.hardEnemies=hard;x.eliteCandidates=elite;x.encounterPool=pool;});
            return new[]
            {
                R("future-ash-ruins","Ash Ruins Ecology","Frontline and explosive Fire pressure for a future region; this is a pool hook, not a completed campaign.",new[]{ElementTag.Fire},Pick("base-cinder-warrior","base-ash-bruiser","base-lantern-ranger"),Pick("variant-fire-warrior","variant-fire-bomber","base-bell-mage"),Pick("variant-void-bruiser"),Enc("frontline-pressure","collapse")),
                R("future-frozen-reach","Frozen Reach Ecology","Precision Chill and short control supported by durable neutral bodies.",new[]{ElementTag.Frost},Pick("base-cinder-warrior","variant-frost-ranger","base-veil-assassin"),Pick("variant-frost-controller","base-ash-bruiser"),Pick("variant-void-mage"),Enc("control-group","ground-ambush")),
                R("future-void-depths","Void Depths Ecology","Spatial distortion combines mobile threats, controllers, and priority mages.",new[]{ElementTag.Void},Pick("base-vault-flyer","base-veil-assassin","base-chain-controller"),Pick("variant-void-mage","variant-lightning-flyer"),Pick("variant-void-bruiser"),Enc("vertical-threat","control-group"))
            };
        }

        private static CombatSpaceDefinition[] CreateCombatSpaces()
        {
            CombatSpaceSection S(string id,float x,float z,float width,float depth,bool path=false,float rotation=0)=>new CombatSpaceSection{id=id,center=new Vector2(x,z),size=new Vector2(width,depth),transitionPath=path,rotation=rotation};
            CombatSpaceObstacle O(float x,float z,float width,float depth,float height=2,float rotation=0)=>new CombatSpaceObstacle{position=new Vector2(x,z),size=new Vector2(width,depth),height=height,rotation=rotation};
            CombatSpaceDefinition C(string id,string name,CombatSpaceCategory category,ArenaLayoutKind layout,Vector2 bounds,float camera,float separation,Vector3 exit,string intent,CombatSpaceSection[] sections,Vector2[] boundary,CombatSpaceObstacle[] obstacles,params string[] landmarks)=>Asset<CombatSpaceDefinition>("Assets/ScriptableObjects/CombatSpaces/"+id+".asset",x=>{x.id=id;x.displayName=name;x.category=category;x.layout=layout;x.technicalBounds=bounds;x.cameraOrthographicSize=camera;x.multiplayerSeparationLimit=separation;x.entrancePosition=new Vector3(0,0,-8);x.exitPosition=exit;x.transitionLength=6;x.spatialIntent=intent;x.sections=sections;x.boundaryPoints=boundary;x.obstacles=obstacles;x.distantLandmarkHooks=landmarks;});
            return new[]
            {
                C("crooked-threshold","Crooked Threshold",CombatSpaceCategory.Small,ArenaLayoutKind.IrregularCourtyard,new Vector2(24,24),12.8f,15,new Vector3(2,0,10),"An offset courtyard with a playable side pocket and a short northern transition path.",new[]{S("court",1,-1,17,14),S("side-pocket",-7,1,6,8),S("north-path",2,7.5f,4,7,true)},new[]{new Vector2(-10,-8),new Vector2(8,-8),new Vector2(10,-3),new Vector2(9,5),new Vector2(4,7),new Vector2(4,11),new Vector2(0,11),new Vector2(0,7),new Vector2(-10,5)},new[]{O(-5,-1,1.4f,3,2.4f,18),O(5,2,2,1.3f,2.2f,-12)},"sunken-bell","distant-vault"),
                C("twin-courts","Twin Courts",CombatSpaceCategory.Medium,ArenaLayoutKind.ConnectedCourtyards,new Vector2(32,25),14.5f,19,new Vector3(6,0,11),"Two combat pockets connected by a wide bridge, with route-readable retreat and regroup space.",new[]{S("west",-6,-1,14,15),S("east",7,2,14,14),S("bridge",1,0,8,5),S("north-path",6,8.5f,5,7,true)},new[]{new Vector2(-13,-9),new Vector2(1,-9),new Vector2(3,-5),new Vector2(13,-5),new Vector2(14,8),new Vector2(9,9),new Vector2(9,12),new Vector2(3,12),new Vector2(3,9),new Vector2(-13,7)},new[]{O(-7,1,1.5f,4,2.5f),O(7,1,2,2,2.8f,45),O(1,4,1,3,1.6f)},"bell-tower","broken-arcade"),
                C("branching-ruins","Branching Ruins",CombatSpaceCategory.Medium,ArenaLayoutKind.BranchingRuins,new Vector2(34,27),15.2f,20,new Vector3(0,0,12),"A central hub branches into three usable lanes; obstacles block clean firing lines without creating dead ends.",new[]{S("hub",0,0,13,13),S("west-branch",-9,1,8,6),S("east-branch",9,1,8,6),S("south-branch",0,-8,7,7),S("north-path",0,8.5f,5,8,true)},new[]{new Vector2(-14,-4),new Vector2(-5,-5),new Vector2(-4,-12),new Vector2(4,-12),new Vector2(5,-5),new Vector2(14,-4),new Vector2(14,5),new Vector2(4,6),new Vector2(3,13),new Vector2(-3,13),new Vector2(-4,6),new Vector2(-14,5)},new[]{O(-3,0,1.4f,4,2.6f,35),O(4,2,1.5f,4,2.6f,-30),O(0,-7,2.2f,1.2f,1.8f)},"collapsed-choir","far-chain"),
                C("broken-ring","Broken Ring",CombatSpaceCategory.Large,ArenaLayoutKind.BrokenRing,new Vector2(38,31),16.8f,23,new Vector3(-2,0,14),"A broad irregular ring supports four-player separation while broken center blocks create circular rotations.",new[]{S("ring-floor",0,0,29,22),S("west-alcove",-14,0,6,10),S("east-alcove",14,2,6,9),S("north-path",-2,11,6,8,true)},new[]{new Vector2(-18,-8),new Vector2(-10,-13),new Vector2(7,-13),new Vector2(17,-7),new Vector2(18,7),new Vector2(9,11),new Vector2(1,11),new Vector2(1,15),new Vector2(-5,15),new Vector2(-5,11),new Vector2(-16,9)},new[]{O(-4,0,3,7,2.5f,18),O(4,1,3,7,2.5f,-18),O(0,-5,4,2,1.5f),O(11,5,1.5f,4,2.2f)},"cinder-chasm","warden-statue","upper-vault"),
                C("divided-hall","Divided Hall",CombatSpaceCategory.Large,ArenaLayoutKind.DividedHall,new Vector2(37,31),17.2f,22,new Vector3(4,0,14),"A split hall with parallel subspaces, cross-connectors, and a visible transition corridor to the next node.",new[]{S("west-hall",-7,0,13,22),S("east-hall",8,1,13,20),S("lower-crossing",0,-6,7,4),S("upper-crossing",1,6,8,4),S("north-path",4,11.5f,5,8,true)},new[]{new Vector2(-14,-12),new Vector2(15,-11),new Vector2(16,9),new Vector2(7,10),new Vector2(7,15),new Vector2(1,15),new Vector2(1,10),new Vector2(-13,11),new Vector2(-16,2)},new[]{O(0,0,2,9,3),O(-8,4,2,3,2.4f,20),O(9,-3,2,4,2.4f,-20)},"throne-silhouette","ashfall","distant-procession")
            };
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

        private static RoomDefinition Room(string assetName, string id, string name, string description, Vector3[] points, LoreEntry lore,EncounterDefinition encounter,CombatSpaceDefinition space) =>
            Asset<RoomDefinition>("Assets/ScriptableObjects/Rooms/" + assetName + ".asset", x =>
            { x.id = id; x.displayName = name; x.description = description; x.isBoss = false; x.spawnPoints = points; x.fragment = lore; x.combatSpace=space;x.waves = new[] { new EnemyWave { enemies = LegacyEnemies(id),encounter=encounter } };
              x.enemyHealthMultiplier = id == "cracked-sanctum" ? 1.35f : id == "last-procession" ? 1.2f : 1; x.targetEncounterSeconds = id == "cracked-sanctum" ? 180 : 145; });

        private static EnemyKind[] LegacyEnemies(string id)
        {
            switch(id)
            {
                case "threshold":return new[]{EnemyKind.Cinderling,EnemyKind.Cinderling,EnemyKind.Lantern};
                case "bell-chamber":return new[]{EnemyKind.Hound,EnemyKind.Lantern,EnemyKind.Cinderling,EnemyKind.Hound};
                case "warden-crossing":return new[]{EnemyKind.Bulwark,EnemyKind.Elite,EnemyKind.Lantern};
                case "ash-gallery":return new[]{EnemyKind.Cinderling,EnemyKind.Hound,EnemyKind.Bulwark,EnemyKind.Lantern};
                case "cracked-sanctum":return new[]{EnemyKind.MiniBoss};
                default:return new[]{EnemyKind.Elite,EnemyKind.Hound,EnemyKind.Lantern,EnemyKind.Bulwark};
            }
        }

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
