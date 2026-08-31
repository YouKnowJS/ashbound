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
        private int hubFacility;
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
                if (Gamepad.all.Any(p => p.startButton.wasPressedThisFrame)) run.StartRun();
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
                if (run.Flow.State == RunState.Lobby) { Lobby(); return; }
                WorldLabels(); Hud();
                if (run.Flow.State == RunState.Reward && run.Draft.Active) Reward();
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
            U.Label(32, 23, 320, 24, run.Rooms.Current.displayName, U.CardTitle);
            string stage = run.Flow.State == RunState.Combat ? "Wave " + (run.Rooms.WaveIndex + 1) + " / " + run.Rooms.Current.waves.Length + "  ·  " + run.Rooms.RemainingEnemies + " remaining" :
                run.Flow.State == RunState.FinalPvP ? "THE INHERITANCE" : run.Flow.State == RunState.Reward ? "Choose a relic" : "The Cinder Vault";
            U.Label(32, 48, 320, 20, stage, U.Small);
            U.Panel(new Rect(1030, 16, 232, 58));
            U.Label(1043, 25, 210, 32, TimeSpan.FromSeconds(run.Telemetry.Record?.runDuration ?? 0).ToString(@"mm\:ss") + "  ·  Esc pause", U.Text);
            U.Label(385,75,520,23,"EXPEDITION MATERIALS · "+run.Progression.RunResources,U.Small);
            if (run.Rooms.Boss && run.Rooms.Boss.Alive)
            {
                var boss = run.Rooms.Boss;
                U.Label(410, 16, 570, 27, boss.DisplayName + (boss.GetComponent<CinderRegentController>().SecondPhase ? " · KINDLED" : ""), U.Center);
                U.Bar(new Rect(410, 48, 570, 12), boss.Health.CurrentHealth / boss.Health.MaxHealth, Palette.Danger);
            }
            if (run.Flow.State == RunState.Exploration || run.Flow.State == RunState.FinalPvP)
                U.Label(300, 90, 680, 64, run.Message, U.Center);
            int count = run.Players.Count;
            for (int i = 0; i < count; i++)
            {
                var player = run.Players[i]; float x = 20 + i * 310;
                U.Panel(new Rect(x, 594, 300, 104));
                U.Label(x + 12, 602, 278, 28, player.Id + (player.Corruption ? " · ASHBOUND" : " · WANDERER"), U.CardTitle);
                Color tint = player.Corruption ? Palette.Corrupted : Palette.Party[i];
                U.Bar(new Rect(x + 12, 633, 276, 10), player.Health.CurrentHealth / player.Health.MaxHealth, tint);
                U.Label(x + 12, 649, 279, 23, player.Alive ? Math.Ceiling(player.Health.CurrentHealth) + " / " + Math.Ceiling(player.Health.MaxHealth) + " HP" + (player.Health.Pool.Shield > 0 ? "  +" + Math.Ceiling(player.Health.Pool.Shield) + " shield" : "") : "Down · returns at the next reward", U.Small);
                U.Label(x + 12, 674, 278, 22, player.Weapon.rarity+" "+player.Weapon.family+" · "+player.Weapon.PrimaryElement+" · Relics "+player.Inventory.Items.Count, U.Small);
            }
            if (count == 1) U.Label(345, 639, 720, 44, "LMB strike   ·   Space dash   ·   E ward / burst   ·   F interact\nTab build & fragments   ·   F1 developer tools", U.Small);
        }
        private static string Ready(float seconds) => seconds <= 0 ? "ready" : seconds.ToString("0.0") + "s";
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
                var option=draft.Options[i];float x=250+i*400;U.Panel(new Rect(x,215,375,350));U.Label(x+20,235,335,24,option.Rarity.ToString().ToUpperInvariant()+" · "+(option.IsWeapon?"WEAPON":"ARMOR"),U.Small);U.Label(x+20,270,335,40,option.DisplayName,U.CardTitle);
                string identity=option.IsWeapon?option.Weapon.family+" · "+option.Element+"\nDamage "+option.Weapon.damage+" · Skill "+(option.Weapon.skill?option.Weapon.skill.displayName:"Locked / none") : option.Armor.slot+" · "+option.Element+" · "+(option.Armor.set?option.Armor.set.displayName:"No set");
                string current=option.IsWeapon?"Current: "+draft.CurrentPlayer.Weapon.displayName+" · "+draft.CurrentPlayer.Weapon.damage+" damage":"Current: "+(draft.CurrentPlayer.Equipment.InSlot(option.Armor.slot)?draft.CurrentPlayer.Equipment.InSlot(option.Armor.slot).displayName:"Empty "+option.Armor.slot);
                U.Label(x+20,325,335,92,identity+"\n\n"+current,U.Small);var salvage=ProgressionEconomy.Salvage(option.Rarity,option.IsWeapon,run.Progression.EffectPower(MetaEffectKind.SalvageYield));U.Label(x+20,430,335,35,"Salvage: "+salvage,U.Small);
                if(U.Click(new Rect(x+20,480,155,38),"Equip")){draft.Equip(i);break;}if(U.Click(new Rect(x+190,480,165,38),"Dismantle")){draft.Dismantle(i);break;}
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
