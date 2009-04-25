using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Mono.Nat.Upnp;
using System.Diagnostics;

namespace Mono.Nat
{
    internal class UpnpSearcher : ISearcher
    {
        private const int SearchPeriod = 5 * 60; // The time in seconds between each search

        public event EventHandler<DeviceEventArgs> DeviceFound;
        public event EventHandler<DeviceEventArgs> DeviceLost;

        private List<INatDevice> devices;
        private DateTime nextSearch;
        private IPEndPoint searchEndpoint;

        public UpnpSearcher()
        {
            devices = new List<INatDevice>();
            searchEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        }
        public void Search(System.Net.Sockets.UdpClient client)
        {
            nextSearch = DateTime.Now.AddSeconds(SearchPeriod);
            byte[] data = DiscoverDeviceMessage.Encode();

            // UDP is unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
            for (int i = 0; i < 3; i++)
                client.Send(data, data.Length, searchEndpoint);
        }

        public IPEndPoint SearchEndpoint
        {
            get { return searchEndpoint; }
        }

        public void Handle(byte[] response, IPEndPoint endpoint)
        {
            // Convert it to a string for easy parsing
            string dataString = null;

            // No matter what, this method should never throw an exception. If something goes wrong
            // we should still be in a position to handle the next reply correctly.
            try
            {
                dataString = System.Text.UTF8Encoding.UTF8.GetString(response);

				if (NatUtility.Verbose)
					NatUtility.Log("UPnP Response: {0}", dataString);
                // If this device does not have a WANIPConnection service, then ignore it
                // Technically i should be checking for WANIPConnection:1 and InternetGatewayDevice:1
                // but there are some routers missing the '1'.
                if ((dataString.IndexOf("schemas-upnp-org:service:WANIPConnection:", StringComparison.InvariantCultureIgnoreCase) == -1) &&
                    (dataString.IndexOf("schemas-upnp-org:device:InternetGatewayDevice:", StringComparison.InvariantCultureIgnoreCase) == -1))
                    return;

				NatUtility.Log("UPnP Response: Detected upnp capable router");
                // We have an internet gateway device now
                UpnpNatDevice d = new UpnpNatDevice(dataString);

                if (this.devices.Contains(d))
                {
                    // We already have found this device, so we just refresh it to let people know it's
                    // Still alive. If a device doesn't respond to a search, we dump it.
                    this.devices[this.devices.IndexOf(d)].LastSeen = DateTime.Now;
                }
                else
                {
                    // Once we've parsed the information we need, we tell the device to retrieve it's service list
                    // Once we successfully receive the service list, the callback provided will be invoked.
					NatUtility.Log("Fetching servce list: {0}", d.HostEndPoint);
                    d.GetServicesList(new NatDeviceCallback(DeviceSetupComplete));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unhandled exception when trying to decode a device's response Send me the following data: ");
                Trace.WriteLine("ErrorMessage:");
                Trace.WriteLine(ex.Message);
                Trace.WriteLine("Data string:");
                Trace.WriteLine(dataString);
            }
        }

        public DateTime NextSearch
        {
            get { return nextSearch; }
        }


        /// <summary>
        /// This method is invoked when an InternetGatewayDevice has finished setting itself up and is now ready to be used
        /// to map ports.
        /// </summary>
        /// <param name="device">The device which has just finished setting up</param>
        private void DeviceSetupComplete(INatDevice device)
        {
            lock (this.devices)
            {
                // We don't want the same device in there twice
                if (devices.Contains(device))
                    return;

                devices.Add(device);
            }

            OnDeviceFound(new DeviceEventArgs(device));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }
    }
}
