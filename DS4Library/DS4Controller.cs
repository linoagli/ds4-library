using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS4Library
{
    public class DS4Controller
    {
        private const int MAX_CONTROLLER_COUNT = 4;

        private const int THUMBSTICK_AXIS_RANGE = 255 / 2;
        private const int THUMBSTICK_DEAD_ZONE_X = 5;
        private const int THUMBSTICK_DEAD_ZONE_Y = 5;

        private const int TRIGGER_FULL_PULL_VALUE = 255;
        private const int TRIGGER_CLICK_THRESHOLD = TRIGGER_FULL_PULL_VALUE / 2;

        public DS4Device Device { get; set; }
        public DS4State CurrentState { get; set; }
        public DS4State PreviousState { get; set; }
        public DS4StateExposed ExposedState { get; set; }

        private bool isLeftTriggerClicked = false;
        private bool isRightTriggerClicked = false;
        private byte prevBatteryState = 0;

        public event EventHandler<DeviceButtonStateChangedEventArgs> DeviceButtonStateChanged;
        public event EventHandler<DeviceAnalogStateChangedEventArgs> DeviceAnalogStateChanged;
        public event EventHandler<DeviceTouchPadMovedEventArgs> DeviceTouchPadMoved;
        public event EventHandler<DeviceGeneralStateChangedEventArgs> DeviceGeneralStateChanged;

        public DS4Controller(DS4Device device)
        {
            Device = device;

            CurrentState = new DS4State();
            PreviousState = new DS4State();
            ExposedState = new DS4StateExposed(CurrentState);

            Device.Removal -= DS4Devices.On_Removal;
            Device.Removal += OnControllerRemoval;
            Device.Removal += DS4Devices.On_Removal;

            Device.Report += OnControllerReport;
            Device.Touchpad.TouchesMoved += OnControllerTouchPadMoved;
        }

        public void SetLightBarColor(Color color)
        {
            Device.LightBarColor = new DS4Color(color);
        }

        public void Purge()
        {
            Device = null;
            CurrentState = null;
            PreviousState = null;
            ExposedState = null;
        }

        private void OnControllerRemoval(object sender, EventArgs e)
        {
            Purge();
        }

        private void OnControllerReport(object sender, EventArgs e)
        {
            if ((sender as DS4Device) != Device)
            {
                Purge();
                return;
            }

            Device.FlushHID();

            if (!string.IsNullOrEmpty(Device.error)) return;

            Device.getExposedState(ExposedState, CurrentState);
            Device.getPreviousState(PreviousState);

            ParseButtonStates(CurrentState, PreviousState);
            ParseAnalogStates(CurrentState);
            ParseDeviceState(CurrentState);
        }

        private void OnControllerTouchPadMoved(object sender, TouchpadEventArgs e)
        {
            int dX = e.touches[0].deltaX;
            int dY = e.touches[0].deltaY;

            // TODO: perform upper and lower bound checks here...

            OnDeviceTouchPadMoved(dX, dY);
        }

        private void ParseButtonStates(DS4State currentState, DS4State previousState)
        {
            if (currentState.PS != previousState.PS) OnDeviceButtonStateChanged(DS4Controls.Button.PS, currentState.PS);

            if (currentState.Share != previousState.Share) OnDeviceButtonStateChanged(DS4Controls.Button.Share, currentState.Share);

            if (currentState.Options != previousState.Options) OnDeviceButtonStateChanged(DS4Controls.Button.Options, currentState.Options);

            if (currentState.TouchButton != previousState.TouchButton) OnDeviceButtonStateChanged(DS4Controls.Button.TouchPad, currentState.TouchButton);

            if (currentState.DpadLeft != previousState.DpadLeft) OnDeviceButtonStateChanged(DS4Controls.Button.DpadLeft, currentState.DpadLeft);

            if (currentState.DpadUp != previousState.DpadUp) OnDeviceButtonStateChanged(DS4Controls.Button.DpadUp, currentState.DpadUp);

            if (currentState.DpadRight != previousState.DpadRight) OnDeviceButtonStateChanged(DS4Controls.Button.DpadRight, currentState.DpadRight);

            if (currentState.DpadDown != previousState.DpadDown) OnDeviceButtonStateChanged(DS4Controls.Button.DpadDown, currentState.DpadDown);

            if (currentState.Cross != previousState.Cross) OnDeviceButtonStateChanged(DS4Controls.Button.Cross, currentState.Cross);

            if (currentState.Circle != previousState.Circle) OnDeviceButtonStateChanged(DS4Controls.Button.Circle, currentState.Circle);

            if (currentState.Square != previousState.Square) OnDeviceButtonStateChanged(DS4Controls.Button.Square, currentState.Square);

            if (currentState.Triangle != previousState.Triangle) OnDeviceButtonStateChanged(DS4Controls.Button.Triangle, currentState.Triangle);

            if (currentState.L1 != previousState.L1) OnDeviceButtonStateChanged(DS4Controls.Button.L1, currentState.L1);

            if (currentState.L3 != previousState.L3) OnDeviceButtonStateChanged(DS4Controls.Button.L3, currentState.L3);

            if (currentState.R1 != previousState.R1) OnDeviceButtonStateChanged(DS4Controls.Button.R1, currentState.R1);

            if (currentState.R3 != previousState.R3) OnDeviceButtonStateChanged(DS4Controls.Button.R3, currentState.R3);
        }

        private void ParseAnalogStates(DS4State currentState)
        {
            int x;
            int y;

            // Pasring left thumbstick state
            x = currentState.LX - THUMBSTICK_AXIS_RANGE;
            y = currentState.LY - THUMBSTICK_AXIS_RANGE;

            if ((-THUMBSTICK_DEAD_ZONE_X <= x) && (x <= THUMBSTICK_DEAD_ZONE_X)) x = 0;

            if ((-THUMBSTICK_DEAD_ZONE_Y <= y) && (y <= THUMBSTICK_DEAD_ZONE_Y)) y = 0;

            if (x != 0 || y != 0) OnDeviceAnalogStateChanged(DS4Controls.Analog.LeftThumbStick, new int[] { x, y });

            // Parsing right thumbstick state
            x = currentState.RX - THUMBSTICK_AXIS_RANGE;
            y = currentState.RY - THUMBSTICK_AXIS_RANGE;

            if ((-THUMBSTICK_DEAD_ZONE_X <= x) && (x <= THUMBSTICK_DEAD_ZONE_X)) x = 0;

            if ((-THUMBSTICK_DEAD_ZONE_Y <= y) && (y <= THUMBSTICK_DEAD_ZONE_Y)) y = 0;

            if (x != 0 || y != 0) OnDeviceAnalogStateChanged(DS4Controls.Analog.RightThumbStick, new int[] { x, y });

            // Parsing left trigger state
            OnDeviceAnalogStateChanged(DS4Controls.Analog.LeftTrigger, new int[] { currentState.L2 });

            if (currentState.L2 > TRIGGER_CLICK_THRESHOLD)
            {
                if (!isLeftTriggerClicked)
                {
                    isLeftTriggerClicked = true;
                    OnDeviceButtonStateChanged(DS4Controls.Button.L2, true);
                }
            }
            else
            {
                if (isLeftTriggerClicked)
                {
                    isLeftTriggerClicked = false;
                    OnDeviceButtonStateChanged(DS4Controls.Button.L2, false);
                }
            }

            // Parsing right trigger state
            OnDeviceAnalogStateChanged(DS4Controls.Analog.RightTrigger, new int[] { currentState.R2 });

            if (currentState.R2 > TRIGGER_CLICK_THRESHOLD)
            {
                if (!isRightTriggerClicked)
                {
                    isRightTriggerClicked = true;
                    OnDeviceButtonStateChanged(DS4Controls.Button.R2, true);
                }
            }
            else
            {
                if (isRightTriggerClicked)
                {
                    isRightTriggerClicked = false;
                    OnDeviceButtonStateChanged(DS4Controls.Button.R2, false);
                }
            }
        }

        private void ParseDeviceState(DS4State currentState)
        {
            if (currentState.Battery != prevBatteryState)
            {
                OnDeviceGeneralStateChanged(currentState.Battery);
                prevBatteryState = currentState.Battery;
            }
        }

        private void OnDeviceButtonStateChanged(DS4Controls.Button button, bool isPressed)
        {
            DeviceButtonStateChangedEventArgs args = new DeviceButtonStateChangedEventArgs();
            args.Button = button;
            args.IsPressed = isPressed;

            DeviceButtonStateChanged?.Invoke(this, args);
        }

        private void OnDeviceAnalogStateChanged(DS4Controls.Analog analog, int[] value)
        {
            DeviceAnalogStateChangedEventArgs args = new DeviceAnalogStateChangedEventArgs();
            args.Analog = analog;
            args.Value = value;

            DeviceAnalogStateChanged?.Invoke(this, args);
        }

        private void OnDeviceTouchPadMoved(int dX, int dY)
        {
            DeviceTouchPadMovedEventArgs args = new DeviceTouchPadMovedEventArgs();
            args.DeltaX = dX;
            args.DeltaY = dY;

            DeviceTouchPadMoved?.Invoke(this, args);
        }

        private void OnDeviceGeneralStateChanged(byte battery)
        {
            DeviceGeneralStateChangedEventArgs args = new DeviceGeneralStateChangedEventArgs();
            args.Battery = battery;

            DeviceGeneralStateChanged?.Invoke(this, args);
        }

        public class DeviceButtonStateChangedEventArgs : EventArgs
        {
            public DS4Controls.Button Button { get; set; }
            public bool IsPressed { get; set; }
        }

        public class DeviceAnalogStateChangedEventArgs : EventArgs
        {
            public DS4Controls.Analog Analog { get; set; }
            public int[] Value { get; set; }
        }

        public class DeviceTouchPadMovedEventArgs : EventArgs
        {
            public int DeltaX { get; set; }
            public int DeltaY { get; set; }
        }

        public class DeviceGeneralStateChangedEventArgs : EventArgs
        {
            public int Battery { get; set; }
        }
    }
}
