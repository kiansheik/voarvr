using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;
using VoarVR.World;
using Object=UnityEngine.Object;

namespace VoarVR.Editor
{
    // Explicit desktop fixtures. Never runs on import and never uses persistent storage.
    // Run() / RunSpecies("Duck") / CaptureVisualFixtures() while BirdFlight is playing.
    public static class RouteHomeReview
    {
        public static string Folder="../artifacts/reviews/route-home/round-01";
        public static string Status {get;private set;}="Idle";
        private static bool running;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()=>Start(new[]{"Duck","Dragon","Magpie"},false);
        public static string RunSpecies(string species)=>Start(new[]{species},false);
        public static string CaptureVisualFixtures()=>Start(new[]{"Duck"},true);
        public static string CaptureGuidanceFixtures()=>Start(new[]{"Duck","Magpie","Dragon"},false,true);
        private static string Start(string[] species,bool visuals,bool guidance=false)
        {
            if(!Application.isPlaying)return "Enter Play Mode in BirdFlight first.";
            if(running)return Status;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();
            if(driver==null || driver.Controller==null || Object.FindAnyObjectByType<WorldStreamer>()==null)return "A ready BirdFlight scene is required.";
            running=true;Status=guidance?"Capturing staged runtime guidance":visuals?"Capturing labeled visual fixtures":"Running controller-input-only Route Home";
            driver.StartCoroutine(Session(driver,species,visuals,guidance));return Status;
        }
        private static IEnumerator Session(BirdFlightDriver original,string[] species,bool visuals,bool guidance)
        {
            Directory.CreateDirectory(Folder);
            try
            {
                using(var scope=new SceneScope(original))
                {
                    if(guidance)
                    {
                        using(var preferences=new GuidancePreferences())yield return GuidanceVisuals(scope,species);
                    }
                    else if(visuals){yield return Visuals(scope);yield return CarriedSeedViews(scope);}
                    else foreach(var name in species)yield return Fly(scope,name);
                    File.WriteAllText(Path.Combine(Folder,"storage-isolation.txt"),"PlayerPrefs unchanged: "+scope.PrefsUnchanged+"\nMemory-only IFlightSaveStorage; original director state restored after fixture.\n");
                }
                Status="Finished: "+Folder;
            }
            finally {running=false;if(!Status.StartsWith("Finished",StringComparison.Ordinal))Status="Interrupted; inspect Console and partial report in "+Folder;}
        }

