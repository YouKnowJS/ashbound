using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public sealed class ExpeditionGraphValidationResult
    {
        public readonly List<string> Errors=new List<string>();
        public readonly List<string> Warnings=new List<string>();
        public bool IsValid=>Errors.Count==0;
        public string Summary=>IsValid?"Valid route graph"+(Warnings.Count>0?" · "+Warnings.Count+" warning(s)":""):"Invalid route graph · "+Errors.Count+" error(s)";
    }

    public static class ExpeditionGraphValidator
    {
        public static ExpeditionGraphValidationResult Validate(ExpeditionRouteGraphDefinition graph)
        {
            var result=new ExpeditionGraphValidationResult();
            if(!graph){result.Errors.Add("Missing graph definition.");return result;}
            var nodes=(graph.nodes??Array.Empty<ExpeditionNodeDefinition>()).Where(x=>x).ToArray();
            if(nodes.Length<8||nodes.Length>10)result.Errors.Add("Graph must contain 8–10 nodes; found "+nodes.Length+".");
            var duplicate=nodes.GroupBy(x=>x.id).FirstOrDefault(x=>string.IsNullOrWhiteSpace(x.Key)||x.Count()>1);if(duplicate!=null)result.Errors.Add("Node IDs must be non-empty and unique: "+duplicate.Key);
            var byId=nodes.Where(x=>!string.IsNullOrWhiteSpace(x.id)).ToDictionary(x=>x.id,x=>x);
            if(!byId.ContainsKey(graph.startNodeId))result.Errors.Add("Start node does not exist: "+graph.startNodeId);
            if(!byId.TryGetValue(graph.bossNodeId,out var boss))result.Errors.Add("Boss node does not exist: "+graph.bossNodeId);else if(boss.nodeType!=ExpeditionNodeType.Boss)result.Errors.Add("Configured boss node is not Boss type.");
            foreach(var node in nodes)
            {
                foreach(string edge in node.outgoingConnections??Array.Empty<string>())if(!byId.ContainsKey(edge))result.Errors.Add(node.id+" has invalid outgoing edge "+edge+".");
                if(node.id!=graph.bossNodeId&&(node.outgoingConnections==null||node.outgoingConnections.Length==0))result.Errors.Add(node.id+" is a non-boss dead end.");
                if(IsCombat(node.nodeType)&&(node.encounter==null||node.combatSpace==null))result.Errors.Add(node.id+" requires encounter and combat-space references.");
                if(node.nodeType==ExpeditionNodeType.Treasure&&node.treasure==null)result.Errors.Add(node.id+" requires TreasureDefinition.");
                if(node.nodeType==ExpeditionNodeType.Merchant&&node.merchant==null)result.Errors.Add(node.id+" requires MerchantDefinition.");
                if(node.nodeType==ExpeditionNodeType.Rest&&node.rest==null)result.Errors.Add(node.id+" requires RestNodeDefinition.");
                if(node.nodeType==ExpeditionNodeType.Event&&node.eventDefinition==null)result.Errors.Add(node.id+" requires EventDefinition.");
                if(node.nodeType==ExpeditionNodeType.Challenge&&node.challenge==null)result.Errors.Add(node.id+" requires ChallengeDefinition.");
                if(node.nodeType==ExpeditionNodeType.Boss&&node.bossReward==null)result.Errors.Add(node.id+" requires BossRewardDefinition.");
            }
            if(byId.ContainsKey(graph.startNodeId))
            {
                var reached=new HashSet<string>();var queue=new Queue<string>();queue.Enqueue(graph.startNodeId);
                while(queue.Count>0){string id=queue.Dequeue();if(!reached.Add(id))continue;foreach(string edge in byId[id].outgoingConnections??Array.Empty<string>())if(byId.ContainsKey(edge))queue.Enqueue(edge);}
                foreach(var node in nodes)if(!reached.Contains(node.id))result.Errors.Add("Orphan node is unreachable: "+node.id);
                if(!reached.Contains(graph.bossNodeId))result.Errors.Add("Boss is unreachable from start.");
            }
            int combat=nodes.Count(x=>IsCombat(x.nodeType));if(combat<graph.minimumCombatNodes)result.Errors.Add("Minimum combat nodes not met: "+combat+" / "+graph.minimumCombatNodes);
            if(nodes.Count(x=>x.nodeType==ExpeditionNodeType.Rest)>graph.maximumRestNodes)result.Errors.Add("Rest node limit exceeded.");
            if(nodes.Count(x=>x.nodeType==ExpeditionNodeType.Merchant)>graph.maximumMerchantNodes)result.Errors.Add("Merchant node limit exceeded.");
            if(byId.ContainsKey(graph.startNodeId))ValidateRepeats(byId,graph.startNodeId,null,0,graph.maximumRepeatedType,new HashSet<string>(),result);
            return result;
        }
        private static void ValidateRepeats(Dictionary<string,ExpeditionNodeDefinition> nodes,string id,ExpeditionNodeType? previous,int repeats,int maximum,HashSet<string> path,ExpeditionGraphValidationResult result)
        {
            if(!nodes.TryGetValue(id,out var node)||!path.Add(id))return;int current=previous==node.nodeType?repeats+1:1;if(current>maximum)result.Errors.Add("Route can repeat "+node.nodeType+" more than "+maximum+" times at "+id+".");
            foreach(string edge in node.outgoingConnections??Array.Empty<string>())ValidateRepeats(nodes,edge,node.nodeType,current,maximum,new HashSet<string>(path),result);
        }
        public static bool IsCombat(ExpeditionNodeType type)=>type==ExpeditionNodeType.NormalCombat||type==ExpeditionNodeType.HardCombat||type==ExpeditionNodeType.Elite||type==ExpeditionNodeType.Challenge||type==ExpeditionNodeType.Boss;
    }
}
