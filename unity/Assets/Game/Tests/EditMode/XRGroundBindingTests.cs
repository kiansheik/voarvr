using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using UnityEngine.XR.OpenXR.Features.Interactions;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class XRGroundBindingTests
    {
        [Test] public void TouchStickControlsResolveToCorrectHands()
        {
            InputSystem.RegisterLayout<OculusTouchControllerProfile.OculusTouchController>();
            var left=InputSystem.AddDevice<OculusTouchControllerProfile.OculusTouchController>();
            var right=InputSystem.AddDevice<OculusTouchControllerProfile.OculusTouchController>();
            InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
            var input=new XRFlightInput();
            try
            {
                var map=(InputActionMap)typeof(XRFlightInput).GetField("actions",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
                var hud=map.FindAction("HudToggle");var walk=map.FindAction("GroundMove");
                Assert.That(hud.controls,Has.Member(right.thumbstickClicked),"Bind by usage; Oculus names this control thumbstickClicked");
                Assert.That(hud.controls,Has.No.Member(left.thumbstickClicked));
                Assert.That(walk.controls,Has.Member(left.thumbstick));
                Assert.That(walk.controls,Has.No.Member(right.thumbstick));
            }
            finally{input.Dispose();InputSystem.RemoveDevice(left);InputSystem.RemoveDevice(right);}
        }
    }
}
