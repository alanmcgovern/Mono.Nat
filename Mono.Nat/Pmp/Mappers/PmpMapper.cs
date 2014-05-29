using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Mono.Nat.Pmp;

namespace Mono.Nat.Pmp.Mappers
{
    internal class PmpMapper : Pmp, IMapper
    {
        public event EventHandler<DeviceEventArgs> DeviceFound;

        static PmpMapper()
        {
            CreateSocketsAndAddGateways();
        }

        public void Map(IPAddress gatewayAddress)
        {
            sockets.ForEach(x => Map(x, gatewayAddress));
        }

        void Map(UdpClient client, IPAddress gatewayAddress)
        {
            // The nat-pmp search message. Must be sent to GatewayIP:53531
            byte[] buffer = new byte[] { PmpConstants.Version, PmpConstants.OperationCode };

            client.Send(buffer, buffer.Length, new IPEndPoint(gatewayAddress, PmpConstants.ServerPort));
        }

        public void Handle(IPAddress localAddres, byte[] response)
        {
            //if (!IsSearchAddress(endpoint.Address))
            //    return;
            if (response.Length != 12)
                return;
            if (response[0] != PmpConstants.Version)
                return;
            if (response[1] != PmpConstants.ServerNoop)
                return;
            int errorcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
            if (errorcode != 0)
                NatUtility.Log("Non zero error: {0}", errorcode);

            IPAddress publicIp = new IPAddress(new byte[] { response[8], response[9], response[10], response[11] });
            OnDeviceFound(new DeviceEventArgs(new PmpNatDevice(localAddres, publicIp)));
        }

        private void OnDeviceFound(DeviceEventArgs args)
        {
            if (DeviceFound != null)
                DeviceFound(this, args);
        }  
    }
}
