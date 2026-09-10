using UnityEngine;
namespace VoarVR.Gameplay
{
    // Session points are separate from expedition medals and never modify flight stats.
    public sealed class ForagingScore
    {
        public int Points { get; private set; }
        public int Caught { get; private set; }
        public int Combo { get; private set; }
        private float lastCatch=-100;
        public int Catch(float time)
        {
            Combo=time>=lastCatch && time-lastCatch<6?Mathf.Min(Combo+1,5):1;
            lastCatch=time;Caught++;int reward=10*Combo;Points+=reward;return reward;
        }
        public static bool Touches(Vector3 from,Vector3 to,Vector3 food,float radius)
        {
            var delta=to-from;float t=delta.sqrMagnitude>1e-6f?Mathf.Clamp01(Vector3.Dot(food-from,delta)/delta.sqrMagnitude):0;
            return (food-(from+delta*t)).sqrMagnitude<=radius*radius;
        }
    }
}
