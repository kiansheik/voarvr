namespace VoarVR.Input
{
    // Deliberate hold, one edge until release. Any grip reserves the click for marking.
    public sealed class ControlModeGesture
    {
        private float held;
        private bool consumed;
        public bool Sample(bool click,bool grip,float dt)
        {
            if(!click){held=0;consumed=false;return false;}
            if(grip){consumed=true;return false;}
            if(consumed)return false;
            held+=dt;if(held<.7f)return false;
            consumed=true;return true;
        }
    }
}
