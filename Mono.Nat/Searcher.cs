//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2019 Alan McGovern
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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	abstract class Searcher : ISearcher
	{
		protected static readonly TimeSpan SearchPeriod = TimeSpan.FromSeconds (5);

		public event EventHandler<DeviceEventArgs> DeviceFound;
		public event EventHandler<DeviceEventArgs> DeviceLost;


		public bool Listening => ListeningTask != null;
		public abstract NatProtocol Protocol { get; }

		Dictionary<NatDevice, NatDevice> Devices { get; }
		Task ListeningTask { get; set; }

		protected CancellationTokenSource Cancellation;
		protected CancellationTokenSource OverallSearchCancellation;
		protected Task SearchTask { get; set; }
		protected SemaphoreSlim SocketSendLocker { get; }

		protected Searcher ()
		{
			Devices = new Dictionary<NatDevice, NatDevice> ();
			SocketSendLocker = new SemaphoreSlim (1, 1);
		}

		protected abstract Task HandleMessageReceived (IPAddress localAddress, UdpReceiveResult result);

		async Task ListenAsync (IEnumerable<UdpClient> sockets, CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				try {
					foreach (UdpClient client in sockets) {
						try {
							if (client.Available > 0) {
								var localAddress = ((IPEndPoint) client.Client.LocalEndPoint).Address;
								var data = await client.ReceiveAsync ();
								await HandleMessageReceived (localAddress, data);
							}
						} catch (Exception) {
							// Ignore any errors
						}
					}

					await Task.Delay (10, token).ConfigureAwait (false);
				} catch (OperationCanceledException) {
					break;
				}
			}
		}

		protected void BeginListening (IEnumerable<UdpClient> sockets)
		{
			// Begin listening, if we are not already listening.
			if (!Listening) {
				Cancellation?.Cancel ();
				Cancellation = new CancellationTokenSource ();
				ListeningTask = ListenAsync (sockets, Cancellation.Token);
			}
		}

		public abstract Task SearchAsync ();

		public abstract Task SearchAsync (IPAddress gatewayAddress);

		public void Stop ()
		{
			Cancellation?.Cancel ();
			ListeningTask?.Wait ();
			SearchTask?.Wait ();

			Cancellation = null;
			ListeningTask = null;
			SearchTask = null;
		}

		protected void RaiseDeviceFound (NatDevice device)
		{
			NatDevice actualDevice;
			lock (Devices) {
				if (Devices.TryGetValue (device, out actualDevice))
					actualDevice.LastSeen = DateTime.UtcNow;
				else
					Devices[device] = device;
			}
			// If we did not find the device in the dictionary, raise an event as it's the first time
			// we've encountered it!
			if (actualDevice == null)
				DeviceFound?.Invoke (this, new DeviceEventArgs (device));
		}

		protected void RaiseDeviceLost (NatDevice device)
		{
			NatDevice actualDevice;
			lock (Devices) {
				// If the device is not in the dictionary, bail out.
				if (!Devices.TryGetValue (device, out actualDevice))
					return;
				Devices.Remove (actualDevice);
			}

			DeviceLost?.Invoke (this, new DeviceEventArgs (actualDevice));
		}
	}
}
