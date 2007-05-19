using System;
using System.Collections.Generic;
using System.Text;
using Nat;
using System.Net;
using System.IO;

namespace Nat.UpnpMessages
{
    internal class GetExternalIPAddressMessage : MessageBase
    {

        #region Constructors
        public GetExternalIPAddressMessage(UpnpNatDevice device)
            :base(device)
        {
        }
        #endregion


        public override WebRequest Encode()
        {
            return CreateRequest("GetExternalIPAddress", string.Empty, "POST");
        }
    }
}
