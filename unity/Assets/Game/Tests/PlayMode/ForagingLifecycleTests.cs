using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Gameplay;
using VoarVR.World;
namespace VoarVR.Tests
{
    public class ForagingLifecycleTests
    {
        private sealed class IsolatedBestStorage : IForagingBestStorage
        {
            public int Best;
            public int Load()=>Best;
            public bool Save(int best){Best=Mathf.Max(Best,best);return true;}
        }
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        [UnityTest] public IEnumerator DisablingForagingFlushesBestBeforeDestruction()
        {
            var root=new GameObject("Foraging disable lifecycle");
            var food=root.AddComponent<SkyForaging>();var storage=new IsolatedBestStorage();
            food.ConfigurePersistence(null,storage);
            food.Score.Catch(1);food.Score.Catch(2);
            Assert.That(storage.Best,Is.Zero);
            food.enabled=false;
            Assert.That(storage.Best,Is.EqualTo(30));
            Assert.That(food.HasUnsavedBest,Is.False);
            Object.Destroy(root);yield return null;
        }
        [UnityTest] public IEnumerator CatchRequiresValidContinuousFlight()
        {
            var root=new GameObject("Foraging lifecycle");root.SetActive(false);
            var bird=root.AddComponent<BirdFlightDriver>();bird.enabled=false;
            var space=root.AddComponent<WorldSpace>();var c=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            Set(bird,"<Controller>k__BackingField",c);
            var food=root.AddComponent<SkyForaging>();food.Configure(bird,space,null);food.ConfigurePersistence(null,new IsolatedBestStorage());
            var homes=(LogicalPosition[])typeof(SkyForaging).GetField("homes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var ready=(float[])typeof(SkyForaging).GetField("available",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var placed=(bool[])typeof(SkyForaging).GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            System.Action prepare=()=>{for(int i=0;i<SkyForaging.Capacity;i++){homes[i]=space.ToLogical(c.State.Position+Vector3.forward*50);ready[i]=999;placed[i]=true;}homes[0]=space.ToLogical(c.State.Position-Vector3.forward*2);ready[0]=-1;Set(food,"previous",c.State.Position);Set(food,"previousValid",true);};
            foreach(var phase in new[]{FlightPhase.Paused,FlightPhase.Perched})
            {var state=c.State;state.Phase=phase;typeof(BirdFlightController).GetProperty("State").SetValue(c,state);prepare();food.Tick(.02f);Assert.That(food.Score.Caught,Is.Zero);}
            var airborne=c.State;airborne.Phase=FlightPhase.Gliding;typeof(BirdFlightController).GetProperty("State").SetValue(c,airborne);
            prepare();food.InvalidateSweep();food.Tick(.02f);Assert.That(food.Score.Caught,Is.Zero,"Recalibration cannot award a pickup");
            prepare();Set(food,"previous",c.State.Position+Vector3.right*1024);food.Tick(.02f);Assert.That(food.Score.Caught,Is.Zero,"Origin jumps cannot sweep through food");
            prepare();food.Tick(.02f);Assert.That(food.Score.Caught,Is.EqualTo(1));Assert.That(food.Score.Points,Is.EqualTo(10));
            Object.Destroy(root);yield return null;
        }

        [UnityTest] public IEnumerator CourseObjectiveBindingResetsOnlyOnANewAttempt()
        {
            var root=new GameObject("Course objective binding");
            var food=root.AddComponent<SkyForaging>();
            var homes=(LogicalPosition[])typeof(SkyForaging).GetField("homes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var ready=(float[])typeof(SkyForaging).GetField("available",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var placed=(bool[])typeof(SkyForaging).GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var definitions=(CollectibleDefinition[])typeof(SkyForaging).GetField("definitions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            homes[0]=new LogicalPosition(999,999,999);ready[0]=42;placed[0]=true;definitions[0]=CollectibleCatalog.SunMoth;
            homes[7]=new LogicalPosition(77,88,99);ready[7]=17;placed[7]=true;
            var targets=new[]{new LogicalPosition(1,10,20),new LogicalPosition(2,11,30)};

            Assert.That(food.ActivateCourseObjective(0,"moth-line.v2.sun-moths",CollectibleCatalog.SunMoth,targets),Is.True);
            Assert.That(food.CourseObjectiveSlotCount,Is.EqualTo(2));
            Assert.That(food.ObjectiveIdAt(0),Is.EqualTo("moth-line.v2.sun-moths"));
            Assert.That(food.HomeAt(0).X,Is.EqualTo(1));
            Assert.That(float.IsNegativeInfinity(food.AvailabilityAt(0)),Is.True);
            ready[0]=float.PositiveInfinity;
            Assert.That(food.ActivateCourseObjective(0,"moth-line.v2.sun-moths",CollectibleCatalog.SunMoth,targets),Is.True);
            Assert.That(float.IsPositiveInfinity(food.AvailabilityAt(0)),Is.True,
                "Repeating the same binding cannot resurrect a consumed moth.");
            Assert.That(food.ActivateCourseObjective(1,"moth-line.v2.sun-moths",CollectibleCatalog.SunMoth,targets),Is.True);
            Assert.That(float.IsNegativeInfinity(food.AvailabilityAt(0)),Is.True,
                "A new attempt restores the full authored quota.");
            Assert.That(food.HomeAt(7).X,Is.EqualTo(77));Assert.That(food.AvailabilityAt(7),Is.EqualTo(17));
            food.ClearCourseObjective();
            Assert.That(food.CourseObjectiveSlotCount,Is.Zero);Assert.That(food.ObjectiveIdAt(0),Is.Null);
            Assert.That(placed[0],Is.False);Assert.That(placed[7],Is.True);
            Object.Destroy(root);yield return null;
        }

        [UnityTest] public IEnumerator CourseObjectiveCatchIsTaggedAndCannotRespawn()
        {
            var root=new GameObject("Course objective catch");root.SetActive(false);
            var bird=root.AddComponent<BirdFlightDriver>();bird.enabled=false;
            var space=root.AddComponent<WorldSpace>();var controller=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            Set(bird,"<Controller>k__BackingField",controller);
            var food=root.AddComponent<SkyForaging>();food.Configure(bird,space,null);food.ConfigurePersistence(null,new IsolatedBestStorage());
            var homes=(LogicalPosition[])typeof(SkyForaging).GetField("homes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var ready=(float[])typeof(SkyForaging).GetField("available",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var placed=(bool[])typeof(SkyForaging).GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            for(int i=1;i<SkyForaging.Capacity;i++){homes[i]=space.ToLogical(controller.State.Position+Vector3.forward*100);ready[i]=float.PositiveInfinity;placed[i]=true;}
            var objectiveHome=space.ToLogical(controller.State.Position-Vector3.forward*2);
            Assert.That(food.ActivateCourseObjective(4,"moth-line.v2.sun-moths",CollectibleCatalog.SunMoth,
                new[]{objectiveHome}),Is.True);
            Set(food,"previous",controller.State.Position-Vector3.forward*4);Set(food,"previousValid",true);Set(food,"catchWasAllowed",true);

            food.Tick(.02f);

            Assert.That(food.Score.Caught,Is.EqualTo(1));
            Assert.That(food.LastCatch.ObjectiveId,Is.EqualTo("moth-line.v2.sun-moths"));
            Assert.That(float.IsPositiveInfinity(food.AvailabilityAt(0)),Is.True);
            food.Tick(.02f);Assert.That(food.Score.Caught,Is.EqualTo(1));
            Object.Destroy(root);yield return null;
        }

        [UnityTest] public IEnumerator ActiveCourseQuotaHidesAndDisablesAmbientMoths()
        {
            var root=new GameObject("Course objective ambient suppression");root.SetActive(false);
            var bird=root.AddComponent<BirdFlightDriver>();bird.enabled=false;
            var space=root.AddComponent<WorldSpace>();var controller=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            Set(bird,"<Controller>k__BackingField",controller);
            var food=root.AddComponent<SkyForaging>();food.Configure(bird,space,null);food.ConfigurePersistence(null,new IsolatedBestStorage());
            var homes=(LogicalPosition[])typeof(SkyForaging).GetField("homes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var ready=(float[])typeof(SkyForaging).GetField("available",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var placed=(bool[])typeof(SkyForaging).GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            for(int i=0;i<SkyForaging.Capacity;i++)
            {homes[i]=space.ToLogical(controller.State.Position+Vector3.forward*100);ready[i]=float.PositiveInfinity;placed[i]=true;}
            homes[11]=space.ToLogical(controller.State.Position-Vector3.forward*2);ready[11]=float.NegativeInfinity;
            Assert.That(food.ActivateCourseObjective(7,"moth-line.v4.sun-moths",CollectibleCatalog.SunMoth,
                new[]{space.ToLogical(controller.State.Position+Vector3.forward*40)}),Is.True);
            Set(food,"previous",controller.State.Position-Vector3.forward*4);Set(food,"previousValid",true);Set(food,"catchWasAllowed",true);

            food.Tick(.02f);

            Assert.That(food.Score.Caught,Is.Zero,
                "An ambient Crown Moth crossing the player must not appear or score during the Sun Moth quota.");
            var mesh=root.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            var source=(Vector3[])typeof(SkyForaging).GetField("source",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var vertices=mesh.vertices;
            for(int v=0;v<source.Length;v++)Assert.That(vertices[11*source.Length+v],Is.EqualTo(Vector3.zero));

            food.ClearCourseObjective();
            Set(food,"previous",controller.State.Position-Vector3.forward*4);Set(food,"previousValid",true);Set(food,"catchWasAllowed",true);
            food.Tick(.02f);
            Assert.That(food.Score.Caught,Is.EqualTo(1),"Ambient foraging resumes after the ranked quota ends.");
            Assert.That(food.LastCatch.ObjectiveId,Is.Null);
            Object.Destroy(root);yield return null;
        }

        [UnityTest] public IEnumerator CrownObjectiveCanBeInterceptedAtNormalCourseSpeed()
        {
            var root=new GameObject("Crown objective intercept");root.SetActive(false);
            var bird=root.AddComponent<BirdFlightDriver>();bird.enabled=false;
            var space=root.AddComponent<WorldSpace>();
            var controller=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            Set(bird,"<Controller>k__BackingField",controller);
            var food=root.AddComponent<SkyForaging>();food.Configure(bird,space,null);
            food.ConfigurePersistence(null,new IsolatedBestStorage());
            var homes=(LogicalPosition[])typeof(SkyForaging).GetField("homes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var ready=(float[])typeof(SkyForaging).GetField("available",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            var placed=(bool[])typeof(SkyForaging).GetField("placed",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(food);
            for(int i=1;i<SkyForaging.Capacity;i++)
            {homes[i]=space.ToLogical(controller.State.Position+Vector3.forward*100);ready[i]=float.PositiveInfinity;placed[i]=true;}
            var objectiveHome=space.ToLogical(controller.State.Position-Vector3.forward*.2f);
            Assert.That(food.ActivateCourseObjective(1,"trick-and-perch.v2.crown-moth",
                CollectibleCatalog.CrownMoth,new[]{objectiveHome}),Is.True);
            Set(food,"previous",controller.State.Position-Vector3.forward*.4f);
            Set(food,"previousValid",true);Set(food,"catchWasAllowed",true);

            food.Tick(.05f);

            Assert.That(food.Score.Caught,Is.EqualTo(1),
                "An 8 m/s capped-frame sweep through the authored home must catch the fleeing moth.");
            Assert.That(food.LastCatch.Type,Is.EqualTo(CollectibleType.CrownMoth));
            Object.Destroy(root);yield return null;
        }
    }
}
