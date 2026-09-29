using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    // Runtime presentation/integration for the reusable, deterministic CourseRuntime.
    // Course definitions own rules; this component owns world markers, retry/rest flow and
    // the local leaderboard for the currently selected ruleset.
    public sealed class ObstacleCourseDirector:MonoBehaviour
    {
        public const float RequiredRestSeconds=6f;
        public const float MaximumRankedFrameSeconds=.05f;
        private BirdFlightDriver bird;
        private WorldSpace space;
        private SkyForaging foraging;
        private CourseDefinition definition;
        private CourseRuntime runtime;
        private IMonotonicClock clock;
        private CourseLeaderboardStore leaderboard;
        private WindField wind;
        private readonly List<CatchResult> catches=new List<CatchResult>();
        private readonly List<LineRenderer> taskMarkers=new List<LineRenderer>();
        private readonly List<LineRenderer> bearingChevrons=new List<LineRenderer>();
        private GameObject worldRoot;
        private LineRenderer routeLine;
        private TextMesh overlay;
        private Material material;
        private Material obstacleMaterial,padMaterial;
        private CourseState previousState;
        private int previousTricks,previousLandings,previousCollisions;
        private FlightTrick pendingTrick;
        private FlightControlMode previousMode;
        private WindMode previousWind;
        private bool awaitingRetry,handledEnd,triggerHeld,waitingRelease;
        private double restStarted;
        private CourseAttemptResult lastResult;
        private string leaderboardError;
        private string cachedResultText;
        private int displayedRestSecond=int.MinValue;
        private int displayedCountdown=int.MinValue;
        private int activeCollectibleTaskIndex=-1;
        private int attemptSequence=-1;

        public CourseRuntime Runtime=>runtime;
        public CourseDefinition Definition=>definition;
        public bool IsResting=>runtime!=null && runtime.State==CourseState.Rest;
        public bool IsPresentingResult=>runtime!=null && (runtime.State==CourseState.Results || runtime.State==CourseState.Rest);
        public bool ModalOverlayVisible=>runtime!=null && (IsCountdown(runtime.State)
            || runtime.State==CourseState.Results || runtime.State==CourseState.Rest
            || runtime.State==CourseState.Ready&&awaitingRetry);
        public bool RequiresFlightPause=>runtime!=null && runtime.State!=CourseState.Running;
        public bool AllowsResetAction=>runtime!=null && (runtime.State==CourseState.Calibration
            || runtime.State==CourseState.Running);
        public string StatusText=>BuildStatus();

        public void Configure(BirdFlightDriver driver,WorldSpace coordinates,SkyForaging skyForaging,
            IMonotonicClock monotonicClock=null,CourseLeaderboardStore leaderboardStore=null)
        {
            bird=driver;space=coordinates;foraging=skyForaging;wind=FindAnyObjectByType<WindField>();
            var authored=CourseCatalog.Find(ActivitySelection.ChosenCourseId)??CourseCatalog.All[0];
            definition=CourseTerrainResolver.Resolve(authored,space!=null?space.Seed:7319);
            clock=monotonicClock??new StopwatchMonotonicClock();
            leaderboard=leaderboardStore??new CourseLeaderboardStore();
            wind?.ConfigureCourse(definition);
            CreateRuntime();
            BuildWorld();
            BuildOverlay(Camera.main);
            if(foraging!=null)foraging.Caught+=OnCaught;
            if(space!=null)space.Rebased+=OnWorldRebased;
            previousTricks=bird?.Controller?.Tricks.Count??0;
            previousLandings=bird?.Controller?.LandingCount??0;
            previousCollisions=bird?.Controller?.CollisionCount??0;
            previousMode=bird?.Controller?.ControlMode??FlightControlMode.Beginner;
            previousWind=wind?.Mode??WindMode.Assisted;
        }

        private void CreateRuntime()
        {
            ClearActiveObjective();
            runtime=new CourseRuntime(definition,clock);
            previousState=runtime.State;handledEnd=false;awaitingRetry=false;
            cachedResultText=null;displayedRestSecond=int.MinValue;displayedCountdown=int.MinValue;
        }

        public void Tick(float deltaTime,FlightInputFrame frame,float wallDeltaTime=-1f)
        {
            if(runtime==null || bird==null || bird.Controller==null)return;
            bool calibrationReady=!bird.UsesXR || bird.Calibration.Captured;
            runtime.SetCalibrationReady(calibrationReady);
            if(!bird.CourseTimingAllowed)
            {
                UpdateForagingObjective();UpdatePresentation();previousState=runtime.State;
                return;
            }
            if(runtime.State==CourseState.Ready && !awaitingRetry && !bird.FlightMenuVisible)
                BeginAttempt();

            runtime.Tick();
            if(previousState!=CourseState.Running && runtime.State==CourseState.Running)
            {
                bird.Controller.SetPaused(false);
                waitingRelease=true;
                // Countdown frames do not contain running simulation. Inputs chosen
                // before GO become the attempt baseline, and a delayed GO render frame
                // cannot invalidate ranking before the bird is released.
                previousTricks=bird.Controller.Tricks.Count;
                previousLandings=bird.Controller.LandingCount;
                previousCollisions=bird.Controller.CollisionCount;
                previousMode=bird.Controller.ControlMode;
                if(wind!=null)previousWind=wind.Mode;
                catches.Clear();pendingTrick=FlightTrick.None;
                UpdateForagingObjective();UpdatePresentation();previousState=runtime.State;
                return;
            }

            if(runtime.State==CourseState.Running)
            {
                CourseNonRankedReason invalidation=CourseNonRankedReason.None;
                bool supportedLanding=bird.Controller.HasSupportedPerch
                    && definition.Tasks[runtime.TaskIndex].Kind==CourseTaskKind.SupportedLanding
                    && bird.Controller.SupportedSurfaceId==definition.Tasks[runtime.TaskIndex].RequiredSurfaceId;
                bool freshSupportedLanding=supportedLanding
                    && bird.Controller.LandingCount>previousLandings;
                if(frame.ResetPressed)invalidation|=CourseNonRankedReason.Reset;
                if(!RankedFrameTimingValid(wallDeltaTime<0f?deltaTime:wallDeltaTime))
                    invalidation|=CourseNonRankedReason.FrameTiming;
                if(bird.RecoveryPending)invalidation|=CourseNonRankedReason.Recovery;
                if(previousMode!=bird.Controller.ControlMode)invalidation|=CourseNonRankedReason.ControlModeChanged;
                if(wind!=null && wind.Mode!=previousWind)invalidation|=CourseNonRankedReason.WeatherChanged;
                int collisionDelta=bird.Controller.CollisionCount-previousCollisions;
                if(collisionDelta>(freshSupportedLanding?1:0))
                    invalidation|=CourseNonRankedReason.Collision;
                if(bird.Controller.Tricks.Count>previousTricks)pendingTrick=bird.Controller.Tricks.Last;
                bool tracking=!bird.UsesXR || frame.HeadTracked && frame.LeftWing.Tracked && frame.RightWing.Tracked;
                var logical=space!=null?space.ToLogical(bird.Controller.State.Position):new LogicalPosition(
                    bird.Controller.State.Position.x,bird.Controller.State.Position.y,bird.Controller.State.Position.z);
                int taskBefore=runtime.TaskIndex;
                runtime.Observe(new CourseObservation(logical,true,calibrationReady,tracking,
                    bird.Controller.IsPaused,bird.Controller.StreamingBlocked,supportedLanding,
                    pendingTrick,catches,invalidation,bird.Controller.SupportedSurfaceId));
                bool advanced=runtime.State==CourseState.Running&&runtime.TaskIndex>taskBefore;
                bool carryCatches=advanced&&definition.Tasks[runtime.TaskIndex].Kind==CourseTaskKind.CollectibleQuota;
                bool carryTrick=advanced&&definition.Tasks[runtime.TaskIndex].Kind==CourseTaskKind.Trick;
                if(!carryCatches)catches.Clear();
                if(!carryTrick)pendingTrick=FlightTrick.None;
            }
            else {catches.Clear();pendingTrick=FlightTrick.None;}

            previousTricks=bird.Controller.Tricks.Count;
            previousLandings=bird.Controller.LandingCount;
            previousCollisions=bird.Controller.CollisionCount;
            previousMode=bird.Controller.ControlMode;
            if(wind!=null)previousWind=wind.Mode;

            if((runtime.State==CourseState.Finished || runtime.State==CourseState.Failed) && !handledEnd)
                PresentAttemptResult();
            if(runtime.State==CourseState.Rest && clock.NowSeconds-restStarted>=RequiredRestSeconds)
            {
                runtime.CompleteRest(calibrationReady);awaitingRetry=true;waitingRelease=true;
            }
            UpdateForagingObjective();
            UpdatePresentation();
            previousState=runtime.State;
        }

        public static bool RankedFrameTimingValid(float wallDeltaTime)=>
            !float.IsNaN(wallDeltaTime)&&!float.IsInfinity(wallDeltaTime)
            &&wallDeltaTime>=0f&&wallDeltaTime<=MaximumRankedFrameSeconds+.00001f;

        // Called before the normal paused-flight trigger router. Course result and enforced
        // rest interactions therefore cannot accidentally open or activate the pause menu.
        public bool HandleInput(FlightInputFrame frame,bool desktopSelect=false,bool inputAvailable=true)
        {
            if(runtime==null)return false;
            if(!inputAvailable)
            {
                // A stale trigger at focus/tracking recovery is not a new confirmation.
                waitingRelease=true;triggerHeld=false;
                return IsPresentingResult || awaitingRetry || IsCountdown(runtime.State);
            }
            // Keep FINISH SESSION and calibration reachable during course results/rest.
            if(bird!=null && bird.FlightMenuVisible)return false;
            bool trigger=triggerHeld?frame.Tuck>.25f:frame.Tuck>=.65f;
            bool edge=trigger&&!triggerHeld;triggerHeld=trigger;
            if(waitingRelease)
            {
                if(frame.Tuck<=.25f)waitingRelease=false;
                return IsPresentingResult || awaitingRetry || IsCountdown(runtime.State);
            }
            bool select=edge||desktopSelect;
            if(runtime.State==CourseState.Results && select)
            {
                runtime.BeginRest();restStarted=clock.NowSeconds;waitingRelease=true;return true;
            }
            if(runtime.State==CourseState.Rest)return true;
            if(runtime.State==CourseState.Ready && awaitingRetry && select)
            {
                awaitingRetry=false;BeginAttempt();waitingRelease=true;return true;
            }
            return IsCountdown(runtime.State) && trigger;
        }

        public void ResetForFreshAttempt()
        {
            CreateRuntime();waitingRelease=true;catches.Clear();
            pendingTrick=FlightTrick.None;
            UpdatePresentation();
        }

        // A live restart becomes an explicit failed attempt and must still pass through
        // results and the recovery break. Countdown restart is safe: timing has not begun.
        public bool RequestRestart()
        {
            if(runtime==null)return false;
            if(runtime.State==CourseState.Running)
            {
                ClearActiveObjective();
                runtime.Fail();
                waitingRelease=true;
                return true;
            }
            if(IsCountdown(runtime.State))
            {
                ResetForFreshAttempt();
                return true;
            }
            return false;
        }

        public bool RequestSafetyReset()
        {
            if(runtime==null||runtime.State!=CourseState.Running)return false;
            ClearActiveObjective();
            runtime.MarkNonRanked(CourseNonRankedReason.Reset);
            runtime.Fail();
            waitingRelease=true;
            return true;
        }

        // Opening a menu must not let a hidden stopwatch countdown become a ranked run.
        // A fresh countdown begins only after the menu is closed again.
        public void PrepareForSessionMenu()
        {
            if(runtime!=null&&IsCountdown(runtime.State))ResetForFreshAttempt();
        }

        public void HandleApplicationSuspended()
        {
            if(runtime==null)return;
            if(runtime.State==CourseState.Running)
            {
                ClearActiveObjective();
                runtime.MarkNonRanked(CourseNonRankedReason.ApplicationSuspended);
                runtime.Fail();waitingRelease=true;
            }
            else if(IsCountdown(runtime.State))ResetForFreshAttempt();
        }

        private void BeginAttempt()
        {
            ClearActiveObjective();
            attemptSequence++;
            bird.PrepareCourseAttempt();
            if(RequiresAcrobaticMode())bird.Controller.SetControlMode(FlightControlMode.Acrobatic);
            runtime.SetCalibrationReady(!bird.UsesXR||bird.Calibration.Captured);
            if(runtime.State!=CourseState.Ready)return;
            runtime.BeginCountdown();handledEnd=false;
            bird.Controller.SetPaused(true);waitingRelease=true;
            previousTricks=bird.Controller.Tricks.Count;previousLandings=bird.Controller.LandingCount;
            previousCollisions=bird.Controller.CollisionCount;pendingTrick=FlightTrick.None;
            previousMode=bird.Controller.ControlMode;
            if(wind!=null)previousWind=wind.Mode;
        }

        private bool RequiresAcrobaticMode()
        {
            foreach(var task in definition.Tasks)if(task.Kind==CourseTaskKind.Trick)return true;
            return false;
        }

        private void PresentAttemptResult()
        {
            ClearActiveObjective();
            handledEnd=true;
            var key=new CourseResultKey(definition,bird.CharacterStableId,
                bird.Controller.ControlMode.ToString().ToLowerInvariant(),
                (wind?.Mode??WindMode.Assisted).ToString().ToLowerInvariant(),
                (wind?.AutomaticFeathering??false)?"assisted":"manual");
            lastResult=runtime.BuildResult(key);
            leaderboardError=null;
            if(lastResult.Completed&&lastResult.Ranked
                && !leaderboard.Record(lastResult,bird.ActiveProfileId,bird.ActiveProfileName,bird.CharacterStableId,DateTime.UtcNow))
                leaderboardError=string.IsNullOrEmpty(leaderboard.LastError)
                    ?"The local record could not be saved.":leaderboard.LastError;
            cachedResultText=ResultText();
            runtime.ShowResults();bird.Controller.SetPaused(true);waitingRelease=true;
        }

        private void UpdateForagingObjective()
        {
            int desired=runtime!=null&&runtime.State==CourseState.Running
                &&runtime.TaskIndex<definition.Tasks.Count
                &&definition.Tasks[runtime.TaskIndex].Kind==CourseTaskKind.CollectibleQuota
                    ?runtime.TaskIndex:-1;
            if(desired==activeCollectibleTaskIndex)return;
            ClearActiveObjective();
            if(desired<0||foraging==null)return;
            var task=definition.Tasks[desired];
            var collectible=CollectibleCatalog.Find(task.CollectibleId);
            string objectiveId=CourseObjectiveIdentity.For(definition,task);
            var homes=CourseCollectibleLayout.Build(task,space!=null?space.Seed:7319);
            if(foraging.ActivateCourseObjective(attemptSequence,objectiveId,collectible,homes))
                activeCollectibleTaskIndex=desired;
            else
                runtime.Fail();
        }

        private void OnCaught(CatchResult result)
        {
            if(!result.IsValid||runtime==null||runtime.State!=CourseState.Running
                ||runtime.TaskIndex>=definition.Tasks.Count)return;
            var task=definition.Tasks[runtime.TaskIndex];
            if(task.Kind==CourseTaskKind.CollectibleQuota
                &&string.Equals(result.ObjectiveId,CourseObjectiveIdentity.For(definition,task),StringComparison.Ordinal))
                catches.Add(result);
        }

        private void ClearActiveObjective()
        {
            foraging?.ClearCourseObjective();
            activeCollectibleTaskIndex=-1;
            catches.Clear();
        }

        private string BuildStatus()
        {
            if(runtime==null)return "COURSE  •  LOADING";
            if(runtime.State==CourseState.Calibration)return "COURSE  •  CALIBRATE";
            if(IsCountdown(runtime.State))return definition.Title.ToUpperInvariant()+"  •  "+runtime.CountdownNumber;
            if(runtime.State==CourseState.Running)
            {
                var p=runtime.Progress;
                string amount=p.Unit==ObjectiveProgressUnit.Metres?Math.Round(p.Current)+"/"+Math.Round(p.Required)+" m"
                    :Math.Round(p.Current)+"/"+Math.Round(p.Required);
                return definition.Title.ToUpperInvariant()+" "+p.DisplayStage+"/"+p.StageCount
                    +" • "+runtime.ElapsedSeconds.ToString("F1")+"s\n"
                    +p.Label.ToUpperInvariant()+" "+amount+" • "+DirectionHint();
            }
            if(runtime.State==CourseState.Ready&&awaitingRetry)return definition.Title.ToUpperInvariant()+(bird.UsesHands ? "  •  REST COMPLETE  •  PINCH TO RETRY" : "  •  REST COMPLETE  •  TRIGGER TO RETRY");
            return definition.Title.ToUpperInvariant()+"  •  "+runtime.State.ToString().ToUpperInvariant();
        }

        private void BuildWorld()
        {
            var kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null||kit.Material==null)return;
            material=new Material(kit.Material);material.SetColor("_BaseColor",new Color(1f,.72f,.18f));
            var world=FindAnyObjectByType<ProceduralFlightWorld>();
            obstacleMaterial=definition.StableId=="canopy-weave"?world?.CanopyMaterial:world?.CityMaterial;
            padMaterial=world?.ForestMaterial??world?.LandingMaterial;
            worldRoot=new GameObject("Obstacle course "+definition.StableId);
            if(space!=null)worldRoot.transform.position=new Vector3(-(float)space.OffsetX,0,-(float)space.OffsetZ);
            int seed=space!=null?space.Seed:7319;
            // Begin the painted path in front of the bird instead of through its body.
            const float routeLeadIn=6f;
            var positions=new List<Vector3>{Local(new CoursePoint(0,
                WorldTerrain.Elevation(seed,0,routeLeadIn)+12,routeLeadIn))};
            for(int i=0;i<definition.Tasks.Count;i++)
            {
                var task=definition.Tasks[i];LineRenderer marker=null;
                if(task.Kind==CourseTaskKind.CollectibleQuota)
                {
                    positions.Add(Local(task.PointA));positions.Add(Local(task.PointB));
                    marker=CreateRing("Course task "+(i+1),TaskAnchor(task),4f,false);
                }
                else if(HasAnchor(task))
                {
                    Vector3 center=TaskAnchor(task);positions.Add(center);
                    marker=CreateRing("Course task "+(i+1),center,(float)Math.Max(2,task.Radius),task.Kind==CourseTaskKind.AltitudeBand);
                    if(task.Kind==CourseTaskKind.Gate)CreateObstacleFrame(task,center,i);
                    if(task.Kind==CourseTaskKind.SupportedLanding)CreateLandingPad(center,i);
                }
                taskMarkers.Add(marker);
            }
            var route=new GameObject("Course flight path");route.transform.SetParent(worldRoot.transform,false);
            routeLine=route.AddComponent<LineRenderer>();routeLine.useWorldSpace=false;routeLine.positionCount=positions.Count;
            routeLine.SetPositions(positions.ToArray());routeLine.widthMultiplier=.09f;routeLine.sharedMaterial=material;
            routeLine.startColor=new Color(1f,.75f,.18f,.3f);routeLine.endColor=new Color(.3f,1f,.72f,.65f);
            routeLine.shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<4;i++)bearingChevrons.Add(CreateChevron(i));
        }

        private LineRenderer CreateRing(string name,Vector3 center,float radius,bool horizontal)
        {
            var go=new GameObject(name);go.transform.SetParent(worldRoot.transform,false);go.transform.localPosition=center;
            if(horizontal)go.transform.localRotation=Quaternion.Euler(90,0,0);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=48;
            line.widthMultiplier=.22f;line.sharedMaterial=material;line.shadowCastingMode=ShadowCastingMode.Off;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
            return line;
        }

        private LineRenderer CreateChevron(int index)
        {
            var go=new GameObject("Course direction chevron "+index);go.transform.SetParent(worldRoot.transform,false);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;line.widthMultiplier=.18f;
            line.sharedMaterial=material;line.startColor=line.endColor=new Color(1f,.77f,.2f,.9f);line.shadowCastingMode=ShadowCastingMode.Off;
            return line;
        }

        private void CreateObstacleFrame(CourseTaskDefinition task,Vector3 center,int index)
        {
            // The scoring circle fits wholly inside this square aperture even for the
            // largest bird collision sphere, so touching a frame cannot also score it.
            float opening=(float)task.Radius+.8f;float thickness=definition.StableId=="canopy-weave"?2.6f:2f;
            float reach=opening+5f;Color tint=definition.StableId=="canopy-weave"?new Color(.16f,.45f,.2f)
                :definition.StableId=="ruin-windows"?new Color(.46f,.43f,.36f):new Color(.24f,.3f,.28f);
            CreateBlock("Left obstacle",center+Vector3.left*(opening+2.5f),new Vector3(5,reach*2,thickness),tint,index);
            CreateBlock("Right obstacle",center+Vector3.right*(opening+2.5f),new Vector3(5,reach*2,thickness),tint,index);
            CreateBlock("Upper obstacle",center+Vector3.up*(opening+2.5f),new Vector3(opening*2,5,thickness),tint,index);
            CreateBlock("Lower obstacle",center+Vector3.down*(opening+2.5f),new Vector3(opening*2,5,thickness),tint,index);
        }

        private void CreateBlock(string name,Vector3 position,Vector3 scale,Color tint,int index)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(worldRoot.transform,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.layer=WorldStreamer.CollisionLayer;
            var surface=go.AddComponent<LandingSurface>();surface.SurfaceId=9400+index;surface.SurfaceKind=FlightSurfaceKind.Structure;surface.MaxSlopeDegrees=0;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=obstacleMaterial??material;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",tint);renderer.SetPropertyBlock(block);
        }

        private void CreateLandingPad(Vector3 center,int index)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Course finish perch";go.transform.SetParent(worldRoot.transform,false);
            go.transform.localPosition=center+Vector3.down*.35f;go.transform.localScale=new Vector3(10,.7f,10);go.layer=WorldStreamer.CollisionLayer;
            var surface=go.AddComponent<LandingSurface>();
            surface.SurfaceId=definition.Tasks[index].RequiredSurfaceId>0
                ?definition.Tasks[index].RequiredSurfaceId:9600+index;
            surface.SurfaceKind=FlightSurfaceKind.Structure;surface.MaxSlopeDegrees=20;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=padMaterial??material;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.25f,.78f,.48f));renderer.SetPropertyBlock(block);
        }

        private void BuildOverlay(Camera camera)
        {
            if(camera==null)return;var go=new GameObject("Course countdown and results");go.transform.SetParent(camera.transform,false);
            go.transform.localPosition=new Vector3(0,0,2);overlay=go.AddComponent<TextMesh>();overlay.anchor=TextAnchor.MiddleCenter;
            overlay.alignment=TextAlignment.Center;overlay.fontSize=52;overlay.characterSize=.009f;overlay.color=new Color(.96f,.98f,.91f);
            go.AddComponent<FlightCard>();go.SetActive(false);
        }

        private void UpdatePresentation()
        {
            if(runtime==null)return;
            bool guideVisible=runtime.State==CourseState.Running&&!bird.FlightOverlayBlocked;
            if(routeLine!=null)routeLine.enabled=guideVisible;
            int current=Math.Min(runtime.TaskIndex,Math.Max(0,taskMarkers.Count-1));
            for(int i=0;i<taskMarkers.Count;i++)if(taskMarkers[i]!=null)
            {
                taskMarkers[i].enabled=guideVisible;
                bool done=i<runtime.TaskIndex;bool active=i==current&&runtime.State==CourseState.Running;
                taskMarkers[i].widthMultiplier=active?.38f:.18f;
                taskMarkers[i].startColor=taskMarkers[i].endColor=done?new Color(.25f,1f,.62f,.22f)
                    :active?new Color(1f,.78f,.15f,1f):new Color(.65f,.82f,.62f,.42f);
            }
            UpdateBearing(current);
            if(overlay==null)return;
            bool hideForMenu=bird.FlightMenuVisible||bird.CalibrationCoachVisible;
            bool visible=!hideForMenu&&(IsCountdown(runtime.State)||runtime.State==CourseState.Results
                ||runtime.State==CourseState.Rest||runtime.State==CourseState.Ready&&awaitingRetry);
            overlay.gameObject.SetActive(visible);if(!visible)return;
            if(IsCountdown(runtime.State))
            {
                int countdown=runtime.CountdownNumber;
                if(countdown!=displayedCountdown)
                {
                    displayedCountdown=countdown;
                    overlay.text=countdown+"\n"+definition.Title.ToUpperInvariant();
                }
            }
            else if(runtime.State==CourseState.Results)overlay.text=cachedResultText??"COURSE RESULT";
            else if(runtime.State==CourseState.Rest)
            {
                int remaining=Mathf.CeilToInt(Mathf.Max(0,RequiredRestSeconds-(float)(clock.NowSeconds-restStarted)));
                if(remaining!=displayedRestSecond)
                {
                    displayedRestSecond=remaining;
                    overlay.text="REST BREAK  "+remaining+"\nLET YOUR ARMS HANG · BREATHE · RESET YOUR SHOULDERS";
                }
            }
            else overlay.text=bird.UsesHands ? "REST COMPLETE\nPINCH: RETRY   ·   HOLD BOTH PINCHES IN FRONT: MENU"
                : "REST COMPLETE\nRIGHT TRIGGER: RETRY   ·   LEFT MENU: FINISH SESSION";
        }

        private string ResultText()
        {
            var text=new StringBuilder();
            text.Append(lastResult.Completed?"COURSE COMPLETE":"COURSE ENDED").Append("  ·  ")
                .Append(lastResult.DurationSeconds.ToString("F2")).Append(" s\n")
                .Append(lastResult.CompletedTasks).Append('/').Append(lastResult.TotalTasks).Append(" tasks  ·  ")
                .Append(!lastResult.Completed?FailureText(lastResult.FailureReason)
                    :lastResult.Ranked?"RANKED":"PRACTICE RUN · NOT ADDED TO LEADERBOARD").Append("\n");
            if(lastResult.Completed&&!lastResult.Ranked)
                text.Append("WHY: ").Append(PracticeReasons(lastResult.NonRankedReasons)).Append("\n");
            var entries=leaderboard.Load(lastResult.Key);
            if(!string.IsNullOrEmpty(leaderboard.LastError))leaderboardError=leaderboard.LastError;
            double personal=double.PositiveInfinity;
            foreach(var entry in entries)if(entry.ProfileId==bird.ActiveProfileId)personal=Math.Min(personal,entry.Seconds);
            if(!double.IsInfinity(personal))text.Append("PERSONAL BEST  ").Append(personal.ToString("F2")).Append(" s\n");
            text.Append("LOCAL LEADERBOARD");
            if(entries.Length==0)text.Append("\nNO RECORDED TIMES YET");
            else for(int i=0;i<Math.Min(5,entries.Length);i++)text.Append("\n").Append(i+1).Append("  ")
                    .Append(entries[i].ProfileName).Append("  ").Append(entries[i].Seconds.ToString("F2")).Append(" s");
            if(!string.IsNullOrEmpty(leaderboardError))text.Append("\nRECORD WARNING  ").Append(leaderboardError);
            text.Append(bird.UsesHands ? "\n\nPINCH: BEGIN REQUIRED REST" : "\n\nRIGHT TRIGGER: BEGIN REQUIRED REST");return text.ToString();
        }

        private static string PracticeReasons(CourseNonRankedReason reasons)
        {
            var labels=new List<string>();
            if((reasons&(CourseNonRankedReason.TrackingLost|CourseNonRankedReason.CalibrationLost))!=0)
                labels.Add("tracking interrupted");
            if((reasons&(CourseNonRankedReason.Paused|CourseNonRankedReason.ApplicationSuspended))!=0)
                labels.Add("flight paused");
            if((reasons&CourseNonRankedReason.Collision)!=0)labels.Add("obstacle contact");
            if((reasons&(CourseNonRankedReason.Reset|CourseNonRankedReason.Recovery))!=0)
                labels.Add("position reset");
            if((reasons&(CourseNonRankedReason.ControlModeChanged|CourseNonRankedReason.WeatherChanged
                |CourseNonRankedReason.AssistanceChanged))!=0)labels.Add("flight settings changed");
            if((reasons&CourseNonRankedReason.FrameTiming)!=0)labels.Add("frame delay");
            if((reasons&CourseNonRankedReason.StreamingBlocked)!=0)labels.Add("world loading");
            if((reasons&(CourseNonRankedReason.ClockInvalid|CourseNonRankedReason.ClockWentBackward))!=0)
                labels.Add("timer interrupted");
            if((reasons&CourseNonRankedReason.ExplicitPractice)!=0)labels.Add("practice selected");
            // Bound the results card even when several interruptions occur in one run.
            string visible=string.Join(" · ",labels.GetRange(0,Math.Min(3,labels.Count)));
            return labels.Count==0?"ranking unavailable":visible+(labels.Count>3?" +"+(labels.Count-3)+" more":"");
        }

        private string FailureText(CourseFailureReason reason)
        {
            if(reason==CourseFailureReason.TimeLimit)
                return "TIME RAN OUT BEFORE "+runtime.Progress.Label.ToUpperInvariant();
            if(reason==CourseFailureReason.CorridorLeft)return "LEFT THE MARKED FLIGHT PATH";
            return "ATTEMPT ENDED";
        }

        private string DirectionHint()
        {
            if(runtime==null||runtime.State!=CourseState.Running||runtime.TaskIndex<0
                ||runtime.TaskIndex>=definition.Tasks.Count)return "FOLLOW THE GOLD PATH";
            var task=definition.Tasks[runtime.TaskIndex];
            if(!HasAnchor(task)||worldRoot==null||bird?.Controller==null)return "COMPLETE IT HERE";
            Vector3 target=worldRoot.transform.TransformPoint(TaskAnchor(task));
            Vector3 delta=target-bird.Controller.State.Position;
            if(delta.sqrMagnitude<4f)return "HERE";
            var camera=Camera.main;
            Vector3 view=camera!=null?camera.transform.InverseTransformDirection(delta.normalized):delta.normalized;
            string direction=view.z<.1f?"TURN AROUND":Mathf.Abs(view.x)>.38f
                ?view.x<0?"LEFT":"RIGHT":view.y>.38f?"UP":view.y<-.38f?"DOWN":"AHEAD";
            return "NEXT: "+direction+" "+Mathf.RoundToInt(delta.magnitude)+"m";
        }

        private void UpdateBearing(int current)
        {
            if(bird==null||bird.FlightOverlayBlocked||runtime.State!=CourseState.Running||current<0||current>=definition.Tasks.Count
                ||!HasAnchor(definition.Tasks[current]))
            {foreach(var c in bearingChevrons)c.enabled=false;return;}
            Vector3 from=bird.Controller.State.Position-worldRoot.transform.position;
            Vector3 target=TaskAnchor(definition.Tasks[current]);Vector3 delta=target-from;
            if(delta.sqrMagnitude<4){foreach(var c in bearingChevrons)c.enabled=false;return;}
            Vector3 direction=delta.normalized;Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
            if(right.sqrMagnitude<.1f)right=Vector3.right;
            for(int i=0;i<bearingChevrons.Count;i++)
            {
                var c=bearingChevrons[i];c.enabled=true;Vector3 tip=Vector3.Lerp(from,target,(i+1f)/(bearingChevrons.Count+1f));
                Vector3 back=tip-direction*1.3f;c.SetPosition(0,back-right*.8f);c.SetPosition(1,tip);c.SetPosition(2,back+right*.8f);
            }
        }

        private bool HasAnchor(CourseTaskDefinition task)=>task.Kind==CourseTaskKind.Gate
            ||task.Kind==CourseTaskKind.AltitudeBand||task.Kind==CourseTaskKind.Corridor
            ||task.Kind==CourseTaskKind.SupportedLanding||task.Kind==CourseTaskKind.CollectibleQuota;
        private Vector3 TaskAnchor(CourseTaskDefinition task)
        {
            if(task.Kind==CourseTaskKind.CollectibleQuota)
                return (Local(task.PointA)+Local(task.PointB))*.5f;
            var point=task.Kind==CourseTaskKind.Corridor?task.PointB:task.PointA;
            if(task.Kind==CourseTaskKind.AltitudeBand)point=new CoursePoint(point.X,(task.MinimumAltitude+task.MaximumAltitude)*.5,point.Z);
            return Local(point);
        }
        private static Vector3 Local(CoursePoint point)=>new Vector3((float)point.X,(float)point.Y,(float)point.Z);
        private void OnWorldRebased(Vector3 delta){if(worldRoot!=null)worldRoot.transform.position-=delta;}
        private static bool IsCountdown(CourseState state)=>state==CourseState.Countdown3||state==CourseState.Countdown2||state==CourseState.Countdown1;

        private void OnDestroy()
        {
            ClearActiveObjective();
            if(foraging!=null)foraging.Caught-=OnCaught;if(space!=null)space.Rebased-=OnWorldRebased;
            if(worldRoot!=null)Destroy(worldRoot);if(overlay!=null)Destroy(overlay.gameObject);if(material!=null)Destroy(material);
        }
    }
}
