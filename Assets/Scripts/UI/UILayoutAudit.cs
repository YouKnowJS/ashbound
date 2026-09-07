using System.Collections.Generic;
using UnityEngine;

namespace Ashbound
{
    public sealed class UILayoutAuditResult
    {
        public Vector2Int Resolution;
        public GameLanguage Language;
        public readonly List<string> Issues=new List<string>();
        public bool Passed=>Issues.Count==0;
        public string Summary=>Passed?Resolution.x+"x"+Resolution.y+" · "+Language+" · PASS":Resolution.x+"x"+Resolution.y+" · "+Language+" · "+string.Join(" | ",Issues);
    }

    public static class UILayoutAudit
    {
        public static readonly Vector2Int[] TargetResolutions={new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3440,1440)};
        public static readonly string[] MajorPanels={"Camp","Expedition Table","Preparations","Party","Route Map","Relic Reward","Equipment Reward","Merchant","Rest","Treasure","Settings","Pause"};

        public static UILayoutAuditResult Validate(Vector2Int resolution,GameLanguage language,PrototypeCatalog catalog)
        {
            var result=new UILayoutAuditResult{Resolution=resolution,Language=language};PrototypeGui.Initialize(language);Rect viewport=PrototypeGui.LogicalViewport(resolution);
            if(viewport.xMin<-.1f||viewport.yMin<-.1f||viewport.xMax>resolution.x+.1f||viewport.yMax>resolution.y+.1f)result.Issues.Add("logical canvas off-screen");
            if(!PrototypeGui.SupportsEnglish)result.Issues.Add("English glyph coverage missing");if(!PrototypeGui.SupportsSimplifiedChinese)result.Issues.Add("Chinese glyph coverage missing");
            if(!PrototypeGui.Button.wordWrap||PrototypeGui.Button.padding.top>5||PrototypeGui.Button.padding.bottom>5)result.Issues.Add("unsafe shared button typography");
            if(catalog&&catalog.preparations!=null)foreach(var preparation in catalog.preparations)
            {
                string label="✓ "+LocalizationService.PreparationNameFor(language,preparation);if(!PrototypeGui.Fits(label,new Rect(0,0,195,PrototypeGui.PreparationButtonHeight),PrototypeGui.Button,10))result.Issues.Add("preparation clipped: "+label);
            }
            Check(result,"party button",LocalizationService.TextFor(language,"camp.add.keyboard","Add shared keyboard"),new Rect(0,0,190,29),PrototypeGui.Button);
            Check(result,"upgrade button",LocalizationService.TextFor(language,"camp.upgrade","Upgrade")+" · "+LocalizationService.TextFor(language,"camp.locked","Prerequisite required"),new Rect(0,0,305,38),PrototypeGui.Button);
            Check(result,"facility description",LocalizationService.TextFor(language,"facility.research-station.description","Research relics, elemental affinity, route intelligence, and randomness."),new Rect(0,0,650,45),PrototypeGui.Small);
            Check(result,"route button",language==GameLanguage.SimplifiedChinese?"选择这条高风险精英路线":"Choose this high-risk Elite route",new Rect(0,0,314,38),PrototypeGui.Button);
            Check(result,"reward card",language==GameLanguage.SimplifiedChinese?"装备、拆解为远征材料或离开":"Equip, dismantle into expedition materials, or leave",new Rect(0,0,340,48),PrototypeGui.Text);
            Check(result,"merchant",language==GameLanguage.SimplifiedChinese?"刷新 · 资源不足":"Reroll · insufficient resources",new Rect(0,0,280,38),PrototypeGui.Button);
            Check(result,"rest",language==GameLanguage.SimplifiedChinese?"休息 · 队伍恢复":"Rest · party recovery",new Rect(0,0,230,54),PrototypeGui.Button);
            Check(result,"treasure",language==GameLanguage.SimplifiedChinese?"开启宝藏并支付当前生命值":"Open treasure and pay current health",new Rect(0,0,314,42),PrototypeGui.Button);
            Check(result,"settings",LocalizationService.TextFor(language,"camp.controls.body","Move: WASD / left stick\nInteract: F / A\nSettings: Esc / Start\nDeveloper tools: F1"),new Rect(0,0,430,95),PrototypeGui.Small);
            return result;
        }

        private static void Check(UILayoutAuditResult result,string name,string text,Rect rect,GUIStyle style)
        {if(!PrototypeGui.Fits(text,rect,style,10))result.Issues.Add(name+" clipped");}
    }
}
