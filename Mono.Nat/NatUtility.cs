//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//   Nicholas Terry <nick.i.terry@gmail.com>
//
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using Mono.Nat.Pmp;
using Mono.Nat.Upnp;
using System.Threading.Tasks;

namespace Mono.Nat
{
	public static class NatUtility
	{
		public static event EventHandler<DeviceEventArgs> DeviceFound;
		public static event EventHandler<DeviceEventArgs> DeviceLost;

		static readonly object Locker = new object ();

		public static TextWriter Logger { get; set; }

		public static bool IsSearching => PmpSearcher.Instance.Listening || UpnpSearcher.Instance.Listening;

		static NatUtility ()
		{
			foreach (var searcher in new ISearcher [] { UpnpSearcher.Instance, PmpSearcher.Instance }) {
				searcher.DeviceFound += (o, e) => DeviceFound?.Invoke (o, e);
				searcher.DeviceLost += (o, e) => DeviceLost?.Invoke (o, e);
			}
		}

		internal static void Log (string format, params object [] args)
		{
			TextWriter logger = Logger;
			if (logger != null)
				logger.WriteLine (format, args);
		}

		/// <summary>
		/// Sends a single (non-periodic) message to the specified IP address to see if it supports the
		/// specified port mapping protocol, and begin listening indefinitely for responses.
		/// </summary>
		/// <param name="gatewayAddress">The IP address</param>
		/// <param name="type"></param>
		public static void Search (IPAddress gatewayAddress, NatProtocol type)
		{
			lock (Locker) {
				if (type == NatProtocol.Pmp) {
					PmpSearcher.Instance.SearchAsync (gatewayAddress).FireAndForget ();
				} else if (type == NatProtocol.Upnp) {
					UpnpSearcher.Instance.SearchAsync (gatewayAddress).FireAndForget ();
				} else {
					throw new InvalidOperationException ("Unsuported type given");
				}
			}
		}

		/// <summary>
		/// Periodically send a multicast UDP message to scan for new devices, and begin listening indefinitely
		/// for responses.
		/// </summary>
		/// <param name="devices">The protocols which should be searched for. An empty array will result in all supported protocols being used.</param>
		public static void StartDiscovery (params NatProtocol [] devices)
		{
			lock (Locker) {
				if (devices.Length == 0 || devices.Contains (NatProtocol.Pmp))
					PmpSearcher.Instance.SearchAsync ().FireAndForget ();

				if (devices.Length == 0 || devices.Contains (NatProtocol.Upnp))
					UpnpSearcher.Instance.SearchAsync ().FireAndForget ();
			}
		}

		/// <summary>
		/// Stop listening for responses to the search messages, and cancel any pending searches.
		/// </summary>
		public static void StopDiscovery ()
		{
			lock (Locker) {
				PmpSearcher.Instance.Stop ();
				UpnpSearcher.Instance.Stop ();
			}
		}
	}
}
