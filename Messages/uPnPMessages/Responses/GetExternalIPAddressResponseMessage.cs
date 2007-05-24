using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Nat
{
    internal class GetExternalIPAddressResponseMessage : MessageBase
    {
        public IPAddress ExternalIPAddress
        {
            get { return this.externalIPAddress; }
        }
        private IPAddress externalIPAddress;

        public GetExternalIPAddressResponseMessage(string ip)
            :base(null)
        {
            this.externalIPAddress = IPAddress.Parse(ip);
        }

        public override WebRequest Encode()
        {
            throw new NotImplementedException();
        }
    }
}
