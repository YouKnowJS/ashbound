using UnityEngine;

namespace Ashbound
{
    public readonly struct RewardRarityContext
    {
        public readonly int Region,Depth,TotalDepth;
        public readonly NodeRiskRating Risk;
        public readonly RewardQuality Quality;
        public readonly bool Elite,Boss,LegendaryUnlocked;
        public readonly float MetaRareBonus,PreparationRareBonus;
        public RewardRarityContext(int region,int depth,int totalDepth,NodeRiskRating risk,RewardQuality quality,bool elite,bool boss,bool legendaryUnlocked,float metaRareBonus=0,float preparationRareBonus=0)
        {Region=Mathf.Max(1,region);Depth=Mathf.Max(1,depth);TotalDepth=Mathf.Max(1,totalDepth);Risk=risk;Quality=quality;Elite=elite;Boss=boss;LegendaryUnlocked=legendaryUnlocked;MetaRareBonus=Mathf.Max(0,metaRareBonus);PreparationRareBonus=Mathf.Max(0,preparationRareBonus);}
        public float Progress=>TotalDepth<=1?0:Mathf.Clamp01((Depth-1f)/(TotalDepth-1f));
    }

    public static class RewardRarityPolicy
    {
        public static float Weight(ProgressionTuningDefinition tuning,WeaponRarity rarity,RewardRarityContext context)
        {
            var data=tuning&&tuning.rarity!=null?tuning.rarity:new RewardRarityTuning();
            if(rarity==WeaponRarity.Legendary&&!context.LegendaryUnlocked)return 0;
            float progress=context.Progress;RarityWeights from,to;float blend;
            if(context.Boss){from=to=data.boss;blend=0;}
            else if(progress<.5f){from=data.early;to=data.mid;blend=progress*2;}
            else{from=data.mid;to=data.late;blend=(progress-.5f)*2;}
            float weight=Mathf.Lerp(from.Get(rarity),to.Get(rarity),blend);int tier=(int)rarity;
            if(tier>=2)
            {
                weight*=1+(context.Region-1)*data.regionRareStep+(int)context.Risk*data.riskRareStep;
                if(context.Elite)weight*=data.eliteRareMultiplier;
                weight*=1+context.MetaRareBonus*data.metaRareMultiplier+context.PreparationRareBonus*data.preparationRareMultiplier;
            }
            int qualityDelta=(int)context.Quality-1;
            if(qualityDelta!=0)weight*=Mathf.Pow(data.qualityStepMultiplier,qualityDelta*(tier-1));
            return Mathf.Max(0,weight);
        }
        public static float Weight(ProgressionTuningDefinition tuning,Rarity rarity,RewardRarityContext context)=>Weight(tuning,(WeaponRarity)(int)rarity,context);
    }
}
