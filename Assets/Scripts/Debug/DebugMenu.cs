using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U = Ashbound.PrototypeGui;

namespace Ashbound
{
    public sealed class DebugMenu : MonoBehaviour
    {
        private RunManager run; private int selectedPlayer, relicPage;
        private string result = "Select a weapon, force a build, or jump to an encounter. Close F1 to resume.";
        public void Configure(RunManager manager) { run = manager; }
        private void Update() { if (run && Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) run.DebugOpen = !run.DebugOpen; }
        private void Mark() { if (run.Telemetry.Record != null) run.Telemetry.Record.debugUsed = true; }
        private void OnGUI()
        {
            if (!run || !run.DebugOpen) return;
            GUI.depth=-20; var old=U.Scale(); U.Box(new Rect(0,0,1280,720),new Color(0,0,0,.68f)); U.Panel(new Rect(70,35,1140,650));
            U.Label(95,52,880,35,"ASHBOUND v0.2 · DEVELOPER TOOLS",U.Heading);
            if(U.Click(new Rect(1060,50,120,34),"Close F1"))run.DebugOpen=false;
            U.Label(95,91,1050,24,"State "+run.Flow.State+" · Room "+run.Rooms.RoomIndex+" · PvP "+run.Combat.PvPEnabled,U.Small);
            if(U.Click(new Rect(95,125,170,35),"Jump Mini-Boss")){result=run.DebugJumpToRoom(4)?"Mini-Boss ready.":"Reset before jumping.";Mark();}
            if(U.Click(new Rect(275,125,170,35),"Jump Final Boss")){result=run.DebugSkipToBoss()?"Final boss ready.":"Reset before jumping.";Mark();}
            GUI.enabled=run.Rooms.Boss&&run.Rooms.Boss.Alive;if(U.Click(new Rect(455,125,150,35),"Kill boss")){run.Rooms.Boss.Health.DebugKill();Mark();}GUI.enabled=true;
            if(U.Click(new Rect(615,125,140,35),"Reset run")){run.ResetRun();run.DebugOpen=true;result="Run reset.";}
            if(run.Combat.Feedback!=null)
            {
                run.Combat.Feedback.HitStopEnabled=GUI.Toggle(new Rect(775,132,110,25),run.Combat.Feedback.HitStopEnabled," Hit stop");
                run.Combat.Feedback.CameraShakeEnabled=GUI.Toggle(new Rect(885,132,125,25),run.Combat.Feedback.CameraShakeEnabled," Shake");
                run.Combat.Feedback.VfxEnabled=GUI.Toggle(new Rect(1010,132,110,25),run.Combat.Feedback.VfxEnabled," VFX");
            }
            if(run.Players.Count==0){U.Label(95,190,900,60,"Start a run or jump to an encounter to expose player build controls.");GUI.matrix=old;return;}
            for(int i=0;i<run.Players.Count;i++)if(U.Click(new Rect(95+i*105,180,95,30),run.Players[i].Id+(i==selectedPlayer?" *":"")))selectedPlayer=i;
            selectedPlayer=Mathf.Clamp(selectedPlayer,0,run.Players.Count-1);var player=run.Players[selectedPlayer];
            U.Label(95,220,160,25,"WEAPONS",U.CardTitle);
            for(int i=0;i<run.Catalog.weapons.Length;i++){var w=run.Catalog.weapons[i];if(U.Click(new Rect(95+(i%4)*180,250+(i/4)*38,170,32),w.family.ToString())){player.Attacks.SetWeapon(w);result="Equipped "+w.displayName;Mark();}}
            if(U.Click(new Rect(835,250,145,32),"Reset build")){player.Inventory.Clear();result="Build cleared.";Mark();}
            if(U.Click(new Rect(990,250,155,32),"Invulnerability")){player.Health.DebugInvulnerable=!player.Health.DebugInvulnerable;Mark();}
            U.Label(95,337,190,25,"GRANT BY TAG",U.CardTitle);
            BuildTag[] tags={BuildTag.Fire,BuildTag.Frost,BuildTag.Lightning,BuildTag.Poison,BuildTag.Void,BuildTag.Critical,BuildTag.Bleed,BuildTag.Heavy,BuildTag.Combo,BuildTag.DashPrecision};
            for(int i=0;i<tags.Length;i++){var tag=tags[i];if(U.Click(new Rect(95+(i%5)*150,368+(i/5)*36,140,30),tag.ToString())){var item=run.Catalog.items.FirstOrDefault(x=>x.tags.Contains(tag)&&player.Inventory.CanAdd(x));result=item&&player.Inventory.TryAdd(item)?"Granted "+item.displayName:"No eligible "+tag+" relic.";Mark();}}
            U.Label(95,450,190,25,"SPECIFIC RELICS",U.CardTitle);int pageSize=10,pages=Mathf.CeilToInt(run.Catalog.items.Length/(float)pageSize);relicPage=Mathf.Clamp(relicPage,0,pages-1);
            if(U.Click(new Rect(250,446,40,28),"<"))relicPage=Mathf.Max(0,relicPage-1);if(U.Click(new Rect(300,446,40,28),">"))relicPage=Mathf.Min(pages-1,relicPage+1);U.Label(350,450,140,22,"Page "+(relicPage+1)+" / "+pages,U.Small);
            for(int n=0;n<pageSize;n++){int index=relicPage*pageSize+n;if(index>=run.Catalog.items.Length)break;var item=run.Catalog.items[index];float x=95+(n%2)*520,y=487+(n/2)*31;U.Label(x,y,325,25,item.displayName+" · "+string.Join("/",item.tags),U.Small);GUI.enabled=player.Inventory.CanAdd(item);if(U.Click(new Rect(x+335,y-2,75,27),"Grant")){player.Inventory.TryAdd(item);Mark();}GUI.enabled=true;}
            U.Label(95,650,1040,24,result+" · Relics "+player.Inventory.Items.Count+" · Dominant "+string.Join(" / ",player.Inventory.DominantTags()),U.Small);
            GUI.matrix=old;
        }
    }
}
