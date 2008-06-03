//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace Mono.Nat.Pmp
{
	//TODO: listen on 224.0.0.1:5351 to receive address change messages
	internal sealed class PmpNatController : AbstractNatController
	{
		private const int SearchPeriod = 60 * 5 * 1000; // The time in seconds between each search
		
		private bool haveSearchResults;
		
		public PmpNatController ()
		{
		}
		
		public PmpNatController (IPAddress[] localAddresses)
			: base (localAddresses)
		{
		}

		public override string Name
		{
			get { return "NAT-PMP"; }
		}
		
		protected override void Init ()
		{
			base.Init ();
			
			TimerUtility.RegisterDelayedDiscovery (this, SearchPeriod);
		}
		
		protected override void ThreadInitialize (out IPEndPoint endPoint)
		{
			endPoint = new IPEndPoint (IPAddress.Any, PmpConstants.Port);
		}
		
		protected override void ProcessData (IPEndPoint endPoint, byte[] data)
		{
			if (data[0] != PmpConstants.Version)
				return;
			
			if (data.Length < 12)
				return;
			
			byte[] ba = new byte[4];
			ba[0] = data[8];
			ba[1] = data[9];
			ba[2] = data[10];
			ba[3] = data[11];

			IPAddress deviceAddress = new IPAddress (ba);
			
			PmpNatDevice device = new PmpNatDevice (endPoint.Address, deviceAddress);
			haveSearchResults = true;
			
			int index = devices.IndexOf (device);
			if (index < 0) {
				// We already have found this device, so we just refresh it to let people know it's still alive
				devices[index].LastSeen = DateTime.Now;
			} else {
				lock (devices)
					devices.Add (device);

				OnDeviceFound (new DeviceEventArgs (device));
			}
			
			if (!currentDevices.Contains (device))
				currentDevices.Add (device);
		}
		
		protected override void Search ()
		{
			byte[] buffer = new byte[] { PmpConstants.Version, PmpConstants.OperationCode };
			
			haveSearchResults = false;

			int addressIndex = 0;
			int addressCount = localAddresses.Length;
			foreach (IPAddress address in localAddresses) {
				addressIndex++;

				byte[] ab = address.GetAddressBytes ();
				ab[3] = (byte)1; //FIXME: we now assume that the gateway address is in the form xxx.xxx.xxx.1, we should get the real address
				
				IPAddress gateway = new IPAddress (ab);
				if (address.Equals (gateway))
					continue;
				
				IPEndPoint ep = new IPEndPoint (gateway, PmpConstants.Port);
				
				ThreadPool.QueueUserWorkItem (delegate (object state) {
                    UdpClient client = new UdpClient();
                    try
                    {
                        int attempt = 0;
                        int delay = PmpConstants.RetryDelay;

                        while (!haveSearchResults && attempt < PmpConstants.RetryAttempts)
                        {
                            client.Send(buffer, 2, ep);
                            Thread.Sleep(delay);

                            delay *= 2;
                            attempt++;
                        }

                        if ((int)state == addressCount)
                            DiscoveryComplete();
                    }
                    finally
                    {
                        client.Close();
                    }
				}, addressIndex);
			}
		}
	}
}