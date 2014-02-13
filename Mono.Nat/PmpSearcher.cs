using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Mono.Nat.EventArgs;
using Mono.Nat.Pmp;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Mono.Nat
{
    internal class PmpSearcher : ISearcher
    {
		static readonly PmpSearcher instance = new PmpSearcher();
        public static List<UdpClient> Sockets;
        static Dictionary<UdpClient, List<IPEndPoint>> gatewayLists;
		

		public static PmpSearcher Instance
		{
			get { return instance; }
		}

        private int timeout;
        private DateTime nextSearch;
        public event EventHandler<DeviceEventArgs> DeviceFound;
        public event EventHandler<DeviceEventArgs> DeviceLost;

        static PmpSearcher()
        {
            CreateSocketsAndAddGateways();
        }

        PmpSearcher()
        {
            timeout = 250;
        }

		private static void CreateSocketsAndAddGateways()
		{
            Sockets = new List<UdpClient>();
            gatewayLists = new Dictionary<UdpClient,List<IPEndPoint>>();

            try
            {
                foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
                {
					if (n.OperationalStatus != OperationalStatus.Up && n.OperationalStatus != OperationalStatus.Unknown)
						continue;
                    IPInterfaceProperties properties = n.GetIPProperties();
                    List<IPEndPoint> gatewayList = (from gateway in properties.GatewayAddresses 
                                                    where gateway.Address.AddressFamily == AddressFamily.InterNetwork 
                                                    select new IPEndPoint(gateway.Address, PmpConstants.ServerPort)).ToList();

                    if (gatewayList.Count == 0)
                    {
                        /* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
                        gatewayList.AddRange(from gw2 in properties.DnsAddresses where gw2.AddressFamily == AddressFamily.InterNetwork 
                                             select new IPEndPoint(gw2, PmpConstants.ServerPort));
                        foreach (var bytes in from unicast in properties.UnicastAddresses 
                                              where unicast.Address.AddressFamily == AddressFamily.InterNetwork 
                                              select unicast.Address.GetAddressBytes ())
                        {
                            bytes[3] = 1;
                            gatewayList.Add(new IPEndPoint(new IPAddress(bytes), PmpConstants.ServerPort));
                        }
                    }

                    if (gatewayList.Count <= 0) continue;
                    foreach (UnicastIPAddressInformation address in properties.UnicastAddresses.Where(address => 
                        address.Address.AddressFamily == AddressFamily.InterNetwork))
                    {
                        UdpClient client;

                        try
                        {
                            client = new UdpClient(new IPEndPoint(address.Address, 0));
                        }
                        catch (SocketException)
                        {
                            continue; // Move on to the next address.
                        }

                        gatewayLists.Add(client, gatewayList);
                        Sockets.Add(client);
                    }
                }
            }
            catch (Exception)
            {
                // NAT-PMP does not use multicast, so there isn't really a good fallback.
            }
		}

        public void Search()
		{
			foreach (UdpClient s in Sockets)
			{
				try
				{
					Search(s);
				}
				catch
				{
					// Ignore any search errors
				}
			}
		}

		void Search (UdpClient client)
        {
            // Sort out the time for the next search first. The spec says the 
            // timeout should double after each attempt. Once it reaches 64 seconds
            // (and that attempt fails), assume no devices available
            nextSearch = DateTime.Now.AddMilliseconds(timeout);
            timeout *= 2;

            // We've tried 9 times as per spec, try searching again in 5 minutes
            if (timeout == 128 * 1000)
            {
                timeout = 250;
                nextSearch = DateTime.Now.AddMinutes(10);
                return;
            }

            // The nat-pmp search message. Must be sent to GatewayIP:53531
            byte[] buffer = { PmpConstants.Version, PmpConstants.OperationCode };
            foreach (IPEndPoint gatewayEndpoint in gatewayLists[client])
                client.Send(buffer, buffer.Length, gatewayEndpoint);
        }

        static bool IsSearchAddress(IPAddress address)
        {
            return gatewayLists.Values.SelectMany(gatewayList => 
                gatewayList).Any(gatewayEndpoint => 
                    gatewayEndpoint.Address.Equals(address));
        }

        public void Handle(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
        {
            if (!IsSearchAddress(endpoint.Address))
                return;
            if (response.Length != 12)
                return;
            if (response[0] != PmpConstants.Version)
                return;
            if (response[1] != PmpConstants.ServerNoop)
                return;
            int errorcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
            if (errorcode != 0)
                NatUtility.Log("Non zero error: {0}", errorcode);

            IPAddress publicIp = new IPAddress(new[] { response[8], response[9], response[10], response[11] });
            nextSearch = DateTime.Now.AddMinutes(5);
            timeout = 250;
            OnDeviceFound(new DeviceEventArgs(new PmpNatDevice(endpoint.Address, publicIp)));
        }

        public DateTime NextSearch
        {
            get { return nextSearch; }
        }
        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }
    }
}
