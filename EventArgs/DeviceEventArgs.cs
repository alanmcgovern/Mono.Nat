using System;
using System.Collections.Generic;
using System.Text;

namespace Nat
{
    public class DeviceEventArgs : EventArgs
    {
        public INatDevice Device
        {
            get { return this.device; }
        }
        private INatDevice device;


        public DeviceEventArgs(INatDevice device)
        {
            this.device = device;
        }
    }
}
