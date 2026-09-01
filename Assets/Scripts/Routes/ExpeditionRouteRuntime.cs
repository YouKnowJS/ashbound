using System;
using System.Collections.Generic;
using System.Linq;

namespace Ashbound
{
    public sealed class ExpeditionNodeRuntime
    {
        public ExpeditionNodeDefinition Definition { get; }
        public bool Discovered { get; internal set; }
        public bool Completed { get; internal set; }
        public float CompletionTime { get; internal set; }
        public ExpeditionNodeRuntime(ExpeditionNodeDefinition definition){Definition=definition;}
    }

    public sealed class ExpeditionRouteRuntime
    {
        private readonly Dictionary<string,ExpeditionNodeRuntime> nodes;
        private readonly List<string> players;
        private readonly Dictionary<string,string> votes=new Dictionary<string,string>();
        private readonly System.Random tieRandom;
        private int revealScore;
        public ExpeditionRegionDefinition Region { get; }
        public ExpeditionRouteGraphDefinition Graph { get; }
        public IReadOnlyCollection<ExpeditionNodeRuntime> Nodes=>nodes.Values;
        public IReadOnlyDictionary<string,string> Votes=>votes;
        public ExpeditionNodeRuntime Current { get; private set; }
        public bool RegionComplete=>Current!=null&&Current.Completed&&Current.Definition.nodeType==ExpeditionNodeType.Boss;
        public bool FullReveal { get; private set; }
        public ExpeditionGraphValidationResult Validation { get; }
        public int RevealScore=>revealScore;

        public ExpeditionRouteRuntime(ExpeditionRegionDefinition region,int seed,int revealScore,IEnumerable<string> playerIds)
        {
            Region=region?region:throw new ArgumentNullException(nameof(region));if(region.graphVariants==null||region.graphVariants.Length==0)throw new InvalidOperationException("Region has no route graph variants.");
            Graph=region.graphVariants[(seed&int.MaxValue)%region.graphVariants.Length];Validation=ExpeditionGraphValidator.Validate(Graph);if(!Validation.IsValid)throw new InvalidOperationException(Validation.Summary+": "+string.Join(" | ",Validation.Errors));
            nodes=Graph.nodes.ToDictionary(x=>x.id,x=>new ExpeditionNodeRuntime(x));players=(playerIds??Array.Empty<string>()).Distinct().ToList();if(players.Count==0)players.Add("P1");this.revealScore=Math.Max(0,revealScore);tieRandom=new System.Random(seed^0x51A7);
            Current=nodes[Graph.startNodeId];Current.Discovered=true;RevealAroundCurrent();
        }
        public IReadOnlyList<ExpeditionNodeRuntime> Available=>Current==null?Array.Empty<ExpeditionNodeRuntime>():Current.Definition.outgoingConnections.Where(nodes.ContainsKey).Select(x=>nodes[x]).ToArray();
        public IReadOnlyList<string> PlayerIds=>players;
        public void CompleteCurrent(float elapsed){if(Current==null)return;Current.Completed=true;Current.CompletionTime=Math.Max(0,elapsed);RevealAroundCurrent();votes.Clear();}
        public void BeginSelection(){votes.Clear();RevealAroundCurrent();}
        public bool CastVote(string playerId,string nodeId,out ExpeditionNodeRuntime selected)
        {
            selected=null;if(!players.Contains(playerId)||!Available.Any(x=>x.Definition.id==nodeId))return false;votes[playerId]=nodeId;
            if(players.Count==1){selected=nodes[nodeId];return true;}if(votes.Count<players.Count)return true;
            var counts=votes.Values.GroupBy(x=>x).Select(x=>new{Id=x.Key,Count=x.Count()}).ToArray();int best=counts.Max(x=>x.Count);var tied=counts.Where(x=>x.Count==best).Select(x=>x.Id).OrderBy(x=>x).ToArray();string winner=tied[0];
            if(tied.Length>1&&Graph.tieBehavior==VoteTieBehavior.HostBreaksTie&&votes.TryGetValue(players[0],out string host)&&tied.Contains(host))winner=host;else if(tied.Length>1&&Graph.tieBehavior==VoteTieBehavior.SeededRandom)winner=tied[tieRandom.Next(tied.Length)];selected=nodes[winner];return true;
        }
        public bool Enter(ExpeditionNodeRuntime selected)
        {
            if(selected==null||!Available.Contains(selected))return false;Current=selected;Current.Discovered=true;votes.Clear();RevealAroundCurrent();return true;
        }
        public bool DebugEnter(string id){if(!nodes.TryGetValue(id,out var node))return false;Current=node;node.Discovered=true;votes.Clear();RevealAroundCurrent();return true;}
        public void SetFullReveal(bool value){FullReveal=value;if(value)foreach(var node in nodes.Values)node.Discovered=true;else RevealAroundCurrent();}
        public RouteVisibilityState Visibility(ExpeditionNodeRuntime target)
        {
            if(target==null)return RouteVisibilityState.Hidden;if(FullReveal||target.Completed||target==Current)return RouteVisibilityState.Visible;int distance=Distance(Current,target);
            if(distance<=1)return RouteVisibilityState.Visible;if(distance<=1+revealScore)return RouteVisibilityState.Visible;if(distance==2+revealScore)return RouteVisibilityState.Obscured;return RouteVisibilityState.Hidden;
        }
        private void RevealAroundCurrent(){if(Current==null)return;Current.Discovered=true;foreach(var node in Available)node.Discovered=true;}
        private int Distance(ExpeditionNodeRuntime from,ExpeditionNodeRuntime target)
        {
            if(from==null)return int.MaxValue;var queue=new Queue<(string,int)>();var seen=new HashSet<string>();queue.Enqueue((from.Definition.id,0));while(queue.Count>0){var item=queue.Dequeue();if(!seen.Add(item.Item1))continue;if(item.Item1==target.Definition.id)return item.Item2;foreach(string edge in nodes[item.Item1].Definition.outgoingConnections??Array.Empty<string>())if(nodes.ContainsKey(edge))queue.Enqueue((edge,item.Item2+1));}return int.MaxValue;
        }
    }
}
