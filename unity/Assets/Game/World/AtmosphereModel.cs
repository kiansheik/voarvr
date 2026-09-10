using System;
using UnityEngine;

namespace VoarVR.World
{
    // Pure bounded atmosphere: integer cells/epochs identify features, never traversal order.
    public static class AtmosphereModel
    {
        public const double CellSize = 192.0;
        public const double EpochSeconds = 80.0;
        public const double Lifetime = 160.0;
        public readonly struct Plume
        {
            public readonly double X, Z, Birth;
            public readonly float Altitude, Radius, Strength;
            public Plume(double x,double z,double birth,float altitude,float radius,float strength)
            { X=x; Z=z; Birth=birth; Altitude=altitude; Radius=radius; Strength=strength; }
            public float Envelope(double time)
            {
                double age=(time-Birth)/Lifetime;
                return age<=0 || age>=1 ? 0f : (float)Math.Pow(Math.Sin(age*Math.PI),2);
            }
            public LogicalPosition Center(double time, double height)
            {
                double age=Math.Max(0,Math.Min(Lifetime,time-Birth));
                return new LogicalPosition(X+age*.24+(height-Altitude)*.32,height,Z+age*.10+(height-Altitude)*.13);
            }
        }
        public static uint Hash(int seed,long x,long z,long epoch)
        {
            unchecked
            {
                ulong h=(ulong)(uint)seed ^ (ulong)x*0x9E3779B185EBCA87UL ^ (ulong)z*0xC2B2AE3D27D4EB4FUL ^ (ulong)epoch*0x165667B19E3779F9UL;
                h^=h>>30; h*=0xBF58476D1CE4E5B9UL; h^=h>>27; h*=0x94D049BB133111EBUL; h^=h>>31;
                return (uint)h;
            }
        }
        public static Plume GetPlume(int seed,long cellX,long cellZ,long epoch,WindMode mode)
        {
            uint h=Hash(seed,cellX,cellZ,epoch);
            float a=(h&1023)/1023f,b=((h>>10)&1023)/1023f,c=((h>>20)&1023)/1023f;
            float strength=mode==WindMode.Assisted ? 9.4f : mode==WindMode.Touring ? 5.6f : 9f;
            if(mode==WindMode.Wild && h%4==0) strength*=-.8f;
            return new Plume((cellX+.2+a*.6)*CellSize,(cellZ+.2+b*.6)*CellSize,epoch*EpochSeconds,
                (mode==WindMode.Assisted ? 30+c*65 : 35+c*85),mode==WindMode.Assisted ? 48 : 29,strength);
        }
        private static float Bell(double distance,double radius)
        {
            double t=distance/radius;
            if(t>=1 || t<=-1) return 0;
            double a=1-t*t; return (float)(a*a);
        }
        public static Vector3 SampleLogical(double x,double y,double z,double time,int seed,WindMode mode)
        {
            if(mode==WindMode.StillAir) return Vector3.zero;
            float scale=mode==WindMode.Assisted ? .75f : mode==WindMode.Touring ? 1f : 1.7f;
            double phase=(seed%997)*.01;
            double angle=.35+Math.Sin(y*.012+phase)*.45+Math.Sin(x*.001+z*.0013+time*.006)*.18;
            float speed=(float)(2.2+.8*Math.Sin(y*.02+time*.009))*scale;
            var wind=new Vector3((float)Math.Cos(angle)*speed,0,(float)Math.Sin(angle)*speed);
            long cx=(long)Math.Floor(x/CellSize),cz=(long)Math.Floor(z/CellSize),epoch=(long)Math.Floor(time/EpochSeconds);
            for(long ix=cx-1;ix<=cx+1;ix++) for(long iz=cz-1;iz<=cz+1;iz++) for(long e=epoch-1;e<=epoch;e++)
            {
                var plume=GetPlume(seed,ix,iz,e,mode);
                var center=plume.Center(time,y);
                double dx=x-center.X,dz=z-center.Z;
                double radius=plume.Radius+Math.Max(-15,Math.Min(60,y-plume.Altitude))*.12;
                float radial=Bell(Math.Sqrt(dx*dx+dz*dz),radius);
                if(radial==0) continue;
                float envelope=plume.Envelope(time)*Bell(y-plume.Altitude,80);
                float lift=plume.Strength*radial*envelope*(.9f+.1f*(float)Math.Sin(time*.18+y*.05));
                wind.y+=lift;
                wind.x+=(float)(-dx*.018-dz*.025)*radial*envelope;
                wind.z+=(float)(-dz*.018+dx*.025)*radial*envelope;
            }
            // Broad convergence streets cross chunk/cell boundaries continuously. Their
            // altitude undulates; lift arrives in moving packets rather than a solid wall.
            double along=x*.92+z*.39,across=z*.92-x*.39;
            double lane=Math.Sin(across*.018+Math.Sin(along*.002+phase)*.7-time*.005);
            double height=65+28*Math.Sin(along*.003-time*.012+phase);
            float street=Bell(lane,.32)*Bell(y-height,24);
            float packet=(float)Math.Pow(.5+.5*Math.Sin(along*.022-time*.11+phase),2);
            wind.y+=street*packet*(mode==WindMode.Assisted ? 5.2f : 3f);
            // Height above terrain limits ridge lift to its actual neighborhood.
            float ground=WorldTerrain.Elevation(seed,x,z);
            if(y-ground<45 && y-ground>-5)
            {
                float slopeX=(WorldTerrain.Elevation(seed,x+4,z)-WorldTerrain.Elevation(seed,x-4,z))*.125f;
                float slopeZ=(WorldTerrain.Elevation(seed,x,z+4)-WorldTerrain.Elevation(seed,x,z-4))*.125f;
                float upslope=wind.x*slopeX+wind.z*slopeZ;
                wind.y+=Mathf.Clamp(upslope*2.5f,-1.5f,2.5f)*Bell(y-ground,45);
            }
            if(mode==WindMode.Wild)
                wind+=new Vector3((float)Math.Sin(z*.04+time*.3),.6f*(float)Math.Sin(x*.035-time*.24),(float)Math.Sin(y*.035-time*.21))*.65f;
            return wind;
        }
    }
}
