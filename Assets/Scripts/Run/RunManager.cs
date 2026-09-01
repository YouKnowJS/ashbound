using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashbound
{
    public sealed class RunManager:MonoBehaviour
    {
        private enum RewardFlow { None,NodeRelic,NodeEquipment,NodeRelicThenEquipment,TreasureEquipment,MimicEquipment,EventEquipment,EventRelic,BossEquipment,BossRelic,BossRelicThenEquipment,DebugEquipment }
        public RunStateMachine Flow { get; }=new RunStateMachine();
        public LobbySession Lobby { get; }=new LobbySession();
        private readonly List<Combatant> players=new List<Combatant>();
        public IReadOnlyList<Combatant> Players=>players;
        public List<LoreEntry> Journal { get; }=new List<LoreEntry>();
        public PrototypeCatalog Catalog { get; private set; }
        public CombatService Combat { get; private set; }
        public RoomDirector Rooms { get; private set; }
        public CorruptionSystem Corruption { get; private set; }
        public MatchTelemetry Telemetry { get; }=new MatchTelemetry();
        public UpgradeDraft Draft { get; private set; }
        public EquipmentRewardDraft EquipmentRewards { get; private set; }
        public MetaProgressionService Progression { get; private set; }
        public ExpeditionRouteRuntime Route { get; private set; }
        public RouteRunModifiers RouteModifiers { get; private set; }=new RouteRunModifiers();
        public TreasureSession Treasure { get; private set; }
        public MerchantSession Merchant { get; private set; }
        public RestSession Rest { get; private set; }
        public EventSession Event { get; private set; }
        public bool RouteSelectionOpen { get; private set; }
        public bool AwaitingRouteGate { get; private set; }
        public bool RegionCompleteAwaitingFinalGate { get; private set; }
        public bool ChallengeActive { get; private set; }
        public float ChallengeRemaining { get; private set; }
        public string Message { get; private set; }="The Cinder Vault";
        public string Outcome { get; private set; }
        public string MissingDevice { get; private set; }
        public bool ManualPaused { get; set; }
        public bool DebugOpen { get; set; }
        public int Seed { get; private set; }
        public string LocationName=>Route?.Current?.Definition?.displayName??Rooms.ActiveDisplayName;
        public ExpeditionNodeRuntime CurrentNode=>Route?.Current;
        public string GraphValidation=>Route?.Validation?.Summary??"No active route graph.";
        public string ActiveMenuTelemetryKey=>RouteSelectionOpen?"Route":Draft!=null&&Draft.Active?"RelicReward":EquipmentRewards!=null&&EquipmentRewards.Active?"EquipmentReward":Treasure!=null&&!Treasure.Completed?"Treasure":Merchant!=null&&!Merchant.Closed?"Merchant":Rest!=null&&!Rest.Completed?"Rest":Event!=null&&!Event.Completed?"Event":"";
        private EntityFactory factory;private Camera view;private System.Random corruptionRandom;private LoreFragment fragment;private RewardFlow rewardFlow;private float nodeStartedAt;private bool eventCombat;private TreasureVariantKind? forcedTreasure;private bool legacyDebugCombat;
        public event Action<RunState> StateChanged;

        public void Configure(PrototypeCatalog catalog,CombatService combat,RoomDirector rooms,EntityFactory factory,Camera view)
        {
            Catalog=catalog;Combat=combat;Rooms=rooms;this.factory=factory;this.view=view;string profilePath=Application.isBatchMode?Path.Combine(Application.temporaryCachePath,"AshboundTests","profile-"+Guid.NewGuid().ToString("N")+".json"):null;
            Progression=new MetaProgressionService(catalog,profilePath);Corruption=new CorruptionSystem(catalog,factory);
            Flow.Changed+=state=>{Combat.State=state;Combat.FriendlyFire=false;StateChanged?.Invoke(state);};
            Rooms.WaveCleared+=OnWaveCleared;Rooms.BossDied+=OnTrueFinalBossDied;Rooms.EncounterStarted+=encounter=>Telemetry.EncounterBegin(encounter,Rooms.ActiveCombatSpace);Rooms.EncounterCompleted+=Telemetry.EncounterEnd;Combat.DamageResolved+=Telemetry.Damage;Combat.BuildProc+=Telemetry.Proc;Combat.ControlApplied+=Telemetry.Control;Progression.RunResourceAdded+=(resource,amount)=>Telemetry.NodeResource(resource,amount);
            Rooms.Load(0);
        }

        public bool StartRun(int seed=0)
        {
            if(Flow.State!=RunState.Lobby||!Catalog.prototypeRegion)return false;Seed=seed==0?Environment.TickCount&int.MaxValue:seed;corruptionRandom=new System.Random(Seed^7243);Combat.Seed(Seed);Lobby.Lock();Journal.Clear();Outcome=null;RouteModifiers=new RouteRunModifiers();Progression.BeginExpedition();Progression.RecordProgress(1,1);
            for(int i=0;i<Lobby.Slots.Count;i++){var player=factory.Player(Lobby.Slots[i],i,new Vector3((i-(Lobby.Slots.Count-1)*.5f)*2,0,-6),view);players.Add(player);player.GetComponent<PlayerController>().Interacted+=Interact;player.Inventory.Added+=item=>Telemetry.Item(player,item,false);Progression.ApplyRunStart(player);}
            Telemetry.Begin(Seed,players,Progression);Route=new ExpeditionRouteRuntime(Catalog.prototypeRegion,Seed,Mathf.RoundToInt(Progression.EffectPower(MetaEffectKind.RouteReveal)),players.Select(x=>x.Id));Telemetry.RouteBegin(Route.Graph.id);
            Draft=new UpgradeDraft(Catalog,Progression,Seed^8721);Draft.Selected+=(player,item)=>Telemetry.Item(player,item,true);Draft.Rerolled+=player=>Telemetry.ShopReroll(player);Draft.Finished+=OnRelicRewardFinished;
            EquipmentRewards=new EquipmentRewardDraft(Catalog,Progression,Seed^3187);EquipmentRewards.Equipped+=(player,option)=>Telemetry.Equipment(player,option,false);EquipmentRewards.Dismantled+=(player,option,value)=>Telemetry.Equipment(player,option,true);EquipmentRewards.Finished+=OnEquipmentRewardFinished;
            Flow.TryAdvance(RunState.StartingRun);StartCoroutine(EnterCurrentNode(true));return true;
        }

        private IEnumerator EnterCurrentNode(bool initial=false)
        {
            CloseNodeSessions();RouteSelectionOpen=AwaitingRouteGate=RegionCompleteAwaitingFinalGate=false;legacyDebugCombat=false;var node=CurrentNode?.Definition;if(!node)yield break;Rooms.LoadNode(node);TeleportParty(Rooms.View.EntrancePosition);
            if(initial){yield return new WaitForSeconds(.25f);Flow.TryAdvance(RunState.Exploration);}nodeStartedAt=Telemetry.Record?.runDuration??0;Telemetry.NodeBegin(node);Message=node.displayName+"\n"+node.description;yield return new WaitForSeconds(.45f);
            switch(node.nodeType)
            {
                case ExpeditionNodeType.NormalCombat:case ExpeditionNodeType.HardCombat:case ExpeditionNodeType.Elite:case ExpeditionNodeType.Boss:BeginNodeCombat();break;
                case ExpeditionNodeType.Challenge:ChallengeActive=true;ChallengeRemaining=node.challenge?node.challenge.duration:45;if(node.challenge&&node.challenge.noHealing)foreach(var player in players)player.Health.HealingBlocked=true;BeginNodeCombat();break;
                case ExpeditionNodeType.Treasure:TreasuryBegin(node);break;
                case ExpeditionNodeType.Merchant:Merchant=new MerchantSession(Catalog,Progression,node.merchant,Seed^node.id.GetHashCode());Message=node.merchant.description;break;
                case ExpeditionNodeType.Rest:Rest=new RestSession(node.rest,Progression);Message=node.rest.description;break;
                case ExpeditionNodeType.Event:Event=new EventSession(node.eventDefinition);Message=node.eventDefinition.description;break;
                case ExpeditionNodeType.Relic:BeginRelicReward(RewardFlow.NodeRelic);break;
                default:CompleteCurrentNode();break;
            }
        }

        private void BeginNodeCombat()
        {
            if(!Flow.TryAdvance(RunState.Combat))return;Rooms.SpawnNextWave(players.Count);ApplyPendingCombatModifiers();Message=CurrentNode.Definition.nodeType==ExpeditionNodeType.Challenge?CurrentNode.Definition.challenge.description:"The route closes behind the party.";
        }
        private void ApplyPendingCombatModifiers()
        {
            if(RouteModifiers.EliteNextCombat){var elite=Catalog.enemies.FirstOrDefault(x=>x&&x.id=="base-ash-bruiser");if(elite)Rooms.DebugSpawnEnemy(elite,players.Count,true);}
            if(RouteModifiers.VoidPressureNextCombat){var pressure=Catalog.enemies.FirstOrDefault(x=>x&&x.id=="variant-void-mage");if(pressure)Rooms.DebugSpawnEnemy(pressure,players.Count,false);}RouteModifiers.ClearCombatModifiers();
        }
        private void TreasuryBegin(ExpeditionNodeDefinition node){Treasure=new TreasureSession(node.treasure,Seed^node.id.GetHashCode(),forcedTreasure);forcedTreasure=null;Message=Treasure.Message;Telemetry.TreasureSeen(Treasure.Variant);}

        private void OnWaveCleared()
        {
            if(legacyDebugCombat){LegacyDebugWaveClear();return;}
            if(Treasure!=null&&Treasure.MimicActive){if(!Flow.TryAdvance(RunState.Reward))return;Treasure.MarkMimicDefeated();Telemetry.MimicResult(true);BeginEquipmentReward(RewardFlow.MimicEquipment,Promote(Treasure.Variant.rewardQuality),Treasure.Variant.offerKind);return;}
            if(eventCombat&&Event!=null){eventCombat=false;Event.CompleteCombat();if(!Flow.TryAdvance(RunState.Reward))return;CompleteCurrentNode();return;}
            var node=CurrentNode?.Definition;if(!node||!Flow.TryAdvance(RunState.Reward))return;Rooms.ClearTransientCombat();foreach(var player in players)player.Motor.Stop();Progression.Award(node.resourceReward);
            if(node.nodeType==ExpeditionNodeType.Challenge){ChallengeActive=false;Progression.Award(node.challenge.successReward);Telemetry.ChallengeResult(node.challenge,true);CompleteCurrentNode();return;}
            if(node.nodeType==ExpeditionNodeType.Boss){ApplyRegionalBossReward(node);return;}
            StartAuthoredNodeReward(node);
        }
        private void StartAuthoredNodeReward(ExpeditionNodeDefinition node)
        {
            if(node.grantRelic&&node.grantEquipment)BeginRelicReward(RewardFlow.NodeRelicThenEquipment);else if(node.grantRelic)BeginRelicReward(RewardFlow.NodeRelic);else if(node.grantEquipment)BeginEquipmentReward(RewardFlow.NodeEquipment,node.rewardQuality);else CompleteCurrentNode();
        }
        private void ApplyRegionalBossReward(ExpeditionNodeDefinition node)
        {
            var reward=node.bossReward;if(reward){Progression.Award(reward.resources);Telemetry.BossReward(reward);if(reward.grantRelic&&reward.grantEquipment){BeginRelicReward(RewardFlow.BossRelicThenEquipment);return;}if(reward.grantRelic){BeginRelicReward(RewardFlow.BossRelic);return;}if(reward.grantEquipment){BeginEquipmentReward(RewardFlow.BossEquipment,reward.equipmentQuality);return;}}CompleteCurrentNode();
        }

        private void BeginRelicReward(RewardFlow flow)
        {
            if(Flow.State==RunState.Exploration&&!Flow.TryAdvance(RunState.Reward))return;rewardFlow=flow;Message="A route-bound relic choice remains.";Draft.Begin(players);
        }
        private void BeginEquipmentReward(RewardFlow flow,RewardQuality quality,EquipmentOfferKind kind=EquipmentOfferKind.Mixed)
        {
            if(Flow.State==RunState.Exploration&&!Flow.TryAdvance(RunState.Reward))return;rewardFlow=flow;Message="Equipment remains at this location.";EquipmentRewards.Begin(players,Route?.Nodes.Count(x=>x.Completed)+1??1,quality,kind);
        }
        private void OnRelicRewardFinished()
        {
            if(rewardFlow==RewardFlow.NodeRelicThenEquipment){BeginEquipmentReward(RewardFlow.NodeEquipment,CurrentNode.Definition.rewardQuality);return;}if(rewardFlow==RewardFlow.BossRelicThenEquipment){BeginEquipmentReward(RewardFlow.BossEquipment,CurrentNode.Definition.bossReward.equipmentQuality);return;}if(rewardFlow==RewardFlow.EventRelic){CompleteCurrentNode();return;}CompleteCurrentNode();
        }
        private void OnEquipmentRewardFinished()
        {
            if(rewardFlow==RewardFlow.TreasureEquipment)
            {
                Flow.TryAdvance(RunState.Exploration);if(Treasure.Completed)CompleteCurrentNode();else Message=Treasure.CanContinueGreed?"Take another reward at a greater cost, or leave.":"Nothing more can be taken safely. Leave the cache.";return;
            }
            if(rewardFlow==RewardFlow.DebugEquipment){Flow.TryAdvance(RunState.Exploration);return;}CompleteCurrentNode();
        }

        private void CompleteCurrentNode()
        {
            if(Flow.State==RunState.Combat)Flow.TryAdvance(RunState.Reward);if(Flow.State==RunState.Reward)Flow.TryAdvance(RunState.Exploration);float duration=Mathf.Max(0,(Telemetry.Record?.runDuration??0)-nodeStartedAt);Route.CompleteCurrent(duration);Progression.RecordProgress(1,Route.Nodes.Count(x=>x.Completed));Telemetry.NodeComplete(CurrentNode.Definition,duration,players);StabilizeDowned();CloseNodeSessions();Rooms.UnlockExit();
            if(Route.RegionComplete){RegionCompleteAwaitingFinalGate=true;Message="The regional keeper falls. Cross the approach to enter the final area.";}else{AwaitingRouteGate=true;Message="The path continues beyond the transition. Approach the route marker and interact.";}
        }
        private void StabilizeDowned(){foreach(var player in players){if(!player.Alive)player.Restore(.3f);player.Motor.Stop();}}
        private void CloseNodeSessions(){Treasure=null;Merchant=null;Rest=null;Event=null;ChallengeActive=false;eventCombat=false;rewardFlow=RewardFlow.None;foreach(var player in players)if(player)player.Health.HealingBlocked=false;}

        public void Interact(Combatant actor)
        {
            if(fragment&&Vector3.Distance(actor.transform.position,fragment.transform.position)<2.2f){if(!Journal.Contains(fragment.Entry))Journal.Add(fragment.Entry);Progression.RecordLore(fragment.Entry.id);Message=fragment.Entry.title+"\n"+fragment.Entry.text;Destroy(fragment.gameObject);fragment=null;return;}
            if(Flow.State!=RunState.Exploration||!Rooms.ExitOpen||Vector3.Distance(actor.transform.position,Rooms.View.ExitPosition)>3)return;
            if(RegionCompleteAwaitingFinalGate){EnterTrueFinalBoss();return;}if(AwaitingRouteGate)OpenRouteSelection();
        }
        public void OpenRouteSelection()
        {
            if(!AwaitingRouteGate||Route==null||Route.RegionComplete)return;AwaitingRouteGate=false;RouteSelectionOpen=true;Route.BeginSelection();Telemetry.RouteOffered(Route.Available.Select(x=>x.Definition.id));Message="Choose the next route. Votes resolve after every local player commits.";
        }
        public bool CastRouteVote(string playerId,string nodeId)
        {
            if(!RouteSelectionOpen||Route==null)return false;bool accepted=Route.CastVote(playerId,nodeId,out var selected);if(!accepted)return false;Telemetry.RouteVote(playerId,nodeId);if(selected!=null){Telemetry.RouteChosen(selected.Definition.id,Route.Votes);if(!Route.Enter(selected))return false;RouteSelectionOpen=false;StartCoroutine(EnterCurrentNode());}return true;
        }

        public bool OpenTreasure()
        {
            if(Treasure==null)return false;var before=Progression.RunResources.Copy();TreasureAction action=Treasure.Open(Progression,players,RouteModifiers);Telemetry.TreasureAction(Treasure.Variant,before.Minus(Progression.RunResources),Treasure.RewardsTaken);return HandleTreasureAction(action);
        }
        public bool ContinueTreasure()
        {
            if(Treasure==null)return false;TreasureAction action=Treasure.ContinueGreed(Progression,players,RouteModifiers);Telemetry.TreasureAction(Treasure.Variant,new ResourceWallet(),Treasure.RewardsTaken);return HandleTreasureAction(action);
        }
        private bool HandleTreasureAction(TreasureAction action)
        {
            Message=Treasure?.Message??Message;if(action==TreasureAction.EquipmentReward){BeginEquipmentReward(RewardFlow.TreasureEquipment,Treasure.Variant.rewardQuality,Treasure.Variant.offerKind);return true;}if(action==TreasureAction.MimicCombat){Telemetry.MimicEncountered();if(!Flow.TryAdvance(RunState.Combat))return false;Rooms.DebugSpawnEncounter(Treasure.Variant.mimicEncounter,players.Count);return true;}if(action==TreasureAction.Complete){CompleteCurrentNode();return true;}return false;
        }
        public void LeaveTreasure(){if(Treasure==null)return;Treasure.Stop();Telemetry.TreasureAction(Treasure.Variant,new ResourceWallet(),Treasure.RewardsTaken);CompleteCurrentNode();}
        public bool BuyMerchantOffer(int offer,int playerIndex)
        {
            if(Merchant==null||playerIndex<0||playerIndex>=players.Count)return false;if(!Merchant.Buy(offer,players[playerIndex],out var bought))return false;Telemetry.MerchantPurchase(bought.Price,bought.Kind.ToString());return true;
        }
        public bool RerollMerchant(){if(Merchant==null||!Merchant.Reroll(out var paid))return false;Telemetry.MerchantReroll(paid);return true;}
        public void LeaveMerchant(){if(Merchant==null)return;Merchant.Close();CompleteCurrentNode();}
        public bool ChooseRest(RestNodeChoice choice,int playerIndex=0,ArmorSlot slot=ArmorSlot.Head)
        {
            if(Rest==null)return false;Combatant player=players.Count==0?null:players[Mathf.Clamp(playerIndex,0,players.Count-1)];bool result=choice==RestNodeChoice.Rest?Rest.Rest(players,RouteModifiers):choice==RestNodeChoice.TemperWeapon?Rest.TemperWeapon(player):choice==RestNodeChoice.TemperArmor?Rest.TemperArmor(player,slot):Rest.Salvage(players);if(result){Telemetry.RestChoice(choice.ToString());CompleteCurrentNode();}return result;
        }
        public bool ChooseEvent(int index)
        {
            if(Event==null||!Event.Choose(index,Progression,players))return false;var choice=Event.Choice;Telemetry.EventChoice(Event.Definition.id,choice.id);Message=Event.Message;if(choice.outcome==EventOutcomeKind.RouteInformation)Route.SetFullReveal(true);
            if(choice.escalationEncounter){eventCombat=true;if(!Flow.TryAdvance(RunState.Combat))return false;Rooms.DebugSpawnEncounter(choice.escalationEncounter,players.Count);return true;}if(choice.outcome==EventOutcomeKind.Equipment){BeginEquipmentReward(RewardFlow.EventEquipment,choice.equipmentQuality);return true;}if(choice.outcome==EventOutcomeKind.Relic){BeginRelicReward(RewardFlow.EventRelic);return true;}CompleteCurrentNode();return true;
        }

        private void EnterTrueFinalBoss()
        {
            RegionCompleteAwaitingFinalGate=false;RouteSelectionOpen=false;StopAllCoroutines();Draft?.Cancel();EquipmentRewards?.Cancel();if(!Flow.DebugSkipToBoss())return;Rooms.Load(Catalog.rooms.Length-1);TeleportParty(new Vector3(0,0,-6));Rooms.SpawnBoss(players.Count);Message=Catalog.boss.displayName;Telemetry.TrueFinalBossEntered();
        }
        private void OnTrueFinalBossDied()
        {
            Telemetry.FinalBossKilled();Progression.Award(new ResourceWallet{ash=28,emberShards=7,ancientAlloy=2,corruptionFragments=1});Progression.RecordBoss(Catalog.boss.id);if(!Flow.TryAdvance(RunState.BossDefeated))return;Rooms.ClearTransientCombat();foreach(var player in players)player.Motor.Stop();Message="The keeper falls.\nFor a moment, the vault is silent.";StartCoroutine(AfterBossDeath());
        }
        private IEnumerator AfterBossDeath(){yield return new WaitForSeconds(2);TryBeginCorruption();}
        public bool TryBeginCorruption(){if(!Flow.TryAdvance(RunState.CorruptionTransition))return false;Message="The fire looks for another vessel.";StartCoroutine(CorruptionTransition());return true;}
        private IEnumerator CorruptionTransition()
        {
            Rooms.ClearTransientCombat();yield return new WaitForSeconds(1.6f);if(!Corruption.Activate(Flow,players,corruptionRandom))yield break;for(int i=0;i<players.Count;i++){float angle=i*Mathf.PI*2/players.Count;players[i].Motor.Teleport(new Vector3(Mathf.Sin(angle),0,-Mathf.Cos(angle))*5.5f);players[i].Health.InvulnerableUntil=Time.time+1.25f;}Telemetry.Record.corruptionType=Catalog.boss.corruption.id;Flow.TryAdvance(RunState.FinalPvP);Message=players.Count==1?"It wears your shape.\nFace what the fire remembers.":"Ash has chosen "+string.Join(" + ",Corruption.CorruptedPlayerIds)+".\nThe violet-crowned stand against the unbound.";
        }

        private void Update()
        {
            if(!Combat)return;if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&Flow.State!=RunState.Lobby){if(DebugOpen)DebugOpen=false;else ManualPaused=!ManualPaused;}MissingDevice=players.FirstOrDefault(x=>x&&!x.GetComponent<PlayerController>().InputSource.Connected)?.Id;Combat.Paused=ManualPaused||DebugOpen||!string.IsNullOrEmpty(MissingDevice);Time.timeScale=Combat.Paused||(Combat.Feedback&&Combat.Feedback.HitStopped)?0:1;
            if(!Combat.Paused){Telemetry.Tick(Time.unscaledDeltaTime,Flow.State);Telemetry.MenuTime(ActiveMenuTelemetryKey,Time.unscaledDeltaTime);if(ChallengeActive&&Flow.State==RunState.Combat){ChallengeRemaining-=Time.unscaledDeltaTime;if(ChallengeRemaining<=0)FailChallenge();}}
        }
        private void LateUpdate()
        {
            if(!Combat||!Combat.Active||players.Count==0)return;if(Flow.State==RunState.FinalPvP){bool unbound=players.Any(x=>x.Alive&&x.Faction==Faction.Wanderers);bool ash=players.Any(x=>x.Alive&&x.Faction==Faction.Corrupted)||(Corruption.Reflection&&Corruption.Reflection.Alive);if(!unbound||!ash)Finish(unbound?"Wanderers":ash?"Corrupted":"Draw",unbound?"The unbound endure.":ash?"The fire has found its heir.":"Only ashes remain.");}
            else if(players.All(x=>!x.Alive)){if(ChallengeActive)FailChallenge(true);else Finish("Hostiles","The vault keeps its silence.");}
        }
        private void FailChallenge(bool revive=false)
        {
            if(!ChallengeActive)return;ChallengeActive=false;var challenge=CurrentNode.Definition.challenge;Telemetry.EncounterEnd(Rooms.CurrentEncounter);Rooms.ClearEnemies();if(Flow.State==RunState.Combat)Flow.TryAdvance(RunState.Reward);Progression.Award(challenge.consolationReward);Telemetry.ChallengeResult(challenge,false);if(revive)foreach(var player in players)player.Restore(.25f);CompleteCurrentNode();
        }
        private void Finish(string winner,string outcome){if(!Flow.TryAdvance(RunState.RunComplete))return;Outcome=outcome;Message=outcome;Rooms.ClearTransientCombat();var settlement=Progression.ResolveRun(winner!="Hostiles",Flow.BossWasDefeated);Telemetry.Progression(Progression,settlement);Telemetry.Finish(players,winner,outcome);}

        public bool DebugSkipToBoss()
        {
            if(Flow.State==RunState.Lobby)StartRun();if(Flow.State==RunState.StartingRun)Flow.TryAdvance(RunState.Exploration);if(!Flow.DebugSkipToBoss())return false;StopAllCoroutines();Draft?.Cancel();EquipmentRewards?.Cancel();CloseNodeSessions();RouteSelectionOpen=false;Rooms.Load(Catalog.rooms.Length-1);TeleportParty(new Vector3(0,0,-6));Rooms.SpawnBoss(players.Count);Message=Catalog.boss.displayName;if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;return true;
        }
        public bool DebugJumpToRoom(int index)
        {
            if(Flow.State==RunState.Lobby)StartRun();if(Flow.State==RunState.StartingRun)Flow.TryAdvance(RunState.Exploration);index=Mathf.Clamp(index,0,Catalog.rooms.Length-2);if(!Flow.DebugJumpToCombat())return false;StopAllCoroutines();Draft?.Cancel();EquipmentRewards?.Cancel();legacyDebugCombat=true;Rooms.Load(index);TeleportParty(new Vector3(0,0,-6));Rooms.SpawnNextWave(players.Count);Message=Catalog.rooms[index].displayName;if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;return true;
        }
        private void LegacyDebugWaveClear(){legacyDebugCombat=false;if(Flow.TryAdvance(RunState.Reward)){Progression.Award(ExpeditionResource.Ash,10);Flow.TryAdvance(RunState.Exploration);Rooms.UnlockExit();}}
        public void DebugGenerateRoute(){if(players.Count==0)return;Route=new ExpeditionRouteRuntime(Catalog.prototypeRegion,Environment.TickCount&int.MaxValue,Mathf.RoundToInt(Progression.EffectPower(MetaEffectKind.RouteReveal)),players.Select(x=>x.Id));RouteSelectionOpen=false;StopAllCoroutines();if(Flow.State==RunState.Combat)Flow.TryAdvance(RunState.Reward);if(Flow.State==RunState.Reward)Flow.TryAdvance(RunState.Exploration);StartCoroutine(EnterCurrentNode());MarkDebug();}
        public void DebugRevealRoute(bool reveal=true){Route?.SetFullReveal(reveal);MarkDebug();}
        public bool DebugJumpToNode(string id){if(Route==null||!Route.DebugEnter(id))return false;StopAllCoroutines();if(Flow.State==RunState.Combat)Flow.TryAdvance(RunState.Reward);if(Flow.State==RunState.Reward)Flow.TryAdvance(RunState.Exploration);StartCoroutine(EnterCurrentNode());MarkDebug();return true;}
        public bool DebugForceNodeType(ExpeditionNodeType type){var node=Route?.Nodes.FirstOrDefault(x=>x.Definition.nodeType==type);return node!=null&&DebugJumpToNode(node.Definition.id);}
        public bool DebugForceTreasure(TreasureVariantKind variant){forcedTreasure=variant;return DebugForceNodeType(ExpeditionNodeType.Treasure);}
        public void DebugAddRunCurrency(){Progression.Award(new ResourceWallet{ash=100,emberShards=20,ancientAlloy=5});MarkDebug();}
        public void DebugSpawnElementalGroup(ElementTag element){if(players.Count>0&&CombatRules.IsCombatState(Flow.State)){Rooms.DebugSpawnElementalGroup(element,players.Count);MarkDebug();}}
        public Combatant DebugSpawnEnemy(EnemyDefinition definition,bool elite=false){if(!definition)return null;if(Flow.State==RunState.Lobby)StartRun();if(!CombatRules.IsCombatState(Flow.State))Flow.DebugJumpToCombat();var enemy=Rooms.DebugSpawnEnemy(definition,Mathf.Max(1,players.Count),elite);MarkDebug();return enemy;}
        public void DebugSpawnEncounter(EncounterDefinition encounter){if(!encounter)return;if(Flow.State==RunState.Lobby)StartRun();if(!CombatRules.IsCombatState(Flow.State))Flow.DebugJumpToCombat();Rooms.DebugSpawnEncounter(encounter,Mathf.Max(1,players.Count));MarkDebug();}
        public void DebugLoadCombatSpace(CombatSpaceDefinition space){if(!space)return;Rooms.View.Build(space,Rooms.RoomIndex);Rooms.View.SetGate(Rooms.ExitOpen);TeleportParty(Rooms.View.EntrancePosition);MarkDebug();}
        public void DebugForceEquipmentReward(WeaponRarity rarity){if(players.Count==0)return;if(Flow.State!=RunState.Reward)Flow.DebugJumpToCombat();Flow.TryAdvance(RunState.Reward);Draft?.Cancel();EquipmentRewards.ForcedRarity=rarity;rewardFlow=RewardFlow.DebugEquipment;EquipmentRewards.Begin(players,1,RewardQuality.Rare);DebugOpen=true;MarkDebug();}
        private void MarkDebug(){if(Telemetry.Record!=null)Telemetry.Record.debugUsed=true;}

        public void ResetRun()
        {
            StopAllCoroutines();if(Progression.RunOpen){var settlement=Progression.ResolveRun(false,Flow.BossWasDefeated,true);Telemetry.Progression(Progression,settlement);}Telemetry.Finish(players,"None","Run reset",true);Rooms.Clear();Corruption.Reset();Draft?.Cancel();EquipmentRewards?.Cancel();foreach(var player in players){Combat.Unregister(player);player.gameObject.SetActive(false);Destroy(player.gameObject);}players.Clear();foreach(var item in FindObjectsByType<ItemPickup>())Destroy(item.gameObject);if(fragment)Destroy(fragment.gameObject);Route=null;CloseNodeSessions();RouteSelectionOpen=AwaitingRouteGate=RegionCompleteAwaitingFinalGate=false;ManualPaused=DebugOpen=false;MissingDevice=null;Time.timeScale=1;Lobby.Unlock();Flow.Reset();Rooms.Load(0);Message="The Cinder Vault";
        }
        private void TeleportParty(Vector3 origin){for(int i=0;i<players.Count;i++)players[i].Motor.Teleport(origin+Vector3.right*(i-(players.Count-1)*.5f)*1.5f);}
        private static RewardQuality Promote(RewardQuality quality)=>(RewardQuality)Mathf.Min((int)RewardQuality.Legendary,(int)quality+1);
        private void OnApplicationQuit(){if(Progression!=null&&Progression.RunOpen){var settlement=Progression.ResolveRun(false,Flow.BossWasDefeated,true);Telemetry.Progression(Progression,settlement);}Telemetry.Finish(players,"None","Application closed",true);}
        private void OnDestroy(){Telemetry.Finish(players.Where(x=>x&&x.Inventory).ToArray(),"None","Scene closed",true);Time.timeScale=1;}
    }
}
