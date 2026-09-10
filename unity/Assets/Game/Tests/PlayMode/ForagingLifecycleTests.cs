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
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        [UnityTest] public IEnumerator CatchRequiresValidContinuousFlight()
        {
            var root=new GameObject("Foraging lifecycle");root.SetActive(false);
            var bird=root.AddComponent<BirdFlightDriver>();bird.enabled=false;
            var space=root.AddComponent<WorldSpace>();var c=new BirdFlightController(new SyntheticFlightInput(),Vector3.up*100);
            Set(bird,"<Controller>k__BackingField",c);
            var food=root.AddComponent<SkyForaging>();food.Configure(bird,space,null);
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
            // Prevent this fixture from persisting a synthetic personal best.
            Set(food.Score,"<Points>k__BackingField",0);
            Object.Destroy(root);yield return null;
        }
    }
}
