using System;
using TPLinkSmartDevices.Devices;

namespace TPLinkSmartDevices.Events
{
    public class DeviceFoundEventArgs : EventArgs
    {
        public TPLinkSmartDevice Device;

        public DeviceFoundEventArgs(TPLinkSmartDevice device)
        {
            Device = device;
        }
    }
}
