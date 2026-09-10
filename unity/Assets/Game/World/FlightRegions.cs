using System;
using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    public enum AltitudeBiome { Lowland, CloudSea, SkyArchipelago, HighSky }
    public enum WeatherRegion { CalmValley, Convective, CloudBank, RainFront, HighBright }

    public static class FlightRegions
    {
        public const double IslandCell=768;
        public static AltitudeBiome Biome(double height)=>height<140?AltitudeBiome.Lowland:height<215?AltitudeBiome.CloudSea:height<480?AltitudeBiome.SkyArchipelago:AltitudeBiome.HighSky;
        public static LogicalPosition Island(int seed,long x,long z)
        {
            if(x==0 && z==0)return new LogicalPosition(420,270,620);
            uint h=AtmosphereModel.Hash(seed,x,z,902);
            return new LogicalPosition((x+.35+(h&255)/850d)*IslandCell,245+((h>>8)&127),(z+.35+((h>>16)&255)/850d)*IslandCell);
        }
        public static WeatherRegion Weather(double x,double y,double z)
        {
            if(y>480)return WeatherRegion.HighBright;
            if(y>130 && y<215)return WeatherRegion.CloudBank;
            if(x>650 && x<1000 && z>300 && z<1000)return WeatherRegion.RainFront;
            return y>30?WeatherRegion.Convective:WeatherRegion.CalmValley;
        }
        public static float ThermalScale(BirdFlightProfile p,WindMode mode)
        {
            // Comfortable bank radius v²/(g tan25°); larger flyers get wider usable air,
            // not additional upward force. Bounded so neighboring-cell sampling stays valid.
            float radius=p.InitialSpeedMps*p.InitialSpeedMps/(p.Gravity*Mathf.Tan(25*Mathf.Deg2Rad));
            float loading=p.MassKg*p.Gravity/p.WingAreaM2;
            return mode==WindMode.Assisted?Mathf.Clamp(1+radius/90+Mathf.Sqrt(loading)/35,1,1.8f):mode==WindMode.Touring?1.15f:1;
        }
        public static LogicalPosition DepartureThermal(double time)
            =>new LogicalPosition(100+Math.Sin(time*.007)*14,150,140+Math.Cos(time*.007)*14);
        private static float Bell(double d,double r) {double a=d/r;if(Math.Abs(a)>=1)return 0;return (float)((1-a*a)*(1-a*a));}
        public static Vector3 HighAir(double x,double y,double z,double time,int seed,WindMode mode,float width)
        {
            if(mode==WindMode.StillAir)return Vector3.zero;
            var departure=DepartureThermal(time);
            float lift=Column(x,y,z,departure.X,departure.Z,165,185,95*width,time);
            long cx=(long)Math.Floor(x/IslandCell),cz=(long)Math.Floor(z/IslandCell);
            for(long ix=cx-1;ix<=cx+1;ix++)for(long iz=cz-1;iz<=cz+1;iz++)
            {
                var island=Island(seed,ix,iz);
                lift+=Column(x,y,z,island.X-90+Math.Sin(time*.004)*20,island.Z-90,island.Y-40,180,100*width,time+ix*13+iz*17);
            }
            var wind=Vector3.up*lift*(mode==WindMode.Assisted?9:mode==WindMode.Touring?6:7);
            if(Weather(x,y,z)==WeatherRegion.RainFront)
                wind+=new Vector3((float)Math.Sin(time*.7+z*.03)*2,-2+(float)Math.Sin(time*.35+x*.02)*2,1.5f);
            return wind;
        }
        private static float Column(double x,double y,double z,double cx,double cz,double mid,double height,double width,double time)
        {
            // Persistent terrain-fed convection makes the expedition route discoverable;
            // the existing finite drifting AtmosphereModel plumes still come and go.
            float envelope=.85f+.15f*(float)Math.Sin(time*.012);
            double dx=x-cx-(y-mid)*.09,dz=z-cz-(y-mid)*.04;
            return Bell(Math.Sqrt(dx*dx+dz*dz),width)*Bell(y-mid,height)*envelope;
        }
    }

    public interface IThermalGuidance
    {
        bool CenteringEnabled { get; }
        Vector3 LiftGradient(Vector3 position,float time);
    }
    public static class ThermalCentering
    {
        public const float MaximumBankContribution=.10f;
        public static float Bias(Vector3 gradient,Quaternion rotation,float playerBank,float verticalAir,float tuck)
        {
            if(verticalAir<1.5f || tuck>.15f || Mathf.Abs(playerBank)<.12f)return 0;
            var right=Quaternion.Euler(0,rotation.eulerAngles.y,0)*Vector3.right;
            float requested=Mathf.Clamp(Vector3.Dot(gradient,right)*2,-1,1)*MaximumBankContribution;
            // Never reverse the player's intended turn; at most one quarter its authority.
            return Mathf.Clamp(requested,-Mathf.Abs(playerBank)*.25f,Mathf.Abs(playerBank)*.25f);
        }
    }
}
