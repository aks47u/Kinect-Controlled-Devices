using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using WindowsInput;

namespace WindowsFormsKinectNXT
{
    public class KeyboardController : KinectControlledDevice
    {
    //    [DllImport("user32.dll")]
      //  static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs, Int32 cbSize);

        #region KinectControlledDevice Members

        public bool IsConnected()
        {
            return true;
        }

        public bool ConnectToDevice(byte comport)
        {
            return true;
        }

        public bool Disconnect()
        {
            return true;
        }

        public bool PerformKeyCodeAction(WindowsInput.VirtualKeyCode keycode)
        {
            InputSimulator.SimulateKeyPress(keycode);

            return true;
        }

        public string GestureFileName()
        {
            return "drivinggamegestures";
        }

        #endregion
    }
}
