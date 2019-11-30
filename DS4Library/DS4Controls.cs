using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4Library
{
    public class DS4Controls
    {
        public enum Button
        {
            PS, Share, Options, TouchPad,
            DpadLeft, DpadUp, DpadRight, DpadDown,
            Cross, Circle, Square, Triangle,
            L1, L2, L3, R1, R2, R3
        }

        public enum Analog
        {
            LeftTrigger, RightTrigger,
            LeftThumbStick, RightThumbStick
        }

        public enum TouchPad
        {
            TouchLeft, TouchRight, TouchUpper, TouchMulti,
            SwipeLeft, SwipeUp, SwipeRight, SwipeDown,
        }
    }
}
