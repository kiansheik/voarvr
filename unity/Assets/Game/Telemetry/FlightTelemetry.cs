using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Telemetry
{
    public sealed class FlightTelemetry : MonoBehaviour
    {
        [Serializable] public sealed class Header
        {
            public int schemaVersion=3;
            public string[] wideFields=TelemetrySample.WideFields;
            public string effortDefinition="valid airborne simulation-time movement proxies only; capped dt excludes pause/perch/tracking gaps; hand speed threshold0.35m/s, quiet3s, EMA2s; not calories or medical fatigue";
            public string encoding="float32 except wideFields float64; clearance sampled10Hz";
            public string session, utc, git, unity, app, device, os, character, input, worldSettings;
            public string[] fields=TelemetrySample.Fields;
            public BirdFlightProfile flight;
            public AcrobaticProfile acrobatic;
            public int activity;
            public double islandCell=FlightRegions.IslandCell;
            public float thermalWidth,thermalAssistMaximum=ThermalCentering.MaximumBankContribution;
            public string verticalAir="persistent terrain-fed quartic columns plus finite drifting plumes; departure midpoint165 halfheight185 radius95; island midpointY-40 halfheight180 radius100; Assisted9 Touring6 Wild7 m/s";
            public BirdMorphology morphology;
            public WingArchitecture architecture;
            public AvianArticulationSettings articulation;
            public Vector3 spawn;
            public int windMode,chunkRadius;
            public float chunkSize,generationBudgetMs;
            public double plumeCellSize,plumeEpochSeconds,plumeLifetime;
            public int seed;
            public bool allocationCounterAvailable=false;
            public string velocityConvention="raw_*_velocity: body-relative finite difference in tracking axes; native_*: XR tracking-space device feature, m/s and rad/s; unavailable values NaN";
        }
        private TelemetryWriter writer;
        private readonly FlightEffort effort=new FlightEffort();
        private bool resting,protection=true;private int lastFood;private float nextEffort,nextGround,groundAt;private double cachedClearance=double.NaN;
        private VoarVR.Gameplay.SkyForaging foraging;
        private BirdFlightDriver driver;
        private BirdRigDriver rig;
        private WorldStreamer world;
        private AvianWingPresentation avian;
        private int marker, collisions, landings, takeoffs, calibrationSequence, trackingMask=-1;
        private FlightPhase previousPhase;
        private bool stall, thermal, blocked, approach, errorLogged;
        private int previousControlMode=-1,previousTricks,previousObjective=-1,previousObjectiveStatus=-1,previousRegion=-1;
        private int previousView=-1, previousWeather=-1;
        private readonly FrameTiming[] timings=new FrameTiming[1];
        private readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
        public string SessionPath { get; private set; }
        public int Dropped => writer?.Dropped??0;
        public static bool DefaultEnabled => Debug.isDebugBuild || Application.isEditor;
        public void Configure(BirdFlightDriver owner,BirdRigDriver bird,WorldStreamer streamed,
            BirdCharacterDefinition character,BirdFlightProfile profile)
        {
            driver=owner; rig=bird; world=streamed;foraging=owner.GetComponent<VoarVR.Gameplay.SkyForaging>();
            if(!DefaultEnabled) { enabled=false; return; }
            avian=GetComponent<AvianWingPresentation>();
            SubsystemManager.GetSubsystems(displays);
            string id=Guid.NewGuid().ToString("N");
            SessionPath=Path.Combine(Application.persistentDataPath,"telemetry",id+".voartlm");
            var build=Resources.Load<TextAsset>("BuildRevision");
            var h=new Header { session=id,utc=DateTime.UtcNow.ToString("O"),git=build!=null?build.text.Trim():"unknown",
                unity=Application.unityVersion,app=Application.version,device=SystemInfo.deviceModel,os=SystemInfo.operatingSystem,
                character=character!=null?character.DisplayName:"unknown",flight=profile,acrobatic=driver.Controller.AdvancedProfile,activity=(int)VoarVR.Gameplay.ActivitySelection.Chosen,morphology=character!=null?character.Morphology:null,
                architecture=character!=null?character.Architecture:WingArchitecture.LegacyAvian,
                articulation=character!=null?character.Articulation:null,spawn=driver.transform.position,
                windMode=WeatherCode(driver.WindModeName),chunkRadius=WorldStreamer.Radius,chunkSize=128,
                generationBudgetMs=world!=null?world.GenerationBudgetMilliseconds:0,
                plumeCellSize=AtmosphereModel.CellSize,plumeEpochSeconds=AtmosphereModel.EpochSeconds,plumeLifetime=AtmosphereModel.Lifetime,
                input=driver.Controller.InputMode,seed=world!=null?world.Space.Seed:0,
                thermalWidth=FlightRegions.ThermalScale(profile,(WindMode)WeatherCode(driver.WindModeName)),
                worldSettings="128m chunks; radius2; sky pool9 near520 collision260; wind="+driver.WindModeName };
            writer=new TelemetryWriter(SessionPath,JsonUtility.ToJson(h));
            Record(TelemetryEvent.SessionStart);
            Debug.Log("VOAR_TELEMETRY_PATH="+SessionPath);
        }
        public void Record(TelemetryEvent kind,double value=0)
        {
            if(kind==TelemetryEvent.CalibrationAccepted) calibrationSequence++;
            writer?.Event(kind,Time.realtimeSinceStartupAsDouble,value);
        }
        public void Capture(FlightInputFrame raw,float dt,bool wingsEnabled=true)
        {
            if(writer==null) return;
            if(writer.Error!=null) { if(!errorLogged) Debug.LogError("Telemetry stopped: "+writer.Error); errorLogged=true; return; }
            long captureStart=System.Diagnostics.Stopwatch.GetTimestamp();
            var c=driver.Controller; var mapped=c.LastInput; var cal=driver.Calibration;
            if(raw.MarkerPressed || Keyboard.current!=null && Keyboard.current.mKey.wasPressedThisFrame)
            { marker++; Record(TelemetryEvent.Marker,marker); driver.ShowTelemetryMarker(marker); }
            int tracked=(raw.HeadTracked?1:0)|(raw.LeftWing.Tracked?2:0)|(raw.RightWing.Tracked?4:0);
            if(tracked!=trackingMask)
            { Record(tracked==7?TelemetryEvent.TrackingRecovered:TelemetryEvent.TrackingLost,tracked); trackingMask=tracked; }
            bool newStall=c.IsStalled;
            Transition(ref stall,newStall,TelemetryEvent.StallEntry,TelemetryEvent.StallRecovery);
            Transition(ref thermal,c.WindVelocity.y>2f,TelemetryEvent.ThermalEntry,TelemetryEvent.ThermalExit);
            Transition(ref blocked,c.StreamingBlocked,TelemetryEvent.StreamingStall,TelemetryEvent.StreamingRecovered);
            if(c.LandingApproach>.1f && !approach) Record(TelemetryEvent.LandingAttempt,c.State.Speed);
            approach=c.LandingApproach>.1f;
            if(c.CollisionCount!=collisions) { Record(TelemetryEvent.Collision,c.LastImpactSpeed); collisions=c.CollisionCount; }
            if(c.LandingCount!=landings) { Record(TelemetryEvent.LandingSuccess,c.LastImpactSpeed); landings=c.LandingCount; }
            if(raw.ResetPressed) Record(TelemetryEvent.FlightReset);
            if(c.TakeoffCount>takeoffs) Record(TelemetryEvent.Takeoff);
            else if(!raw.ResetPressed && previousPhase==FlightPhase.Perched && c.State.Phase!=FlightPhase.Perched && c.State.Phase!=FlightPhase.Paused) Record(TelemetryEvent.SupportLost);
            takeoffs=c.TakeoffCount;
            if(previousPhase!=FlightPhase.Paused && c.State.Phase==FlightPhase.Paused) Record(TelemetryEvent.Paused);
            if(previousPhase==FlightPhase.Paused && c.State.Phase!=FlightPhase.Paused) Record(TelemetryEvent.Resumed);
            previousPhase=c.State.Phase;
            int weather=WeatherCode(driver.WindModeName), view=(int)driver.ViewMode;
            if(weather!=previousWeather) { Record(TelemetryEvent.WeatherChanged,weather); previousWeather=weather; }
            if(view!=previousView) { Record(TelemetryEvent.ViewChanged,view); previousView=view; }
            var leftDevice=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rightDevice=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool leftVelocityAvailable=leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity,out var leftVelocity);
            bool rightVelocityAvailable=rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity,out var rightVelocity);
            bool leftAngularAvailable=leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAngularVelocity,out var leftAngular);
            bool rightAngularAvailable=rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAngularVelocity,out var rightAngular);
            var unavailable=new Vector3(float.NaN,float.NaN,float.NaN);
            if(!leftVelocityAvailable) leftVelocity=unavailable; if(!rightVelocityAvailable) rightVelocity=unavailable;
            if(!leftAngularAvailable) leftAngular=unavailable; if(!rightAngularAvailable) rightAngular=unavailable;
            var logical=world!=null?world.Space.ToLogical(c.State.Position):new LogicalPosition(c.State.Position.x,c.State.Position.y,c.State.Position.z);
            var key=world!=null?world.KeyAt(c.State.Position):default;
            FrameTimingManager.CaptureFrameTimings();
            bool timingAvailable=FrameTimingManager.GetLatestTimings(1,timings)>0 && timings[0].cpuFrameTime>0;
            double cpuMs=timingAvailable?timings[0].cpuFrameTime:double.NaN;
            double gpuMs=timingAvailable && timings[0].gpuFrameTime>0?timings[0].gpuFrameTime:double.NaN;
            float refreshHz=float.NaN; bool refreshAvailable=displays.Count>0 && displays[0].TryGetDisplayRefreshRate(out refreshHz);
            if(!refreshAvailable) refreshHz=float.NaN;
            if(Time.unscaledTime>=nextGround || raw.ResetPressed){groundAt=Time.unscaledTime;nextGround=groundAt+.1f;cachedClearance=Physics.Raycast(c.State.Position,Vector3.down,out var ground,3000f,1<<WorldStreamer.CollisionLayer,QueryTriggerInteraction.Ignore)?ground.distance:double.NaN;}
            bool clearanceAvailable=!double.IsNaN(cachedClearance);double clearance=cachedClearance;
            effort.Step(mapped,dt,wingsEnabled && raw.HeadTracked && raw.LeftWing.Tracked && raw.RightWing.Tracked && c.State.Phase!=FlightPhase.Paused && c.State.Phase!=FlightPhase.Perched && !c.StreamingBlocked);
            Transition(ref resting,effort.Resting,TelemetryEvent.RestStarted,TelemetryEvent.RestEnded);
            bool protectedNow=c.FeatherSupport>.5f;Transition(ref protection,protectedNow,TelemetryEvent.StallProtectionRecovered,TelemetryEvent.StallProtectionLost);
            if(foraging!=null && foraging.Score.Caught>lastFood){Record(TelemetryEvent.CollectibleCaught,foraging.Score.Points);lastFood=foraging.Score.Caught;}
            if(Time.unscaledTime>=nextEffort){Record(TelemetryEvent.EffortSummary,effort.SmoothedSpeed);nextEffort=Time.unscaledTime+10;}
            var challenge=driver.Expedition!=null?driver.Expedition.Challenge:null;
            int mode=(int)c.ControlMode,stage=challenge!=null?challenge.Stage:-1,status=challenge!=null?(int)challenge.Status:0;
            if(mode!=previousControlMode){Record(TelemetryEvent.ControlModeChanged,mode);previousControlMode=mode;}
            if(c.Tricks.Count>previousTricks)Record(TelemetryEvent.TrickCompleted,(int)c.Tricks.Last);previousTricks=c.Tricks.Count;
            if(challenge!=null && challenge.Activity!=VoarVR.Gameplay.FlightActivity.FreeFlight)
            {
                if(previousObjective<0)Record(TelemetryEvent.ObjectiveStarted,(int)challenge.Activity);
                else if(stage!=previousObjective)Record(TelemetryEvent.ObjectiveProgress,stage);
                if(status!=previousObjectiveStatus && challenge.Status==VoarVR.Gameplay.ChallengeStatus.Completed)Record(TelemetryEvent.ObjectiveCompleted,challenge.Score);
                if(status!=previousObjectiveStatus && challenge.Status==VoarVR.Gameplay.ChallengeStatus.Failed)Record(TelemetryEvent.ObjectiveFailed);
            }
            previousObjective=stage;previousObjectiveStatus=status;
            var logicalPosition=world!=null?world.Space.ToLogical(c.State.Position):new LogicalPosition(c.State.Position.x,c.State.Position.y,c.State.Position.z);
            int region=(int)FlightRegions.Weather(logicalPosition.X,logicalPosition.Y,logicalPosition.Z);
            if(region!=previousRegion){Record(TelemetryEvent.RegionChanged,region);previousRegion=region;}
            var island=FlightRegions.Island(world!=null?world.Space.Seed:7319,(long)Math.Floor(logicalPosition.X/768),(long)Math.Floor(logicalPosition.Z/768));
            var sample=new TelemetrySample
            {
                span_ratio=c.SpanRatio,inferred_tuck=c.InferredTuck,feather_support=c.FeatherSupport,feather_degrees=c.WingFeatherDeg,
                aero_torque_x=c.Acrobatic.AerodynamicTorque.x,aero_torque_y=c.Acrobatic.AerodynamicTorque.y,aero_torque_z=c.Acrobatic.AerodynamicTorque.z,
                effort_active_seconds=effort.ActiveSeconds,effort_rest_seconds=effort.RestSeconds,effort_hand_travel_m=effort.HandTravelMeters,effort_speed_ema=effort.SmoothedSpeed,effort_strokes=effort.Strokes,effort_continuous_seconds=effort.ContinuousActiveSeconds,effort_resting=effort.Resting?1:0,
                food_caught=foraging!=null?foraging.Score.Caught:0,food_points=foraging!=null?foraging.Score.Points:0,food_combo=foraging!=null?foraging.Score.Combo:0,
                objective_quiet_gain=challenge!=null?challenge.SoaringGain:0,objective_highest_altitude=challenge!=null?challenge.HighestAltitude:0,clearance_age_seconds=Time.unscaledTime-groundAt,
                timestamp = Time.realtimeSinceStartupAsDouble,
                frame = Time.frameCount,
                simulation_time = c.SimulationTime,
                dt = dt,
                render_dt = Time.deltaTime,
                unscaled_dt = Time.unscaledDeltaTime,
                raw_left_tracked = raw.LeftWing.Tracked ? 1 : 0,
                raw_left_position_x = raw.LeftWing.Position.x,
                raw_left_position_y = raw.LeftWing.Position.y,
                raw_left_position_z = raw.LeftWing.Position.z,
                raw_left_velocity_x = raw.LeftWing.Velocity.x,
                raw_left_velocity_y = raw.LeftWing.Velocity.y,
                raw_left_velocity_z = raw.LeftWing.Velocity.z,
                raw_left_rotation_x = raw.LeftWing.Orientation.x,
                raw_left_rotation_y = raw.LeftWing.Orientation.y,
                raw_left_rotation_z = raw.LeftWing.Orientation.z,
                raw_left_rotation_w = raw.LeftWing.Orientation.w,
                raw_right_tracked = raw.RightWing.Tracked ? 1 : 0,
                raw_right_position_x = raw.RightWing.Position.x,
                raw_right_position_y = raw.RightWing.Position.y,
                raw_right_position_z = raw.RightWing.Position.z,
                raw_right_velocity_x = raw.RightWing.Velocity.x,
                raw_right_velocity_y = raw.RightWing.Velocity.y,
                raw_right_velocity_z = raw.RightWing.Velocity.z,
                raw_right_rotation_x = raw.RightWing.Orientation.x,
                raw_right_rotation_y = raw.RightWing.Orientation.y,
                raw_right_rotation_z = raw.RightWing.Orientation.z,
                raw_right_rotation_w = raw.RightWing.Orientation.w,
                raw_head_tracked = raw.HeadTracked ? 1 : 0,
                raw_head_position_x = raw.HeadPosition.x,
                raw_head_position_y = raw.HeadPosition.y,
                raw_head_position_z = raw.HeadPosition.z,
                raw_head_rotation_x = raw.HeadOrientation.x,
                raw_head_rotation_y = raw.HeadOrientation.y,
                raw_head_rotation_z = raw.HeadOrientation.z,
                raw_head_rotation_w = raw.HeadOrientation.w,
                raw_look_x = raw.LookDirection.x,
                raw_look_y = raw.LookDirection.y,
                raw_look_z = raw.LookDirection.z,
                raw_body_rotation_x = raw.BodyOrientation.x,
                raw_body_rotation_y = raw.BodyOrientation.y,
                raw_body_rotation_z = raw.BodyOrientation.z,
                raw_body_rotation_w = raw.BodyOrientation.w,
                raw_body_tracked = raw.BodyTracked ? 1 : 0,
                raw_bank = raw.Bank,
                raw_tuck = raw.Tuck,
                raw_flare = raw.Flare,
                raw_ground_move_x = raw.GroundMove.x,
                raw_ground_move_y = raw.GroundMove.y,
                raw_reset = raw.ResetPressed ? 1 : 0,
                raw_recalibrate = raw.RecalibratePressed ? 1 : 0,
                raw_characterselect = raw.CharacterSelectPressed ? 1 : 0,
                raw_pause = raw.PausePressed ? 1 : 0,
                raw_viewtoggle = raw.ViewTogglePressed ? 1 : 0,
                raw_windmode = raw.WindModePressed ? 1 : 0,
                raw_hudtoggle = raw.HudTogglePressed ? 1 : 0,
                raw_marker = raw.MarkerPressed ? 1 : 0,
                raw_buttons = raw.ButtonsHeld,
                mapped_left_tracked = mapped.LeftWing.Tracked ? 1 : 0,
                mapped_left_position_x = mapped.LeftWing.Position.x,
                mapped_left_position_y = mapped.LeftWing.Position.y,
                mapped_left_position_z = mapped.LeftWing.Position.z,
                mapped_left_velocity_x = mapped.LeftWing.Velocity.x,
                mapped_left_velocity_y = mapped.LeftWing.Velocity.y,
                mapped_left_velocity_z = mapped.LeftWing.Velocity.z,
                mapped_left_rotation_x = mapped.LeftWing.Orientation.x,
                mapped_left_rotation_y = mapped.LeftWing.Orientation.y,
                mapped_left_rotation_z = mapped.LeftWing.Orientation.z,
                mapped_left_rotation_w = mapped.LeftWing.Orientation.w,
                mapped_right_tracked = mapped.RightWing.Tracked ? 1 : 0,
                mapped_right_position_x = mapped.RightWing.Position.x,
                mapped_right_position_y = mapped.RightWing.Position.y,
                mapped_right_position_z = mapped.RightWing.Position.z,
                mapped_right_velocity_x = mapped.RightWing.Velocity.x,
                mapped_right_velocity_y = mapped.RightWing.Velocity.y,
                mapped_right_velocity_z = mapped.RightWing.Velocity.z,
                mapped_right_rotation_x = mapped.RightWing.Orientation.x,
                mapped_right_rotation_y = mapped.RightWing.Orientation.y,
                mapped_right_rotation_z = mapped.RightWing.Orientation.z,
                mapped_right_rotation_w = mapped.RightWing.Orientation.w,
                mapped_head_tracked = mapped.HeadTracked ? 1 : 0,
                mapped_head_position_x = mapped.HeadPosition.x,
                mapped_head_position_y = mapped.HeadPosition.y,
                mapped_head_position_z = mapped.HeadPosition.z,
                mapped_head_rotation_x = mapped.HeadOrientation.x,
                mapped_head_rotation_y = mapped.HeadOrientation.y,
                mapped_head_rotation_z = mapped.HeadOrientation.z,
                mapped_head_rotation_w = mapped.HeadOrientation.w,
                mapped_look_x = mapped.LookDirection.x,
                mapped_look_y = mapped.LookDirection.y,
                mapped_look_z = mapped.LookDirection.z,
                mapped_body_rotation_x = mapped.BodyOrientation.x,
                mapped_body_rotation_y = mapped.BodyOrientation.y,
                mapped_body_rotation_z = mapped.BodyOrientation.z,
                mapped_body_rotation_w = mapped.BodyOrientation.w,
                mapped_body_tracked = mapped.BodyTracked ? 1 : 0,
                mapped_bank = mapped.Bank,
                mapped_tuck = mapped.Tuck,
                mapped_flare = mapped.Flare,
                mapped_ground_move_x = mapped.GroundMove.x,
                mapped_ground_move_y = mapped.GroundMove.y,
                mapped_reset = mapped.ResetPressed ? 1 : 0,
                mapped_recalibrate = mapped.RecalibratePressed ? 1 : 0,
                mapped_characterselect = mapped.CharacterSelectPressed ? 1 : 0,
                mapped_pause = mapped.PausePressed ? 1 : 0,
                mapped_viewtoggle = mapped.ViewTogglePressed ? 1 : 0,
                mapped_windmode = mapped.WindModePressed ? 1 : 0,
                mapped_hudtoggle = mapped.HudTogglePressed ? 1 : 0,
                mapped_marker = mapped.MarkerPressed ? 1 : 0,
                mapped_buttons = mapped.ButtonsHeld,
                native_left_velocity_available = leftVelocityAvailable ? 1 : 0,
                native_left_velocity_x = leftVelocity.x,
                native_left_velocity_y = leftVelocity.y,
                native_left_velocity_z = leftVelocity.z,
                native_left_angular_available = leftAngularAvailable ? 1 : 0,
                native_left_angular_x = leftAngular.x,
                native_left_angular_y = leftAngular.y,
                native_left_angular_z = leftAngular.z,
                native_right_velocity_available = rightVelocityAvailable ? 1 : 0,
                native_right_velocity_x = rightVelocity.x,
                native_right_velocity_y = rightVelocity.y,
                native_right_velocity_z = rightVelocity.z,
                native_right_angular_available = rightAngularAvailable ? 1 : 0,
                native_right_angular_x = rightAngular.x,
                native_right_angular_y = rightAngular.y,
                native_right_angular_z = rightAngular.z,
                calibrated = cal.Captured ? 1 : 0,
                calibration_sequence = calibrationSequence,
                input_wings_enabled = wingsEnabled?1:0,
                human_span = cal.HumanSpanMeters,
                bird_half_span = cal.BirdHalfSpanMeters,
                motion_scale = cal.MotionScale,
                calibration_head_origin_x = cal.HeadOrigin.x,
                calibration_head_origin_y = cal.HeadOrigin.y,
                calibration_head_origin_z = cal.HeadOrigin.z,
                calibration_heading_x = cal.Heading.x,
                calibration_heading_y = cal.Heading.y,
                calibration_heading_z = cal.Heading.z,
                calibration_heading_w = cal.Heading.w,
                calibration_pitch_x = cal.NeutralLookPitch.x,
                calibration_pitch_y = cal.NeutralLookPitch.y,
                calibration_pitch_z = cal.NeutralLookPitch.z,
                calibration_pitch_w = cal.NeutralLookPitch.w,
                calibration_left_neutral_x = cal.LeftNeutral.x,
                calibration_left_neutral_y = cal.LeftNeutral.y,
                calibration_left_neutral_z = cal.LeftNeutral.z,
                calibration_left_rotation_x = cal.LeftRotation.x,
                calibration_left_rotation_y = cal.LeftRotation.y,
                calibration_left_rotation_z = cal.LeftRotation.z,
                calibration_left_rotation_w = cal.LeftRotation.w,
                target_left_x = rig.LeftTarget.x,
                target_left_y = rig.LeftTarget.y,
                target_left_z = rig.LeftTarget.z,
                reach_left = rig.LeftReachError,
                joint_left_upper_x = rig.leftUpper.localRotation.x,
                joint_left_upper_y = rig.leftUpper.localRotation.y,
                joint_left_upper_z = rig.leftUpper.localRotation.z,
                joint_left_upper_w = rig.leftUpper.localRotation.w,
                joint_left_forearm_x = rig.leftForearm.localRotation.x,
                joint_left_forearm_y = rig.leftForearm.localRotation.y,
                joint_left_forearm_z = rig.leftForearm.localRotation.z,
                joint_left_forearm_w = rig.leftForearm.localRotation.w,
                joint_left_hand_x = rig.leftHand.localRotation.x,
                joint_left_hand_y = rig.leftHand.localRotation.y,
                joint_left_hand_z = rig.leftHand.localRotation.z,
                joint_left_hand_w = rig.leftHand.localRotation.w,
                calibration_right_neutral_x = cal.RightNeutral.x,
                calibration_right_neutral_y = cal.RightNeutral.y,
                calibration_right_neutral_z = cal.RightNeutral.z,
                calibration_right_rotation_x = cal.RightRotation.x,
                calibration_right_rotation_y = cal.RightRotation.y,
                calibration_right_rotation_z = cal.RightRotation.z,
                calibration_right_rotation_w = cal.RightRotation.w,
                target_right_x = rig.RightTarget.x,
                target_right_y = rig.RightTarget.y,
                target_right_z = rig.RightTarget.z,
                reach_right = rig.RightReachError,
                joint_right_upper_x = rig.rightUpper.localRotation.x,
                joint_right_upper_y = rig.rightUpper.localRotation.y,
                joint_right_upper_z = rig.rightUpper.localRotation.z,
                joint_right_upper_w = rig.rightUpper.localRotation.w,
                joint_right_forearm_x = rig.rightForearm.localRotation.x,
                joint_right_forearm_y = rig.rightForearm.localRotation.y,
                joint_right_forearm_z = rig.rightForearm.localRotation.z,
                joint_right_forearm_w = rig.rightForearm.localRotation.w,
                joint_right_hand_x = rig.rightHand.localRotation.x,
                joint_right_hand_y = rig.rightHand.localRotation.y,
                joint_right_hand_z = rig.rightHand.localRotation.z,
                joint_right_hand_w = rig.rightHand.localRotation.w,
                position_x = c.State.Position.x,
                position_y = c.State.Position.y,
                position_z = c.State.Position.z,
                velocity_x = c.State.Velocity.x,
                velocity_y = c.State.Velocity.y,
                velocity_z = c.State.Velocity.z,
                rotation_x = c.State.Rotation.x,
                rotation_y = c.State.Rotation.y,
                rotation_z = c.State.Rotation.z,
                rotation_w = c.State.Rotation.w,
                logical_x = logical.X,
                logical_y = logical.Y,
                logical_z = logical.Z,
                wind_x = c.WindVelocity.x,
                wind_y = c.WindVelocity.y,
                wind_z = c.WindVelocity.z,
                lift_x = c.LiftForce.x,
                lift_y = c.LiftForce.y,
                lift_z = c.LiftForce.z,
                drag_x = c.DragForce.x,
                drag_y = c.DragForce.y,
                drag_z = c.DragForce.z,
                stroke_x = c.StrokeForce.x,
                stroke_y = c.StrokeForce.y,
                stroke_z = c.StrokeForce.z,
                phase = (int)c.State.Phase,
                airspeed = (c.State.Velocity-c.WindVelocity).magnitude,
                groundspeed = c.State.Speed,
                aoa = c.AngleOfAttackDeg,
                head_pitch = c.HeadPitchInput,
                brake = c.LandingBrake,
                energy = c.MechanicalEnergy,
                landing_approach = c.LandingApproach,
                collision_count = c.CollisionCount,
                landing_count = c.LandingCount,
                impact_speed = c.LastImpactSpeed,
                streaming_blocked = c.StreamingBlocked ? 1 : 0,
                chunks = world != null ? world.ActiveCount : 0,
                chunk_queue = world != null ? world.PendingCount : 0,
                chunk_x = key.X,
                chunk_z = key.Z,
                generation_ms = world != null ? world.LastGenerationMilliseconds : 0,
                view = (int)driver.ViewMode,
                weather = WeatherCode(driver.WindModeName),
                marker = marker,
                dropped = writer.Dropped,
                cpu_ms = cpuMs,
                gpu_ms = gpuMs,
                timing_available = timingAvailable ? 1 : 0,
                refresh_hz = refreshHz,
                refresh_available = refreshAvailable ? 1 : 0,
                clearance = clearance,
                clearance_available = clearanceAvailable ? 1 : 0,
                avian_leftfan = avian != null ? avian.State.LeftFan : double.NaN,
                avian_rightfan = avian != null ? avian.State.RightFan : double.NaN,
                avian_leftfold = avian != null ? avian.State.LeftFold : double.NaN,
                avian_rightfold = avian != null ? avian.State.RightFold : double.NaN,
                avian_alula = avian != null ? avian.State.Alula : double.NaN,
                avian_tailspread = avian != null ? avian.State.TailSpread : double.NaN,
                avian_tailpitch = avian != null ? avian.State.TailPitch : double.NaN,
                avian_tailyaw = avian != null ? avian.State.TailYaw : double.NaN,
                raw_control_mode_pressed=raw.ControlModePressed?1:0,control_mode=mode,
                angular_velocity_x=c.Acrobatic.AngularVelocity.x,angular_velocity_y=c.Acrobatic.AngularVelocity.y,angular_velocity_z=c.Acrobatic.AngularVelocity.z,
                control_torque_x=c.Acrobatic.ControlTorque.x,control_torque_y=c.Acrobatic.ControlTorque.y,control_torque_z=c.Acrobatic.ControlTorque.z,
                trick_count=c.Tricks.Count,trick_score=c.Tricks.Score,last_trick=(int)c.Tricks.Last,
                mission_id=challenge!=null?(int)challenge.Activity:0,objective_stage=stage,objective_progress=challenge!=null?challenge.Progress:0,objective_status=status,mission_score=challenge!=null?challenge.Score:0,
                thermal_gradient_x=c.ThermalGradient.x,thermal_gradient_z=c.ThermalGradient.z,thermal_assist_bank=c.ThermalAssist,
                altitude_biome=(int)FlightRegions.Biome(logicalPosition.Y),weather_region=region,island_distance=VoarVR.Gameplay.FlightChallenge.HorizontalDistance(logicalPosition,island),
            };
            sample.capture_cpu_ms=(System.Diagnostics.Stopwatch.GetTimestamp()-captureStart)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            sample.stalled=c.IsStalled?1:0;sample.takeoff_count=c.TakeoffCount;
            writer.Capture(sample);
        }
        private void Transition(ref bool state,bool next,TelemetryEvent enter,TelemetryEvent exit)
        { if(state==next) return; state=next; Record(next?enter:exit); }
        private static int WeatherCode(string name)
        { if(name.IndexOf("Assisted",StringComparison.OrdinalIgnoreCase)>=0)return 0; if(name.IndexOf("Tour",StringComparison.OrdinalIgnoreCase)>=0)return 1; if(name.IndexOf("Wild",StringComparison.OrdinalIgnoreCase)>=0)return 2; return 3; }
        private void OnApplicationPause(bool paused) { if(paused) writer?.Flush(); }
        private void OnApplicationFocus(bool focused) { if(!focused) writer?.Flush(); }
        private void OnDestroy() { Record(TelemetryEvent.SessionEnd); writer?.Stop(); }
        private void OnApplicationQuit() { Record(TelemetryEvent.SessionEnd); writer?.Stop(); }
    }
}
