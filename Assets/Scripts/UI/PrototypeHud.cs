using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U = Ashbound.PrototypeGui;

namespace Ashbound
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private RunManager run;
        private Camera view;
        private bool journalOpen;
        private int buildPlayer;
        private int hubFacility,routeVotePlayer,merchantPlayer,restPlayer;
        private string hubMessage="Choose where the recovered materials should go.";
        public void Configure(RunManager manager, Camera camera) { run = manager; view = camera; }
        private void Update()
        {
            if (!run) return;
            if (journalOpen && !run.ManualPaused) journalOpen = false;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && run.Flow.State != RunState.Lobby)
            { journalOpen = !journalOpen; run.ManualPaused = journalOpen; }
            if (run.Flow.State == RunState.Lobby)
            {
                journalOpen = false;
                return;
            }
            if(run.RouteSelectionOpen&&!run.DebugOpen&&!run.ManualPaused)
            {
                int routeChoice=-1;if(keyboard!=null){if(keyboard.digit1Key.wasPressedThisFrame)routeChoice=0;else if(keyboard.digit2Key.wasPressedThisFrame)routeChoice=1;else if(keyboard.digit3Key.wasPressedThisFrame)routeChoice=2;}
                if(routeChoice>=0&&routeChoice<run.Route.Available.Count){routeVotePlayer=Mathf.Clamp(routeVotePlayer,0,run.Players.Count-1);run.CastRouteVote(run.Players[routeVotePlayer].Id,run.Route.Available[routeChoice].Definition.id);routeVotePlayer=Mathf.Min(run.Players.Count-1,routeVotePlayer+1);}
                foreach(var routeSlot in run.Lobby.Slots.Where(x=>x.InputKind==InputKind.Gamepad)){var pad=Gamepad.all.FirstOrDefault(x=>x.deviceId==routeSlot.DeviceId);if(pad==null)continue;int padChoice=pad.buttonSouth.wasPressedThisFrame?0:pad.buttonEast.wasPressedThisFrame?1:pad.buttonNorth.wasPressedThisFrame?2:-1;if(padChoice>=0&&padChoice<run.Route.Available.Count)run.CastRouteVote(routeSlot.PlayerId,run.Route.Available[padChoice].Definition.id);}
                return;
            }
            if (run.Flow.State != RunState.Reward || run.DebugOpen || run.ManualPaused) return;
            if(!run.Draft.Active)
            {
                if(!run.EquipmentRewards.Active)return;var reward=run.EquipmentRewards;var rewardSlot=run.Lobby.Slots.FirstOrDefault(s=>s.PlayerId==reward.CurrentPlayer.Id);if(rewardSlot!=null&&rewardSlot.InputKind==InputKind.Gamepad){var rewardPad=Gamepad.all.FirstOrDefault(p=>p.deviceId==rewardSlot.DeviceId);if(rewardPad!=null){if(rewardPad.buttonSouth.wasPressedThisFrame)reward.Equip(0);else if(rewardPad.buttonEast.wasPressedThisFrame&&reward.Options.Length>1)reward.Equip(1);else if(rewardPad.buttonNorth.wasPressedThisFrame)reward.Dismantle(0);else if(rewardPad.selectButton.wasPressedThisFrame)reward.Leave();}}return;
            }
            int choice = -1;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) choice = 0;
                else if (keyboard.digit2Key.wasPressedThisFrame) choice = 1;
                else if (keyboard.digit3Key.wasPressedThisFrame) choice = 2;
                else if (keyboard.rKey.wasPressedThisFrame) run.Draft.Reroll();
            }
            var slot = run.Lobby.Slots.FirstOrDefault(s => s.PlayerId == run.Draft.CurrentPlayer.Id);
            if (slot != null && slot.InputKind == InputKind.Gamepad)
            {
                var pad = Gamepad.all.FirstOrDefault(p => p.deviceId == slot.DeviceId);
                if (pad != null)
                {
                    if (pad.buttonSouth.wasPressedThisFrame) choice = 0;
                    else if (pad.buttonEast.wasPressedThisFrame) choice = 1;
                    else if (pad.buttonNorth.wasPressedThisFrame) choice = 2;
                }
            }
            if (choice >= 0) run.Draft.Choose(choice);
        }
        private void OnGUI()
        {
            if (!run) return;
            GUI.depth = 0; Matrix4x4 old = U.Scale();
            try
            {
                if (run.Flow.State == RunState.Lobby) return;
                WorldLabels(); Hud();
                if(run.RouteSelectionOpen)RouteMap();
                else if(run.Treasure!=null&&!run.Treasure.Completed)Treasure();
                else if(run.Merchant!=null&&!run.Merchant.Closed)Merchant();
                else if(run.Rest!=null&&!run.Rest.Completed)Rest();
                else if(run.Event!=null&&!run.Event.Completed)Event();
                else if (run.Flow.State == RunState.Reward && run.Draft.Active) Reward();
                else if(run.Flow.State==RunState.Reward&&run.EquipmentRewards.Active)EquipmentReward();
                if (run.Flow.State == RunState.BossDefeated || run.Flow.State == RunState.CorruptionTransition)
                {
                    U.Box(new Rect(0, 0, 1280, 720), new Color(.035f, .025f, .045f, .65f));
                    U.Label(260, 275, 760, 150, run.Message, U.Center);
                }
                if (run.Flow.State == RunState.RunComplete) Complete();
                if (journalOpen && run.ManualPaused) Journal();
                else if (run.ManualPaused && !run.DebugOpen) Pause();
                if (!string.IsNullOrEmpty(run.MissingDevice))
                {
                    U.Panel(new Rect(320, 245, 640, 210));
                    U.Label(350, 270, 580, 90, run.MissingDevice + " controller disconnected.\nReconnect the same controller to resume. The roster is locked.", U.Center);
                    if (U.Click(new Rect(505, 380, 270, 46), "Return to lobby")) run.ResetRun();
                }
            }
            finally { GUI.matrix = old; }
        }
        private void Lobby()
        {
            U.Box(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .05f, .72f));
            U.Label(45,25,700,48,"A S H B O U N D  ·  EXPEDITION HUB",U.Heading);U.Label(45,69,900,25,run.Progression.Profile.currencies.ToString(),U.CardTitle);
            var facilities=run.Catalog.facilities;for(int i=0;i<facilities.Length;i++)if(U.Click(new Rect(45+i*198,105,188,31),facilities[i].displayName))hubFacility=i;hubFacility=Mathf.Clamp(hubFacility,0,facilities.Length-1);var facility=facilities[hubFacility];var progress=run.Progression.Profile.Facility(facility.id);
            U.Panel(new Rect(45,150,770,390));U.Label(70,170,720,35,facility.displayName+"  ·  LEVEL "+progress.level+" / "+facility.MaxLevel,U.Heading);U.Label(70,214,720,42,facility.description,U.Small);
            if(facility.kind==HubFacilityKind.ExpeditionTable)
            {
                var stats=run.Progression.Profile.lifetime;U.Label(70,270,710,60,"Expeditions "+stats.expeditionsStarted+"  ·  Completed "+stats.expeditionsCompleted+"  ·  Failed "+stats.expeditionsFailed+"\nHighest region "+stats.highestRegionReached+"  ·  Encounter "+stats.highestEncounterReached+"  ·  Bosses "+stats.bossesDefeated,U.Small);
                U.Label(70,342,700,25,"ONE PREPARATION",U.CardTitle);int prep=0;foreach(var definition in run.Catalog.preparations){bool prepAvailable=run.Progression.PreparationAvailable(definition);GUI.enabled=prepAvailable;if(U.Click(new Rect(70+(prep%2)*350,375+(prep/2)*39,335,31),(run.Progression.Profile.selectedPreparation==definition.id?"✓ ":"")+definition.displayName))run.Progression.SelectPreparation(definition);GUI.enabled=true;prep++;}
            }
            else if(facility.kind==HubFacilityKind.Archive)
            {U.Label(70,275,710,180,run.Progression.Profile.discoveredLore.Count==0?"No recovered notes. The Archive remains optional.":"Recovered: "+string.Join("  ·  ",run.Progression.Profile.discoveredLore)+"\nBoss observations: "+string.Join("  ·  ",run.Progression.Profile.defeatedBosses),U.Small);}
            if(progress.level<facility.MaxLevel){var tier=facility.tiers[progress.level];U.Label(70,455,520,48,"NEXT · "+tier.displayName+"\n"+tier.description,U.Small);U.Label(600,448,185,28,tier.cost.ToString(),U.Small);if(U.Click(new Rect(600,482,185,32),"Upgrade")){hubMessage=run.Progression.TryUpgrade(facility,out string reason)?"Upgraded "+tier.displayName:reason;}}
            else U.Label(70,475,700,30,"Facility prototype complete.",U.CardTitle);
            U.Label(70,515,710,20,hubMessage,U.Small);

            U.Panel(new Rect(835,150,400,390));U.Label(860,170,350,32,"LOCAL PARTY",U.Heading);
            for(int i=0;i<4;i++){U.Box(new Rect(860,215+i*43,350,35),new Color(.12f,.15f,.19f));U.Label(872,220+i*43,325,25,i<run.Lobby.Slots.Count?run.Lobby.Slots[i].PlayerId+" · "+run.Lobby.Slots[i].DeviceLabel:"— Empty slot",U.Small);}
            GUI.enabled = run.Lobby.Slots.Count < 4 && run.Lobby.Slots.All(s => s.InputKind != InputKind.SecondKeyboard);
            if (U.Click(new Rect(860,395,215,34), "Add second keyboard")) run.Lobby.TryJoin(InputKind.SecondKeyboard, -2, "Shared keyboard · arrows / IJKL");
            GUI.enabled = run.Lobby.Slots.Count > 1;
            if (U.Click(new Rect(1082,395,128,34), "Remove last")) run.Lobby.RemoveLast();
            GUI.enabled = true;
            var available = Gamepad.all.FirstOrDefault(p => run.Lobby.Slots.All(s => s.InputKind != InputKind.Gamepad || s.DeviceId != p.deviceId));
            GUI.enabled = available != null && run.Lobby.Slots.Count < 4;
            if (U.Click(new Rect(860,440,350,34), available == null ? "Connect a gamepad" : "Add gamepad · " + available.displayName))
                run.Lobby.TryJoin(InputKind.Gamepad, available.deviceId, available.displayName);
            GUI.enabled = true;
            if(U.Click(new Rect(835,565,400,58),run.Lobby.Slots.Count==1?"LAUNCH EXPEDITION · SOLO":"LAUNCH · "+run.Lobby.Slots.Count+" PLAYERS"))run.StartRun();
            U.Label(45,570,740,92,"Selected: "+(run.Catalog.preparations.FirstOrDefault(x=>x.id==run.Progression.Profile.selectedPreparation)?.displayName??"None")+"\nPermanent HP cap: 8% · Current: "+(run.Progression.EffectPower(MetaEffectKind.MaxHealth)*100).ToString("0")+"%\nF1 progression/debug tools · Save: "+run.Progression.SavePath,U.Small);
            U.Label(45,665,1180,30,"Hub progress uses one local host profile. Run weapons, armor, relics and corruption still reset between expeditions.",U.Small);
        }
        private void Hud()
        {
            U.Panel(new Rect(18, 16, 350, 58));
            U.Label(32, 23, 320, 24, run.LocationName, U.CardTitle);
            string stage = run.ChallengeActive?LocalizationService.Text("hud.challenge","CHALLENGE")+" · "+run.ChallengeRemaining.ToString("0.0")+"s · "+run.Rooms.RemainingEnemies+" "+LocalizationService.Text("hud.remaining","remaining"):run.Flow.State == RunState.Combat ? run.Rooms.RemainingEnemies + " "+LocalizationService.Text("hud.hostiles","hostiles remaining") :
                run.Flow.State == RunState.FinalPvP ? LocalizationService.Text("hud.inheritance","THE INHERITANCE") : run.Flow.State == RunState.Reward ? LocalizationService.Text("hud.chooseRelic","Choose a relic") : "The Cinder Vault";
            U.Label(32, 48, 320, 20, stage, U.Small);
            U.Panel(new Rect(1030, 16, 232, 58));
            U.Label(1043, 25, 210, 32, TimeSpan.FromSeconds(run.Telemetry.Record?.runDuration ?? 0).ToString(@"mm\:ss") + "  ·  "+LocalizationService.Text("hud.pause","Esc pause"), U.Text);
            U.Label(385,75,520,23,LocalizationService.Text("hud.materials","EXPEDITION MATERIALS")+" · "+LocalizationService.Wallet(run.Progression.RunResources),U.Small);
            if (run.Rooms.Boss && run.Rooms.Boss.Alive)
            {
                var boss = run.Rooms.Boss;
                U.Label(410, 16, 570, 27, boss.DisplayName + (boss.GetComponent<CinderRegentController>().SecondPhase ? " · "+LocalizationService.Text("state.kindled","KINDLED") : ""), U.Center);
                U.Bar(new Rect(410, 48, 570, 12), boss.Health.CurrentHealth / boss.Health.MaxHealth, Palette.Danger);
            }
            if (run.Flow.State == RunState.Exploration || run.Flow.State == RunState.FinalPvP)
                U.Label(300, 90, 680, 64, run.Message, U.Center);
            int count = run.Players.Count;
            for (int i = 0; i < count; i++)
            {
                var player = run.Players[i]; float x = 20 + i * 310;
                U.Panel(new Rect(x, 594, 300, 104));
                U.Label(x + 12, 602, 278, 28, player.Id + " · "+(player.Corruption ? LocalizationService.Text("state.ashbound","ASHBOUND") : LocalizationService.Text("state.wanderer","WANDERER")), U.CardTitle);
                Color tint = player.Corruption ? Palette.Corrupted : Palette.Party[i];
                U.Bar(new Rect(x + 12, 633, 276, 10), player.Health.CurrentHealth / player.Health.MaxHealth, tint);
                U.Label(x + 12, 649, 279, 23, player.Alive ? Math.Ceiling(player.Health.CurrentHealth) + " / " + Math.Ceiling(player.Health.MaxHealth) + " HP" + (player.Health.Pool.Shield > 0 ? "  +" + Math.Ceiling(player.Health.Pool.Shield) + " "+LocalizationService.Text("hud.shield","shield") : "") : LocalizationService.Text("hud.down","Down · returns at the next reward"), U.Small);
                U.Label(x + 12, 674, 278, 22, LocalizationService.Rarity(player.Weapon.rarity)+" "+LocalizationService.Weapon(player.Weapon.family)+" · "+LocalizationService.Element(player.Weapon.PrimaryElement)+" · "+LocalizationService.Text("hud.relics","Relics")+" "+player.Inventory.Items.Count, U.Small);
            }
            if (count == 1) U.Label(345, 639, 720, 44, LocalizationService.Text("hud.controls","LMB strike · Space dash · E ward / burst · F interact\nTab build & fragments · F1 developer tools"), U.Small);
        }
        private static string Ready(float seconds) => seconds <= 0 ? "ready" : seconds.ToString("0.0") + "s";

        private void RouteMap()
        {
            var route=run.Route;U.Box(new Rect(0,0,1280,720),new Color(.018f,.025f,.04f,.92f));U.Label(75,40,1130,44,route.Region.displayName+" · "+LocalizationService.Text("route.vote","ROUTE VOTE"),U.Title);U.Label(75,91,1130,44,route.Graph.displayName+" · "+route.Graph.tieBehavior+" · "+run.GraphValidation,U.Small);
            U.Label(75,139,1130,25,LocalizationService.Text("route.current","CURRENT")+" · "+route.Current.Definition.displayName+"  /  "+LocalizationService.Node(route.Current.Definition.nodeType),U.CardTitle);
            for(int i=0;i<run.Players.Count;i++){bool voted=route.Votes.ContainsKey(run.Players[i].Id);if(U.Click(new Rect(75+i*145,178,135,31),run.Players[i].Id+(voted?" ✓":"")+(i==routeVotePlayer?" *":"")))routeVotePlayer=i;}
            U.Label(75,218,1130,25,run.Players[Mathf.Clamp(routeVotePlayer,0,run.Players.Count-1)].Id+" · "+LocalizationService.Text("route.choose","keys 1/2/3 or that player's gamepad A/B/Y"),U.Small);
            for(int i=0;i<route.Available.Count;i++)
            {
                var node=route.Available[i];float x=75+i*390;U.Panel(new Rect(x,255,365,215));U.Label(x+18,274,329,24,(i+1)+" · "+LocalizationService.Node(node.Definition.nodeType),U.Small);U.Label(x+18,311,329,35,node.Definition.displayName,U.CardTitle);U.Label(x+18,353,329,48,node.Definition.risk+" "+LocalizationService.Text("route.risk","risk")+" · "+node.Definition.rewardCategory,U.Small);U.Label(x+18,404,329,26,route.Votes.Count(v=>v.Value==node.Definition.id)+" "+LocalizationService.Text("route.votes","vote(s)"),U.Small);
                if(U.Click(new Rect(x+18,435,329,28),LocalizationService.Text("route.button","Vote for this route"))){run.CastRouteVote(run.Players[Mathf.Clamp(routeVotePlayer,0,run.Players.Count-1)].Id,node.Definition.id);routeVotePlayer=Mathf.Min(run.Players.Count-1,routeVotePlayer+1);}
            }
            U.Label(75,500,1130,26,LocalizationService.Text("route.intelligence","ROUTE INTELLIGENCE · visible one layer beyond the current decision"),U.CardTitle);int shown=0;foreach(var node in route.Nodes.OrderBy(x=>x.Definition.id)){var visibility=route.Visibility(node);if(visibility==RouteVisibilityState.Hidden)continue;string text=visibility==RouteVisibilityState.Obscured?"? · "+node.Definition.risk+" "+LocalizationService.Text("route.risk","risk"):(node.Completed?"✓ ":node==route.Current?"◆ ":"○ ")+LocalizationService.Node(node.Definition.nodeType)+" · "+node.Definition.displayName;U.Box(new Rect(75+(shown%4)*285,540+(shown/4)*40,270,32),node.Completed?new Color(.12f,.26f,.2f):new Color(.11f,.14f,.2f));U.Label(84+(shown%4)*285,546+(shown/4)*40,250,21,text,U.Small);shown++;}
        }
        private void Treasure()
        {
            var session=run.Treasure;var variant=session.Variant;U.Box(new Rect(0,0,1280,720),new Color(.03f,.02f,.045f,.9f));U.Panel(new Rect(285,105,710,500));U.Label(325,135,630,46,variant.displayName.ToUpperInvariant(),U.Title);U.Label(325,195,630,75,variant.description,U.Text);U.Label(325,285,630,55,"Quality "+variant.rewardQuality+" · Rewards taken "+session.RewardsTaken+(variant.openCost.Empty?"":"\nOpen cost · "+variant.openCost),U.Small);U.Label(325,355,630,42,session.Message,U.CardTitle);
            if(!session.Opened){if(U.Click(new Rect(325,430,300,44),"Open cache"))run.OpenTreasure();if(U.Click(new Rect(655,430,300,44),"Leave sealed"))run.LeaveTreasure();}
            else{GUI.enabled=session.CanContinueGreed;if(U.Click(new Rect(325,430,300,44),"Take another reward"))run.ContinueTreasure();GUI.enabled=true;if(U.Click(new Rect(655,430,300,44),"Leave cache"))run.LeaveTreasure();}
            U.Label(325,515,630,45,"Costs use the expedition wallet or bounded current health. A health cost cannot down a player.",U.Small);
        }
        private void Merchant()
        {
            var merchant=run.Merchant;merchantPlayer=Mathf.Clamp(merchantPlayer,0,run.Players.Count-1);U.Box(new Rect(0,0,1280,720),new Color(.025f,.03f,.04f,.92f));U.Label(70,45,1140,45,"EMBER QUARTERMASTER",U.Title);U.Label(70,96,1140,30,"Run wallet · "+run.Progression.RunResources,U.CardTitle);for(int i=0;i<run.Players.Count;i++)if(U.Click(new Rect(70+i*130,137,120,30),run.Players[i].Id+(i==merchantPlayer?" *":"")))merchantPlayer=i;
            for(int i=0;i<merchant.Offers.Length;i++){var offer=merchant.Offers[i];float x=70+(i%3)*380,y=190+(i/3)*190;U.Panel(new Rect(x,y,360,170));U.Label(x+16,y+16,328,24,offer.Kind+" · "+offer.Rarity,U.Small);U.Label(x+16,y+48,328,36,offer.DisplayName,U.CardTitle);U.Label(x+16,y+90,328,24,offer.Sold?"SOLD":"Price · "+offer.Price,U.Small);GUI.enabled=!offer.Sold;if(U.Click(new Rect(x+16,y+124,328,31),offer.Sold?"Unavailable":"Buy for "+run.Players[merchantPlayer].Id))run.BuyMerchantOffer(i,merchantPlayer);GUI.enabled=true;}
            GUI.enabled=merchant.RerollsUsed<merchant.MaximumRerolls;if(U.Click(new Rect(330,600,280,38),"Reroll · "+merchant.RerollPrice()))run.RerollMerchant();GUI.enabled=true;if(U.Click(new Rect(670,600,280,38),"Leave merchant"))run.LeaveMerchant();
        }
        private void Rest()
        {
            restPlayer=Mathf.Clamp(restPlayer,0,run.Players.Count-1);U.Box(new Rect(0,0,1280,720),new Color(.025f,.035f,.035f,.92f));U.Panel(new Rect(235,95,810,535));U.Label(275,125,730,46,"QUIET BRAZIER",U.Title);U.Label(275,180,730,48,"Choose exactly one action for the party or selected player's equipment.",U.Small);for(int i=0;i<run.Players.Count;i++)if(U.Click(new Rect(275+i*125,235,115,30),run.Players[i].Id+(i==restPlayer?" *":"")))restPlayer=i;
            if(U.Click(new Rect(275,295,230,54),"Rest · party recovery"))run.ChooseRest(RestNodeChoice.Rest,restPlayer);if(U.Click(new Rect(525,295,230,54),"Temper weapon"))run.ChooseRest(RestNodeChoice.TemperWeapon,restPlayer);if(U.Click(new Rect(775,295,230,54),"Salvage / repair"))run.ChooseRest(RestNodeChoice.Salvage,restPlayer);
            U.Label(275,385,730,24,"TEMPER ARMOR SLOT",U.CardTitle);ArmorSlot[] slots={ArmorSlot.Head,ArmorSlot.Chest,ArmorSlot.Gloves,ArmorSlot.Boots};for(int i=0;i<slots.Length;i++)if(U.Click(new Rect(275+i*180,425,165,38),slots[i].ToString()))run.ChooseRest(RestNodeChoice.TemperArmor,restPlayer,slots[i]);U.Label(275,510,730,55,run.Rest.Message??"Rest recovery is bounded; Temper improves one item by one rarity step up to the node cap.",U.Small);
        }
        private void Event()
        {
            var current=run.Event;U.Box(new Rect(0,0,1280,720),new Color(.035f,.025f,.04f,.92f));U.Label(90,55,1100,45,current.Definition.displayName.ToUpperInvariant(),U.Title);U.Label(90,112,1100,55,current.Definition.description,U.Text);for(int i=0;i<current.Definition.choices.Length;i++){var choice=current.Definition.choices[i];float x=90+i*370;U.Panel(new Rect(x,210,350,285));U.Label(x+18,230,314,38,choice.displayName,U.CardTitle);string outcome=choice.outcomeInitiallyHidden?"Outcome: uncertain":"Outcome: "+choice.outcome;U.Label(x+18,280,314,95,outcome+(choice.cost.Empty?"":"\nCost · "+choice.cost)+(choice.currentHealthCost>0?"\nCurrent health cost · "+(choice.currentHealthCost*100).ToString("0")+"%":""),U.Small);if(U.Click(new Rect(x+18,435,314,38),"Choose")){run.ChooseEvent(i);break;}}U.Label(90,535,1100,54,current.Message??"Choices may grant resources, recovery, information, equipment, relics, or a combat escalation.",U.Small);
        }
        private void WorldLabels()
        {
            foreach (var actor in run.Combat.Actors)
            {
                if (!actor || !actor.Alive || actor.IsBoss) continue;
                Vector3 screen = view.WorldToScreenPoint(actor.transform.position + Vector3.up * 2.2f);
                if (screen.z < 0) continue;
                float x = screen.x * 1280 / Screen.width, y = 720 - screen.y * 720 / Screen.height;
                if (actor.IsPlayer) U.Label(x - 20, y - 12, 80, 22, actor.Id, U.Small);
                else U.Bar(new Rect(x - 26, y, 52, 5), actor.Health.CurrentHealth / actor.Health.MaxHealth, actor.Corruption ? Palette.Corrupted : Palette.Danger);
                int bleed = actor.Statuses.StackCount(StatusKind.Bleed);
                if (bleed > 0) U.Label(x + 30, y - 10, 80, 25, "BLEED " + bleed, U.Small);
            }
            foreach (var fragment in FindObjectsByType<LoreFragment>())
            {
                Vector3 screen = view.WorldToScreenPoint(fragment.transform.position + Vector3.up);
                U.Label(screen.x * 1280 / Screen.width - 65, 720 - screen.y * 720 / Screen.height, 190, 22, "Fragment · interact", U.Small);
            }
            foreach (var pickup in FindObjectsByType<ItemPickup>())
            {
                Vector3 screen = view.WorldToScreenPoint(pickup.transform.position + Vector3.up);
                U.Label(screen.x * 1280 / Screen.width - 70, 720 - screen.y * 720 / Screen.height, 200, 24, pickup.Item.displayName, U.Small);
            }
        }
        private void Reward()
        {
            U.Box(new Rect(0, 0, 1280, 720), new Color(.02f, .03f, .045f, .8f));
            U.Label(210, 135, 860, 54, "A RELIC IN THE ASHES", U.Title);
            U.Label(210, 200, 860, 38, run.Draft.CurrentPlayer.Id + " · Choose one. Your build carries forward.");
            for (int i = 0; i < run.Draft.Options.Length; i++)
            {
                var item = run.Draft.Options[i]; float x = 210 + 295 * i;
                U.Panel(new Rect(x, 260, 275, 269));
                U.Label(x + 18, 277, 236, 25, item.rarity.ToString().ToUpperInvariant() + "  /  " + (i + 1), U.Small);
                U.Label(x + 18, 313, 236, 55, item.displayName, U.CardTitle);
                U.Label(x + 18, 373, 236, 79, item.description);
                U.Label(x + 18, 455, 236, 30, string.Join(" · ", item.tags), U.Small);
                if (!string.IsNullOrEmpty(item.requiredItemId)) U.Label(x + 18, 475, 236, 18, "Requires: " + item.requiredItemId, U.Small);
                if (U.Click(new Rect(x + 18, 489, 237, 31), "Take relic  [" + (i + 1) + "]")) { run.Draft.Choose(i); break; }
            }
            U.Label(210, 555, 860, 40, "Click or press 1 / 2 / 3. Current player's gamepad: A / B / Y.\nEach player chooses in turn. Health restored at this checkpoint.", U.Small);
            GUI.enabled=run.Draft.RerollsRemaining>0;if(U.Click(new Rect(505,610,270,34),"Reroll · "+run.Draft.RerollsRemaining+" remaining"))run.Draft.Reroll();GUI.enabled=true;
        }
        private void EquipmentReward()
        {
            var draft=run.EquipmentRewards;U.Box(new Rect(0,0,1280,720),new Color(.02f,.03f,.045f,.84f));U.Label(210,85,860,54,"EXPEDITION EQUIPMENT",U.Title);U.Label(210,150,860,40,draft.CurrentPlayer.Id+" · Equip, dismantle into run materials, or leave. Gear resets after the expedition.");
            for(int i=0;i<draft.Options.Length;i++)
            {
                var option=draft.Options[i];float width=draft.Options.Length>2?340:375;float x=draft.Options.Length>2?100+i*360:250+i*400;U.Panel(new Rect(x,215,width,350));U.Label(x+20,235,width-40,24,option.Rarity.ToString().ToUpperInvariant()+" · "+(option.IsWeapon?"WEAPON":"ARMOR"),U.Small);U.Label(x+20,270,width-40,40,option.DisplayName,U.CardTitle);
                string identity=option.IsWeapon?option.Weapon.family+" · "+option.Element+"\nDamage "+option.Weapon.damage+" · Skill "+(option.Weapon.skill?option.Weapon.skill.displayName:"Locked / none") : option.Armor.slot+" · "+option.Element+" · "+(option.Armor.set?option.Armor.set.displayName:"No set");
                string current=option.IsWeapon?"Current: "+draft.CurrentPlayer.Weapon.displayName+" · "+draft.CurrentPlayer.Weapon.damage+" damage":"Current: "+(draft.CurrentPlayer.Equipment.InSlot(option.Armor.slot)?draft.CurrentPlayer.Equipment.InSlot(option.Armor.slot).displayName:"Empty "+option.Armor.slot);
                U.Label(x+20,325,width-40,92,identity+"\n\n"+current,U.Small);var salvage=ProgressionEconomy.Salvage(option.Rarity,option.IsWeapon,run.Progression.EffectPower(MetaEffectKind.SalvageYield));U.Label(x+20,430,width-40,35,"Salvage: "+salvage,U.Small);
                if(U.Click(new Rect(x+20,480,(width-55)/2,38),"Equip")){draft.Equip(i);break;}if(U.Click(new Rect(x+30+(width-55)/2,480,(width-55)/2,38),"Dismantle")){draft.Dismantle(i);break;}
            }
            if(U.Click(new Rect(505,595,270,38),"Leave equipment"))draft.Leave();U.Label(260,650,760,30,"Gamepad: A equip first · B equip second · Y dismantle first · Select leave",U.Small);
        }
        private void Complete()
        {
            U.Panel(new Rect(285, 165, 710, 377));
            U.Label(315, 193, 650, 45, run.Outcome, U.Heading);
            U.Label(315, 255, 650, 70, "Run " + TimeSpan.FromSeconds(run.Telemetry.Record.runDuration).ToString(@"mm\:ss") + "   ·   Seed " + run.Seed + "\nWinner: " + run.Telemetry.Record.winner);
            var settlement=run.Progression.LastSettlement;string resources=settlement.HasValue?"Collected: "+settlement.Value.Collected+"\nRetained: "+settlement.Value.Retained+"\nLost: "+settlement.Value.Lost:"No resource settlement.";
            U.Label(315, 330, 650, 110, resources+"\n"+(run.Telemetry.LastError != null ? "Telemetry save failed: " + run.Telemetry.LastError : "Telemetry saved: "+run.Telemetry.LastPath), U.Small);
            if (U.Click(new Rect(315, 451, 650, 52), "RETURN TO THE THRESHOLD")) { journalOpen = false; run.ResetRun(); }
        }
        private void Pause()
        {
            U.Panel(new Rect(390, 215, 500, 285));
            U.Label(420, 240, 440, 40, "PAUSED", U.Heading);
            U.Label(420, 297, 440, 60, "Esc resumes. Tab opens builds and collected fragments.");
            if (U.Click(new Rect(420, 371, 440, 44), "Resume")) run.ManualPaused = false;
            if (U.Click(new Rect(420, 433, 440, 44), "Abandon run")) run.ResetRun();
        }
        private void Journal()
        {
            U.Panel(new Rect(180, 110, 920, 470));
            U.Label(210, 130, 860, 40, "BUILD & FRAGMENTS", U.Heading);
            for (int i = 0; i < run.Players.Count; i++) if (U.Click(new Rect(210 + i * 105, 185, 95, 32), run.Players[i].Id)) buildPlayer = i;
            var player = run.Players[Mathf.Clamp(buildPlayer, 0, run.Players.Count - 1)];
            string weapon=player.Weapon.displayName+"\n"+player.Weapon.rarity+" "+player.Weapon.family+" · "+player.Weapon.PrimaryElement+"\nSkill: "+(player.Weapon.skill?player.Weapon.skill.displayName:"None")+"\nTags: "+string.Join(" · ",player.Weapon.tags);
            string equipment=player.Equipment.Equipped.Count==0?"No armor equipped.":string.Join("\n",player.Equipment.Equipped.Select(x=>x.Key+": "+x.Value.displayName));
            string sets=string.Join(" · ",player.Equipment.ActiveBonuses().Select(x=>x.Set.displayName+" "+x.Tier.pieces));
            U.Label(210, 225, 450, 310, weapon+"\n\n"+equipment+(string.IsNullOrEmpty(sets)?"":"\nSets: "+sets)+"\n\n"+(player.Inventory.Items.Count==0?"No relics yet.":string.Join("\n",player.Inventory.Items.Select(x=>x.displayName+" · "+string.Join("/",x.tags)))), U.Small);
            U.Label(700, 186, 365, 350, run.Journal.Count == 0 ? "Fragments await in the vault." : string.Join("\n\n", run.Journal.Select(x => x.title + "\n" + x.text)), U.Small);
            if (U.Click(new Rect(210, 519, 850, 36), "Close · Tab")) { journalOpen = false; run.ManualPaused = false; }
        }
    }
}
