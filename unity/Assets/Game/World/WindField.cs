using UnityEngine;
using VoarVR.Flight;
using VoarVR.Gameplay;

namespace VoarVR.World
{
    public enum WindMode { Assisted, Touring, Wild, StillAir }

    public sealed class WindField : MonoBehaviour, IWindField, IWindAssistance, IThermalGuidance
    {
        [SerializeField] private WindMode mode=WindMode.Assisted;
        private WorldSpace space;
        private BirdFlightProfile speciesProfile;
        private CourseLiftColumn[] courseColumns = new CourseLiftColumn[0];
        private struct CourseLiftColumn
        {
            public double X, Z;
            public float Radius, Lower, Maximum, Upper;
        }
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
        public void ConfigureCourse(CourseDefinition definition)
        {
            if (definition == null) { courseColumns = new CourseLiftColumn[0]; return; }
            int count = 0;
            foreach (var task in definition.Tasks) if (task.Kind == CourseTaskKind.AltitudeBand) count++;
            courseColumns = new CourseLiftColumn[count];
            int index = 0;
            foreach (var task in definition.Tasks)
            {
                if (task.Kind != CourseTaskKind.AltitudeBand) continue;
                courseColumns[index++] = new CourseLiftColumn
                {
                    X = task.PointA.X,
                    Z = task.PointA.Z,
                    Radius = (float)task.Radius * 1.45f,
                    Lower = (float)task.PointA.Y - 3f,
                    Maximum = (float)task.MaximumAltitude,
                    Upper = (float)task.MaximumAltitude + 12f
                };
            }
        }
        public void CycleMode() => mode=(WindMode)(((int)mode+1)%4);
        public void SetMode(WindMode value) => mode=value;
        public Vector3 Sample(Vector3 position,float simulationTime)
        {
            var logical=space!=null ? space.ToLogical(position) : new LogicalPosition(position.x,position.y,position.z);
            int seed=space!=null?space.Seed:7319;
            float width=UsabilityScale;
            return AtmosphereModel.SampleLogical(logical.X,logical.Y,logical.Z,simulationTime,seed,mode,width)
                +(VerticalWorldEnabled?FlightRegions.HighAir(logical.X,logical.Y,logical.Z,simulationTime,seed,mode,width):Vector3.zero)
                +CourseLift(courseColumns,logical,mode);
        }

        // Altitude-band course tasks provide deterministic, visible practice columns.
        // Definitions remain data-driven: adding another ladder requires no wind special-case.
        public static Vector3 CourseLift(CourseDefinition definition,LogicalPosition position,WindMode windMode)
        {
            if(definition==null||windMode==WindMode.StillAir)return Vector3.zero;
            float strongest=0f;
            foreach(var task in definition.Tasks)
            {
                if(task.Kind!=CourseTaskKind.AltitudeBand)continue;
                double dx=position.X-task.PointA.X,dz=position.Z-task.PointA.Z;
                float radius=(float)task.Radius*1.45f;
                float horizontal=Bell01((float)(System.Math.Sqrt(dx*dx+dz*dz)/radius));
                float lower=(float)task.PointA.Y-3f,upper=(float)task.MaximumAltitude+12f;
                float vertical=position.Y<lower-5||position.Y>upper?0f
                    :position.Y<lower?Mathf.InverseLerp(lower-5,lower,(float)position.Y)
                    :position.Y>(float)task.MaximumAltitude+4?Mathf.InverseLerp(upper,(float)task.MaximumAltitude+4,(float)position.Y):1f;
                strongest=Mathf.Max(strongest,horizontal*vertical);
            }
            float strength=windMode==WindMode.Assisted?6.5f:windMode==WindMode.Touring?4.5f:6f;
            return Vector3.up*(strongest*strength);
        }

        private static Vector3 CourseLift(CourseLiftColumn[] columns, LogicalPosition position, WindMode windMode)
        {
            if (columns == null || columns.Length == 0 || windMode == WindMode.StillAir) return Vector3.zero;
            float strongest = 0f;
            for (int i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                double dx = position.X - column.X, dz = position.Z - column.Z;
                float horizontal = Bell01((float)(System.Math.Sqrt(dx * dx + dz * dz) / column.Radius));
                float vertical = position.Y < column.Lower - 5 || position.Y > column.Upper ? 0f
                    : position.Y < column.Lower ? Mathf.InverseLerp(column.Lower - 5, column.Lower, (float)position.Y)
                    : position.Y > column.Maximum + 4 ? Mathf.InverseLerp(column.Upper, column.Maximum + 4, (float)position.Y) : 1f;
                strongest = Mathf.Max(strongest, horizontal * vertical);
            }
            float strength = windMode == WindMode.Assisted ? 6.5f : windMode == WindMode.Touring ? 4.5f : 6f;
            return Vector3.up * (strongest * strength);
        }

        private static float Bell01(float normalizedDistance)
        {
            if(normalizedDistance>=1f)return 0f;
            float value=1f-normalizedDistance*normalizedDistance;
            return value*value;
        }
    }
}