        // Stage/checkpoint and tracking samples are explicit fixtures. Visibility uses
        // the actual runtime Tick path, wind probes, FlightCamera and RouteBearing.
        private static IEnumerator GuidanceVisuals(SceneScope scope,string[] species)
        {
            var evidence=new GuidanceReport {EditorVersion=Application.unityVersion,Scene=scope.Original.gameObject.scene.path};
            foreach(var name in species)
            {
                using(var fixture=new PilotFixture(scope,name))
                using(var camera=new GuidanceCamera(scope,fixture))
                {
                    var bearing=fixture.Driver.gameObject.AddComponent<RouteBearing>();
                    bearing.Configure(fixture.Driver,fixture.Presentation,scope.Camera);
                    float clock=100;
                    if(name=="Duck")
                    {
                        var positions=new[]{new Vector3(0,25,0),new Vector3(100,160,154),new Vector3(140,245,150),new Vector3(420,287,500),new Vector3(420,276,598)};
                        var labels=new[]{"discover-lift","soar","find-seed","arch-with-seed","land-with-seed"};
                        for(int stage=0;stage<positions.Length;stage++)
                        {
                            StageGuidance(fixture,stage,clock);
                            yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"stage-"+stage+"-"+labels[stage],positions[stage],Quaternion.Euler(0,stage==0?35:0,0),FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                            clock+=2;
                        }
                        StageGuidance(fixture,1,clock);
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"soar-above-departure-column",new Vector3(100,335,154),Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                        clock+=2;scope.Wind.SetMode(WindMode.StillAir);
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"soar-still-air",new Vector3(100,160,154),Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                        scope.Wind.SetMode(WindMode.Assisted);clock+=2;
                    }
                    if(name=="Magpie")
                    {
                        StageGuidance(fixture,2,clock);
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"quest-position-seed-behind-1138m",new Vector3(1047.3629f,317.8151f,-483.2899f),Quaternion.Euler(0,94.54f,0),FlightControlMode.Beginner,FlightViewMode.ThirdPerson,clock,0,0,Vector3.zero);
                        clock+=2;
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"quest-position-seed-reacquired-1138m",new Vector3(1047.3629f,317.8151f,-483.2899f),Quaternion.Euler(0,94.54f,0),FlightControlMode.Beginner,FlightViewMode.ThirdPerson,clock,-147.56f,10,Vector3.zero);
                        clock+=2;StageGuidance(fixture,2,clock);
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"quest-position-seed-behind-370m",new Vector3(384.0333f,278.1418f,475.8159f),Quaternion.Euler(0,-12.17f,0),FlightControlMode.Beginner,FlightViewMode.ThirdPerson,clock,0,0,Vector3.zero);
                        clock+=2;
                    }
                    StageGuidance(fixture,2,clock);
                    var seed=RouteHomeChapter.SeedPosition;
                    var before=new LogicalPosition(seed.X,seed.Y,seed.Z-12);
                    fixture.Director.Challenge.Step(GuidanceObservation(before)); // Restore anchor only.
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"pickup-before",new Vector3((float)before.X,(float)before.Y,(float)before.Z),Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                    var after=new LogicalPosition(seed.X,seed.Y,seed.Z-5);
                    fixture.Director.Challenge.Step(GuidanceObservation(after));
                    if(!fixture.Director.Challenge.SeedCollected)throw new InvalidOperationException("Guidance fixture did not physically cross the seed pickup radius.");
                    clock+=.1f;
                    var pickupPosition=new Vector3((float)after.X,(float)after.Y,(float)after.Z);
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"pickup-transfer-start",pickupPosition,Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                    clock+=.4f;
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"pickup-transfer-midpoint",pickupPosition,Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                    clock+=.5f;
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"pickup-settled",pickupPosition,Quaternion.identity,FlightControlMode.Beginner,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                    clock+=2;
                    var approach=new Vector3(350,290,430);
                    foreach(var pose in new[]{"beginner","advanced-embodied","advanced-stabilized"})
                    {
                        bool advanced=pose!="beginner";
                        PlayerPrefs.SetInt(FlightPreferences.KeyPrefix+"FirstPersonStabilized",pose=="advanced-stabilized"?1:0);
                        var mode=advanced?FlightControlMode.Acrobatic:FlightControlMode.Beginner;
                        var body=Quaternion.Euler(advanced?-10:0,29,advanced?14:0);
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"carried-"+pose+"-neutral-head",approach,body,mode,FlightViewMode.FirstPerson,clock,0,0,Vector3.zero);
                        clock+=2;
                        yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"carried-"+pose+"-head-yaw60-up30-shift",approach,body,mode,FlightViewMode.FirstPerson,clock,60,30,new Vector3(.18f,.08f,.14f));
                        clock+=2;
                    }
                    PlayerPrefs.SetInt(FlightPreferences.KeyPrefix+"FirstPersonStabilized",0);
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"carried-third-person-neutral-head",approach,Quaternion.Euler(0,29,0),FlightControlMode.Beginner,FlightViewMode.ThirdPerson,clock,0,0,Vector3.zero);
                    clock+=2;
                    yield return GuidanceShot(scope,fixture,camera,bearing,evidence,"carried-third-person-head-yaw60-up30-shift",approach,Quaternion.Euler(0,29,0),FlightControlMode.Beginner,FlightViewMode.ThirdPerson,clock,60,30,new Vector3(.18f,.08f,.14f));
                    bearing.enabled=false;
                }
                yield return null;
            }
            File.WriteAllText(Path.Combine(Folder,"runtime-guidance-fixtures.json"),JsonUtility.ToJson(evidence,true));
            File.WriteAllText(Path.Combine(Folder,"runtime-guidance-fixtures.txt"),evidence.Evidence+"\n"+evidence.CameraEvidence+"\n"+evidence.Limits+"\nCaptured "+evidence.Frames.Count+" labeled states. See JSON for exact stages, positions, view samples and visible runtime cues.\nSee storage-isolation.txt for the final restored save check.\n");
        }

        private static void StageGuidance(PilotFixture fixture,int stage,float clock)
        {
            var checkpoint=fixture.Director.Challenge.Capture();
            checkpoint.Stage=stage;checkpoint.Status=ChallengeStatus.Active;checkpoint.Score=0;
            checkpoint.SeedCollected=stage>=3;checkpoint.ActiveSeconds=clock;
            checkpoint.HighestAltitude=stage>=2?245:stage==1?160:25;checkpoint.SoaringGain=stage>=2?100:stage==1?42:0;
            if(!fixture.Director.Challenge.Restore(checkpoint))throw new InvalidOperationException("Invalid staged RouteHome checkpoint: "+stage);
        }
        private static ChallengeObservation GuidanceObservation(LogicalPosition p)=>new ChallengeObservation(p,Vector3.forward*12,4,1,.1f,false,0,0);

        private static IEnumerator GuidanceShot(SceneScope scope,PilotFixture fixture,GuidanceCamera camera,RouteBearing bearing,GuidanceReport report,string label,Vector3 logical,Quaternion body,FlightControlMode mode,FlightViewMode view,float clock,float headYaw,float headUp,Vector3 headTranslation)
        {
            var position=scope.Space.ToLocal(logical.x,logical.y,logical.z);
            var controller=fixture.Driver.Controller;controller.SetControlMode(mode);controller.StreamingBlocked=false;
            typeof(BirdFlightController).GetProperty("State").SetValue(controller,new BirdState {Position=position,Velocity=body*Vector3.forward*12,Rotation=body,Phase=FlightPhase.Gliding});
            Set(controller,"simulationTime",clock);Set(fixture.Driver,"comfortYaw",body.eulerAngles.y);Set(fixture.Driver,"viewMode",view);
            fixture.Driver.transform.SetPositionAndRotation(position,body);
            for(int i=0;i<240;i++)scope.World.TickStreaming(position);
            scope.Sky.Tick(position);scope.Weather?.Tick(position);Physics.SyncTransforms();
            camera.Apply(headYaw,headUp,headTranslation);
            fixture.Presentation.Tick(position);bearing.TickBearing();
            yield return null;
            // Refresh after LateUpdate with the exact same runtime head sample before
            // rendering. No direct Present() call bypasses guidance eligibility.
            camera.Apply(headYaw,headUp,headTranslation);bearing.TickBearing();
            string file="runtime-guidance-"+fixture.Driver.gameObject.name.Replace("Route Home ","").Replace(" input fixture","").ToLowerInvariant()+"-"+label;
            DuckReview.CaptureCurrent(Folder,file);
            var challenge=fixture.Director.Challenge;var target=fixture.Presentation.TargetMarkerWorldPosition;
            report.Frames.Add(new GuidanceFrame {File=file+".png",Stage=challenge.Stage,Kind=challenge.Kind.ToString(),SeedCollected=challenge.SeedCollected,SeedVisible=fixture.Presentation.SeedVisible,CarryVisible=fixture.Presentation.CarryVisible,PickupTransfer=fixture.Presentation.PickupTransferActive,GuideVisible=fixture.Presentation.GuideVisible,TargetVisible=fixture.Presentation.TargetMarkerVisible,BearingVisible=bearing.Visible,NavigationAvailable=fixture.Presentation.NavigationAvailable,LogicalPosition=logical,CameraPosition=scope.Camera.transform.position,CameraEuler=scope.Camera.transform.eulerAngles,TargetWorldPosition=target,BearingViewport=scope.Camera.WorldToViewportPoint(bearing.CuePosition),TargetViewport=scope.Camera.WorldToViewportPoint(target),View=view.ToString(),ControlMode=mode.ToString(),HeadYaw=headYaw,HeadUp=headUp,HeadTranslation=headTranslation,SimulationTime=clock,WindMode=scope.Wind.ModeName,FlightTextVisible=fixture.Driver.ShowFlightText});
            Status="Captured "+file;
        }

        private sealed class GuidanceCamera:IDisposable
        {
            private readonly VoarVR.Core.FlightCamera camera;private readonly PilotFixture fixture;private readonly FlightInputFrame neutral;
            public GuidanceCamera(SceneScope scope,PilotFixture actor)
            {
                fixture=actor;neutral=FlightInputFrame.Neutral;neutral.HeadTracked=true;neutral.BodyTracked=true;neutral.BodyOrientation=Quaternion.identity;
                neutral.HeadPosition=new Vector3(0,1.65f,0);neutral.LeftWing.Position=new Vector3(-.7f,1.25f,.1f);neutral.RightWing.Position=new Vector3(.7f,1.25f,.1f);
                if(!fixture.Driver.Calibration.CaptureComfortableGlide(neutral))throw new InvalidOperationException("The fixture's tracked neutral was rejected.");
                typeof(BirdFlightDriver).GetProperty("UsesXR").SetValue(fixture.Driver,true);
                camera=scope.Camera.gameObject.AddComponent<VoarVR.Core.FlightCamera>();Set(camera,"bird",fixture.Driver);
                // Keep real ApplyPose including tracked translation, while an unbound
                // tracked action prevents any connected Editor headset replacing the
                // explicit sample. All original camera bindings are restored on dispose.
                ((UnityEngine.InputSystem.InputAction)typeof(VoarVR.Core.FlightCamera).GetField("tracked",Private).GetValue(camera)).Dispose();
                var stagedTracking=new UnityEngine.InputSystem.InputAction("Review fixture tracking is supplied explicitly");stagedTracking.Enable();Set(camera,"tracked",stagedTracking);
            }
            public void Apply(float yaw,float up,Vector3 translation)
            {
                var sample=neutral;sample.HeadPosition+=translation;sample.HeadOrientation=Quaternion.Euler(-up,yaw,0);sample.LookDirection=sample.HeadOrientation*Vector3.forward;
                fixture.Input.Frame=sample;typeof(BirdFlightController).GetProperty("LastInput").SetValue(fixture.Driver.Controller,sample);
                Set(camera,"lastValidPosition",sample.HeadPosition);Set(camera,"lastValidRotation",sample.HeadOrientation);
                typeof(VoarVR.Core.FlightCamera).GetMethod("ApplyPose",Private).Invoke(camera,null);
            }
            public void Dispose(){if(camera!=null){camera.enabled=false;Object.Destroy(camera);}}
        }
        private sealed class GuidancePreferences:IDisposable
        {
            private readonly Dictionary<string,int?> values=new Dictionary<string,int?>();
            public GuidancePreferences()
            {
                foreach(var suffix in new[]{"GuidanceEnabled","AudioEnabled","FirstPersonStabilized"})
                {var key=FlightPreferences.KeyPrefix+suffix;values[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;}
                // In-memory only: setters deliberately do not call PlayerPrefs.Save.
                PlayerPrefs.SetInt(FlightPreferences.KeyPrefix+"GuidanceEnabled",1);PlayerPrefs.SetInt(FlightPreferences.KeyPrefix+"AudioEnabled",0);PlayerPrefs.SetInt(FlightPreferences.KeyPrefix+"FirstPersonStabilized",0);
            }
            public void Dispose(){foreach(var entry in values)if(entry.Value.HasValue)PlayerPrefs.SetInt(entry.Key,entry.Value.Value);else PlayerPrefs.DeleteKey(entry.Key);}
        }
        [Serializable] private sealed class GuidanceReport
        {
            public string Evidence="STAGED RUNTIME GUIDANCE FIXTURES: checkpoints/positions and tracking samples are explicitly placed; actual JourneyPresentation.Tick, WindField and RouteBearing.TickBearing determine cues. Pickup uses a real swept ChallengeObservation, not an assigned seed flag.";
            public string CameraEvidence="Actual FlightCamera.ApplyPose with injected tracked head position/orientation, accepted comfortable calibration, authored species eyes, first/third person and Beginner/Acrobatic/stabilized variants. Flight text remains hidden. Audio disabled only for these visual fixtures; exact preferences restored afterward.";
            public string Limits="These frames do not prove input-only route completion, eye tracking, stereo/Quest readability, wearer comprehension, comfort, performance or perceived pickup timing. Telemetry-position fixtures use recorded position and horizontal velocity heading, not full input replay.";
            public string EditorVersion,Scene;public List<GuidanceFrame> Frames=new List<GuidanceFrame>();
        }
        [Serializable] private sealed class GuidanceFrame
        {
            public string File,Kind,View,ControlMode,WindMode;public int Stage;
            public bool SeedCollected,SeedVisible,CarryVisible,PickupTransfer,GuideVisible,TargetVisible,BearingVisible,NavigationAvailable,FlightTextVisible;
            public Vector3 LogicalPosition,CameraPosition,CameraEuler,TargetWorldPosition,BearingViewport,TargetViewport,HeadTranslation;
            public float SimulationTime,HeadYaw,HeadUp;
        }

        private static IEnumerator Fly(SceneScope scope,string species)
        {
            using(var fixture=new PilotFixture(scope,species))
            {
                var controller=fixture.Driver.Controller;var input=fixture.Input;
                var report=new Report {Species=species,EditorVersion=Application.unityVersion,Scene=scope.Original.gameObject.scene.path};
                var log=new StringBuilder();int lastStage=-1;bool departureClimbed=false,arrivalClimbed=false,contactStopped=false;float continuousLowSpeedContact=0;
                var seedPilot=new PortalPilot(false);var archPilot=new PortalPilot(true);
                for(int j=0;j<240;j++)scope.World.TickStreaming(controller.State.Position);
                for(int step=0;step<108000 && fixture.Director.Challenge.Status==ChallengeStatus.Active;step++)
                {
                    var challenge=fixture.Director.Challenge;var pos=controller.State.Position;float time=controller.SimulationTime;
                    var departure=FlightRegions.DepartureThermal(time);var center=scope.Space.ToLocal(departure.X,150,departure.Z);
                    var arrival=scope.Space.ToLocal(330+Math.Sin(time*.004)*20,300,530);
                    var garden=scope.Space.ToLocal(challenge.Destination.X,challenge.Destination.Y,challenge.Destination.Z);
                    var seed=scope.Space.ToLocal(RouteHomeChapter.SeedPosition.X,RouteHomeChapter.SeedPosition.Y,RouteHomeChapter.SeedPosition.Z);
                    var aim=garden;bool quiet=false;
                    if(challenge.Kind==ObjectiveKind.DiscoverLift)aim=new Vector3(center.x,70,center.z);
                    else if(challenge.Kind==ObjectiveKind.Soar)
                    {aim=Circle(pos,center,26);quiet=pos.y>45 && controller.State.Phase!=FlightPhase.Perched;}
                    else if(challenge.Kind==ObjectiveKind.CollectSeed)
                        aim=seedPilot.Aim(pos,controller.State.Velocity,seed);
                    else if(challenge.Kind==ObjectiveKind.Precision)
                    {
                        if(pos.y>=285)departureClimbed=true;
                        if(!departureClimbed && Horizontal(pos,center)<130)
                        {aim=Circle(pos,center,26);quiet=pos.y>45 && controller.State.Phase!=FlightPhase.Perched;}
                        else
                        {
                            float distance=Horizontal(pos,arrival);
                            if(distance<110 && pos.y>=300)arrivalClimbed=true;
                            if(!arrivalClimbed && pos.y<300 && distance<110)
                            {aim=Circle(pos,arrival,24);quiet=pos.y>45 && controller.State.Phase!=FlightPhase.Perched;}
                            else
                            {
                                aim=archPilot.Aim(pos,controller.State.Velocity,new Vector3(garden.x,garden.y+17,garden.z-65));
                            }
                        }
                    }
                    var desired=aim-pos;float heading=Mathf.Atan2(controller.State.Velocity.x,controller.State.Velocity.z)*Mathf.Rad2Deg;
                    float desiredYaw=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
                    var frame=FlightInputFrame.Neutral;frame.Bank=Mathf.Clamp(Mathf.DeltaAngle(heading,desiredYaw)/45,-.7f,.7f);
                    float pitch=quiet?0:Mathf.Abs(aim.y-pos.y)<3?0:Mathf.Clamp(Mathf.Sign(aim.y-pos.y)*18+(aim.y-pos.y)*.5f,-35,14);
                    frame.LookDirection=Quaternion.Euler(-pitch,0,0)*Vector3.forward;
                    if(controller.State.Phase==FlightPhase.Perched || (!quiet && challenge.Kind!=ObjectiveKind.Land && pos.y<aim.y-3))
                    {float stroke=Mathf.Max(0,Mathf.Sin(time*Mathf.PI*2))*2.8f;frame.LeftWing.Velocity=frame.RightWing.Velocity=Vector3.down*stroke;}
                    if(challenge.Kind==ObjectiveKind.Land && Horizontal(pos,aim)<24)frame.Flare=1;
                    input.Frame=frame;
                    // Only semantic input enters the existing driver/controller. Origin
                    // shifts are the normal driver's logical-coordinate-preserving path.
                    int stageBefore=challenge.Stage,contactsBefore=controller.CollisionCount;
                    scope.World.TickStreaming(pos);scope.Sky.Tick(pos);Physics.SyncTransforms();fixture.Driver.Tick(1f/120);
                    int contacts=controller.CollisionCount-contactsBefore;
                    bool lowSpeedContact=contacts>0 && controller.State.Velocity.magnitude<2;
                    if(contacts>0)
                    {
                        report.ContactFrames++;if(lowSpeedContact)report.LowSpeedContactFrames++;
                        var counters=report.StageContacts[stageBefore];counters.Contacts+=contacts;counters.ContactFrames++;
                        if(lowSpeedContact)counters.LowSpeedContactFrames++;
                        counters.MaxImpact=Mathf.Max(counters.MaxImpact,controller.LastImpactSpeed);
                    }
                    report.MaxImpact=Mathf.Max(report.MaxImpact,controller.LastImpactSpeed);
                    continuousLowSpeedContact=lowSpeedContact?continuousLowSpeedContact+1f/120:0;
                    if(continuousLowSpeedContact>3)
                    {contactStopped=true;log.AppendLine("STOP: more than3s continuous low-speed contact; no clean route proof.");break;}
                    if(lastStage!=challenge.Stage)
                    {
                        var p=scope.Space.ToLogical(controller.State.Position);
                        report.Stages.Add(new StageMark {Stage=challenge.Stage,Kind=challenge.Kind.ToString(),Time=controller.SimulationTime,X=p.X,Y=p.Y,Z=p.Z});
                        log.AppendLine("STAGE "+challenge.Stage+" "+challenge.Kind+" t="+controller.SimulationTime.ToString("F2")+" logical="+p.X.ToString("F2")+","+p.Y.ToString("F2")+","+p.Z.ToString("F2"));lastStage=challenge.Stage;
                    }
                    if(step%1200==0)
                    {
                        Status=species+" stage "+challenge.Stage+" t="+time.ToString("F1");
                        log.AppendLine(Status+" pos="+pos+" wind="+controller.WindVelocity+" gain="+challenge.SoaringGain+" phase="+controller.State.Phase);
                        report.Status="Running";report.Seconds=time;WriteReport(species,report,log);
                    }
                    if(step%120==0){FollowCamera(fixture.Driver,scope.Camera);scope.Weather?.Tick(controller.State.Position);yield return null;}
                }
                var result=fixture.Director.Challenge;fixture.Director.SaveCheckpoint();
                report.Status=result.Status==ChallengeStatus.Completed?"Completed":contactStopped?"Stopped: sustained low-speed contact":"Timed out";report.Seconds=controller.SimulationTime;
                report.FinalStage=result.Stage;report.Score=result.Score;report.SeedCollected=result.SeedCollected;
                report.MovementSeconds=result.MovementSeconds;report.RestSeconds=result.RestSeconds;report.CollisionCount=controller.CollisionCount;
                report.LandingCount=controller.LandingCount;report.FinalPhase=controller.State.Phase.ToString();
                report.GardenRestoredAfterReload=new FlightJourneyStore(fixture.Storage).Load().GardenRestored;report.PlayerPrefsUnchanged=scope.PrefsUnchanged;
                var final=scope.Space.ToLogical(controller.State.Position);report.FinalPosition=new[]{final.X,final.Y,final.Z};
                log.AppendLine("RESULT "+report.Status+" stage="+result.Stage+" phase="+report.FinalPhase+" restoredAfterReload="+report.GardenRestoredAfterReload);
                WriteReport(species,report,log);
                fixture.Presentation.Present(controller.State.Position,result,report.GardenRestoredAfterReload,true,controller.SimulationTime+4);
                FollowCamera(fixture.Driver,scope.Camera);yield return null;
                DuckReview.CaptureCurrent(Folder,species.ToLowerInvariant()+"-actual-controller-result-v2");
            }
            yield return null;
        }

        private static IEnumerator CarriedSeedViews(SceneScope scope)
        {
            var evidence=new StringBuilder("STAGED FIRST-PERSON VISUAL FIXTURES ONLY. All three species use their real rig, FlightCamera.FirstPersonBasis and driver eye anchors.\nHUD absent, guidance enabled, no tracked head offset. These images do not establish binocular comfort, movement feel, route completion or headset performance.\n");
            foreach(var species in new[]{"Duck","Dragon","Magpie"})
            {
                using(var fixture=new PilotFixture(scope,species))
                {
                    var challenge=new FlightChallenge(FlightActivity.RouteHome);
                    challenge.Step(new ChallengeObservation(new LogicalPosition(100,130,154),Vector3.forward*12,4,1,1,false,0,0));
                    challenge.Step(new ChallengeObservation(new LogicalPosition(100,231,154),Vector3.forward*12,4,1,1,false,0,0));
                    challenge.Step(new ChallengeObservation(RouteHomeChapter.SeedPosition,Vector3.forward*12,4,1,1,false,0,0));
                    var p=scope.Space.ToLocal(350,290,430);const float yaw=29;
                    var rig=fixture.Driver.GetComponent<BirdRigDriver>();rig.SetFirstPersonVisibility(true);
                    for(int j=0;j<240;j++)scope.World.TickStreaming(p);
                    foreach(var pose in new[]{"beginner","advanced-embodied","advanced-stabilized"})
                    {
                        bool advanced=pose!="beginner",stabilized=pose=="advanced-stabilized";
                        var mode=advanced?FlightControlMode.Acrobatic:FlightControlMode.Beginner;
                        var body=Quaternion.Euler(advanced?-10:0,yaw,advanced?14:0);
                        fixture.Driver.Controller.SetControlMode(mode);Set(fixture.Driver,"comfortYaw",yaw);
                        fixture.Driver.transform.SetPositionAndRotation(p,body);
                        var basis=VoarVR.Core.FlightCamera.FirstPersonBasis(mode,body,fixture.Driver.Heading,stabilized);
                        var eye=advanced?fixture.Driver.ImmersiveEyeAnchor:fixture.Driver.CameraEyeAnchor;
                        scope.Camera.transform.SetPositionAndRotation(p+basis*eye,basis*(advanced?Quaternion.identity:Quaternion.Euler(6,0,0)));
                        scope.Sky.Tick(p);scope.Weather?.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
                        fixture.Presentation.Present(p,challenge,false,true,100);
                        yield return null;
                        string name="visual-fixture-"+species.ToLowerInvariant()+"-carried-seed-"+pose;
                        DuckReview.CaptureCurrent(Folder,name);
                        evidence.AppendLine(name+" eyeAnchor="+eye+" bodyEuler="+body.eulerAngles+" viewEuler="+scope.Camera.transform.eulerAngles+" seedCollected="+challenge.SeedCollected+" guideVisible="+fixture.Presentation.GuideVisible);
                    }
                }
                yield return null;
            }
            File.WriteAllText(Path.Combine(Folder,"first-person-visual-fixtures.txt"),evidence.ToString());
        }

        private static IEnumerator Visuals(SceneScope scope)
        {
            using(var fixture=new PilotFixture(scope,"Duck"))
            {
                var challenge=new FlightChallenge(FlightActivity.RouteHome);
                challenge.Step(new ChallengeObservation(new LogicalPosition(100,130,154),Vector3.forward*12,4,1,1,false,0,0));
                challenge.Step(new ChallengeObservation(new LogicalPosition(100,231,154),Vector3.forward*12,4,1,1,false,0,0));
                var positions=new[]{new Vector3(0,25,0),new Vector3(100,145,140),new Vector3(350,315,430),new Vector3(420,281,510),new Vector3(420,273,610),new Vector3(420,273,610),new Vector3(140,245,183)};
                var targets=new[]{new Vector3(100,140,150),new Vector3(420,270,620),new Vector3(420,270,620),new Vector3(420,287,565),new Vector3(397,291,630),new Vector3(397,291,630),new Vector3(140,245,200)};
                var names=new[]{"lowland-departure","cloud-ascent","island-approach","split-arch","garden-dormant","garden-restored","story-seed"};
                var inventory=new StringBuilder("VISUAL FIXTURES ONLY: observer/actor positions and story presentation are staged. These images do not prove route completion, comfort, hardware performance or persistence.\n");
                for(int i=0;i<positions.Length;i++)
                {
                    var p=scope.Space.ToLocal(positions[i].x,positions[i].y,positions[i].z);var target=scope.Space.ToLocal(targets[i].x,targets[i].y,targets[i].z);
                    fixture.Driver.transform.position=p+Vector3.down*2;
                    for(int j=0;j<240;j++)scope.World.TickStreaming(p);
                    scope.Camera.transform.position=p;scope.Camera.transform.LookAt(target);
                    scope.Sky.Tick(p);scope.Weather?.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);Physics.SyncTransforms();
                    fixture.Presentation.Present(p,challenge,i==5,false,100+i);
                    if(i==5)fixture.Presentation.Present(p,challenge,true,false,104+i);
                    yield return null;
                    Shot(p,target,"visual-fixture-"+names[i]);
                    inventory.AppendLine(names[i]+" observer="+positions[i]+" target="+targets[i]+" chunks="+scope.World.ActiveCount+" terrainVertices="+scope.World.VertexCount+" islands="+scope.Sky.ActiveIslands);
                }
                var menuObject=new GameObject("Review-only flight menu");
                try
                {
                    scope.Camera.transform.SetPositionAndRotation(scope.Space.ToLocal(420,275,610),Quaternion.LookRotation(Vector3.forward));
                    var menu=menuObject.AddComponent<FlightMenu>();menu.Configure(scope.Camera,_=>{},()=>"A ROUTE HOME\nCarry the seed to the quiet garden.\nVisual menu fixture; actions are disconnected.");menu.Open();Canvas.ForceUpdateCanvases();
                    yield return null;DuckReview.CaptureCurrent(Folder,"visual-fixture-flight-menu");
                }
                finally {Object.Destroy(menuObject);}
                inventory.AppendLine("flight-menu: actual FlightMenu component with staged status and disconnected actions.");
                File.WriteAllText(Path.Combine(Folder,"visual-fixtures.txt"),inventory.ToString());
            }
            yield return null;
        }

        private static Vector3 Circle(Vector3 p,Vector3 center,float radius)
        {var radial=p-center;radial.y=0;float r=radial.magnitude;radial=r>.1f?radial/r:Vector3.right;return p+Vector3.Cross(Vector3.up,radial)*18+radial*(radius-r)*1.2f;}
        private static float Horizontal(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        private sealed class PortalPilot
        {
            private readonly bool solidArch;private bool following,committed,escaping;
            public PortalPilot(bool solid)=>solidArch=solid;
            public Vector3 Aim(Vector3 position,Vector3 velocity,Vector3 gate)
            {
                var setup=gate-Vector3.forward*150;
                if(escaping)
                {
                    var escape=gate+new Vector3(-110,15,-140);
                    if(Horizontal(position,escape)>35)return escape;
                    escaping=false;
                }
                if(position.z>gate.z+50){following=committed=false;}
                if(!following)
                {
                    if(Horizontal(position,setup)<55 && position.z<gate.z-95)following=true;
                    else return setup;
                }
                float heading=Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(velocity.x,velocity.z)*Mathf.Rad2Deg,0));
                if(solidArch && !committed && position.z>gate.z-85)
                {
                    if(Mathf.Abs(position.x-gate.x)<6 && Mathf.Abs(position.y-gate.y)<7 && heading<12)committed=true;
                    else
                    {
                        // Abort while there is room to turn, instead of accepting a
                        // near waypoint and carrying sideways velocity into a pillar.
                        following=false;escaping=true;return gate+new Vector3(-110,15,-140);
                    }
                }
                float speed=new Vector2(velocity.x,velocity.z).magnitude;
                return new Vector3(gate.x,gate.y,Mathf.Min(gate.z+65,position.z+Mathf.Clamp(speed*3,24,50)));
            }
        }
        private static void FollowCamera(BirdFlightDriver d,Camera camera)
        {if(camera==null)return;var forward=d.Heading*Vector3.forward;camera.transform.position=d.transform.position-forward*5+Vector3.up*2;camera.transform.LookAt(d.transform.position+forward*8);}
        private static void WriteReport(string species,Report report,StringBuilder log)
        {string name=species.ToLowerInvariant()+"-controller-route-v2";File.WriteAllText(Path.Combine(Folder,name+".json"),JsonUtility.ToJson(report,true));File.WriteAllText(Path.Combine(Folder,name+".txt"),log.ToString());}
        private static void Shot(Vector3 position,Vector3 target,string name)
        {string prior=FlightGameReview.Folder;try{FlightGameReview.Folder=Folder;FlightGameReview.Shot(position,target,name);}finally{FlightGameReview.Folder=prior;}}
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Private).SetValue(target,value);

        [Serializable] private sealed class StageMark {public int Stage;public string Kind;public float Time;public double X,Y,Z;}
        [Serializable] private sealed class StageContact {public int Stage,Contacts,ContactFrames,LowSpeedContactFrames;public float MaxImpact;}
        [Serializable] private sealed class Report
        {
            public string Evidence="Actual controller/driver input-only flight, native streamed collisions; initial logical spawn (0,25,0). Scripted semantic input is not wearer or Quest performance evidence.";
            public int PilotVersion=2;
            public string ContactDefinition="ContactFrames counts simulation steps whose solver contact count increased; LowSpeedContactFrames additionally requires resulting speed below2m/s. MaxImpact is maximum LastImpactSpeed in m/s, not a medical/comfort measure.";
            public string Species,EditorVersion,Scene,Status,FinalPhase;public float Seconds,MovementSeconds,RestSeconds;
            public int FinalStage,Score,CollisionCount,LandingCount,ContactFrames,LowSpeedContactFrames;public float MaxImpact;
            public bool SeedCollected,GardenRestoredAfterReload,PlayerPrefsUnchanged;
            public double[] FinalPosition;public List<StageMark> Stages=new List<StageMark>();
            public StageContact[] StageContacts={new StageContact{Stage=0},new StageContact{Stage=1},new StageContact{Stage=2},new StageContact{Stage=3},new StageContact{Stage=4}};
        }
        private sealed class MemoryStorage:IFlightSaveStorage
        {
            private readonly Dictionary<string,string> values=new Dictionary<string,string>();
            public bool HasKey(string key)=>values.ContainsKey(key);
            public string GetString(string key,string fallback="")=>values.TryGetValue(key,out var value)?value:fallback;
            public int GetInt(string key,int fallback=0)=>int.TryParse(GetString(key),out var value)?value:fallback;
            public void SetString(string key,string value)=>values[key]=value;
            public void Save(){}
        }
        private sealed class PilotFixture:IDisposable
        {
            private readonly GameObject root;
            public readonly FlightGameMeasurements.Controls Input=new FlightGameMeasurements.Controls();
            public readonly MemoryStorage Storage=new MemoryStorage();
            public readonly BirdFlightDriver Driver;public readonly ExpeditionDirector Director;public readonly JourneyPresentation Presentation;
            public PilotFixture(SceneScope scope,string species)
            {
                var definition=Resources.Load<BirdCharacterDefinition>("Characters/"+species);
                if(definition==null)throw new ArgumentException("Unknown installed species: "+species);
                var profile=definition.BuildProfile();scope.Wind.ConfigureSpecies(profile);scope.Wind.SetMode(WindMode.Assisted);
                root=new GameObject("Route Home "+species+" input fixture");Driver=root.AddComponent<BirdFlightDriver>();Driver.enabled=false;
                var environment=root.AddComponent<UnityFlightEnvironment>();environment.ConfigureTerrain(p=>scope.World.TryGetReadyChunk(p,out var chunk)?chunk:null);
                var controller=new BirdFlightController(Input,scope.Space.ToLocal(0,25,0),profile:profile,wind:scope.Wind,environment:environment);
                Set(Driver,"input",Input);Set(Driver,"world",scope.World);Set(Driver,"wind",scope.Wind);Set(Driver,"profile",profile);Set(Driver,"character",definition);
                typeof(BirdFlightDriver).GetProperty("Controller").SetValue(Driver,controller);
                typeof(BirdFlightDriver).GetMethod("SpawnCharacterRig",Private).Invoke(Driver,new object[]{definition});
                var rig=root.GetComponent<BirdRigDriver>();Set(Driver,"rig",rig);Driver.Calibration.ConfigureBirdHalfSpan(definition.RestArmSpan);
                var ground=root.AddComponent<BirdGroundPresentation>();ground.Configure(rig,profile.CollisionRadius);Set(Driver,"groundPresentation",ground);
                Director=root.AddComponent<ExpeditionDirector>();typeof(BirdFlightDriver).GetProperty("Expedition").SetValue(Driver,Director);
                ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;Director.Configure(Driver,scope.Space,Storage);
                Presentation=root.AddComponent<JourneyPresentation>();Presentation.Configure(scope.Space,Director);Set(Driver,"journeyPresentation",Presentation);
                root.transform.position=controller.State.Position;
            }
            public void Dispose(){if(root!=null)Object.Destroy(root);}
        }

        private sealed class SceneScope:IDisposable
        {
            public readonly BirdFlightDriver Original;public readonly WorldStreamer World;public readonly SkyArchipelago Sky;
            public readonly SkyWeatherPresentation Weather;public readonly WindField Wind;public readonly WorldSpace Space;public readonly Camera Camera;
            private readonly bool driverEnabled,worldEnabled,skyEnabled,weatherEnabled,cameraEnabled;
            private readonly VoarVR.Core.FlightCamera flightCamera;private readonly Vector3 cameraPosition;private readonly Quaternion cameraRotation;
            private readonly FlightActivity activity;private readonly bool resume;private readonly WindMode windMode;
            private readonly BirdFlightProfile windProfile;private readonly double originX,originZ;
            private readonly Dictionary<FieldInfo,object> directorState=new Dictionary<FieldInfo,object>();
            private readonly List<GameObject> hidden=new List<GameObject>();
            private readonly List<Behaviour> suspended=new List<Behaviour>();
            private readonly Dictionary<string,string> prefs=new Dictionary<string,string>();private readonly int foragingBest;
            private readonly Dictionary<string,int?> preferenceValues=new Dictionary<string,int?>();
            public SceneScope(BirdFlightDriver original)
            {
                Original=original;World=Object.FindAnyObjectByType<WorldStreamer>();Sky=Object.FindAnyObjectByType<SkyArchipelago>();Weather=Object.FindAnyObjectByType<SkyWeatherPresentation>();Wind=Object.FindAnyObjectByType<WindField>();Space=World.Space;Camera=UnityEngine.Camera.main;
                if(Sky==null || Wind==null || Camera==null)throw new InvalidOperationException("World, archipelago, wind and main camera must be loaded.");
                activity=ActivitySelection.Chosen;resume=ActivitySelection.ResumeRequested;windMode=Wind.Mode;windProfile=(BirdFlightProfile)typeof(WindField).GetField("speciesProfile",Private).GetValue(Wind);
                originX=Space.OffsetX;originZ=Space.OffsetZ;cameraPosition=Camera.transform.position;cameraRotation=Camera.transform.rotation;
                driverEnabled=original.enabled;worldEnabled=World.enabled;skyEnabled=Sky.enabled;weatherEnabled=Weather!=null && Weather.enabled;
                flightCamera=Object.FindAnyObjectByType<VoarVR.Core.FlightCamera>();cameraEnabled=flightCamera!=null && flightCamera.enabled;
                foreach(var key in new[]{FlightJourneyStore.SaveKey,FlightJourneyStore.BackupKey,FlightJourneyStore.RecoveryKey,FlightJourneyStore.LegacyKey})prefs[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetString(key):null;
                foreach(var suffix in new[]{"GuidanceEnabled","AudioEnabled","HapticsEnabled","FirstPersonStabilized"})
                {var key=FlightPreferences.KeyPrefix+suffix;preferenceValues[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;}
                foragingBest=PlayerPrefs.GetInt(FlightJourneyStore.ForagingKey,-1);
                // Isolate lifecycle callbacks on the original actor too, then restore its
                // exact store/checkpoint objects instead of reloading or overwriting saves.
                foreach(var field in typeof(ExpeditionDirector).GetFields(Private|BindingFlags.DeclaredOnly))directorState[field]=field.GetValue(original.Expedition);
                original.Expedition.Configure(original,Space,new MemoryStorage());
                original.enabled=false;World.enabled=false;Sky.enabled=false;if(Weather!=null)Weather.enabled=false;if(flightCamera!=null)flightCamera.enabled=false;
                // These independent callbacks would otherwise re-show old mission
                // text/bearings after the original driver's Update has been suspended.
                if(original.Expedition.enabled){suspended.Add(original.Expedition);original.Expedition.enabled=false;}
                foreach(var bearing in Object.FindObjectsByType<RouteBearing>(FindObjectsSortMode.None))
                    if(bearing.enabled){suspended.Add(bearing);bearing.enabled=false;}
                foreach(Transform child in original.transform)if(child.gameObject.activeSelf){hidden.Add(child.gameObject);child.gameObject.SetActive(false);}
                foreach(Transform child in Space.transform)if(child.name=="A Route Home presentation" && child.gameObject.activeSelf){hidden.Add(child.gameObject);child.gameObject.SetActive(false);}
                foreach(var canvas in Camera.GetComponentsInChildren<Canvas>())if(canvas.gameObject.activeSelf){hidden.Add(canvas.gameObject);canvas.gameObject.SetActive(false);}
                foreach(var text in Camera.GetComponentsInChildren<TextMesh>())if(text.gameObject.activeSelf){hidden.Add(text.gameObject);text.gameObject.SetActive(false);}
            }
            public bool PrefsUnchanged
            {
                get
                {
                    foreach(var entry in prefs)if((PlayerPrefs.HasKey(entry.Key)?PlayerPrefs.GetString(entry.Key):null)!=entry.Value)return false;
                    foreach(var entry in preferenceValues)if((PlayerPrefs.HasKey(entry.Key)?PlayerPrefs.GetInt(entry.Key):(int?)null)!=entry.Value)return false;
                    return PlayerPrefs.GetInt(FlightJourneyStore.ForagingKey,-1)==foragingBest;
                }
            }
            public void Dispose()
            {
                var delta=new Vector3((float)(Space.OffsetX-originX),0,(float)(Space.OffsetZ-originZ));Original.Controller.RebaseOrigin(delta);
                Original.transform.position=Original.Controller.State.Position;
                foreach(var entry in directorState)entry.Key.SetValue(Original.Expedition,entry.Value);
                ActivitySelection.Chosen=activity;ActivitySelection.ResumeRequested=resume;Wind.SetMode(windMode);Wind.ConfigureSpecies(windProfile);
                foreach(var item in hidden)if(item!=null)item.SetActive(true);
                foreach(var component in suspended)if(component!=null)component.enabled=true;
                Camera.transform.SetPositionAndRotation(cameraPosition-delta,cameraRotation);
                World.TickStreaming(Original.Controller.State.Position);Sky.Tick(Original.Controller.State.Position);Weather?.Tick(Original.Controller.State.Position);
                Original.enabled=driverEnabled;World.enabled=worldEnabled;Sky.enabled=skyEnabled;if(Weather!=null)Weather.enabled=weatherEnabled;if(flightCamera!=null)flightCamera.enabled=cameraEnabled;
            }
        }
    }
}
