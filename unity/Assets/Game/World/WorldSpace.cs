using System;
using UnityEngine;

namespace VoarVR.World
{
    public readonly struct LogicalPosition
    {
        public readonly double X, Y, Z;
        public LogicalPosition(double x, double y, double z) { X=x; Y=y; Z=z; }
    }

    // Render-origin changes never touch HMD/controller tracking coordinates.
    public sealed class WorldSpace : MonoBehaviour
    {
        public int Seed = 7319;
        public double OffsetX { get; private set; }
        public double OffsetZ { get; private set; }
        public event Action<Vector3> Rebased;
        public LogicalPosition ToLogical(Vector3 local) => new LogicalPosition(OffsetX+local.x,local.y,OffsetZ+local.z);
        public Vector3 ToLocal(double x, double y, double z) => new Vector3((float)(x-OffsetX),(float)y,(float)(z-OffsetZ));
        public void ResetOrigin()
        {
            var delta = new Vector3((float)-OffsetX, 0f, (float)-OffsetZ);
            OffsetX = OffsetZ = 0d;
            Rebased?.Invoke(delta);
        }
        public void Shift(Vector3 delta)
        {
            OffsetX += delta.x; OffsetZ += delta.z;
            Rebased?.Invoke(delta);
        }
    }
}
