using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using U=Ashbound.PrototypeGui;

namespace Ashbound
{
    public sealed class CampStation:MonoBehaviour
    {
        public HubFacilityKind Kind { get; private set; }
        public string NpcKey { get; private set; }
        public Transform Npc { get; private set; }
        private Transform body;
        private Vector3 origin;
        private Vector3 baseScale;
        private Transform focusTarget;
        private bool focused;
        private float talk;
        public void Configure(HubFacilityKind kind,string npcKey,Transform npc,Transform visual){Kind=kind;NpcKey=npcKey;Npc=npc;body=visual;origin=visual.localPosition;baseScale=visual.localScale;}
        public void Talk(){talk=.7f;}
        public void SetFocus(bool value,Transform target){focused=value;focusTarget=target;}
        private void Update()
        {
            if(!body)return;float bob=Mathf.Sin(Time.unscaledTime*1.7f+(int)Kind)*.035f;body.localPosition=origin+Vector3.up*(bob+(talk>0?Mathf.Sin(Time.unscaledTime*10)*.04f:0));body.localScale=Vector3.Lerp(body.localScale,baseScale*(focused?1.06f:1),Time.unscaledDeltaTime*8);
            if(focused&&focusTarget){Vector3 direction=focusTarget.position-Npc.position;direction.y=0;if(direction.sqrMagnitude>.1f)Npc.rotation=Quaternion.Slerp(Npc.rotation,Quaternion.LookRotation(direction),Time.unscaledDeltaTime*5);}
            if(talk>0)talk-=Time.unscaledDeltaTime;
        }
    }

    public sealed class CampfirePulse:MonoBehaviour
    {
        private Vector3 scale;private Light glow;
        public void Configure(Light light){glow=light;scale=transform.localScale;}
        private void Update(){float wave=1+Mathf.Sin(Time.unscaledTime*6.3f)*.09f+Mathf.Sin(Time.unscaledTime*9.7f)*.04f;transform.localScale=scale*wave;if(glow)glow.intensity=3.3f+Mathf.Sin(Time.unscaledTime*7f)*.35f;}
    }

    public sealed class CampHub:MonoBehaviour
    {
        private static readonly ExpeditionResource[] Resources=(ExpeditionResource[])Enum.GetValues(typeof(ExpeditionResource));
        private readonly List<CampStation> stations=new List<CampStation>();
        private readonly Dictionary<ExpeditionResource,int> previousResources=new Dictionary<ExpeditionResource,int>();
        private readonly Dictionary<ExpeditionResource,float> resourcePulse=new Dictionary<ExpeditionResource,float>();
        private RunManager run;private RoomDirector rooms;private Camera view;private ArenaCamera arenaCamera;private AudioDirector audioDirector;
        private Transform root,avatar,avatarVisual;private CharacterController controller;private CampStation nearby,openStation;
        private bool settingsOpen;private bool resourceHudVisible=true;private string message="";private float panelAlpha;private float gainTimer;private ExpeditionResource lastGain;private int lastGainAmount;
        public static CampHub Instance { get; private set; }
        public bool Active=>run&&run.Flow.State==RunState.Lobby&&root&&root.gameObject.activeSelf;
        public bool ResourceHudVisible=>resourceHudVisible;
        public IReadOnlyList<CampStation> Stations=>stations;
        public Transform Avatar=>avatar;
        public CampStation Nearby=>nearby;
        public HubFacilityDefinition OpenFacility=>openStation?Facility(openStation.Kind):null;
        public int FeedbackCount { get; private set; }

        private void Awake(){Instance=this;}
        public void Configure(RunManager manager,RoomDirector director,Camera camera,ArenaCamera followCamera,AudioDirector audioDirector)
        {
            run=manager;rooms=director;view=camera;arenaCamera=followCamera;this.audioDirector=audioDirector;BuildWorld();
            run.StateChanged+=OnState;run.Progression.ProfileChanged+=ReadResourceChanges;foreach(var resource in Resources)previousResources[resource]=run.Progression.Profile.currencies.Get(resource);
            OnState(run.Flow.State);
        }

