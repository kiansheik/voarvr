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
            Assert.That(labels,Does.Contain("WITHOUT LOSING YOUR FLIGHT"));
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
            var rivals=Object.FindAnyObjectByType<SkyRivals>();Assert.That(rivals,Is.Not.Null);Assert.That(rivals.Count,Is.EqualTo(3));
            foreach(var collider in GameObject.FindObjectsByType<SphereCollider>(FindObjectsSortMode.None)
                .Where(c=>c.name.StartsWith("Wind rival")))
                Assert.That(collider.gameObject.layer,Is.EqualTo(WorldStreamer.CollisionLayer));
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
            var frame=FlightInputFrame.Neutral;frame.Tuck=1;
            menu.HandleInput(frame,true);Assert.That(returns,Is.Zero);
            frame.Tuck=0;menu.HandleInput(frame,true);frame.Tuck=1;menu.HandleInput(frame,true);
            Assert.That(returns,Is.EqualTo(1));
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
    }
}
