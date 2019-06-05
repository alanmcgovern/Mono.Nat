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
using System.Threading;

namespace Mono.Nat.Test {
	class NatTest {
		public NatTest() {
			NatUtility.DeviceFound += DeviceFound;
			NatUtility.DeviceLost += DeviceLost;

			// If you know the gateway address, you can directly search for a device at that IP
			//NatUtility.Search (System.Net.IPAddress.Parse ("192.168.0.1"), NatProtocol.Pmp);
			//NatUtility.Search (System.Net.IPAddress.Parse ("192.168.0.1"), NatProtocol.Upnp);
			NatUtility.StartDiscovery ();

			Console.WriteLine("Discovery started");

			while (true) {
				Thread.Sleep(500000);
				NatUtility.StopDiscovery();
				NatUtility.StartDiscovery();
			}
		}

		public static void Main(string[] args) {
			new NatTest();
		}

		readonly SemaphoreSlim locker = new SemaphoreSlim (1, 1);

		private async void DeviceFound(object sender, DeviceEventArgs args) {

			using (await locker.DisposableWaitAsync ()) {
				INatDevice device = args.Device;

				// Only interact with one device at a time. Some devices support both
				// upnp and nat-pmp.

				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Device found: {0}", device.NatProtocol);
				Console.ResetColor();
				Console.WriteLine("Type: {0}", device.GetType().Name);

				Console.WriteLine("IP: {0}", await device.GetExternalIPAsync());

				Console.WriteLine("---");

				//return;

				/******************************************/
				/*         Advanced test suite.           */
				/******************************************/

				// Try to create a new port map:
				var mapping = new Mapping(Protocol.Tcp, 6001, 6001);
				await device.CreatePortMapAsync(mapping);
				Console.WriteLine("Create Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort,
				                  mapping.PrivatePort);

				// Try to retrieve confirmation on the port map we just created:
				try {
					Mapping m = await device.GetSpecificMappingAsync(Protocol.Tcp, 6001);
					Console.WriteLine("Specific Mapping: protocol={0}, public={1}, private={2}", m.Protocol, m.PublicPort,
					                  m.PrivatePort);
				} catch {
					Console.WriteLine("Couldn't get specific mapping");
				}

				// Try retrieving all port maps:
				try {
					var mappings = await device.GetAllMappingsAsync();
					if (mappings.Length == 0)
						Console.WriteLine ("No existing uPnP mappings found.");
					foreach (Mapping mp in mappings)
						Console.WriteLine("Existing Mappings: protocol={0}, public={1}, private={2}", mp.Protocol, mp.PublicPort, mp.PrivatePort);
				} catch {
					Console.WriteLine("Couldn't get all mappings");
				}

				// Try deleting the port we opened before:
				try {
					await device.DeletePortMapAsync(mapping);
					Console.WriteLine("Deleting Mapping: protocol={0}, public={1}, private={2}", mapping.Protocol, mapping.PublicPort, mapping.PrivatePort);
				} catch {
					Console.WriteLine("Couldn't delete specific mapping");
				}

				// Try retrieving all port maps:
				try {
					var mappings = await device.GetAllMappingsAsync();
					if (mappings.Length == 0)
						Console.WriteLine ("No existing uPnP mappings found.");
					foreach (Mapping mp in mappings)
						Console.WriteLine("Existing Mapping: protocol={0}, public={1}, private={2}", mp.Protocol, mp.PublicPort, mp.PrivatePort);
				} catch {
					Console.WriteLine("Couldn't get all mappings");

				}

				Console.WriteLine("External IP: {0}", await device.GetExternalIPAsync());
				Console.WriteLine("Done...");
			}
		}

		private void DeviceLost(object sender, DeviceEventArgs args) {
			INatDevice device = args.Device;

			Console.WriteLine("Device Lost");
			Console.WriteLine("Type: {0}", device.GetType().Name);
		}
	}
}