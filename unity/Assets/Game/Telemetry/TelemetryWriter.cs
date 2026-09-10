using System;
using System.IO;
using System.Text;
using System.Threading;

namespace VoarVR.Telemetry
{
    public enum TelemetryEvent
    {
        SessionStart=1, SessionEnd, CalibrationAccepted, CalibrationRejected, Recenter,
        TrackingLost, TrackingRecovered, ViewChanged, WeatherChanged, StallEntry, StallRecovery,
        ThermalEntry, ThermalExit, Collision, LandingAttempt, LandingSuccess, Takeoff,
        StreamingStall, StreamingRecovered, CharacterReturn, Marker, Paused, Resumed, FlightReset, SupportLost, ControlModeChanged, TrickCompleted, ObjectiveStarted, ObjectiveProgress, ObjectiveCompleted, ObjectiveFailed, RegionChanged, RestStarted, RestEnded, CollectibleCaught, EffortSummary, StallProtectionLost, StallProtectionRecovered
    }

    // Single producer/main thread, single consumer/writer. Publishing the write index
    // happens AFTER copying the value struct. A full ring drops the NEW record; it cannot
    // overwrite a slot currently read by the worker. No locks, file IO or joins in Capture.
    public sealed class TelemetryWriter
    {
        private struct Record { public TelemetrySample Sample; public TelemetryEvent Event; public double Time, Value; }
        private readonly Record[] ring;
        private readonly string path, header;
        private readonly Thread worker;
        private int written, read, stopping, flushRequested, dropped;
        private string error;
        public int Dropped => Volatile.Read(ref dropped);
        public string Error => Volatile.Read(ref error);
        private int finished;
        public bool Finished => Volatile.Read(ref finished)!=0;

        public TelemetryWriter(string path, string header, int capacity=512)
        {
            if(capacity<2) throw new ArgumentOutOfRangeException(nameof(capacity));
            this.path=path; this.header=header; ring=new Record[capacity];
            worker=new Thread(WriteLoop) { IsBackground=true, Name="Voar telemetry writer" };
            worker.Start();
        }
        public bool Capture(TelemetrySample sample) => Enqueue(new Record { Sample=sample });
        public bool Event(TelemetryEvent kind,double time,double value=0) => Enqueue(new Record { Event=kind,Time=time,Value=value });
        private bool Enqueue(Record record)
        {
            if(Volatile.Read(ref stopping)!=0 || Error!=null) return false;
            int next=(written+1)%ring.Length;
            if(next==Volatile.Read(ref read)) { Interlocked.Increment(ref dropped); return false; }
            ring[written]=record;
            Volatile.Write(ref written,next);
            return true;
        }
        public void Flush() => Interlocked.Exchange(ref flushRequested,1);
        public void Stop() { Flush(); Volatile.Write(ref stopping,1); }
        private void WriteLoop()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536))
                using(var buffer=new BufferedStream(file,65536))
                using(var w=new BinaryWriter(buffer,Encoding.UTF8))
                {
                    w.Write(Encoding.ASCII.GetBytes("VOARTLM1")); w.Write(3);
                    var bytes=Encoding.UTF8.GetBytes(header); w.Write(bytes.Length); w.Write(bytes);
                    var clock=System.Diagnostics.Stopwatch.StartNew(); long flushed=0;
                    while(true)
                    {
                        int end=Volatile.Read(ref written);
                        while(read!=end)
                        {
                            var item=ring[read];
                            if(item.Event==0) { w.Write((byte)1); w.Write(TelemetrySample.CompactSize); item.Sample.WriteCompact(w); }
                            else { w.Write((byte)2); w.Write(20); w.Write((int)item.Event); w.Write(item.Time); w.Write(item.Value); }
                            Volatile.Write(ref read,(read+1)%ring.Length);
                        }
                        if(Interlocked.Exchange(ref flushRequested,0)!=0 || clock.ElapsedMilliseconds-flushed>=1000)
                        { w.Flush(); flushed=clock.ElapsedMilliseconds; }
                        if(Volatile.Read(ref stopping)!=0 && read==Volatile.Read(ref written)) break;
                        Thread.Sleep(5);
                    }
                    // Terminal record makes a clean close and total loss count distinguishable
                    // from a killed app or a partially pulled live session.
                    w.Write((byte)3); w.Write(4); w.Write(Dropped); w.Flush();
                }
            }
            catch(Exception ex) { Volatile.Write(ref error,ex.GetType().Name+": "+ex.Message); }
            finally { Volatile.Write(ref finished,1); }
        }
    }
}
