using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Mono.Nat.Pmp
{
    internal abstract class Pmp
    {
        public static List<UdpClient> sockets;
        protected static Dictionary<UdpClient, List<IPEndPoint>> gatewayLists;

        internal static void CreateSocketsAndAddGateways()
        {
            sockets = new List<UdpClient>();
            gatewayLists = new Dictionary<UdpClient, List<IPEndPoint>>();

            try
            {
                foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (n.OperationalStatus != OperationalStatus.Up && n.OperationalStatus != OperationalStatus.Unknown)
                        continue;
                    IPInterfaceProperties properties = n.GetIPProperties();
                    List<IPEndPoint> gatewayList = new List<IPEndPoint>();

                    foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
                    {
                        if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            gatewayList.Add(new IPEndPoint(gateway.Address, PmpConstants.ServerPort));
                        }
                    }
                    if (gatewayList.Count == 0)
                    {
                        /* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
                        foreach (var gw2 in properties.DnsAddresses)
                        {
                            if (gw2.AddressFamily == AddressFamily.InterNetwork)
                            {
                                gatewayList.Add(new IPEndPoint(gw2, PmpConstants.ServerPort));
                            }
                        }
                        foreach (var unicast in properties.UnicastAddresses)
                        {
                            if (/*unicast.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred
							    && unicast.AddressPreferredLifetime != UInt32.MaxValue
							    && */unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                var bytes = unicast.Address.GetAddressBytes();
                                bytes[3] = 1;
                                gatewayList.Add(new IPEndPoint(new IPAddress(bytes), PmpConstants.ServerPort));
                            }
                        }
                    }

                    if (gatewayList.Count > 0)
                    {
                        foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
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
                                sockets.Add(client);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // NAT-PMP does not use multicast, so there isn't really a good fallback.
            }
        }
    }
}
