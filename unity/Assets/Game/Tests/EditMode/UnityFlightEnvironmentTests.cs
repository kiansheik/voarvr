using NUnit.Framework;
using UnityEngine;
using VoarVR.World;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class UnityFlightEnvironmentTests
    {
        private GameObject root, ground;
        private UnityFlightEnvironment environment;
        private sealed class TestInput : IFlightInput
        {
            public FlightInputFrame Frame = FlightInputFrame.Neutral;
            public string Mode => "Contact fixture";
            public FlightInputFrame Sample(float dt) => Frame;
        }

        [SetUp]
        public void Setup()
        {
            root = new GameObject("Contact test environment");
            environment = root.AddComponent<UnityFlightEnvironment>();
            ground = new GameObject("Contact test platform") { layer = UnityFlightEnvironment.CollisionLayer };
            ground.transform.SetParent(root.transform);
            ground.transform.position = new Vector3(20000, -1, 20000);
            ground.AddComponent<BoxCollider>().size = new Vector3(20, 2, 20);
            ground.AddComponent<LandingSurface>().SurfaceId = 123;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Teardown() => Object.DestroyImmediate(root);

        [Test]
        public void WalkingStopsAtNativeWallThenLeavesLedgeAndLandsBelow()
        {
            var wall=new GameObject("Walking wall") {layer=UnityFlightEnvironment.CollisionLayer};
            wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(20001,1,20000);
            wall.AddComponent<BoxCollider>().size=new Vector3(.2f,3,10);
            var input=new TestInput();var profile=BirdFlightProfile.Duck();profile.InitialSpeedMps=0;
            var c=new BirdFlightController(input,new Vector3(20000,.23f,20000),profile:profile,environment:environment);
            Physics.SyncTransforms();
            for(int i=0;i<120;i++)c.Step(1f/120);
            Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
            input.Frame.GroundMove=Vector2.right;
            for(int i=0;i<240;i++)c.Step(1f/120);
            Assert.That(c.State.Position.x,Is.LessThanOrEqualTo(20000.70f),"Walking must stop its body before the wall");
            Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
            wall.SetActive(false);ground.GetComponent<BoxCollider>().size=new Vector3(2,2,20);
            var lower=new GameObject("Lower landing") {layer=UnityFlightEnvironment.CollisionLayer};
            lower.transform.SetParent(root.transform);lower.transform.position=new Vector3(20000,-4,20000);
            lower.AddComponent<BoxCollider>().size=new Vector3(20,2,20);
            lower.AddComponent<LandingSurface>().SurfaceId=124;
            Physics.SyncTransforms();bool leftLedge=false,landedBelow=false;
            for(int i=0;i<1200;i++)
            {
                c.Step(1f/120);
                leftLedge|=c.State.Phase!=FlightPhase.Perched;
                if(leftLedge && c.State.Phase==FlightPhase.Perched && c.State.Position.y < -2){landedBelow=true;break;}
            }
            Assert.That(leftLedge,Is.True,"No invisible support beyond the ledge");
            Assert.That(landedBelow,Is.True,"The lower native surface should automatically catch the falling bird");
        }

        [Test]
        public void LoadedSurfaceAppearsAndDisappearsWithCollider()
        {
            var p = new Vector3(20000, 2, 20000);
            Assert.That(environment.TryFindLanding(p, 10, out var candidate), Is.True);
            Assert.That(candidate.Position.y, Is.EqualTo(0).Within(.001));
            Assert.That(candidate.SurfaceId, Is.EqualTo(123));
            ground.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(environment.TryFindLanding(p, 10, out _), Is.False);
            ground.SetActive(true);
            Physics.SyncTransforms();
            Assert.That(environment.TryFindLanding(p, 10, out _), Is.True);
        }

        [Test]
        public void RealSphereSweepAndInsideRecoveryKeepBodyAbovePlatform()
        {
            Assert.That(environment.Sweep(new Vector3(20000, 10, 20000), new Vector3(20000, -10, 20000), .55f, out var hit), Is.True);
            Assert.That(hit.Position.y, Is.GreaterThanOrEqualTo(.55f));
            Assert.That(hit.Landable, Is.True);
            Assert.That(environment.Sweep(new Vector3(20000, .1f, 20000), new Vector3(20000, -1, 20000), .55f, out hit), Is.True);
            Assert.That(hit.Position.y, Is.GreaterThanOrEqualTo(.55f));
            Assert.That(environment.IsSupported(hit.Position, .55f, 123), Is.True);
        }

        [Test]
        public void WallIsSolidButNotLandable()
        {
            Assert.That(environment.Sweep(new Vector3(19980, -1, 20000), new Vector3(20020, -1, 20000), .22f, out var hit), Is.True);
            Assert.That(hit.Position.x, Is.LessThan(19990));
            Assert.That(hit.Landable, Is.False);
            Assert.That(FlightContactSolver.CanLand(hit, Vector3.right * 2, true), Is.False);
        }

        [Test]
        public void ControllerLandsOnLoadedSurfaceAndFlapsAwayWithoutRecapture()
        {
            var input = new TestInput();
            var profile = BirdFlightProfile.Duck();
            profile.InitialSpeedMps = 2f;
            var controller = new BirdFlightController(input, new Vector3(20000, .3f, 20000), profile: profile, environment: environment);
            for (int i = 0; i < 120 && controller.State.Phase != FlightPhase.Perched; i++) controller.Step(1f / 120f);
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Perched));
            Assert.That(controller.State.Position.y, Is.GreaterThanOrEqualTo(profile.CollisionRadius));
            input.Frame.LeftWing.Velocity = input.Frame.RightWing.Velocity = Vector3.down * 3;
            controller.Step(1f / 120f);
            input.Frame = FlightInputFrame.Neutral;
            for (int i = 0; i < 12; i++) controller.Step(1f / 120f);
            Assert.That(controller.State.Phase, Is.Not.EqualTo(FlightPhase.Perched));
            Assert.That(controller.State.Position.y, Is.GreaterThan(profile.CollisionRadius + .2f));
        }

        [Test]
        public void LosingLoadedSupportReleasesPerchedController()
        {
            var input = new TestInput();
            var profile = BirdFlightProfile.Duck(); profile.InitialSpeedMps = 0f;
            var controller = new BirdFlightController(input, new Vector3(20000, .23f, 20000), profile: profile, environment: environment);
            for (int i = 0; i < 120 && controller.State.Phase != FlightPhase.Perched; i++) controller.Step(1f / 120f);
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Perched));
            ground.SetActive(false); Physics.SyncTransforms();
            controller.Step(1f / 60f);
            Assert.That(controller.State.Phase, Is.Not.EqualTo(FlightPhase.Perched));
            Assert.That(controller.State.Velocity.y, Is.LessThan(0f));
        }
    }
}
