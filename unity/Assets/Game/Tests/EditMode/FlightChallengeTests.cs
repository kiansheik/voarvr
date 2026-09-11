using NUnit.Framework;
using UnityEngine;
using VoarVR.Gameplay;
using VoarVR.World;
namespace VoarVR.Tests
{
    public class FlightChallengeTests
    {
        static ChallengeObservation O(double x,double y,double z,float air=4,float stroke=0,bool landed=false,int collisions=0)=>new ChallengeObservation(new LogicalPosition(x,y,z),Vector3.forward*8,air,stroke,.02f,landed,collisions,0);
        [Test] public void FreeFlightNeverStartsOrAwardsProgress(){var c=new FlightChallenge(FlightActivity.FreeFlight);c.Step(O(100,150,140));Assert.That(c.Status,Is.EqualTo(ChallengeStatus.Available));Assert.That(c.Score,Is.Zero);}
        [Test] public void ExpeditionRequiresLiftQuietWingsArchAndRealLanding()
        {
            var c=new FlightChallenge(FlightActivity.SkywardExpedition);c.Step(O(100,50,140,0));Assert.That(c.Stage,Is.Zero);c.Step(O(100,50,140));Assert.That(c.Stage,Is.EqualTo(1));
            for(int y=51;y<240;y++)c.Step(O(100,y,140,4,2));Assert.That(c.Stage,Is.EqualTo(1),"Flapping alone cannot satisfy soaring");
            c.Step(O(100,80,140));for(int y=81;y<=240;y++)c.Step(O(100,y,140));Assert.That(c.Stage,Is.EqualTo(2));
            c.Step(O(420,287,540));c.Step(O(420,287,570));Assert.That(c.Stage,Is.EqualTo(3));
            c.Step(O(420,270,620,0));Assert.That(c.Status,Is.EqualTo(ChallengeStatus.Active));c.Step(O(420,270.3,620,0,0,true));Assert.That(c.Status,Is.EqualTo(ChallengeStatus.Completed));Assert.That(c.Score,Is.EqualTo(1000));Assert.That(c.Medal,Is.Zero,"Adventure completion is separate from Training efficiency medals");
            int score=c.Score;c.Step(O(0,0,0));Assert.That(c.Score,Is.EqualTo(score));
        }
        [Test] public void ProgressRoundtripRetainsBestWithoutChangingSpecies()
        {var p=new FlightProgress();p.Apply(FlightActivity.SkywardExpedition,650,100);p.Apply(FlightActivity.SkywardExpedition,300,0);var restored=JsonUtility.FromJson<FlightProgress>(JsonUtility.ToJson(p));Assert.That(restored.SkywardBest,Is.EqualTo(650));Assert.That(restored.RidgeUnlocked,Is.True);Assert.That(restored.TrickBest,Is.EqualTo(100));}
        [Test] public void FailureAndTrainingAreExplicit()
        {var c=new FlightChallenge(FlightActivity.Training);c.Step(O(100,30,140));for(int y=31;y<65;y++)c.Step(O(100,y,140));Assert.That(c.Status,Is.EqualTo(ChallengeStatus.Completed));var fail=new FlightChallenge(FlightActivity.SkywardExpedition);fail.Fail();fail.Step(O(100,100,140));Assert.That(fail.Status,Is.EqualTo(ChallengeStatus.Failed));}
        [Test] public void DestinationDoesNotDependOnRenderOrigin()
        {var c=new FlightChallenge(FlightActivity.SkywardExpedition);var target=c.Destination;var go=new GameObject();var s=go.AddComponent<WorldSpace>();s.Shift(new Vector3(1024,0,-1024));var local=s.ToLocal(target.X,target.Y,target.Z);var logical=s.ToLogical(local);Assert.That(logical.X,Is.EqualTo(target.X));Assert.That(logical.Z,Is.EqualTo(target.Z));Object.DestroyImmediate(go);}
    }
}
