using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    public enum WindMode { Assisted, Touring, Wild, StillAir }

    public sealed class WindField : MonoBehaviour, IWindField, IWindAssistance, IThermalGuidance
    {
        [SerializeField] private WindMode mode=WindMode.Assisted;
        private WorldSpace space;
        private BirdFlightProfile speciesProfile;
        public float UsabilityScale=>speciesProfile!=null?FlightRegions.ThermalScale(speciesProfile,mode):1;
        public bool VerticalWorldEnabled {get;private set;}
        public bool CenteringEnabled=>mode==WindMode.Assisted;
        public void ConfigureSpecies(BirdFlightProfile profile){speciesProfile=profile;VerticalWorldEnabled=true;}
        public Vector3 LiftGradient(Vector3 p,float time)
        { const float d=6;return new Vector3(Sample(p+Vector3.right*d,time).y-Sample(p-Vector3.right*d,time).y,0,Sample(p+Vector3.forward*d,time).y-Sample(p-Vector3.forward*d,time).y)/(2*d); }
        public WindMode Mode => mode;
        public bool AutomaticFeathering => mode == WindMode.Assisted;
        public WorldSpace Space => space;
        public string ModeName => mode switch
        {
            WindMode.Assisted => "Assisted thermals",
            WindMode.Touring => "Touring breeze",
            WindMode.Wild => "Wild weather",
            _ => "Still air / hard"
        };
        public void Configure(WorldSpace worldSpace) => space=worldSpace;
        public void CycleMode() => mode=(WindMode)(((int)mode+1)%4);
        public void SetMode(WindMode value) => mode=value;
        public Vector3 Sample(Vector3 position,float simulationTime)
        {
            var logical=space!=null ? space.ToLogical(position) : new LogicalPosition(position.x,position.y,position.z);
            int seed=space!=null?space.Seed:7319;
            float width=UsabilityScale;
            return AtmosphereModel.SampleLogical(logical.X,logical.Y,logical.Z,simulationTime,seed,mode,width)
                +(VerticalWorldEnabled?FlightRegions.HighAir(logical.X,logical.Y,logical.Z,simulationTime,seed,mode,width):Vector3.zero);
        }
    }
}
