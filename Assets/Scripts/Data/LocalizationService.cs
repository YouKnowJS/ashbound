using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public enum GameLanguage { English, SimplifiedChinese }

    public static class LocalizationService
    {
        private const string PreferenceKey="ashbound.language";
        private static readonly Dictionary<string,string> English=new Dictionary<string,string>();
        private static readonly Dictionary<string,string> Chinese=new Dictionary<string,string>
        {
            {"camp.title","余烬营地"},{"camp.subtitle","下一次远征从这里开始"},{"camp.interact","F / A — 交互"},{"camp.talk","交谈"},{"camp.use","使用设施"},{"camp.back","返回营地"},
            {"camp.settings","设置"},{"camp.controls","控制"},{"camp.language","语言"},{"camp.english","English"},{"camp.chinese","简体中文"},{"camp.exit","退出游戏"},{"camp.close","关闭"},
            {"camp.party","本地队伍"},{"camp.add.keyboard","添加共享键盘玩家"},{"camp.add.gamepad","添加手柄玩家"},{"camp.remove","移除最后一位"},{"camp.launch","开始远征"},{"camp.preparation","远征准备"},
            {"camp.upgrade","升级"},{"camp.available","可以升级"},{"camp.unaffordable","资源不足"},{"camp.locked","前置条件未满足"},{"camp.complete","已全部升级"},{"camp.level","等级"},{"camp.next","下一项"},
            {"camp.prerequisite","前置条件"},{"camp.prerequisite.none","前置条件：无"},
            {"camp.saved","设置已保存"},{"camp.controls.body","移动：WASD / 左摇杆\n交互：F / A\n暂停与设置：Esc / Start\n开发工具：F1"},
            {"hud.challenge","挑战"},{"hud.remaining","剩余敌人"},{"hud.hostiles","名敌人存活"},{"hud.inheritance","继承之战"},{"hud.chooseRelic","选择遗物"},{"hud.materials","远征材料"},{"hud.pause","Esc 暂停"},{"hud.shield","护盾"},{"hud.down","倒下 · 将在下个奖励点返回"},{"hud.relics","遗物"},{"hud.controls","鼠标左键攻击 · 空格闪避 · E 防护 / 爆发 · F 交互\nTab 构筑与碎片 · F1 开发工具"},
            {"route.vote","路线投票"},{"route.current","当前位置"},{"route.choose","选择玩家；按 1 / 2 / 3 或该玩家手柄的 A / B / Y"},{"route.risk","风险"},{"route.votes","票"},{"route.button","选择这条路线"},{"route.intelligence","路线情报 · 当前选择后额外显示一层"},
            {"state.wanderer","流浪者"},{"state.ashbound","烬缚者"},{"state.kindled","燃起"},
            {"npc.expedition.name","玛拉"},{"npc.expedition.title","远征领队 / 制图师"},{"npc.expedition.line","路线已经标好。准备好就出发。"},
            {"npc.forge.name","布兰"},{"npc.forge.title","锻造大师"},{"npc.forge.line","需要重新锻造什么？"},
            {"npc.quartermaster.name","伊薇"},{"npc.quartermaster.title","军需官"},{"npc.quartermaster.line","补给有限。把每一份灰烬都花在刀刃上。"},
            {"npc.infirmary.name","萨伦"},{"npc.infirmary.title","战地医师"},{"npc.infirmary.line","活着回来，比英勇倒下更有用。"},
            {"npc.research.name","维尔"},{"npc.research.title","研究员"},{"npc.research.line","可能性会留下痕迹。让我看看。"},
            {"npc.archive.name","奥伦"},{"npc.archive.title","档案管理员"},{"npc.archive.line","我们只记录远征带回来的事实。"},
            {"resource.ash.name","灰烬"},{"resource.ash.description","常见远征材料，用于营地的基础升级。"},{"resource.ash.use","主要用途：设施研究与锻造"},
            {"resource.ember.name","余烬碎片"},{"resource.ember.description","仍保有热量的碎片，用于元素与高级研究。"},{"resource.ember.use","主要用途：高级设施升级"},
            {"resource.alloy.name","古代合金"},{"resource.alloy.description","用于高级锻造和传说研究的稀有材料。"},{"resource.alloy.use","主要用途：史诗与传说研究"},
            {"resource.corruption.name","腐化碎片"},{"resource.corruption.description","从危险远征中回收的不稳定残片。"},{"resource.corruption.use","主要用途：最高阶研究"},
            {"facility.expedition-table.name","远征桌"},{"facility.expedition-table.description","查看路线记录、选择准备并组织下一次远征。"},
            {"facility.forge.name","锻造"},{"facility.forge.description","研究武器、元素变体、武器技能和护甲套装。"},
            {"facility.quartermaster.name","军需处"},{"facility.quartermaster.description","改善商人物资、刷新与远征经济。"},
            {"facility.infirmary.name","医疗帐篷"},{"facility.infirmary.description","提供受限的生存、恢复与资源保留升级。"},
            {"facility.archive.name","档案馆"},{"facility.archive.description","保存可选的残缺记录、Boss 观察与传说条目。"},
            {"facility.research-station.name","研究站"},{"facility.research-station.description","研究遗物、元素倾向、路线情报与随机性。"},
            {"node.NormalCombat","普通战斗"},{"node.HardCombat","困难战斗"},{"node.Elite","精英"},{"node.Treasure","宝藏"},{"node.Relic","遗物"},{"node.Merchant","商人"},{"node.Rest","休息"},{"node.Event","事件"},{"node.Challenge","挑战"},{"node.Boss","Boss"},{"node.Secret","秘密"},
            {"rarity.Common","普通"},{"rarity.Advanced","进阶"},{"rarity.Rare","稀有"},{"rarity.Epic","史诗"},{"rarity.Legendary","传说"},
            {"element.None","无"},{"element.Fire","火焰"},{"element.Frost","寒霜"},{"element.Lightning","雷电"},{"element.Poison","剧毒"},{"element.Void","虚空"},
            {"weapon.Sword","剑"},{"weapon.Spear","长矛"},{"weapon.Greatsword","巨剑"},{"weapon.Katana","太刀"},{"weapon.DualBlades","双刃"},{"weapon.Bow","弓"},{"weapon.Staff","法杖"},{"weapon.Spellblade","魔刃"},
            {"armor.Head","头部"},{"armor.Chest","胸部"},{"armor.Gloves","手套"},{"armor.Boots","靴子"},
            {"label.relic","遗物"},{"label.temper","淬炼"},{"label.expedition","远征"},{"label.elite","精英"},{"label.treasure","宝藏"},
            {"prep.hunters-preparation","猎人准备"},{"prep.frost-research","寒霜研究"},{"prep.cartographers-notes","制图师笔记"},{"prep.merchant-contract","商人契约"},{"prep.field-supplies","野战补给"}
        };

        public static GameLanguage Current { get; private set; }=Load();
        public static event Action Changed;
        public static bool IsChinese=>Current==GameLanguage.SimplifiedChinese;
        public static void SetLanguage(GameLanguage language)
        {
            if(Current==language)return;Current=language;PlayerPrefs.SetInt(PreferenceKey,(int)language);PlayerPrefs.Save();PrototypeGui.ResetStyles();Changed?.Invoke();
        }
        public static string Text(string key,string fallback=null)
        {
            if(IsChinese&&Chinese.TryGetValue(key,out string translated))return translated;if(English.TryGetValue(key,out string english))return english;return fallback??key;
        }
        public static string FacilityName(HubFacilityDefinition value)=>value?Text("facility."+value.id+".name",value.displayName):"";
        public static string FacilityDescription(HubFacilityDefinition value)=>value?Text("facility."+value.id+".description",value.description):"";
        public static string TierName(FacilityUpgradeTier value)=>IsChinese?TranslateTier(value.id,value.displayName):value.displayName;
        public static string TierDescription(FacilityUpgradeTier value)=>IsChinese?TranslateEffect(value):value.description;
        public static string PreparationName(PreparationDefinition value)=>value?Text("prep."+value.id,value.displayName):"";
        public static string Node(ExpeditionNodeType value)=>Text("node."+value,value.ToString());
        public static string Rarity(WeaponRarity value)=>Text("rarity."+value,value.ToString());
        public static string Element(ElementTag value)=>Text("element."+value,value.ToString());
        public static string Weapon(WeaponFamily value)=>Text("weapon."+value,value.ToString());
        public static string Armor(ArmorSlot value)=>Text("armor."+value,value.ToString());
        public static string ResourceName(ExpeditionResource value)=>Text(value==ExpeditionResource.Ash?"resource.ash.name":value==ExpeditionResource.EmberShards?"resource.ember.name":value==ExpeditionResource.AncientAlloy?"resource.alloy.name":"resource.corruption.name",value.ToString());
        public static string ResourceDescription(ExpeditionResource value)
        {
            string prefix=value==ExpeditionResource.Ash?"resource.ash":value==ExpeditionResource.EmberShards?"resource.ember":value==ExpeditionResource.AncientAlloy?"resource.alloy":"resource.corruption";return Text(prefix+".description")+"\n"+Text(prefix+".use");
        }
        public static string Wallet(ResourceWallet value)=>value==null?"":string.Join("  ·  ",new[]{ExpeditionResource.Ash,ExpeditionResource.EmberShards,ExpeditionResource.AncientAlloy,ExpeditionResource.CorruptionFragments}.Select(resource=>ResourceName(resource)+" "+value.Get(resource)));
        private static GameLanguage Load()=>PlayerPrefs.GetInt(PreferenceKey,0)==1?GameLanguage.SimplifiedChinese:GameLanguage.English;
        private static string TranslateTier(string id,string fallback)
        {
            var names=new Dictionary<string,string>{{"table-survey","勘察记录"},{"table-routes","路线注记"},{"table-boss-ledger","Boss 档案"},{"table-deep-chart","深层地图"},{"forge-weapons","武器研究"},{"forge-elements","元素锻造"},{"forge-skills","武器技能研究"},{"forge-craftsmanship","稀有工艺"},{"forge-legendary","传说研究"},{"quartermaster-stock","商人库存 I"},{"quartermaster-network","商人网络"},{"quartermaster-negotiation","议价"},{"quartermaster-salvage","拆解训练"},{"quartermaster-cache","补给储备"},{"infirmary-medicine","野战医疗"},{"infirmary-recovery","恢复训练"},{"infirmary-emergency","紧急补给"},{"infirmary-vitality-1","体魄训练 I"},{"infirmary-vitality-2","体魄训练 II"},{"infirmary-vitality-3","体魄训练 III"},{"archive-shelves","回收书架"},{"archive-insignia","徽记索引"},{"archive-observations","Boss 观察"},{"archive-legends","传说卷宗"},{"research-scavenging","搜集研究"},{"research-cartography","制图学"},{"research-relics","遗物分析"},{"research-appraisal","装备鉴定"},{"research-affinity","元素亲和"}};return names.TryGetValue(id,out string result)?result:fallback;
        }
        private static string TranslateEffect(FacilityUpgradeTier value)
        {
            if(value.effect==MetaEffectKind.None)return "解锁新的装备、技能或研究内容。";string effect=value.effect==MetaEffectKind.RouteReveal?"增加路线情报":value.effect==MetaEffectKind.RareWeight?"提高高稀有度奖励权重":value.effect==MetaEffectKind.MerchantStock?"增加商人库存":value.effect==MetaEffectKind.MerchantChance?"改善商人路线权重":value.effect==MetaEffectKind.RerollDiscount?"降低刷新成本":value.effect==MetaEffectKind.SalvageYield?"提高拆解收益":value.effect==MetaEffectKind.StartingAsh?"增加远征初始灰烬":value.effect==MetaEffectKind.RestRecovery?"提高休息恢复":value.effect==MetaEffectKind.FailureRetention?"提高失败后的资源保留":value.effect==MetaEffectKind.MaxHealth?"小幅提高最大生命":value.effect==MetaEffectKind.RelicReroll?"增加遗物刷新":value.effect==MetaEffectKind.ElementBias?"增加元素倾向":"扩展营地功能";return effect+"。";
        }
    }
}
