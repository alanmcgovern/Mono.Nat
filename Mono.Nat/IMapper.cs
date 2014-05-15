using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Mono.Nat
{
    internal interface IMapper
    {
        event EventHandler<DeviceEventArgs> DeviceFound;

        UdpClient Client { get; set; }

        void Map(IPAddress gatewayAddress);
        void Handle(byte[] response);
    }
}
