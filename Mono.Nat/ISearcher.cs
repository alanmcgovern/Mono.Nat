using System;
using System.Net;
using Mono.Nat.EventArgs;

namespace Mono.Nat
{
    public delegate void NatDeviceCallback(INatDevice device);

    internal interface ISearcher
    {
        event EventHandler<DeviceEventArgs> DeviceFound;
        event EventHandler<DeviceEventArgs> DeviceLost;

        void Search();
        void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint);
        DateTime NextSearch { get; }
    }
}
