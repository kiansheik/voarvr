using System;
using System.IO;
using System.Linq;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.Core;

namespace VoarVR.Editor
{
    public static class DuckReview
    {
        // Invoke only during Play mode. Freezes the real driver after an explicit input sequence.
        public static string Pose(SyntheticGesture gesture, float seconds)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
            var bird=UnityEngine.Object.FindAnyObjectByType<BirdFlightDriver>();
            bird.enabled=false;bird.Controller.Reset();bird.SetSyntheticGesture(gesture, true);
            for(int i=0;i<Mathf.RoundToInt(seconds*120);i++)bird.Tick(1f/120f);
            var rig=bird.GetComponent<BirdRigDriver>();
            return JsonUtility.ToJson(new Snapshot {
                gesture=gesture.ToString(),position=bird.transform.position,velocity=bird.Controller.State.Velocity,
                rotation=bird.transform.eulerAngles,leftHand=rig.leftHand.position,rightHand=rig.rightHand.position,
                leftElbow=rig.leftForearm.position,rightElbow=rig.rightForearm.position,
                leftTarget=rig.LeftTarget,rightTarget=rig.RightTarget,leftReachError=rig.LeftReachError,rightReachError=rig.RightReachError,
                energy=bird.Controller.MechanicalEnergy,aoa=bird.Controller.AngleOfAttackDeg},true);
        }
        [Serializable] private class Snapshot
        {
            public string gesture;
            public Vector3 position,velocity,rotation,leftHand,rightHand,leftElbow,rightElbow,leftTarget,rightTarget;
            public float leftReachError,rightReachError,energy,aoa;
        }
        public static void Capture(string folder, string name, Vector3 offset, Vector3 lookOffset, bool firstPerson=false)
        {
            var bird=UnityEngine.Object.FindAnyObjectByType<BirdFlightDriver>();
            var camera=Camera.main;var follow=camera.GetComponent<FlightCamera>();
            bool wasEnabled=follow.enabled;follow.enabled=false;
            var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var rig=bird.GetComponent<BirdRigDriver>();
            var hidden=rig.firstPersonHidden ?? Array.Empty<Renderer>();
            var oldVisible=hidden.Select(r=>r.enabled).ToArray();
            rig.SetFirstPersonVisibility(firstPerson);
            var rt=new RenderTexture(1440,900,24);var previous=RenderTexture.active;
            var oldTarget=camera.targetTexture;var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try
            {
                camera.transform.position=bird.transform.position+bird.Heading*offset;
                camera.transform.LookAt(bird.transform.position+bird.Heading*lookOffset);
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();
                Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);
                rt.Release();UnityEngine.Object.DestroyImmediate(rt);
                for(int i=0;i<hidden.Length;i++)hidden[i].enabled=oldVisible[i];
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation);follow.enabled=wasEnabled;
            }
        }

        public static void CaptureCurrent(string folder,string name)
        {
            var camera=Camera.main;var rt=new RenderTexture(1440,900,24);var previous=RenderTexture.active;
            var oldTarget=camera.targetTexture;var pixels=new Texture2D(1440,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,1440,900),0,0);pixels.Apply();
                Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=oldTarget;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);
                rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        public static string CaptureRound(string round)
        {
            string folder="../artifacts/reviews/duck-flight-v1/"+round;
            Directory.CreateDirectory(folder);
            foreach(var gesture in new[]{SyntheticGesture.Glide,SyntheticGesture.Flap,SyntheticGesture.BankLeft,SyntheticGesture.BankRight,SyntheticGesture.Dive,SyntheticGesture.Flare})
            {
                var snapshot=Pose(gesture,gesture==SyntheticGesture.Flap ? .625f : .4f);
                File.WriteAllText(Path.Combine(folder,gesture+".json"),snapshot);
                Capture(folder,gesture+"-external",new Vector3(1.1f,.7f,1.35f),new Vector3(0,.05f,0));
                Capture(folder,gesture+"-left-wing",BirdTrackingCalibration.EyeAnchor,new Vector3(-.55f,.03f,.02f),true);
                Capture(folder,gesture+"-right-wing",BirdTrackingCalibration.EyeAnchor,new Vector3(.55f,.03f,.02f),true);
            }
            Pose(SyntheticGesture.Glide,.4f);
            Capture(folder,"forward-first-person",BirdTrackingCalibration.EyeAnchor,BirdTrackingCalibration.EyeAnchor+Vector3.forward*3,true);
            return folder;
        }
    }
}
