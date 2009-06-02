using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Mono.Nat
{
    public delegate void NatDeviceCallback(INatDevice device);

    internal interface ISearcher
    {
        event EventHandler<DeviceEventArgs> DeviceFound;
        event EventHandler<DeviceEventArgs> DeviceLost;

        void Search();
        IPEndPoint SearchEndpoint { get; }
        void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint);
        DateTime NextSearch { get; }
    }
}
