using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class PlayerFacingFeatureTests
    {
        private sealed class ScriptedInput:IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Final results tracking regression";
            public FlightInputFrame Sample(float deltaTime)=>Frame;
        }

        private sealed class CourseInputClock:IMonotonicClock
        {
            public double NowSeconds {get;set;}=100;
        }

        [UnityTest]
        public IEnumerator CalibrationCoachAlwaysShowsPoseQuestAndRedoExplanation()
        {
            var cameraObject=new GameObject("Calibration test eye");var camera=cameraObject.AddComponent<Camera>();
            var owner=new GameObject("Calibration test owner");var coach=owner.AddComponent<CalibrationCoach>();
            coach.Configure(camera);coach.Show(false);
            Assert.That(coach.Visible,Is.True);
            var labels=string.Join("\n",coach.Panel.GetComponentsInChildren<Text>().Select(t=>t.text));
            Assert.That(labels,Does.Contain("relaxed T pose"));
            Assert.That(labels,Does.Contain("below the horizon"));
            Assert.That(labels,Does.Contain("return here at any time"));
            Assert.That(coach.Panel.transform.Find("Pose diagram/Quest headset"),Is.Not.Null);
            Assert.That(coach.Panel.transform.Find("Pose diagram/Left controller"),Is.Not.Null);
            coach.Show(true);
            labels=string.Join("\n",coach.Panel.GetComponentsInChildren<Text>().Select(t=>t.text));
            Assert.That(labels,Does.Contain("RECALIBRATE YOUR FLIGHT"));
            Assert.That(labels,Does.Contain("Your current flight and progress are kept."));
            Object.Destroy(owner);Object.Destroy(cameraObject);yield return null;yield return null;
            Assert.That(coach.Panel==null,Is.True);
        }

        [UnityTest]
        public IEnumerator CourseModeBuildsCountdownRouteCollidersRibbonAndFinishPerch()
        {
            ActivitySelection.Chosen=FlightActivity.ObstacleCourse;
            ActivitySelection.ChosenCourseId="moth-line";
            CharacterSelection.Chosen=Resources.LoadAll<BirdCharacterDefinition>(CharacterSelection.ResourcesFolder)
                .First(c=>c.name==CharacterSelection.DefaultCharacterName);
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();
            Assert.That(driver,Is.Not.Null);Assert.That(driver.ObstacleCourse,Is.Not.Null);
            Assert.That(driver.ObstacleCourse.Definition.StableId,Is.EqualTo("moth-line"));
            Assert.That(driver.ObstacleCourse.Runtime.State,Is.EqualTo(CourseState.Countdown3)
                .Or.EqualTo(CourseState.Countdown2).Or.EqualTo(CourseState.Countdown1));
            var course=GameObject.Find("Obstacle course moth-line");Assert.That(course,Is.Not.Null);
            Assert.That(course.GetComponentsInChildren<Collider>().Length,Is.GreaterThanOrEqualTo(9));
            Assert.That(course.GetComponentsInChildren<LandingSurface>().Any(s=>s.name=="Course finish perch"),Is.True);
            var ribbon=driver.GetComponent<GameplayRibbon>();Assert.That(ribbon,Is.Not.Null);Assert.That(ribbon.Label,Is.Not.Null);
            var hud=driver.GetComponent<FlightHud>();Assert.That(hud,Is.Not.Null);
            ribbon.TickRibbon();Assert.That(ribbon.Visible,Is.False,"Countdown owns the view without overlapping HUD text.");
            hud.SetVisible(true);hud.TickInstruments();Assert.That(hud.Instruments.enabled,Is.False);
            yield return new WaitForSecondsRealtime(3.2f);yield return null;
            Assert.That(driver.ObstacleCourse.Runtime.State,Is.EqualTo(CourseState.Running));
            var route=course.GetComponentsInChildren<LineRenderer>(true)
                .First(line=>line.name=="Course flight path");
            Assert.That(route.enabled,Is.True);
            ribbon.TickRibbon();Assert.That(ribbon.Visible,Is.True,"The goal/catch ribbon remains when optional gauges are hidden.");
            Assert.That(ribbon.Label.text,Does.Contain("NEXT"),
                "Hidden instruments must still leave an actionable direction and distance.");
            hud.SetVisible(false);Assert.That(hud.Visible,Is.False);Assert.That(ribbon.Visible,Is.True);
            var menu=driver.GetComponent<FlightMenu>();menu.Open();
            typeof(ObstacleCourseDirector).GetMethod("UpdatePresentation",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(driver.ObstacleCourse,null);
            Assert.That(route.enabled,Is.False,"World guidance must not draw through a full-screen rest card.");
            menu.Close();
            Assert.That(driver.GetComponent<SkyRivals>(),Is.Null,"Ranked courses must not add random rival hazards");
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ChosenCourseId="moth-line";CharacterSelection.Chosen=null;
        }

        [UnityTest]
        public IEnumerator FreeFlightAddsBoundedPhysicalAerialRivals()
        {
            ActivitySelection.Chosen=FlightActivity.FreeFlight;CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;yield return null;
            Assert.That(Object.FindAnyObjectByType<BirdFlightDriver>().Expedition.Challenge.Title,
                Is.EqualTo("FREE FLIGHT"),"Session results must identify the activity that was selected.");
            var rivals=Object.FindAnyObjectByType<SkyRivals>();Assert.That(rivals,Is.Not.Null);Assert.That(rivals.Count,Is.EqualTo(3));
            foreach(var collider in GameObject.FindObjectsByType<SphereCollider>(FindObjectsSortMode.None)
                .Where(c=>c.name.StartsWith("Wind rival")))
            {
                Assert.That(collider.gameObject.layer,Is.EqualTo(WorldStreamer.CollisionLayer));
                Assert.That(collider.GetComponent<LandingSurface>().CanLand,Is.False,
                    "Moving rivals collide but must never become saved perches.");
            }
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        [UnityTest]
        public IEnumerator CourseResultsIgnoreUnavailableInputAndRequireReleaseAfterRecovery()
        {
            ActivitySelection.Chosen=FlightActivity.ObstacleCourse;
            ActivitySelection.ChosenCourseId="moth-line";CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var input=new ScriptedInput();
            input.Frame.HeadTracked=true;
            var gate=new FlightActionGate(input);
            var controller=new BirdFlightController(gate,driver.Controller.State.Position,
                environment:driver.GetComponent<UnityFlightEnvironment>());
            const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(BirdFlightDriver).GetField("input",fields).SetValue(driver,input);
            typeof(BirdFlightDriver).GetField("actionGate",fields).SetValue(driver,gate);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.Controller)).SetValue(driver,controller);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.UsesXR)).SetValue(driver,true);
            Assert.That(driver.Calibration.Capture(input.Frame),Is.True);
            var course=driver.ObstacleCourse;
            typeof(BirdFlightDriver).GetField("applicationFocused",fields).SetValue(driver,true);
            typeof(BirdFlightDriver).GetField("applicationPaused",fields).SetValue(driver,false);
            var clock=new CourseInputClock();
            typeof(ObstacleCourseDirector).GetField("clock",fields).SetValue(course,clock);
            course.ResetForFreshAttempt();course.Tick(.02f,input.Frame,.02f);
            clock.NowSeconds+=3.1;course.Tick(.02f,input.Frame,.02f);
            Assert.That(course.Runtime.State,Is.EqualTo(CourseState.Running));
            course.Runtime.Fail();course.Tick(.02f,input.Frame,.02f);
            Assert.That(course.Runtime.State,Is.EqualTo(CourseState.Results));

            for(int unavailable=0;unavailable<4;unavailable++)
            {
                input.Frame.Tuck=0;driver.Tick(.02f);
                if(unavailable==0)input.Frame.HeadTracked=false;
                if(unavailable==1)input.Frame.LeftWing.Tracked=false;
                if(unavailable==2)typeof(BirdFlightDriver).GetField("applicationFocused",fields).SetValue(driver,false);
                if(unavailable==3)typeof(BirdFlightDriver).GetField("applicationPaused",fields).SetValue(driver,true);
                driver.Tick(.02f);input.Frame.Tuck=1;driver.Tick(.02f);
                Assert.That(course.Runtime.State,Is.EqualTo(CourseState.Results),"Unavailable input case "+unavailable);
                input.Frame.HeadTracked=input.Frame.LeftWing.Tracked=true;
                typeof(BirdFlightDriver).GetField("applicationFocused",fields).SetValue(driver,true);
                typeof(BirdFlightDriver).GetField("applicationPaused",fields).SetValue(driver,false);
                driver.Tick(.02f);
                Assert.That(course.Runtime.State,Is.EqualTo(CourseState.Results),"Held input on recovery case "+unavailable);
            }
            input.Frame.Tuck=0;driver.Tick(.02f);input.Frame.Tuck=1;driver.Tick(.02f);
            Assert.That(course.Runtime.State,Is.EqualTo(CourseState.Rest),"A new deliberate confirmation must still work.");
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        [UnityTest]
        public IEnumerator SessionResultsRequireAReleasedConfirmationBeforeReturn()
        {
            var cameraObject=new GameObject("Results test eye");var camera=cameraObject.AddComponent<Camera>();
            var owner=new GameObject("Results test owner");int returns=0;
            var menu=owner.AddComponent<FlightMenu>();menu.Configure(camera,a=>{if(a==FlightMenuAction.ReturnAfterResults)returns++;},()=>"");
            menu.ShowResults("PRIMARY"+FlightMenu.ResultColumnSeparator+"DETAILS");
            var resultLabels=menu.Panel.GetComponentsInChildren<Text>(true).Select(t=>t.text).ToArray();
            Assert.That(resultLabels,Does.Contain("PRIMARY"));Assert.That(resultLabels,Does.Contain("DETAILS"));
            var comfortNote=menu.Panel.transform.Find("Comfort note").gameObject;
            Assert.That(comfortNote.activeSelf,Is.False,"Rest-menu guidance must not leak below final results.");
            var frame=FlightInputFrame.Neutral;frame.Tuck=1;
            menu.HandleInput(frame,true);Assert.That(returns,Is.Zero);
            frame.Tuck=0;menu.HandleInput(frame,true);frame.Tuck=1;menu.HandleInput(frame,true);
            Assert.That(returns,Is.EqualTo(1));
            menu.Open();Assert.That(comfortNote.activeSelf,Is.True);
            Object.Destroy(owner);Object.Destroy(cameraObject);yield return null;yield return null;
        }

        [UnityTest]
        public IEnumerator FinalizedSessionRemainsAnAuthoritativePauseWithoutItsCanvas()
        {
            var owner=new GameObject("Finalized session authority");owner.SetActive(false);
            var driver=owner.AddComponent<BirdFlightDriver>();
            typeof(BirdFlightDriver).GetField("finalizedSession",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(driver,new FlightSessionSummary());
            bool paused=(bool)typeof(BirdFlightDriver).GetProperty("AuthoritativePause",
                BindingFlags.Instance|BindingFlags.NonPublic).GetValue(driver);
            Assert.That(paused,Is.True,
                "Focus loss or recenter may hide results, but finalized gameplay can never resume untracked.");
            Object.Destroy(owner);yield return null;
        }

        [UnityTest]
        public IEnumerator FinalResultsSurviveXrTrackingDropoutAndBlockNormalMenuActions()
        {
            ActivitySelection.Chosen=FlightActivity.FreeFlight;CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var input=new ScriptedInput();input.Frame.HeadTracked=true;
            input.Frame.LeftWing.Tracked=input.Frame.RightWing.Tracked=true;
            var gate=new FlightActionGate(input);
            var controller=new BirdFlightController(gate,driver.Controller.State.Position,
                environment:driver.GetComponent<UnityFlightEnvironment>());
            controller.SetPaused(true);
            typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,input);
            typeof(BirdFlightDriver).GetField("actionGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,gate);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.Controller)).SetValue(driver,controller);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.UsesXR)).SetValue(driver,true);
            typeof(BirdFlightDriver).GetField("finalizedSession",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(driver,new FlightSessionSummary());
            typeof(BirdFlightDriver).GetField("finalizedResultsText",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(driver,"FINAL RESULTS");
            var menu=driver.GetComponent<FlightMenu>();menu.ShowResults("FINAL RESULTS");
            Assert.That(menu.ResultsVisible,Is.True);

            input.Frame.HeadTracked=false;
            driver.Tick(.02f);
            Assert.That(menu.ResultsVisible,Is.True,"Tracking loss cannot dismiss committed results.");

            input.Frame.HeadTracked=true;input.Frame.CharacterSelectPressed=true;
            driver.Tick(.02f);
            Assert.That(menu.ResultsVisible,Is.True,"LEFT MENU cannot reopen route actions after finalization.");
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("BirdFlight"));

            input.Frame.CharacterSelectPressed=false;input.Frame.Tuck=0;
            driver.Tick(.02f);
            input.Frame.Tuck=1;
            driver.Tick(.02f);
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("CharacterSelect"),
                "A released right-trigger confirmation remains the only exit from final results.");
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        [UnityTest]
        public IEnumerator LeftMenuDuringPlatformRecenterKeepsFlightAndCalibrationContext()
        {
            ActivitySelection.Chosen=FlightActivity.FreeFlight;CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var input=new ScriptedInput();input.Frame.HeadTracked=true;
            input.Frame.LeftWing.Tracked=input.Frame.RightWing.Tracked=true;
            var gate=new FlightActionGate(input);
            var controller=new BirdFlightController(gate,driver.Controller.State.Position,
                environment:driver.GetComponent<UnityFlightEnvironment>());
            typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,input);
            typeof(BirdFlightDriver).GetField("actionGate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,gate);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.Controller)).SetValue(driver,controller);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.UsesXR)).SetValue(driver,true);
            driver.NotifyTrackingOriginUpdated();

            input.Frame.CharacterSelectPressed=true;
            driver.Tick(.02f);

            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("BirdFlight"),
                "LEFT MENU during an automatic recenter must never abandon the active session.");
            Assert.That(Object.FindAnyObjectByType<CalibrationCoach>().Visible,Is.True);
            Assert.That(driver.Controller.IsPaused,Is.True);
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        [UnityTest]
        public IEnumerator HiddenInstrumentRibbonShowsBothUnmetSoaringConditions()
        {
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
            CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            driver.GetComponent<FlightHud>().SetVisible(false);
            var ribbon=driver.GetComponent<GameplayRibbon>();
            var challenge=driver.Expedition.Challenge;
            var checkpoint=challenge.Capture();checkpoint.Stage=1;
            checkpoint.SoaringGain=100;checkpoint.HighestAltitude=180;
            Assert.That(challenge.Restore(checkpoint),Is.True);
            RefreshRibbon(ribbon);
            Assert.That(ribbon.Visible,Is.True);
            Assert.That(challenge.Status,Is.EqualTo(ChallengeStatus.Active));
            Assert.That(ribbon.Label.text,Does.Contain("100/100 m climb"));
            Assert.That(ribbon.Label.text,Does.Contain("180/230 m peak altitude"),
                "A completed climb must not hide the altitude still required for progression.");

            checkpoint.SoaringGain=99.9f;checkpoint.HighestAltitude=229.9;
            Assert.That(challenge.Restore(checkpoint),Is.True);
            RefreshRibbon(ribbon);
            Assert.That(ribbon.Label.text,Does.Contain("99/100 m climb"));
            Assert.That(ribbon.Label.text,Does.Contain("229/230 m peak altitude"),
                "Rounding must not present unmet progression thresholds as complete.");
        }

        [UnityTest]
        public IEnumerator CompletionRibbonShowsEarnedMedalAndKnownGardenRestoration()
        {
            ActivitySelection.Chosen=FlightActivity.Training;ActivitySelection.ResumeRequested=false;
            CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var ribbon=driver.GetComponent<GameplayRibbon>();
            var training=driver.Expedition.Challenge;
            var target=training.Target(0);
            training.Step(new ChallengeObservation(new LogicalPosition(target.X,10,target.Z),Vector3.forward,3,0,1,false,0,0));
            training.Step(new ChallengeObservation(new LogicalPosition(target.X,61,target.Z),Vector3.forward,3,0,1,false,0,0));
            Assert.That(training.Status,Is.EqualTo(ChallengeStatus.Completed));
            RefreshRibbon(ribbon);
            Assert.That(ribbon.Label.text,Does.Contain("GOLD  1000"),
                "The efficiency reward promised by preflight must be visible with hidden instruments.");

            var route=new FlightChallenge(FlightActivity.RouteHome);
            var checkpoint=route.Capture();checkpoint.Stage=4;checkpoint.SeedCollected=true;
            checkpoint.Status=ChallengeStatus.Completed;checkpoint.Score=1000;
            Assert.That(route.Restore(checkpoint),Is.True);
            typeof(ExpeditionDirector).GetProperty(nameof(ExpeditionDirector.Challenge)).SetValue(driver.Expedition,route);
            driver.Expedition.Journey.GardenRestored=false;
            RefreshRibbon(ribbon);
            Assert.That(ribbon.Label.text,Does.Not.Contain("GARDEN RESTORED"));
            driver.Expedition.Journey.ApplyCompletion(route);
            RefreshRibbon(ribbon);
            Assert.That(ribbon.Label.text,Does.Contain("GARDEN RESTORED"));
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        [UnityTest]
        public IEnumerator PracticeResultsExplainWhyACompletedRunCannotRank()
        {
            ActivitySelection.Chosen=FlightActivity.ObstacleCourse;ActivitySelection.ChosenCourseId="moth-line";
            CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var course=driver.ObstacleCourse;
            var key=new CourseResultKey(course.Definition,driver.CharacterStableId,"beginner","assisted","assisted");
            var result=new CourseAttemptResult(key,true,20,course.Definition.Tasks.Count,course.Definition.Tasks.Count,
                false,CourseNonRankedReason.TrackingLost|CourseNonRankedReason.CalibrationLost
                    |CourseNonRankedReason.FrameTiming,CourseFailureReason.None);
            typeof(ObstacleCourseDirector).GetField("lastResult",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(course,result);
            string text=(string)typeof(ObstacleCourseDirector).GetMethod("ResultText",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(course,null);
            Assert.That(text,Does.Contain("PRACTICE RUN"));
            Assert.That(text,Does.Contain("tracking interrupted"));
            Assert.That(text,Does.Contain("frame delay"));
            Assert.That(text.Split(new[]{"tracking interrupted"},System.StringSplitOptions.None).Length,Is.EqualTo(2),
                "Related tracking and calibration flags should explain one player-facing interruption.");
            ActivitySelection.Chosen=FlightActivity.RouteHome;
        }

        private static void RefreshRibbon(GameplayRibbon ribbon)
        {
            typeof(GameplayRibbon).GetField("nextUpdate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ribbon,0f);
            ribbon.TickRibbon();
        }

        [UnityTest]
        public IEnumerator EveryCoursePreflightKeepsDescriptionAndRecordsInsideTheirOwnRows()
        {
            ActivitySelection.Chosen=FlightActivity.ObstacleCourse;CharacterSelection.Chosen=null;
            yield return SceneManager.LoadSceneAsync("CharacterSelect");yield return null;
            var selection=Object.FindAnyObjectByType<CharacterSelectController>();
            foreach(var course in CourseCatalog.All)
            {
                typeof(CharacterSelectController).GetMethod("ChooseCourse",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(selection,new object[]{course.StableId});
                Canvas.ForceUpdateCanvases();
                var labels=selection.PreflightPanel.GetComponentsInChildren<Text>();
                var description=labels.First(t=>t.name=="Route description");
                var records=labels.First(t=>t.name=="Course records");
                Assert.That(records.text,Does.Contain("TARGET "+course.ExpectedSeconds.ToString("F0")+" s"));
                Assert.That(records.text,Does.Contain("PERSONAL BEST"));
                foreach(var label in new[]{description,records}.Concat(labels.Where(t=>t.text.Contains("\n—"))))
                    Assert.That(label.preferredHeight,Is.LessThanOrEqualTo(label.rectTransform.rect.height+.5f),
                        course.StableId+" clips "+label.name+": "+label.text);
                float descriptionBottom=description.rectTransform.anchoredPosition.y-description.rectTransform.rect.height*.5f;
                float recordsTop=records.rectTransform.anchoredPosition.y+records.rectTransform.rect.height*.5f;
                Assert.That(recordsTop,Is.LessThan(descriptionBottom),"Metrics must have space independent of description wrapping.");
            }
            selection.ChooseActivity(FlightActivity.RouteHome);
            Assert.That(selection.PreflightPanel.GetComponentsInChildren<Text>().Any(t=>t.name=="Course records"),Is.False);
        }
    }
}
