using System;
using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
namespace VoarVR.Editor
{
    public static class FlightGameMeasurements
    {
        public sealed class Controls:IFlightInput {public FlightInputFrame Frame=FlightInputFrame.Neutral;public string Mode=>"Flight Game measurement";public FlightInputFrame Sample(float dt)=>Frame;}
        public static string Measure()
        {
            var log=new StringBuilder();
            foreach(string species in new[]{"Duck","Dragon","Magpie"})
            {
                var p=Resources.Load<BirdCharacterDefinition>("Characters/"+species).BuildProfile();
                foreach(string action in new[]{"glide","turn","roll","loop"})
                {
                    var input=new Controls();var c=new BirdFlightController(input,Vector3.up*500,profile:p);
                    if(action=="roll" || action=="loop")c.SetControlMode(FlightControlMode.Acrobatic);
                    float maximumEnergyGain=0,energy=c.MechanicalEnergy,minUp=1,minZ=0,maxZ=0,minY=500,maxY=500;
                    for(int i=0;i<2400;i++)
                    {
                        input.Frame=FlightInputFrame.Neutral;
                        if(action=="turn" || action=="roll")input.Frame.Bank=.6f;
                        if(action=="loop")input.Frame.LeftWing.Orientation=input.Frame.RightWing.Orientation=Quaternion.Euler(-12,0,0);
                        c.Step(1f/120);minUp=Mathf.Min(minUp,(c.State.Rotation*Vector3.up).y);minZ=Mathf.Min(minZ,c.State.Position.z);maxZ=Mathf.Max(maxZ,c.State.Position.z);minY=Mathf.Min(minY,c.State.Position.y);maxY=Mathf.Max(maxY,c.State.Position.y);
                        maximumEnergyGain=Mathf.Max(maximumEnergyGain,c.MechanicalEnergy-energy);energy=c.MechanicalEnergy;
                    }
                    log.AppendLine(species+" "+action+" pos="+c.State.Position.ToString("F5")+" vel="+c.State.Velocity.ToString("F5")+" rot="+c.State.Rotation+" minUp="+minUp+" tricks="+c.Tricks.Count+" last="+c.Tricks.Last+" maxEnergyGain="+maximumEnergyGain+" yRange="+(maxY-minY)+" zRange="+(maxZ-minZ));
                }
            }
            Directory.CreateDirectory(FlightGameReview.Folder);File.WriteAllText(Path.Combine(FlightGameReview.Folder,"flight-measurements.txt"),log.ToString());return log.ToString();
        }
    }
}
