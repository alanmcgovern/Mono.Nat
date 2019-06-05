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
		public static UpnpSearcher Instance { get; } = new UpnpSearcher ();
		static readonly IPEndPoint SearchEndpoint = new IPEndPoint (IPAddress.Parse ("239.255.255.250"), 1900);

		static readonly List<UdpClient> sockets = CreateSockets ();

		public override NatProtocol Protocol => NatProtocol.Upnp;

		Dictionary<Uri, DateTime> LastFetched { get; }
		SemaphoreSlim Locker { get; }

		UpnpSearcher ()
		{
			LastFetched = new Dictionary<Uri, DateTime> ();
			Locker = new SemaphoreSlim (1, 1);
		}

		static List<UdpClient> CreateSockets ()
		{
			List<UdpClient> clients = new List<UdpClient> ();
			try {
				foreach (NetworkInterface n in NetworkInterface.GetAllNetworkInterfaces ()) {
					foreach (UnicastIPAddressInformation address in n.GetIPProperties ().UnicastAddresses) {
						if (address.Address.AddressFamily == AddressFamily.InterNetwork) {
							try {
								clients.Add (new UdpClient (new IPEndPoint (address.Address, 0)));
							} catch {
								continue; // Move on to the next address.
							}
						}
					}
				}
			} catch (Exception) {
				clients.Add (new UdpClient (0));
			}
			return clients;
		}

		public override void Search ()
		{
			var data = DiscoverDeviceMessage.EncodeSSDP ();
			Search (data, SearchEndpoint, SearchPeriod);
		}

		public override void Search (IPAddress gatewayAddress)
		{
			var data = DiscoverDeviceMessage.EncodeUnicast (gatewayAddress);
			Search (data, new IPEndPoint (gatewayAddress, SearchEndpoint.Port), null);
		}

		void Search (byte [] data, IPEndPoint gatewayAddress, TimeSpan? searchInternal)
		{
			PrepareToSearch (sockets);
			SearchTask = Search (data, gatewayAddress, searchInternal, OverallSearchCancellation.Token);
		}

		async Task Search (byte [] data, IPEndPoint endpoint, TimeSpan? searchInternal, CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				try {
					foreach (var client in sockets) {
						for (int i = 0; i < 3; i++) {
							try {
								client.Send (data, data.Length, endpoint);
							} catch {

							}
						}
					}

					if (searchInternal == null)
						break;

					await Task.Delay (searchInternal.Value, token);
				} catch (OperationCanceledException) {
					break;
				}
			}
		}

		protected override async Task HandleMessageReceived (IPAddress localAddress, UdpReceiveResult data)
		{
			await Handle (localAddress, data.Buffer);
		}

		async Task Handle (IPAddress localAddress, byte [] response)
		{
			// Convert it to a string for easy parsing
			string dataString = null;

			// No matter what, this method should never throw an exception. If something goes wrong
			// we should still be in a position to handle the next reply correctly.
			try {
				dataString = Encoding.UTF8.GetString (response);

				NatUtility.Log ("UPnP Response: {0}", dataString);

				/* For UPnP Port Mapping we need ot find either WANPPPConnection or WANIPConnection. 
				 Any other device type is no good to us for this purpose. See the IGP overview paper 
				 page 5 for an overview of device types and their hierarchy.
				 http://upnp.org/specs/gw/UPnP-gw-InternetGatewayDevice-v1-Device.pdf */

				/* TODO: Currently we are assuming version 1 of the protocol. We should figure out which
				 version it is and apply the correct URN. */

				/* Some routers don't correctly implement the version ID on the URN, so we only search for the type
				 prefix. */

				string log = "UPnP Response: Router advertised a '{0}' service";
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

				using (await Locker.DisposableWaitAsync ()) {
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
				var d = await GetServicesList (localAddress, deviceServiceUri).ConfigureAwait (false);
				if (d != null)
					RaiseDeviceFound (new DeviceEventArgs (d));
			} catch (Exception ex) {
				Trace.WriteLine ("Unhandled exception when trying to decode a device's response Send me the following data: ");
				Trace.WriteLine ("ErrorMessage:");
				Trace.WriteLine (ex.Message);
				Trace.WriteLine ("Data string:");
				Trace.WriteLine (dataString);
			}
		}

		async Task<UpnpNatDevice> GetServicesList (IPAddress localAddress, Uri deviceServiceUri)
		{
			// Save the callback so i can use it again later when i've finished parsing the services available

			// Create a HTTPWebRequest to download the list of services the device offers
			byte [] body;
			WebRequest request = new GetServicesMessage (deviceServiceUri).Encode (out body);
			if (body.Length > 0)
				NatUtility.Log ("Error: Services Message contained a body");
			var response = (HttpWebResponse) await request.GetResponseAsync ().ConfigureAwait (false);
			return await ServicesReceived (localAddress, deviceServiceUri, response).ConfigureAwait (false);
		}

		async Task<UpnpNatDevice> ServicesReceived (IPAddress localAddress, Uri deviceServiceUri, HttpWebResponse response)
		{
			try {
				int abortCount = 0;
				int bytesRead = 0;
				byte [] buffer = new byte [10240];
				StringBuilder servicesXml = new StringBuilder ();
				XmlDocument xmldoc = new XmlDocument ();
				Stream s = response.GetResponseStream ();

				if (response.StatusCode != HttpStatusCode.OK) {
					NatUtility.Log ("{0}: Couldn't get services list: {1}", response.ResponseUri, response.StatusCode);
					return null; // FIXME: This the best thing to do??
				}

				while (true) {
					bytesRead = s.Read (buffer, 0, buffer.Length);
					servicesXml.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
					try {
						xmldoc.LoadXml (servicesXml.ToString ());
						response.Close ();
						break;
					} catch (XmlException) {
						// If we can't receive the entire XML within 500ms, then drop the connection
						// Unfortunately not all routers supply a valid ContentLength (mine doesn't)
						// so this hack is needed to keep testing our recieved data until it gets successfully
						// parsed by the xmldoc. Without this, the code will never pick up my router.
						if (abortCount++ > 50) {
							response.Close ();
							return null;
						}
						NatUtility.Log ("{0}: Couldn't parse services list", response.ResponseUri);
						await Task.Delay (10);
					}
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
						StringComparison c = StringComparison.OrdinalIgnoreCase;
						// TODO: Add support for version 2 of UPnP.
						if (serviceType.Equals ("urn:schemas-upnp-org:service:WANPPPConnection:1", c) ||
							serviceType.Equals ("urn:schemas-upnp-org:service:WANIPConnection:1", c)) {
							var controlUrl = new Uri (service ["controlURL"].InnerText, UriKind.RelativeOrAbsolute);
							IPEndPoint deviceEndpoint = new IPEndPoint (IPAddress.Parse (response.ResponseUri.Host), response.ResponseUri.Port);
							NatUtility.Log ("{0}: Found upnp service at: {1}", response.ResponseUri, controlUrl.OriginalString);
							try {
								if (controlUrl.IsAbsoluteUri) {
									deviceEndpoint = new IPEndPoint (IPAddress.Parse (controlUrl.Host), controlUrl.Port);
									NatUtility.Log ("{0}: New control url: {1}", deviceEndpoint, controlUrl);
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
			} catch (WebException ex) {
				// Just drop the connection, FIXME: Should i retry?
				NatUtility.Log ("{0}: Device denied the connection attempt: {1}", ex.Response.ResponseUri, ex);
			} finally {
				if (response != null)
					response.Close ();
			}

			//If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
			//So we don't invoke the callback, so this device is never added to our lists
			return null;
		}
	}
}
