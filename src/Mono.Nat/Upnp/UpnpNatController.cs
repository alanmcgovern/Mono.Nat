//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
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
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Mono.Nat.Upnp
{
	public sealed class UpnpNatController : AbstractNatController
	{
		private const int SearchPeriod = 120 * 1000; // The time in seconds between each search
		
		private IPEndPoint searchEndPoint; // The local endpoint that search requests are sent to		
		
		public UpnpNatController ()
		{
		}
		
		public UpnpNatController (IPAddress[] localAddresses)
			: base (localAddresses)
		{
		}
		
		public override string Name
		{
			get { return "uPnP"; }
		}
		
		protected override void ThreadInitialize (out IPEndPoint endPoint)
		{
			endPoint = searchEndPoint;
		}
		
		protected override void ProcessData (IPEndPoint endPoint, byte[] data)
		{
			// Convert it to a string for easy parsing
			string dataString = null;

			// No matter what, this method should never throw an exception. If something goes wrong
			// we should still be in a position to handle the next reply correctly.
			try
			{
				dataString = System.Text.UTF8Encoding.UTF8.GetString(data);

				// If this device does not have a WANIPConnection service, then ignore it
				if ((dataString.IndexOf("schemas-upnp-org:service:WANIPConnection:1", StringComparison.InvariantCultureIgnoreCase) == -1) &&
				    (dataString.IndexOf("schemas-upnp-org:device:InternetGatewayDevice:1", StringComparison.InvariantCultureIgnoreCase) == -1))
					return;

				// We have an internet gateway device now
				UpnpNatDevice d = new UpnpNatDevice (this, dataString);

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
		
		protected override void Init ()
		{
			base.Init ();
			
			TimerUtility.RegisterDelayedDiscovery (this, SearchPeriod);
			
			ThreadPool.QueueUserWorkItem (delegate (object state) {
				Thread.Sleep (3000); //if we don't have a reply after 3 seconds, the discovery failed
				
				DiscoveryComplete ();
			});
			
			this.searchEndPoint = new IPEndPoint (IPAddress.Parse ("239.255.255.250"), 1900);
		}
		
		protected override void Search ()
		{
			byte[] data = DiscoverDeviceMessage.Encode();

			// UDP is a bit unreliable, so send 3 requests at a time (per Upnp spec, sec 1.1.2)
			for (int i = 0; i < 3; i++)
				this.udpClient.Send(data, data.Length, this.searchEndPoint);
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

				devices.Add (device);

				if (!currentDevices.Contains (device))
					currentDevices.Add (device);
			}
			
			OnDeviceFound (new DeviceEventArgs (device));
		}
	}
}