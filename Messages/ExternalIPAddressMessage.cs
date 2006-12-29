using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Nat
{
    public class ExternalIPAddressMessage : Nat.IMessage
    {
        public IPAddress ExternalIPAddress
        {
            get { return this.externalIPAddress; }
        }
        private IPAddress externalIPAddress;

        public ExternalIPAddressMessage(string ip)
        {
            this.externalIPAddress = IPAddress.Parse(ip);
        }

        public void Decode(string data)
        {
        }
    }
}