        private void BuildWorld()
        {
            root=new GameObject("Expedition Camp · Presentation Foundation").transform;root.SetParent(transform,false);
            PrimitiveFactory.Shape("Camp ground",PrimitiveType.Cube,root,new Vector3(0,-.45f,0),new Vector3(34,.8f,29),new Color(.105f,.105f,.095f),true);
            PrimitiveFactory.Shape("Worn center path",PrimitiveType.Cube,root,new Vector3(0,-.015f,0),new Vector3(7,.05f,24),new Color(.22f,.18f,.12f));
            PrimitiveFactory.Shape("Cross camp path",PrimitiveType.Cube,root,new Vector3(0,-.005f,0),new Vector3(27,.04f,4.5f),new Color(.19f,.16f,.12f));
            BuildFire();BuildProps();BuildStations();BuildAvatar();BuildLights();
        }
        private void BuildFire()
        {
            var fire=new GameObject("Central campfire").transform;fire.SetParent(root,false);
            for(int i=0;i<9;i++){float angle=i*Mathf.PI*2/9;PrimitiveFactory.Shape("Fire ring stone",PrimitiveType.Sphere,fire,new Vector3(Mathf.Cos(angle)*1.15f,.18f,Mathf.Sin(angle)*1.15f),new Vector3(.62f,.35f,.58f),new Color(.24f,.22f,.2f));}
            var logA=PrimitiveFactory.Shape("Fire log",PrimitiveType.Cylinder,fire,new Vector3(0,.32f,0),new Vector3(.18f,1.7f,.18f),new Color(.19f,.08f,.035f));logA.transform.rotation=Quaternion.Euler(0,0,90);
            var logB=PrimitiveFactory.Shape("Fire log",PrimitiveType.Cylinder,fire,new Vector3(0,.34f,0),new Vector3(.18f,1.7f,.18f),new Color(.19f,.08f,.035f));logB.transform.rotation=Quaternion.Euler(90,0,0);
            var flame=PrimitiveFactory.Shape("Procedural flame",PrimitiveType.Sphere,fire,new Vector3(0,1.05f,0),new Vector3(.72f,1.45f,.72f),new Color(1f,.25f,.035f));
            var lightObject=new GameObject("Campfire warm light");lightObject.transform.SetParent(fire,false);lightObject.transform.localPosition=Vector3.up*2.2f;var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=16;light.intensity=3.4f;light.color=new Color(1f,.43f,.18f);light.shadows=LightShadows.Soft;flame.AddComponent<CampfirePulse>().Configure(light);
            Particles(fire,"Campfire smoke",new Vector3(0,1.6f,0),new Color(.22f,.23f,.24f,.42f),1.8f,4.2f,.75f,2.1f);Particles(fire,"Rising embers",new Vector3(0,1.15f,0),new Color(1f,.34f,.05f,.9f),8,1.7f,.1f,1.1f);
        }
        private void Particles(Transform parent,string name,Vector3 position,Color color,float rate,float lifetime,float size,float speed)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=position;var particles=obj.AddComponent<ParticleSystem>();var main=particles.main;main.startLifetime=lifetime;main.startSpeed=speed;main.startSize=size;main.startColor=color;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.Local;var emission=particles.emission;emission.rateOverTime=rate;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.35f;shape.angle=8;particles.Play();
        }
        private void BuildProps()
        {
            Tent(new Vector3(-10,0,8),25,new Color(.22f,.18f,.13f));Tent(new Vector3(10,0,8),-28,new Color(.16f,.19f,.19f));Tent(new Vector3(11,0,-8),-145,new Color(.19f,.15f,.12f));
            for(int i=0;i<10;i++){float a=i*.62f;Crate(new Vector3(Mathf.Cos(a)*(12+i%2),.45f,Mathf.Sin(a)*(10+i%3)),i%2==0?.9f:.65f);}
            for(int i=0;i<8;i++){float x=-25+i*7;PrimitiveFactory.Shape("Distant ruin silhouette",PrimitiveType.Cube,root,new Vector3(x,2.2f,18+(i%3)*3),new Vector3(2.5f,4+i%4,2.2f),new Color(.04f,.055f,.065f));}
            for(int i=0;i<6;i++)PrimitiveFactory.Shape("Boundary rock",PrimitiveType.Sphere,root,new Vector3(-15+i*6,.4f,-13+(i%2)*1.5f),new Vector3(2.2f+i%2,.8f,1.6f),new Color(.12f,.13f,.13f),true);
        }
        private void Tent(Vector3 position,float yaw,Color color)
        {
            var tent=new GameObject("Field tent").transform;tent.SetParent(root,false);tent.localPosition=position;tent.rotation=Quaternion.Euler(0,yaw,0);
            var canvas=PrimitiveFactory.Shape("Tent canvas",PrimitiveType.Cube,tent,new Vector3(0,1.3f,0),new Vector3(4.2f,.12f,5.2f),color,true);canvas.transform.localRotation=Quaternion.Euler(0,0,22);
            var canvas2=PrimitiveFactory.Shape("Tent canvas",PrimitiveType.Cube,tent,new Vector3(0,1.3f,0),new Vector3(4.2f,.12f,5.2f),color,true);canvas2.transform.localRotation=Quaternion.Euler(0,0,-22);
        }
        private void Crate(Vector3 position,float size){PrimitiveFactory.Shape("Supply crate",PrimitiveType.Cube,root,position,new Vector3(size,size,size),new Color(.26f,.16f,.075f),true);}
        private void BuildStations()
        {
            Station(HubFacilityKind.ExpeditionTable,"expedition",new Vector3(1.8f,0,9.5f),new Color(.38f,.31f,.18f),PrimitiveType.Cube);
            Station(HubFacilityKind.Forge,"forge",new Vector3(-10,0,1.5f),new Color(.32f,.18f,.12f),PrimitiveType.Cylinder);
            Station(HubFacilityKind.Quartermaster,"quartermaster",new Vector3(10,0,1.7f),new Color(.28f,.23f,.14f),PrimitiveType.Cube);
            Station(HubFacilityKind.Infirmary,"infirmary",new Vector3(8.5f,0,-8.2f),new Color(.22f,.28f,.25f),PrimitiveType.Capsule);
            Station(HubFacilityKind.ResearchStation,"research",new Vector3(-8.5f,0,-8.3f),new Color(.19f,.22f,.31f),PrimitiveType.Sphere);
            Station(HubFacilityKind.Archive,"archive",new Vector3(-1.2f,0,-11.2f),new Color(.26f,.18f,.13f),PrimitiveType.Cube);
        }
        private void Station(HubFacilityKind kind,string key,Vector3 position,Color propColor,PrimitiveType propShape)
        {
            var anchor=new GameObject(kind+" station");anchor.transform.SetParent(root,false);anchor.transform.localPosition=position;var station=anchor.AddComponent<CampStation>();
            PrimitiveFactory.Shape(kind+" prop",propShape,anchor.transform,new Vector3(0,.55f,0),new Vector3(kind==HubFacilityKind.ExpeditionTable?3.5f:2.2f,kind==HubFacilityKind.ExpeditionTable?.75f:1.4f,kind==HubFacilityKind.ExpeditionTable?2.2f:1.8f),propColor,true);
            if(kind==HubFacilityKind.Forge){PrimitiveFactory.Shape("Anvil",PrimitiveType.Cube,anchor.transform,new Vector3(0,1.45f,0),new Vector3(1.35f,.25f,.55f),new Color(.36f,.4f,.43f));var forgeLight=new GameObject("Forge glow").AddComponent<Light>();forgeLight.transform.SetParent(anchor.transform,false);forgeLight.transform.localPosition=new Vector3(0,2,0);forgeLight.type=LightType.Point;forgeLight.range=7;forgeLight.intensity=1.8f;forgeLight.color=new Color(1,.25f,.06f);}
            var npc=new GameObject("NPC · "+key).transform;npc.SetParent(anchor.transform,false);npc.localPosition=new Vector3(kind==HubFacilityKind.ExpeditionTable?-2.4f:kind==HubFacilityKind.Archive?2.1f:1.8f,0,kind==HubFacilityKind.ExpeditionTable?-.4f:0);
            var visual=new GameObject("Animated NPC placeholder").transform;visual.SetParent(npc,false);Color npcColor=kind==HubFacilityKind.Infirmary?new Color(.64f,.72f,.66f):kind==HubFacilityKind.ResearchStation?new Color(.35f,.42f,.64f):new Color(.48f,.32f,.2f);
            PrimitiveFactory.Shape("NPC body",PrimitiveType.Capsule,visual,new Vector3(0,1.1f,0),new Vector3(.75f,1.05f,.75f),npcColor);
            PrimitiveFactory.Shape("NPC head",PrimitiveType.Sphere,visual,new Vector3(0,2.25f,0),new Vector3(.62f,.62f,.62f),new Color(.58f,.45f,.34f));
            PrimitiveFactory.Shape("NPC facing",PrimitiveType.Cube,visual,new Vector3(0,2.28f,.32f),new Vector3(.28f,.12f,.12f),Palette.Gold);
            station.Configure(kind,key,npc,visual);stations.Add(station);
        }
        private void BuildAvatar()
        {
            var obj=new GameObject("Camp wanderer");obj.transform.SetParent(root,false);obj.transform.localPosition=new Vector3(0,1,-4);avatar=obj.transform;controller=obj.AddComponent<CharacterController>();controller.height=2.1f;controller.radius=.45f;controller.center=new Vector3(0,1.05f,0);
            avatarVisual=new GameObject("Procedural camp avatar").transform;avatarVisual.SetParent(avatar,false);PrimitiveFactory.Shape("Wanderer body",PrimitiveType.Capsule,avatarVisual,new Vector3(0,1.1f,0),new Vector3(.8f,1.1f,.8f),Palette.Player);PrimitiveFactory.Shape("Wanderer face",PrimitiveType.Cube,avatarVisual,new Vector3(0,2.2f,.35f),new Vector3(.35f,.14f,.18f),Palette.Gold);
        }
        private void BuildLights(){var fill=new GameObject("Cool moon fill").AddComponent<Light>();fill.transform.SetParent(root,false);fill.transform.localPosition=new Vector3(-6,12,-8);fill.type=LightType.Point;fill.range=32;fill.intensity=1.1f;fill.color=new Color(.28f,.43f,.7f);}

