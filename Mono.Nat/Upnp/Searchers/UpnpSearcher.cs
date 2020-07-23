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
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Mono.Nat.Upnp
{
	class UpnpSearcher : Searcher
	{
		public static ISearcher Instance { get; }

		static UpnpSearcher ()
		{
			var clients = new Dictionary<UdpClient, List<IPAddress>> ();
			var gateways = new List<IPAddress> { IPAddress.Parse ("239.255.255.250") };

			try {
				foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces ()) {
					foreach (UnicastIPAddressInformation address in n.GetIPProperties ().UnicastAddresses) {
						if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
							try {
								var client = new UdpClient (new IPEndPoint (address.Address, 0));
								clients.Add (client, gateways);
							} catch {
								continue; // Move on to the next address.
							}
						}
					}
				}
			} catch (Exception) {
				clients.Add (new UdpClient (0), gateways);
			}

			Instance = new UpnpSearcher (new SocketGroup (clients, 1900));
		}

		public override NatProtocol Protocol => NatProtocol.Upnp;

		Dictionary<Uri, DateTime> LastFetched { get; }
		SemaphoreSlim Locker { get; }

		UpnpSearcher (SocketGroup sockets)
			: base (sockets)
		{
			LastFetched = new Dictionary<Uri, DateTime> ();
			Locker = new SemaphoreSlim (1, 1);
		}

		protected override async Task SearchAsync (IPAddress gatewayAddress, TimeSpan? repeatInterval, CancellationToken token)
		{
			var buffer = gatewayAddress == null ? DiscoverDeviceMessage.EncodeSSDP () : DiscoverDeviceMessage.EncodeUnicast (gatewayAddress);

			do {
				await Clients.SendAsync (buffer, gatewayAddress, token);
				if (!repeatInterval.HasValue)
					break;
				await Task.Delay (repeatInterval.Value, token);
			} while (true);
		}

		protected override async Task HandleMessageReceived (IPAddress localAddress, UdpReceiveResult result, CancellationToken token)
		{
			// Convert it to a string for easy parsing
			string dataString = null;
			var response = result.Buffer;

			// No matter what, this method should never throw an exception. If something goes wrong
			// we should still be in a position to handle the next reply correctly.
			try {
				dataString = Encoding.UTF8.GetString (response);

				NatUtility.Log ("uPnP Search Response: {0}", dataString);

				/* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection.
				 Any other device type is no good to us for this purpose. See the IGP overview paper
				 page 5 for an overview of device types and their hierarchy.
				 http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

				/* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
				 version it is and apply the correct URN. */

				/* Some routers don't correctly implement the version ID on the URN, so we only search for the type
				 prefix. */

				string log = "uPnP Search Response: Router advertised a '{0}' service";
				StringComparison c = StringComparison.OrdinalIgnoreCase;
				if (dataString.IndexOf ("urn:schemas-upnp-org:service:WANIPConnection:", c) != -1) {
					NatUtility.Log (log, "urn:schemas-upnp-org:service:WANIPConnection:1");
				} else if (dataString.IndexOf ("urn:schemas-upnp-org:service:WANPPPConnection:", c) != -1) {
					NatUtility.Log (log, "urn:schemas-upnp-org:service:WANPPPConnection:");
				} else
					return;

				var location = dataString.Split (new [] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
					.Select (t => t.Trim ())
					.FirstOrDefault (t => t.StartsWith ("LOCATION", StringComparison.OrdinalIgnoreCase));

				if (location == null)
					return;

				var deviceLocation = location.Split (new [] { ':' }, 2).Skip (1).FirstOrDefault ();
				var deviceServiceUri = new Uri (deviceLocation);

				using (await Locker.DisposableWaitAsync (token)) {
					// If we send 3 requests at a time, ensure we only fetch the services list once
					// even if three responses are received
					if (LastFetched.TryGetValue (deviceServiceUri, out DateTime last))
						if ((DateTime.Now - last) < TimeSpan.FromSeconds (20))
							return;

					LastFetched [deviceServiceUri] = DateTime.Now;
				}

				// Once we've parsed the information we need, we tell the device to retrieve it's service list
				// Once we successfully receive the service list, the callback provided will be invoked.
				NatUtility.Log ("Fetching service list: {0}", deviceServiceUri);
				var d = await GetServicesList (localAddress, deviceServiceUri, token).ConfigureAwait (false);
				if (d != null)
					RaiseDeviceFound (d);
			} catch (Exception ex) {
				Trace.WriteLine ("Unhandled exception when trying to decode a device's response Send me the following data: ");
				Trace.WriteLine ("ErrorMessage:");
				Trace.WriteLine (ex.Message);
				Trace.WriteLine ("Data string:");
				Trace.WriteLine (dataString);
			}
		}

		async Task<UpnpNatDevice> GetServicesList (IPAddress localAddress, Uri deviceServiceUri, CancellationToken token)
		{
			// Create a HTTPWebRequest to download the list of services the device offers
			var request = new GetServicesMessage (deviceServiceUri).Encode (out byte[] body);
			if (body.Length > 0)
				NatUtility.Log ("Error: Services Message contained a body");
			using (token.Register (() => request.Abort ()))
			using (var response = (HttpWebResponse) await request.GetResponseAsync ().ConfigureAwait (false))
				return await ServicesReceived (localAddress, deviceServiceUri, response).ConfigureAwait (false);
		}

		async Task<UpnpNatDevice> ServicesReceived (IPAddress localAddress, Uri deviceServiceUri, HttpWebResponse response)
		{
			Stream s = response.GetResponseStream ();

			if (response.StatusCode != HttpStatusCode.OK) {
				NatUtility.Log ("{0}: Couldn't get services list: {1}", response.ResponseUri, response.StatusCode);
				return null; // FIXME: This the best thing to do??
			}

			int abortCount = 0;
			StringBuilder servicesXml = new StringBuilder ();
			XmlDocument xmldoc = new XmlDocument ();
			byte [] buffer = ArrayPool<byte>.Shared.Rent (10240);
			try {
				while (true) {
					var bytesRead = await s.ReadAsync (buffer, 0, buffer.Length);
					servicesXml.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
					try {
						xmldoc.LoadXml (servicesXml.ToString ());
						break;
					} catch (XmlException) {
						// If we can't receive the entire XML within 5 seconds, then drop the connection
						// Unfortunately not all routers supply a valid ContentLength (mine doesn't)
						// so this hack is needed to keep testing our recieved data until it gets successfully
						// parsed by the xmldoc. Without this, the code will never pick up my router.
						if (abortCount++ > 5000) {
							return null;
						}
						NatUtility.Log ("{0}: Couldn't parse services list", response.ResponseUri);
						await Task.Delay (10);
					}
				}
			} finally {
				ArrayPool<byte>.Shared.Return (buffer);
			}

			NatUtility.Log ("{0}: Parsed services list", response.ResponseUri);
			XmlNamespaceManager ns = new XmlNamespaceManager (xmldoc.NameTable);
			ns.AddNamespace ("ns", "urn:schemas-upnp-org:device-1-0");
			XmlNodeList nodes = xmldoc.SelectNodes ("//*/ns:serviceList", ns);

			foreach (XmlNode node in nodes) {
				//Go through each service there
				foreach (XmlNode service in node.ChildNodes) {
					//If the service is a WANIPConnection, then we have what we want
					string serviceType = service ["serviceType"].InnerText;
					NatUtility.Log ("{0}: Found service: {1}", response.ResponseUri, serviceType);
					const StringComparison C = StringComparison.OrdinalIgnoreCase;
					// TODO: Add support for version 2 of UPnP.
					if (serviceType.Equals ("urn:schemas-upnp-org:service:WANPPPConnection:1", C) ||
						serviceType.Equals ("urn:schemas-upnp-org:service:WANIPConnection:1", C)) {
						var controlUrl = new Uri (service ["controlURL"].InnerText, UriKind.RelativeOrAbsolute);
						IPEndPoint deviceEndpoint = new IPEndPoint (IPAddress.Parse (response.ResponseUri.Host), response.ResponseUri.Port);
						NatUtility.Log ("{0}: Found upnp service at: {1}", response.ResponseUri, controlUrl.OriginalString);
						try {
							if (controlUrl.IsAbsoluteUri) {
								deviceEndpoint = new IPEndPoint (IPAddress.Parse (controlUrl.Host), controlUrl.Port);
								NatUtility.Log ("{0}: New control url: {1}", deviceEndpoint, controlUrl);
							} else {
								controlUrl = new Uri (deviceServiceUri, controlUrl.OriginalString);
							}
						} catch {
							controlUrl = new Uri (deviceServiceUri, controlUrl.OriginalString);
							NatUtility.Log ("{0}: Assuming control Uri is relative: {1}", deviceEndpoint, controlUrl);
						}
						NatUtility.Log ("{0}: Handshake Complete", deviceEndpoint);
						return new UpnpNatDevice (localAddress, deviceEndpoint, controlUrl, serviceType);
					}
				}
			}

			//If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
			//So we don't invoke the callback, so this device is never added to our lists
			return null;
		}
	}
}
