using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ashbound
{
    public sealed class ThreatBudgetPlan
    {
        public int BaseCount { get; internal set; }
        public int TotalCount { get; internal set; }
        public int ReinforcementCount=>TotalCount-BaseCount;
        public float BaseThreat { get; internal set; }
        public float TargetThreat { get; internal set; }
        public IReadOnlyList<EnemyDefinition> Reinforcements=>reinforcements;
        internal readonly List<EnemyDefinition> reinforcements=new List<EnemyDefinition>();
    }

    public static class ThreatBudget
    {
        public static float Cost(EnemyDefinition enemy)
        {
            if(!enemy)return 1;float role=enemy.role==EnemyRole.Bruiser||enemy.role==EnemyRole.Mage||enemy.role==EnemyRole.Controller?1.45f:enemy.role==EnemyRole.Support||enemy.role==EnemyRole.Bomber?1.25f:1;
            if(enemy.elite)role*=2.2f;if(enemy.element!=ElementTag.None)role*=1.12f;return role;
        }
        public static ThreatBudgetPlan Plan(EncounterDefinition encounter,int partySize,float depthProgress,NodeRiskRating risk,bool elite,ThreatBudgetTuning tuning)
        {
            tuning??=new ThreatBudgetTuning();var plan=new ThreatBudgetPlan();var pool=(encounter?.groups??Array.Empty<EnemySpawnGroup>()).Where(x=>x.enemy).ToArray();
            foreach(var group in pool){int count=Mathf.Max(1,group.count);plan.BaseCount+=count;plan.BaseThreat+=Cost(group.enemy)*count;}
            float multiplier=tuning.baseDensityMultiplier+Mathf.Clamp01(depthProgress)*tuning.depthMultiplier+(int)risk*tuning.riskStep+(elite?tuning.eliteBonus:0)+Mathf.Max(0,partySize-1)*tuning.playerBonusPerExtraPlayer;
            plan.TargetThreat=plan.BaseThreat*multiplier;float threat=plan.BaseThreat;int cursor=0;
            while(pool.Length>0&&threat<plan.TargetThreat&&plan.BaseCount+plan.reinforcements.Count<tuning.maximumEnemies){var enemy=pool[cursor++%pool.Length].enemy;plan.reinforcements.Add(enemy);threat+=Cost(enemy);}
            plan.TotalCount=plan.BaseCount+plan.reinforcements.Count;return plan;
        }
    }
}
