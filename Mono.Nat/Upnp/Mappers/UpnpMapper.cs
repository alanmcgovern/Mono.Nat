using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mono.Nat.Upnp.Mappers
{
    internal class UpnpMapper : Upnp, IMapper
    {

        public event EventHandler<DeviceEventArgs> DeviceFound;

        public UdpClient Client { get; set; }

        public UpnpMapper()
        {
            //Bind to local port 1900 for ssdp responses
            Client = new UdpClient(1900);
        }

        public void Map(IPAddress gatewayAddress)
        {
            //Get the httpu request payload
            byte[] data = DiscoverDeviceMessage.EncodeUnicast(gatewayAddress);

            Client.Send(data, data.Length, new IPEndPoint(gatewayAddress, 1900));

            new Thread(Receive).Start(); 
        }

        public void Receive()
        {
            while (true)
            {
                IPEndPoint received = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 5351);
                if (Client.Available > 0)
                {
                    IPAddress localAddress = ((IPEndPoint)Client.Client.LocalEndPoint).Address;
                    byte[] data = Client.Receive(ref received);
                    Handle(localAddress, data, received);
                }
            }
        }

        public void Handle(IPAddress localAddres, byte[] response)
        {
            Handle(localAddres, response, null);
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            // No matter what, this method should never throw an exception. If something goes wrong
            // we should still be in a position to handle the next reply correctly.
            try
            {
                UpnpNatDevice d = base.Handle(localAddress, response, endpoint);               
                d.GetServicesList(DeviceSetupComplete);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unhandled exception when trying to decode a device's response Send me the following data: ");
                Trace.WriteLine("ErrorMessage:");
                Trace.WriteLine(ex.Message);
                Trace.WriteLine("Data string:");
                Trace.WriteLine(Encoding.UTF8.GetString(response));
            }
        }

        private void DeviceSetupComplete(INatDevice device)
        {
            OnDeviceFound(new DeviceEventArgs(device));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }
    }
}
