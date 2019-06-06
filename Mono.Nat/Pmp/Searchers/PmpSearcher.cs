//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//   Nicholas Terry <nick.i.terry@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
// Copyright (C) 2014 Nicholas Terry
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat.Pmp
{
	class PmpSearcher : Searcher
	{
		public static PmpSearcher Instance { get; } = new PmpSearcher ();

		static List<UdpClient> sockets;
		static Dictionary<UdpClient, List<IPEndPoint>> gatewayLists;

		public override NatProtocol Protocol => NatProtocol.Pmp;

		CancellationTokenSource CurrentSearchCancellation;

		static PmpSearcher ()
		{
			sockets = new List<UdpClient> ();
			gatewayLists = new Dictionary<UdpClient, List<IPEndPoint>> ();

			try {
				foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces ()) {
					if (n.OperationalStatus != OperationalStatus.Up && n.OperationalStatus != OperationalStatus.Unknown)
						continue;
					IPInterfaceProperties properties = n.GetIPProperties ();
					List<IPEndPoint> gatewayList = new List<IPEndPoint> ();

					foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses) {
						if (gateway.Address.AddressFamily == AddressFamily.InterNetwork) {
							gatewayList.Add (new IPEndPoint (gateway.Address, PmpConstants.ServerPort));
						}
					}
					if (gatewayList.Count == 0) {
						/* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
						foreach (var gw2 in properties.DnsAddresses) {
							if (gw2.AddressFamily == AddressFamily.InterNetwork) {
								gatewayList.Add (new IPEndPoint (gw2, PmpConstants.ServerPort));
							}
						}
						foreach (var unicast in properties.UnicastAddresses) {
							if (/*unicast.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred
							    && unicast.AddressPreferredLifetime != UInt32.MaxValue
							    && */unicast.Address.AddressFamily == AddressFamily.InterNetwork) {
								var bytes = unicast.Address.GetAddressBytes ();
								bytes [3] = 1;
								gatewayList.Add (new IPEndPoint (new IPAddress (bytes), PmpConstants.ServerPort));
							}
						}
					}

					if (gatewayList.Count > 0) {
						foreach (UnicastIPAddressInformation address in properties.UnicastAddresses) {
							if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
								UdpClient client;

								try {
									client = new UdpClient (new IPEndPoint (address.Address, 0));
								} catch (SocketException) {
									continue; // Move on to the next address.
								}

								gatewayLists.Add (client, gatewayList);
								sockets.Add (client);
							}
						}
					}
				}
			} catch (Exception) {
				// NAT-PMP does not use multicast, so there isn't really a good fallback.
			}
		}

		public override async Task SearchAsync ()
		{
			// Cancel any existing continuous search operation.
			OverallSearchCancellation?.Cancel ();
			if (SearchTask != null)
				await SearchTask.CatchExceptions ();

			// Create a CancellationTokenSource for the search we're about to perform.
			BeginListening (sockets);
			OverallSearchCancellation = CancellationTokenSource.CreateLinkedTokenSource (Cancellation.Token);
			SearchTask = Search (null, SearchPeriod, OverallSearchCancellation.Token);
			await SearchTask;
		}

		public override async Task SearchAsync (IPAddress gatewayAddress)
		{
			BeginListening (sockets);
			await Search (gatewayAddress, null, Cancellation.Token).ConfigureAwait (false);
		}

		async Task Search (IPAddress gatewayAddress, TimeSpan? repeatInterval, CancellationToken overallSearchToken)
		{
			var delay = PmpConstants.RetryDelay;
			var buffer = new [] { PmpConstants.Version, PmpConstants.OperationCode };
			while (!overallSearchToken.IsCancellationRequested) {
				var currentSearch = CancellationTokenSource.CreateLinkedTokenSource (overallSearchToken);
				if (repeatInterval.HasValue) {
					var oldSearch = Interlocked.Exchange (ref CurrentSearchCancellation, currentSearch);
					oldSearch?.Cancel ();
				}

				for (int i = 0; i < 9; i++) {
					using (await SocketSendLocker.DisposableWaitAsync (overallSearchToken)) {
						foreach (var client in sockets) {
							try {
								if (gatewayAddress == null) {
									foreach (IPEndPoint gatewayEndpoint in gatewayLists [client])
										await client.SendAsync (buffer, buffer.Length, new IPEndPoint (gatewayEndpoint.Address, PmpConstants.ServerPort));
								} else {
									await client.SendAsync (buffer, buffer.Length, new IPEndPoint (gatewayAddress, PmpConstants.ServerPort));
								}
							} catch (Exception) {

							}
						}
					}

					try {
						await Task.Delay (delay, currentSearch.Token);
						delay = TimeSpan.FromTicks (delay.Ticks * 2);
					} catch (OperationCanceledException) {
						break;
					}
				}

				if (repeatInterval == null)
					break;

				await Task.Delay (repeatInterval.Value, overallSearchToken);
			}
		}

		protected override Task HandleMessageReceived (IPAddress localAddress, UdpReceiveResult result, CancellationToken token)
		{
			var response = result.Buffer;
			var endpoint = result.RemoteEndPoint;

			if (!IsSearchAddress (endpoint.Address))
				return Task.CompletedTask;
			if (response.Length != 12)
				return Task.CompletedTask;
			if (response [0] != PmpConstants.Version)
				return Task.CompletedTask;
			if (response [1] != PmpConstants.ServerNoop)
				return Task.CompletedTask;

			int errorcode = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (response, 2));
			if (errorcode != 0)
				NatUtility.Log ("Non zero error: {0}", errorcode);

			var publicIp = new IPAddress (new byte [] { response [8], response [9], response [10], response [11] });

			CurrentSearchCancellation?.Cancel ();

			RaiseDeviceFound (new PmpNatDevice (endpoint, publicIp));
			return Task.CompletedTask;
		}

		bool IsSearchAddress (IPAddress address)
		{
			foreach (List<IPEndPoint> gatewayList in gatewayLists.Values)
				foreach (IPEndPoint gatewayEndpoint in gatewayList)
					if (gatewayEndpoint.Address.Equals (address))
						return true;
			return false;
		}
	}
}
