using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput;

namespace WindowsFormsKinectNXT
{
    /*
     * Implement this interface to allow Kinect control
     */
    interface KinectControlledDevice
    {
        //check if device is connected
        bool IsConnected();

        //connect to device, returns true if it connects
        bool ConnectToDevice(byte comport);

        //disconnect from the device
        bool Disconnect();

        //actions are triggered using a keycode 
        bool PerformKeyCodeAction(VirtualKeyCode keycode);

        //should be an Xml file containing the gestures
        String GestureFileName();
    }
}
