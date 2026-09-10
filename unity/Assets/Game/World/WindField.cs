using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    public enum WindMode { Assisted, Touring, Wild, StillAir }

    public sealed class WindField : MonoBehaviour, IWindField, IWindAssistance
    {
        [SerializeField] private WindMode mode=WindMode.Assisted;
        private WorldSpace space;
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
            return AtmosphereModel.SampleLogical(logical.X,logical.Y,logical.Z,simulationTime,space!=null ? space.Seed : 7319,mode);
        }
    }
}