        private void Update()
        {
            if(!Active)return;foreach(var resource in Resources)resourcePulse[resource]=Mathf.Max(0,resourcePulse.GetValueOrDefault(resource)-Time.unscaledDeltaTime);foreach(var station in stations)station.SetFocus(station==openStation||station==nearby,avatar);
            gainTimer=Mathf.Max(0,gainTimer-Time.unscaledDeltaTime);panelAlpha=Mathf.MoveTowards(panelAlpha,openStation||settingsOpen?1:0,Time.unscaledDeltaTime*5);
            var keyboard=Keyboard.current;var pad=Gamepad.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame||(pad!=null&&pad.startButton.wasPressedThisFrame)){if(openStation)ClosePanel();else settingsOpen=!settingsOpen;audioDirector?.Emit(AudioCue.UiBack);}
            if(run.DebugOpen||openStation||settingsOpen){AnimateAvatar(Vector2.zero);return;}
            Vector2 input=Vector2.zero;if(keyboard!=null){if(keyboard.wKey.isPressed)input.y++;if(keyboard.sKey.isPressed)input.y--;if(keyboard.aKey.isPressed)input.x--;if(keyboard.dKey.isPressed)input.x++;}if(pad!=null&&pad.leftStick.ReadValue().sqrMagnitude>input.sqrMagnitude)input=pad.leftStick.ReadValue();input=Vector2.ClampMagnitude(input,1);
            Vector3 motion=new Vector3(input.x,0,input.y);controller.Move((motion*5.4f+Vector3.down*2)*Time.unscaledDeltaTime);Vector3 local=avatar.localPosition;local.x=Mathf.Clamp(local.x,-15,15);local.z=Mathf.Clamp(local.z,-12.5f,13);avatar.localPosition=local;if(motion.sqrMagnitude>.01f)avatar.rotation=Quaternion.Slerp(avatar.rotation,Quaternion.LookRotation(motion),Time.unscaledDeltaTime*12);AnimateAvatar(input);
            nearby=stations.OrderBy(x=>Vector3.Distance(avatar.position,x.Npc.position)).FirstOrDefault();if(nearby&&Vector3.Distance(avatar.position,nearby.Npc.position)>3.2f)nearby=null;
            if(nearby&&((keyboard!=null&&keyboard.fKey.wasPressedThisFrame)||(pad!=null&&pad.buttonSouth.wasPressedThisFrame)))Open(nearby.Kind);
        }
        private void AnimateAvatar(Vector2 input){if(!avatarVisual)return;float speed=input.magnitude;avatarVisual.localPosition=Vector3.up*(Mathf.Sin(Time.unscaledTime*(speed>.05f?11:2.2f))*(speed>.05f?.09f:.025f));avatarVisual.localRotation=Quaternion.Euler(speed>.05f?Mathf.Sin(Time.unscaledTime*11)*4:0,0,0);}
        private void OnState(RunState state)
        {
            bool camp=state==RunState.Lobby;if(root)root.gameObject.SetActive(camp);if(rooms)rooms.gameObject.SetActive(!camp);if(camp){openStation=null;settingsOpen=false;if(avatar)avatar.localPosition=new Vector3(0,1,-4);arenaCamera?.SetLobbyTarget(avatar);arenaCamera?.SetLobbyFocus(null);audioDirector?.Emit(AudioCue.Campfire);}else{nearby=null;arenaCamera?.SetLobbyFocus(null);}
        }
        private HubFacilityDefinition Facility(HubFacilityKind kind)=>run.Catalog.facilities.FirstOrDefault(x=>x&&x.kind==kind);
        public void Open(HubFacilityKind kind){openStation=stations.FirstOrDefault(x=>x.Kind==kind);settingsOpen=false;if(openStation){openStation.Talk();arenaCamera?.SetLobbyFocus(openStation.Npc.position);audioDirector?.Emit(kind==HubFacilityKind.ExpeditionTable?AudioCue.MapInteraction:kind==HubFacilityKind.Forge?AudioCue.ForgeHammer:AudioCue.NpcInteraction);}}
        public void ClosePanel(){openStation=null;arenaCamera?.SetLobbyFocus(null);audioDirector?.Emit(AudioCue.UiBack);}
        public void TeleportTo(HubFacilityKind kind){var station=stations.FirstOrDefault(x=>x.Kind==kind);if(!station||!avatar)return;controller.enabled=false;avatar.position=station.Npc.position+Vector3.back*2;controller.enabled=true;nearby=station;arenaCamera?.SnapToTargets();}
        public void ToggleResourceHud(){resourceHudVisible=!resourceHudVisible;}
        public void SwitchLanguage(){LocalizationService.SetLanguage(LocalizationService.IsChinese?GameLanguage.English:GameLanguage.SimplifiedChinese);}
        public void TestResourceGain(){run.Progression.DebugAdd(ExpeditionResource.Ash,8);}
        public void TestNpcInteraction(){Open(nearby?nearby.Kind:HubFacilityKind.ExpeditionTable);}
        public void TestCamera(){arenaCamera?.SetLobbyFocus(openStation?openStation.Npc.position:(Vector3?)new Vector3(8,0,2));arenaCamera?.SnapToTargets();}
        public bool LaunchExpedition(){if(!Active)return false;return run.StartRun();}
        public bool TryUpgradeOpen(){var facility=OpenFacility;if(!facility)return false;bool result=run.Progression.TryUpgrade(facility,out message);if(result){message=LocalizationService.Text("camp.available","Upgrade complete");audioDirector?.Emit(AudioCue.UiConfirm);}return result;}
        private void ReadResourceChanges()
        {
            foreach(var resource in Resources){int value=run.Progression.Profile.currencies.Get(resource);int before=previousResources.GetValueOrDefault(resource,value);if(value>before){lastGain=resource;lastGainAmount=value-before;gainTimer=2;resourcePulse[resource]=1;FeedbackCount++;audioDirector?.Emit(AudioCue.ResourceGain);}previousResources[resource]=value;}
        }

        private void OnGUI()
        {
            if(!Active)return;GUI.depth=-5;Matrix4x4 old=U.Scale();try{U.Label(28,22,480,40,LocalizationService.Text("camp.title","ASHBOUND CAMP"),U.Heading);U.Label(28,57,480,25,LocalizationService.Text("camp.subtitle","The next expedition begins here."),U.Small);if(resourceHudVisible)ResourceHud();WorldPrompt();if(openStation)FacilityPanel();else if(settingsOpen)SettingsPanel();else U.Label(28,674,760,26,LocalizationService.Text("camp.controls.body","WASD / left stick move · F / A interact · Esc settings · F1 developer tools"),U.Small);}finally{GUI.matrix=old;}
        }
        private void ResourceHud()
        {
            float x=742;U.Panel(new Rect(x,18,510,58));for(int i=0;i<Resources.Length;i++){var resource=Resources[i];float bx=x+12+i*122;float pulse=resourcePulse.GetValueOrDefault(resource);if(pulse>0)U.Box(new Rect(bx-3,24,117,44),new Color(1,.52f,.18f,.16f*pulse));GUI.Box(new Rect(bx,28,32,32),new GUIContent(ResourceIconLibrary.Icon(resource),LocalizationService.ResourceDescription(resource)));U.Label(bx+39,31,70,29,run.Progression.Profile.currencies.Get(resource).ToString(),U.CardTitle);}if(!string.IsNullOrEmpty(GUI.tooltip)){Vector2 mouse=Event.current.mousePosition;U.Panel(new Rect(Mathf.Clamp(mouse.x,760,980),85,260,68));U.Label(Mathf.Clamp(mouse.x,760,980)+10,92,240,54,GUI.tooltip,U.Small);}if(gainTimer>0)U.Label(980,82,245,24,"+"+lastGainAmount+" "+LocalizationService.ResourceName(lastGain),U.CardTitle);
        }
        private void WorldPrompt()
        {
            if(!nearby||openStation||settingsOpen)return;Vector3 screen=view.WorldToScreenPoint(nearby.Npc.position+Vector3.up*3);if(screen.z<=0)return;float x=screen.x*1280/Screen.width,y=(Screen.height-screen.y)*720/Screen.height;string title=LocalizationService.Text("npc."+nearby.NpcKey+".name",nearby.NpcKey)+" · "+LocalizationService.Text("npc."+nearby.NpcKey+".title",nearby.Kind.ToString());U.Panel(new Rect(x-150,y-20,300,70));U.Label(x-140,y-13,280,24,title,U.CardTitle);U.Label(x-140,y+17,280,24,LocalizationService.Text("camp.interact","F / A — interact"),U.Small);
        }
        private void FacilityPanel()
        {
            var facility=OpenFacility;if(!facility)return;GUI.color=new Color(1,1,1,Mathf.Max(.08f,panelAlpha));U.Box(new Rect(0,0,1280,720),new Color(.015f,.018f,.025f,.72f));U.Panel(new Rect(175,105,930,520));var progress=run.Progression.Profile.Facility(facility.id);string npcKey=openStation.NpcKey;
            U.Label(210,132,650,34,LocalizationService.FacilityName(facility)+"  ·  "+LocalizationService.Text("camp.level","LEVEL")+" "+progress.level+" / "+facility.MaxLevel,U.Heading);U.Label(210,170,650,45,LocalizationService.FacilityDescription(facility),U.Small);U.Label(210,224,650,27,LocalizationService.Text("npc."+npcKey+".name",npcKey)+" · "+LocalizationService.Text("npc."+npcKey+".title",facility.kind.ToString()),U.CardTitle);U.Label(210,255,650,36,"“"+LocalizationService.Text("npc."+npcKey+".line","")+"”",U.Text);
            if(facility.kind==HubFacilityKind.ExpeditionTable)ExpeditionPanel();else if(facility.kind==HubFacilityKind.Archive)ArchivePanel();
            float y=facility.kind==HubFacilityKind.ExpeditionTable?467:350;if(progress.level<facility.MaxLevel){var tier=facility.tiers[progress.level];U.Label(210,y,520,48,LocalizationService.Text("camp.next","NEXT")+" · "+LocalizationService.TierName(tier)+"\n"+LocalizationService.TierDescription(tier),U.Small);DrawCost(tier.cost,750,y);bool prerequisite=string.IsNullOrEmpty(tier.prerequisiteFacilityId)||run.Progression.Profile.Facility(tier.prerequisiteFacilityId).level>=tier.prerequisiteLevel;bool affordable=run.Progression.Profile.currencies.CanAfford(tier.cost);var prerequisiteFacility=run.Catalog.facilities.FirstOrDefault(x=>x&&x.id==tier.prerequisiteFacilityId);string prerequisiteText=string.IsNullOrEmpty(tier.prerequisiteFacilityId)?LocalizationService.Text("camp.prerequisite.none","Prerequisite: none"):LocalizationService.Text("camp.prerequisite","Prerequisite")+": "+LocalizationService.FacilityName(prerequisiteFacility)+" "+tier.prerequisiteLevel;U.Label(750,y+31,305,24,prerequisiteText,U.Small);GUI.enabled=prerequisite&&affordable;if(U.Click(new Rect(750,y+61,305,38),LocalizationService.Text("camp.upgrade","Upgrade")+" · "+(prerequisite?(affordable?LocalizationService.Text("camp.available","Available"):LocalizationService.Text("camp.unaffordable","Insufficient resources")):LocalizationService.Text("camp.locked","Prerequisite required"))))TryUpgradeOpen();GUI.enabled=true;}else U.Label(210,y,600,32,LocalizationService.Text("camp.complete","Facility development complete"),U.CardTitle);
            if(!string.IsNullOrEmpty(message))U.Label(210,576,530,25,message,U.Small);if(U.Click(new Rect(890,565,165,38),LocalizationService.Text("camp.back","Back to camp")))ClosePanel();GUI.color=Color.white;
        }
        private void ExpeditionPanel()
        {
            U.Label(210,305,370,23,LocalizationService.Text("camp.preparation","ONE PREPARATION"),U.CardTitle);int i=0;foreach(var prep in run.Catalog.preparations){bool available=run.Progression.PreparationAvailable(prep);GUI.enabled=available;if(U.Click(new Rect(210+(i%2)*205,333+(i/2)*32,195,27),(run.Progression.Profile.selectedPreparation==prep.id?"✓ ":"")+LocalizationService.PreparationName(prep)))run.Progression.SelectPreparation(prep);GUI.enabled=true;i++;}
            U.Label(650,305,390,23,LocalizationService.Text("camp.party","LOCAL PARTY"),U.CardTitle);for(int p=0;p<run.Lobby.Slots.Count;p++)U.Label(650,333+p*22,390,20,run.Lobby.Slots[p].PlayerId+" · "+run.Lobby.Slots[p].DeviceLabel,U.Small);
            GUI.enabled=run.Lobby.Slots.Count<4&&run.Lobby.Slots.All(s=>s.InputKind!=InputKind.SecondKeyboard);if(U.Click(new Rect(650,390,190,29),LocalizationService.Text("camp.add.keyboard","Add shared keyboard")))run.Lobby.TryJoin(InputKind.SecondKeyboard,-2,"Shared keyboard");GUI.enabled=run.Lobby.Slots.Count>1;if(U.Click(new Rect(850,390,190,29),LocalizationService.Text("camp.remove","Remove last")))run.Lobby.RemoveLast();GUI.enabled=true;
            if(U.Click(new Rect(750,430,290,40),LocalizationService.Text("camp.launch","LAUNCH EXPEDITION")+" · "+run.Lobby.Slots.Count))LaunchExpedition();
        }
        private void ArchivePanel(){var profile=run.Progression.Profile;U.Label(210,305,800,70,"Expeditions "+profile.lifetime.expeditionsStarted+"  ·  Completed "+profile.lifetime.expeditionsCompleted+"  ·  Bosses "+profile.lifetime.bossesDefeated+"\nRecovered: "+(profile.discoveredLore.Count==0?"—":string.Join(" · ",profile.discoveredLore)),U.Small);}
        private void DrawCost(ResourceWallet cost,float x,float y){int shown=0;foreach(var resource in Resources){int amount=cost?.Get(resource)??0;if(amount<=0)continue;GUI.DrawTexture(new Rect(x+shown*72,y,24,24),ResourceIconLibrary.Icon(resource));U.Label(x+27+shown*72,y+1,42,22,amount.ToString(),U.Small);shown++;}}
        private void SettingsPanel()
        {
            U.Box(new Rect(0,0,1280,720),new Color(.01f,.015f,.02f,.75f));U.Panel(new Rect(380,145,520,420));U.Label(420,180,440,36,LocalizationService.Text("camp.settings","SETTINGS"),U.Heading);U.Label(420,234,200,25,LocalizationService.Text("camp.language","LANGUAGE"),U.CardTitle);if(U.Click(new Rect(420,270,205,38),"English"))LocalizationService.SetLanguage(GameLanguage.English);if(U.Click(new Rect(645,270,205,38),"简体中文"))LocalizationService.SetLanguage(GameLanguage.SimplifiedChinese);U.Label(420,330,430,24,LocalizationService.Text("camp.controls","CONTROLS"),U.CardTitle);U.Label(420,363,430,95,LocalizationService.Text("camp.controls.body","Move: WASD / left stick\nInteract: F / A\nSettings: Esc / Start\nDeveloper tools: F1"),U.Small);if(U.Click(new Rect(420,490,205,38),LocalizationService.Text("camp.close","Close")))settingsOpen=false;if(U.Click(new Rect(645,490,205,38),LocalizationService.Text("camp.exit","Exit game")))Application.Quit();
        }
        private void OnDestroy(){if(run!=null){run.StateChanged-=OnState;if(run.Progression!=null)run.Progression.ProfileChanged-=ReadResourceChanges;}if(Instance==this)Instance=null;ResourceIconLibrary.Dispose();}
    }
}
