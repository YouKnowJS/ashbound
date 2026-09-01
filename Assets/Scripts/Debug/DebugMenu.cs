using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U=Ashbound.PrototypeGui;

namespace Ashbound
{
    public sealed class DebugMenu:MonoBehaviour
    {
        private RunManager run;private int selectedPlayer,weaponFamily,weaponRarity,weaponElement,skillIndex,setIndex,relicPage,metaFacility,page,enemyRole,enemyElement,encounterIndex,spaceIndex;private bool eliteEnemy;
        private string result="Configure equipment, then close F1 to resume.";
        public void Configure(RunManager manager){run=manager;}
        private void Update(){if(run&&Keyboard.current!=null&&Keyboard.current.f1Key.wasPressedThisFrame)run.DebugOpen=!run.DebugOpen;}
        private void Mark(){if(run.Telemetry.Record!=null)run.Telemetry.Record.debugUsed=true;}
        private void OnGUI()
        {
            if(!run||!run.DebugOpen)return;GUI.depth=-20;var old=U.Scale();U.Box(new Rect(0,0,1280,720),new Color(0,0,0,.72f));U.Panel(new Rect(35,20,1210,680));
            U.Label(60,34,500,32,"ASHBOUND v0.7 · DEVELOPMENT LAB",U.Heading);if(U.Click(new Rect(555,32,98,32),"Equipment"))page=0;if(U.Click(new Rect(658,32,92,32),"Meta"))page=1;if(U.Click(new Rect(755,32,92,32),"Ecology"))page=2;if(U.Click(new Rect(852,32,92,32),"Route"))page=3;if(U.Click(new Rect(949,32,92,32),"Camera"))page=4;if(U.Click(new Rect(1060,32,150,32),"Close F1"))run.DebugOpen=false;
            if(page==1){MetaPanel();GUI.matrix=old;return;}if(page==2){EcologyPanel();GUI.matrix=old;return;}if(page==3){RoutePanel();GUI.matrix=old;return;}if(page==4){CameraPanel();GUI.matrix=old;return;}
            if(U.Click(new Rect(60,78,145,31),"Mini-Boss")){result=run.DebugJumpToRoom(4)?"Mini-Boss ready":"Reset first";Mark();}
            if(U.Click(new Rect(215,78,145,31),"Final Boss")){result=run.DebugSkipToBoss()?"Final boss ready":"Reset first";Mark();}
            if(U.Click(new Rect(370,78,120,31),"Reset run")){run.ResetRun();run.DebugOpen=true;result="Run reset";}
            if(run.Combat.Feedback){run.Combat.Feedback.HitStopEnabled=GUI.Toggle(new Rect(520,83,100,22),run.Combat.Feedback.HitStopEnabled,"Hit stop");run.Combat.Feedback.CameraShakeEnabled=GUI.Toggle(new Rect(625,83,90,22),run.Combat.Feedback.CameraShakeEnabled,"Shake");run.Combat.Feedback.VfxEnabled=GUI.Toggle(new Rect(720,83,150,22),run.Combat.Feedback.VfxEnabled,"Status/VFX");}
            if(run.Players.Count==0){U.Label(60,140,850,40,"Start a run or jump to an encounter first.");GUI.matrix=old;return;}
            for(int i=0;i<run.Players.Count;i++)if(U.Click(new Rect(900+i*72,78,66,30),run.Players[i].Id+(i==selectedPlayer?"*":"")))selectedPlayer=i;
            selectedPlayer=Mathf.Clamp(selectedPlayer,0,run.Players.Count-1);var player=run.Players[selectedPlayer];

            U.Label(60,125,550,24,"WEAPON CONFIGURATION",U.CardTitle);
            WeaponFamily[] families=(WeaponFamily[])System.Enum.GetValues(typeof(WeaponFamily));for(int i=0;i<families.Length;i++)if(U.Click(new Rect(60+i*70,155,65,27),families[i].ToString().Substring(0,Mathf.Min(8,families[i].ToString().Length))))weaponFamily=i;
            WeaponRarity[] rarities=(WeaponRarity[])System.Enum.GetValues(typeof(WeaponRarity));for(int i=0;i<rarities.Length;i++)if(U.Click(new Rect(60+i*100,190,94,27),rarities[i].ToString()))weaponRarity=i;
            ElementTag[] elements=(ElementTag[])System.Enum.GetValues(typeof(ElementTag));for(int i=0;i<elements.Length;i++)if(U.Click(new Rect(60+i*88,225,82,27),elements[i].ToString()))weaponElement=i;
            if(run.Catalog.weaponSkills.Length>0){skillIndex=Mathf.Clamp(skillIndex,0,run.Catalog.weaponSkills.Length-1);if(U.Click(new Rect(60,260,35,28),"<"))skillIndex=(skillIndex+run.Catalog.weaponSkills.Length-1)%run.Catalog.weaponSkills.Length;if(U.Click(new Rect(100,260,35,28),">"))skillIndex=(skillIndex+1)%run.Catalog.weaponSkills.Length;U.Label(145,263,270,24,"Skill: "+run.Catalog.weaponSkills[skillIndex].displayName,U.Small);}
            if(U.Click(new Rect(420,260,180,30),"Apply weapon profile"))
            {
                var baseWeapon=run.Catalog.FindWeapon(families[weaponFamily]);var weapon=Instantiate(baseWeapon);weapon.name="Debug weapon";weapon.rarity=rarities[weaponRarity];weapon.elements=elements[weaponElement]==ElementTag.None?System.Array.Empty<ElementTag>():new[]{elements[weaponElement]};weapon.skill=weapon.rarity>=WeaponRarity.Rare?run.Catalog.weaponSkills[skillIndex]:null;player.Attacks.SetWeapon(weapon);result="Applied "+weapon.rarity+" "+weapon.PrimaryElement+" "+weapon.family;Mark();
            }
            U.Label(60,300,550,42,player.Weapon.displayName+" · "+player.Weapon.rarity+" "+player.Weapon.family+" · "+player.Weapon.PrimaryElement+"\nSkill: "+(player.Weapon.skill?player.Weapon.skill.displayName:"None"),U.Small);

            U.Label(650,125,520,24,"ARMOR & SETS",U.CardTitle);setIndex=Mathf.Clamp(setIndex,0,run.Catalog.armorSets.Length-1);
            for(int i=0;i<run.Catalog.armorSets.Length;i++)if(U.Click(new Rect(650+i*105,155,98,28),run.Catalog.armorSets[i].displayName))setIndex=i;
            var set=run.Catalog.armorSets[setIndex];if(U.Click(new Rect(650,193,160,30),"Equip 2-piece")){EquipSet(player,set,2);result=set.displayName+" 2-piece active";Mark();}if(U.Click(new Rect(820,193,160,30),"Equip 4-piece")){EquipSet(player,set,4);result=set.displayName+" 4-piece active";Mark();}
            if(U.Click(new Rect(990,193,170,30),"Clear equipment")){player.Equipment.Clear();result="Armor cleared";Mark();}
            int armorRow=0;foreach(var pair in player.Equipment.Equipped){U.Label(650,232+armorRow*23,500,21,pair.Key+": "+pair.Value.displayName,U.Small);armorRow++;}
            U.Label(650,332,520,42,"Active: "+string.Join(" · ",player.Equipment.ActiveBonuses().Select(x=>x.Set.displayName+" "+x.Tier.pieces))+"\nAnalysis: "+string.Join(" / ",player.DominantBuildTags()),U.Small);

            U.Label(60,365,540,24,"FORCE BUILD / RELICS",U.CardTitle);BuildTag[] tags={BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void,BuildTag.Critical,BuildTag.Bleed,BuildTag.Heavy,BuildTag.Combo,BuildTag.DashPrecision};
            for(int i=0;i<tags.Length;i++){var tag=tags[i];if(U.Click(new Rect(60+(i%5)*105,397+(i/5)*33,98,27),tag.ToString())){var item=run.Catalog.items.FirstOrDefault(x=>x.tags.Contains(tag)&&player.Inventory.CanAdd(x));result=item&&player.Inventory.TryAdd(item)?"Granted "+item.displayName:"No eligible relic";Mark();}}
            if(U.Click(new Rect(60,470,150,29),"Clear relics")){player.Inventory.Clear();Mark();}
            if(U.Click(new Rect(220,470,185,29),"Spawn elemental group")){run.DebugSpawnElementalGroup(elements[weaponElement]);result="Spawned "+elements[weaponElement]+" test group";Mark();}
            U.Label(60,515,540,24,"SPECIFIC RELICS",U.CardTitle);int pageSize=8,pages=Mathf.CeilToInt(run.Catalog.items.Length/(float)pageSize);relicPage=Mathf.Clamp(relicPage,0,pages-1);if(U.Click(new Rect(225,511,35,28),"<"))relicPage=Mathf.Max(0,relicPage-1);if(U.Click(new Rect(265,511,35,28),">"))relicPage=Mathf.Min(pages-1,relicPage+1);
            for(int n=0;n<pageSize;n++){int index=relicPage*pageSize+n;if(index>=run.Catalog.items.Length)break;var item=run.Catalog.items[index];float x=60+(n%2)*275,y=550+(n/2)*30;U.Label(x,y,190,24,item.displayName,U.Small);GUI.enabled=player.Inventory.CanAdd(item);if(U.Click(new Rect(x+195,y,65,25),"Grant")){player.Inventory.TryAdd(item);Mark();}GUI.enabled=true;}
            U.Label(650,400,520,110,"ELEMENT IDENTITY\nFire: AOE / Burn / burst\nFrost: control / single target / critical\nLightning: chain AOE / critical\nPoison: DoT / spread / throttled recovery\nVoid: rifts / pull / delayed control",U.Small);
            U.Label(650,535,520,85,result+"\nDominant: "+string.Join(" / ",player.DominantBuildTags())+"\nDebug runs are marked in local telemetry.",U.Small);
            GUI.matrix=old;
        }
        private void EquipSet(Combatant player,ArmorSetDefinition set,int count){player.Equipment.Clear();foreach(var armor in run.Catalog.armor.Where(x=>x.set==set).Take(count))player.Equipment.Equip(armor);}
        private void MetaPanel()
        {
            var meta=run.Progression;U.Label(60,88,1120,25,"PROFILE · "+meta.Profile.profileId+"   "+meta.Profile.currencies,U.CardTitle);U.Label(60,116,1120,22,"Save: "+meta.SavePath,U.Small);
            ExpeditionResource[] resources=(ExpeditionResource[])System.Enum.GetValues(typeof(ExpeditionResource));for(int i=0;i<resources.Length;i++)if(U.Click(new Rect(60+i*185,155,175,32),"+100 "+resources[i])){meta.DebugAdd(resources[i],100);Mark();}
            if(U.Click(new Rect(810,155,165,32),"Zero currencies")){meta.DebugZeroCurrencies();Mark();}if(U.Click(new Rect(985,155,195,32),"Unlock all gear")){meta.DebugUnlockAll();Mark();}
            U.Label(60,210,500,25,"FACILITY LEVELS",U.CardTitle);for(int i=0;i<run.Catalog.facilities.Length;i++)if(U.Click(new Rect(60+(i%3)*190,245+(i/3)*38,180,30),run.Catalog.facilities[i].displayName))metaFacility=i;metaFacility=Mathf.Clamp(metaFacility,0,run.Catalog.facilities.Length-1);var facility=run.Catalog.facilities[metaFacility];var progress=meta.Profile.Facility(facility.id);
            U.Label(650,210,530,60,facility.displayName+" · "+progress.level+" / "+facility.MaxLevel+"\n"+facility.description,U.Small);if(U.Click(new Rect(650,282,125,32),"Level -")){meta.DebugSetFacility(facility,progress.level-1);Mark();}if(U.Click(new Rect(785,282,125,32),"Level +")){meta.DebugSetFacility(facility,progress.level+1);Mark();}if(U.Click(new Rect(920,282,260,32),"Set max / unlock")){meta.DebugSetFacility(facility,facility.MaxLevel);Mark();}
            U.Label(60,345,500,25,"PREPARATION",U.CardTitle);for(int i=0;i<run.Catalog.preparations.Length;i++){var prep=run.Catalog.preparations[i];GUI.enabled=meta.PreparationAvailable(prep);if(U.Click(new Rect(60+(i%3)*220,380+(i/3)*38,210,30),prep.displayName)){meta.SelectPreparation(prep);result="Forced "+prep.displayName;Mark();}GUI.enabled=true;}
            U.Label(60,465,500,25,"OUTCOME / REWARD",U.CardTitle);if(U.Click(new Rect(60,500,205,34),"Simulate failed expedition")){var s=meta.DebugSimulateOutcome(false);result="Retained "+s.Retained;Mark();}if(U.Click(new Rect(275,500,215,34),"Simulate successful expedition")){var s=meta.DebugSimulateOutcome(true);result="Retained "+s.Retained;Mark();}
            WeaponRarity[] rarities=(WeaponRarity[])System.Enum.GetValues(typeof(WeaponRarity));for(int i=0;i<rarities.Length;i++)if(U.Click(new Rect(510+i*120,500,112,34),rarities[i].ToString()))weaponRarity=i;GUI.enabled=run.Players.Count>0;if(U.Click(new Rect(510,545,250,34),"Force equipment reward")){run.DebugForceEquipmentReward(rarities[weaponRarity]);result="Forced "+rarities[weaponRarity]+" reward";Mark();}GUI.enabled=true;
            if(U.Click(new Rect(60,605,240,38),"RESET META PROFILE")){meta.ResetProfile();result="Meta profile reset";Mark();}U.Label(330,600,850,70,result+"\nRun resources: "+meta.RunResources+"\nThe profile is host-owned; run player IDs remain separate.",U.Small);
        }
        private void EcologyPanel()
        {
            EnemyRole[] roles=(EnemyRole[])System.Enum.GetValues(typeof(EnemyRole));ElementTag[] elements=(ElementTag[])System.Enum.GetValues(typeof(ElementTag));
            U.Label(60,88,1120,25,"ENEMY ROLE & ELEMENT TESTS",U.CardTitle);
            for(int i=0;i<roles.Length;i++)if(U.Click(new Rect(60+(i%5)*142,125+(i/5)*36,134,29),roles[i]+(i==enemyRole?" *":"")))enemyRole=i;
            U.Label(60,206,150,24,"Variant element",U.Small);for(int i=0;i<elements.Length;i++)if(U.Click(new Rect(205+i*103,201,96,29),elements[i]+(i==enemyElement?" *":"")))enemyElement=i;
            eliteEnemy=GUI.Toggle(new Rect(845,205,125,24),eliteEnemy,"Elite override");
            var candidates=run.Catalog.enemies.Where(x=>x&&x.role==roles[enemyRole]&&(elements[enemyElement]==ElementTag.None?x.element==ElementTag.None:x.element==elements[enemyElement])).ToArray();
            GUI.enabled=candidates.Length>0;if(U.Click(new Rect(985,199,195,34),"Spawn role test")){run.DebugSpawnEnemy(candidates.FirstOrDefault(),eliteEnemy);result=candidates.Length>0?"Spawned "+candidates[0].displayName:"No authored combination";Mark();}GUI.enabled=true;
            U.Label(60,260,520,24,"ENCOUNTER PRESETS",U.CardTitle);if(run.Catalog.encounters.Length>0){encounterIndex=Mathf.Clamp(encounterIndex,0,run.Catalog.encounters.Length-1);for(int i=0;i<run.Catalog.encounters.Length;i++)if(U.Click(new Rect(60+(i%3)*205,294+(i/3)*36,196,29),run.Catalog.encounters[i].displayName))encounterIndex=i;var encounter=run.Catalog.encounters[encounterIndex];U.Label(60,375,565,70,encounter.intent+"\n"+encounter.difficulty+" · "+encounter.riskTier+" · "+encounter.requiredArenaSize,U.Small);if(U.Click(new Rect(60,445,220,34),"Spawn selected preset")){run.DebugSpawnEncounter(encounter);result="Loaded "+encounter.displayName;Mark();}}
            U.Label(650,260,520,24,"COMBAT SPACE",U.CardTitle);if(run.Catalog.combatSpaces.Length>0){spaceIndex=Mathf.Clamp(spaceIndex,0,run.Catalog.combatSpaces.Length-1);for(int i=0;i<run.Catalog.combatSpaces.Length;i++)if(U.Click(new Rect(650+(i%2)*250,294+(i/2)*36,240,29),run.Catalog.combatSpaces[i].displayName))spaceIndex=i;var space=run.Catalog.combatSpaces[spaceIndex];U.Label(650,408,520,60,space.category+" · "+space.layout+" · "+space.ScaledTechnicalBounds+"\nZoom "+space.cameraOrthographicSize.ToString("0.0")+"–"+space.cameraMaximumOrthographicSize.ToString("0.0")+" · "+space.spatialIntent,U.Small);if(U.Click(new Rect(650,475,220,34),"Load selected space")){run.DebugLoadCombatSpace(space);result="Loaded "+space.displayName;Mark();}}
            EnemyBrain.AiEnabled=GUI.Toggle(new Rect(60,525,140,26),EnemyBrain.AiEnabled,"Enemy AI");EnemyBrain.TelegraphsEnabled=GUI.Toggle(new Rect(210,525,160,26),EnemyBrain.TelegraphsEnabled,"Telegraphs");
            U.Label(60,570,1120,82,"ROLE READS\nWarrior baseline · Bruiser space control · Assassin flank · Ranger sustained shots · Mage AOE · Flyer dive windows · Burrower eruption · Bomber countdown/chain · Support capped aid · Controller short slow/pull\n"+result,U.Small);
        }
        private void RoutePanel()
        {
            U.Label(60,86,1120,25,"EXPEDITION ROUTE GRAPH",U.CardTitle);if(run.Route==null){U.Label(60,122,760,42,"No active graph. Start a deterministic local route to inspect nodes and services.",U.Small);if(U.Click(new Rect(850,110,330,36),"Start route · seed 606")){run.StartRun(606);result="Started deterministic route";Mark();}return;}
            U.Label(60,118,1120,46,run.Route.Graph.displayName+" · "+run.Route.Graph.id+" · "+run.GraphValidation+"\nCurrent: "+run.CurrentNode.Definition.id+" · Reveal score "+run.Route.RevealScore,U.Small);
            if(U.Click(new Rect(60,175,190,34),"Regenerate · reset seed")){run.ResetRun();run.StartRun(606);run.DebugOpen=true;result="Regenerated route seed 606";Mark();}if(U.Click(new Rect(260,175,155,34),"Reveal all")){run.DebugRevealRoute(true);result="Full route revealed";}if(U.Click(new Rect(425,175,155,34),"Limited reveal")){run.DebugRevealRoute(false);result="Limited route restored";}if(U.Click(new Rect(590,175,210,34),"+ run currency")){run.DebugAddRunCurrency();result="Added run currency";}
            U.Label(60,230,1120,24,"FORCE NODE TYPE",U.CardTitle);ExpeditionNodeType[] types={ExpeditionNodeType.NormalCombat,ExpeditionNodeType.HardCombat,ExpeditionNodeType.Elite,ExpeditionNodeType.Treasure,ExpeditionNodeType.Relic,ExpeditionNodeType.Merchant,ExpeditionNodeType.Rest,ExpeditionNodeType.Event,ExpeditionNodeType.Challenge,ExpeditionNodeType.Boss};for(int i=0;i<types.Length;i++){var type=types[i];if(U.Click(new Rect(60+(i%5)*218,265+(i/5)*38,208,30),type.ToString())){result=run.DebugForceNodeType(type)?"Loaded "+type:"Node unavailable";Mark();}}
            U.Label(60,355,1120,24,"FORCE TREASURE VARIANT",U.CardTitle);TreasureVariantKind[] variants=(TreasureVariantKind[])System.Enum.GetValues(typeof(TreasureVariantKind));for(int i=0;i<variants.Length;i++){var variant=variants[i];if(U.Click(new Rect(60+(i%3)*300,390+(i/3)*38,288,30),variant.ToString())){result=run.DebugForceTreasure(variant)?"Forced "+variant:"Treasure unavailable";Mark();}}
            U.Label(60,488,1120,24,"DIRECT NODE IDS",U.CardTitle);var nodes=run.Route.Nodes.OrderBy(x=>x.Definition.id).ToArray();for(int i=0;i<nodes.Length;i++){var node=nodes[i];string visibility=run.Route.Visibility(node).ToString();if(U.Click(new Rect(60+(i%3)*370,523+(i/3)*34,356,27),(node.Completed?"✓ ":"")+node.Definition.id+" · "+visibility)){run.DebugJumpToNode(node.Definition.id);result="Jumped to "+node.Definition.id;Mark();}}
            U.Label(60,657,1120,24,result+" · Graph variants validate on construction; debug telemetry is marked.",U.Small);
        }
        private void CameraPanel()
        {
            var camera=ArenaCamera.Instance;U.Label(60,86,1120,25,"FOLLOW CAMERA & WORLD SCALE",U.CardTitle);
            if(!camera){U.Label(60,125,900,40,"Arena camera is not available.",U.Small);return;}
            camera.FollowEnabled=GUI.Toggle(new Rect(60,125,190,26),camera.FollowEnabled,"Follow camera enabled");
            U.Label(60,164,330,24,"FOLLOW TARGET",U.CardTitle);
            CameraFollowMode[] follows=(CameraFollowMode[])System.Enum.GetValues(typeof(CameraFollowMode));for(int i=0;i<follows.Length;i++)if(U.Click(new Rect(60+i*185,198,175,31),follows[i]+(camera.FollowMode==follows[i]?" *":""))){camera.FollowMode=follows[i];Mark();}
            U.Label(650,164,330,24,"ZOOM OVERRIDE",U.CardTitle);
            CameraZoomOverride[] zooms=(CameraZoomOverride[])System.Enum.GetValues(typeof(CameraZoomOverride));for(int i=0;i<zooms.Length;i++)if(U.Click(new Rect(650+i*175,198,165,31),zooms[i]+(camera.ZoomOverride==zooms[i]?" *":""))){camera.ZoomOverride=zooms[i];Mark();}
            U.Label(60,255,330,24,"OVERLAYS",U.CardTitle);
            camera.DisplayPartyCentroid=GUI.Toggle(new Rect(60,292,190,25),camera.DisplayPartyCentroid,"Party centroid");camera.DisplayPartySpread=GUI.Toggle(new Rect(270,292,180,25),camera.DisplayPartySpread,"Party spread");camera.DisplayClampBounds=GUI.Toggle(new Rect(470,292,200,25),camera.DisplayClampBounds,"Camera clamp bounds");
            if(U.Click(new Rect(700,285,175,34),"Snap to targets")){camera.SnapToTargets();Mark();}
            if(run.Players.Count==0&&U.Click(new Rect(895,285,285,34),"Start camera test · seed 606")){run.StartRun(606);result="Started camera test route";Mark();}
            U.Label(60,350,1120,25,"ENLARGED TEST SPACES",U.CardTitle);
            var medium=run.Catalog.combatSpaces.FirstOrDefault(x=>x.category==CombatSpaceCategory.Medium);var large=run.Catalog.combatSpaces.FirstOrDefault(x=>x.category==CombatSpaceCategory.Large);
            GUI.enabled=run.Players.Count>0&&medium;if(U.Click(new Rect(60,386,330,38),"Load enlarged Medium")){run.DebugLoadCombatSpace(medium);camera.SnapToTargets();result="Loaded "+medium.displayName;Mark();}GUI.enabled=run.Players.Count>0&&large;if(U.Click(new Rect(410,386,330,38),"Load enlarged Large")){run.DebugLoadCombatSpace(large);camera.SnapToTargets();result="Loaded "+large.displayName;Mark();}GUI.enabled=true;
            var bounds=camera.ClampBounds;float soft=run.Rooms&&run.Rooms.ActiveCombatSpace?run.Rooms.ActiveCombatSpace.multiplayerSeparationLimit:0;
            U.Label(60,465,550,130,"Context: "+camera.Context+"\nFollow: "+camera.FollowMode+" · Zoom: "+camera.ZoomOverride+"\nFocus: "+camera.FocusPoint.ToString("F1")+"\nCentroid: "+camera.PartyCentroid.ToString("F1")+"\nSpread: "+camera.PartySpread.ToString("0.0")+" / "+soft.ToString("0.0")+(camera.PartyBeyondSoftLimit?"  REGROUP HOOK ACTIVE":""),U.Small);
            U.Label(650,465,530,130,"Zoom: "+camera.CurrentZoom.ToString("0.0")+"  ["+camera.MinimumZoom.ToString("0.0")+"–"+camera.MaximumZoom.ToString("0.0")+"]\nClamp: "+bounds.xMin.ToString("0.0")+","+bounds.yMin.ToString("0.0")+" → "+bounds.xMax.ToString("0.0")+","+bounds.yMax.ToString("0.0")+"\nEdge indicators: "+camera.OffscreenIndicatorCount+"\nNo hard party teleport is used; soft-spread exposes a regroup event hook.",U.Small);
            U.Label(60,625,1120,45,result+" · Follow, centroid, spread, zoom limits, clamp bounds, Medium and Large spaces can be tested here.",U.Small);
        }
    }
}
