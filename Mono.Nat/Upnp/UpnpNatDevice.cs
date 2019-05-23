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
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Mono.Nat.Upnp
{
	public sealed class UpnpNatDevice : AbstractNatDevice, IEquatable<UpnpNatDevice> 
	{
		private EndPoint hostEndPoint;
		private IPAddress localAddress;
		private string serviceDescriptionUrl;
		private string controlUrl;
		private string serviceType;

		public override IPAddress LocalAddress
		{
			get { return localAddress; }
		}
		
		/// <summary>
		/// The callback to invoke when we are finished setting up the device
		/// </summary>
		private NatDeviceCallback callback;
		
		internal UpnpNatDevice (IPAddress localAddress, string deviceDetails)
		{
			this.LastSeen = DateTime.Now;
			this.localAddress = localAddress;

			// Split the string at the "location" section so i can extract the ipaddress and service description url
			string locationDetails = deviceDetails.Substring(deviceDetails.IndexOf("Location", StringComparison.InvariantCultureIgnoreCase) + 9).Split('\r')[0];

			// Make sure we have no excess whitespace
			locationDetails = locationDetails.Trim();

			// FIXME: Is this reliable enough. What if we get a hostname as opposed to a proper http address
			// Are we going to get addresses with the "http://" attached?
			if (locationDetails.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
			{
				NatUtility.Log("Found device at: {0}", locationDetails);
				// This bit strings out the "http://" from the string
				locationDetails = locationDetails.Substring(7);

				// We then split off the end of the string to get something like: 192.168.0.3:241 in our string
				string hostAddressAndPort = locationDetails.Remove(locationDetails.IndexOf('/'));

				// From this we parse out the IP address and Port
				if (hostAddressAndPort.IndexOf(':') > 0)
				{
					this.hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort.Remove(hostAddressAndPort.IndexOf(':'))),
					Convert.ToUInt16(hostAddressAndPort.Substring(hostAddressAndPort.IndexOf(':') + 1), System.Globalization.CultureInfo.InvariantCulture));
				}
				else
				{
					// there is no port specified, use default port (80)
					this.hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort), 80);
				}

				NatUtility.Log("Parsed device as: {0}", this.hostEndPoint.ToString());
				
				// The service description URL is the remainder of the "locationDetails" string. The bit that was originally after the ip
				// and port information
				this.serviceDescriptionUrl = locationDetails.Substring(locationDetails.IndexOf('/'));
			}
			else
			{
				Trace.WriteLine("Couldn't decode address. Please send following string to the developer: ");
				Trace.WriteLine(deviceDetails);
			}
		}

		/// <summary>
		/// The EndPoint that the device is at
		/// </summary>
		internal EndPoint HostEndPoint
		{
			get { return this.hostEndPoint; }
		}

		/// <summary>
		/// The relative url of the xml file that describes the list of services is at
		/// </summary>
		internal string ServiceDescriptionUrl
		{
			get { return this.serviceDescriptionUrl; }
		}

		/// <summary>
		/// The relative url that we can use to control the port forwarding
		/// </summary>
		internal string ControlUrl
		{
			get { return this.controlUrl; }
		}

		/// <summary>
		/// The service type we're using on the device
		/// </summary>
		public string ServiceType
		{
			get { return serviceType; }
		}

		public override async Task CreatePortMapAsync(Mapping mapping)
		{
			var message = new CreatePortMappingMessage(mapping, localAddress, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is CreatePortMappingResponseMessage))
				throw new MappingException (-1, "Invalid response received when creating the port map");
		}

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
			var message = new DeletePortMappingMessage(mapping, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is DeletePortMapResponseMessage))
				throw new MappingException (-1, "Invalid response received when deleting the port map");
		}

		public override async Task<Mapping[]> GetAllMappingsAsync()
		{
			var mappings = new List<Mapping> ();

			// Is it OK to hardcode 1000 mappings as the maximum? Probably better than an infinite loop
			// which would rely on routers correctly reporting all the mappings have been retrieved...
			try {
				for (int i = 0; i < 1000; i++) {
					var message = new GetGenericPortMappingEntry(i, this);
					// If we get a null response, or it's the wrong type, bail out.
					// It means we've iterated over the entire array.
					var resp = await SendMessageAsync (message).ConfigureAwait (false);
					if (!(resp is GetGenericPortMappingEntryResponseMessage response))
						break;

					mappings.Add (new Mapping (response.Protocol, response.InternalPort, response.ExternalPort, response.LeaseDuration) {
						Description = response.PortMappingDescription
					});
				}
			} catch (MappingException ex) {
				// Error code 713 means we successfully iterated to the end of the array and have all the mappings.
				// Exception driven code flow ftw!
				if (ex.ErrorCode != 713)
					throw;
			}

			return mappings.ToArray ();
		}

		public override async Task<IPAddress> GetExternalIPAsync()
		{
			var message = new GetExternalIPAddressMessage(this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is GetExternalIPAddressResponseMessage msg))
				throw new MappingException (-1, "Invalid response received when getting the external IP address");
			return msg.ExternalIPAddress;
		}

		public override async Task<Mapping> GetSpecificMappingAsync(Protocol protocol, int port)
		{
			var message = new GetSpecificPortMappingEntryMessage(protocol, port, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is GetSpecificPortMappingEntryResponseMessage msg))
				throw new MappingException (-1, "Invalid response received when getting the specific mapping");
			return new Mapping(protocol, msg.InternalPort, port, msg.LeaseDuration) {
				Description = msg.PortMappingDescription
			};
		}

		async Task<MessageBase> SendMessageAsync(MessageBase message)
		{
			WebRequest request = message.Encode(out byte[] body);
			if (body.Length > 0) {
				request.ContentLength = body.Length;
				using (var stream = await request.GetRequestStreamAsync ().ConfigureAwait (false))
					stream.Write(body, 0, body.Length);
			}

			try
			{
				using (var response = await request.GetResponseAsync ().ConfigureAwait (false))
					return DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
			}
			catch (WebException ex)
			{
				// Even if the request "failed" i want to continue on to read out the response from the router
				using (var response = ex.Response as HttpWebResponse) {
					if (response == null)
						throw new MappingException ((int)ex.Status, ex.Message);
					else
						return DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
				}
			}
		}

		/// <summary>
		///  Maps the specified port to this computer
		/// </summary>
		public override IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (CreatePortMapAsync (mapping), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		/// <summary>
		/// Removes a port mapping from this computer  
		/// </summary>
		public override IAsyncResult BeginDeletePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
		{
			 var result = new TaskAsyncResult (DeletePortMapAsync (mapping), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="asyncState"></param>
		/// <returns></returns>
		public override IAsyncResult BeginGetAllMappings(AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (GetAllMappingsAsync (), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}
		
		/// <summary>
		/// Begins an async call to get the external ip address of the router
		/// </summary>
		public override IAsyncResult BeginGetExternalIP(AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (GetExternalIPAsync (), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="protocol"></param>
		/// <param name="port"></param>
		/// <param name="callback"></param>
		/// <param name="asyncState"></param>
		/// <returns></returns>
		public override IAsyncResult BeginGetSpecificMapping (Protocol protocol, int externalPort, AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (GetSpecificMappingAsync (protocol, externalPort), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		public override void EndCreatePortMap(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			if (!(result is TaskAsyncResult mappingResult))
				throw new ArgumentException("Invalid AsyncResult", nameof(result));

			mappingResult.Task.GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		public override void EndDeletePortMap(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			if (!(result is TaskAsyncResult mappingResult))
				throw new ArgumentException("Invalid AsyncResult", nameof(result));

			mappingResult.Task.GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public override Mapping[] EndGetAllMappings(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			if (!(result is TaskAsyncResult mappingResult))
				throw new ArgumentException("Invalid AsyncResult", nameof(result));

			return ((Task<Mapping[]>)mappingResult.Task).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// Ends an async request to get the external ip address of the router
		/// </summary>
		public override IPAddress EndGetExternalIP(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			if (!(result is TaskAsyncResult mappingResult))
				throw new ArgumentException("Invalid AsyncResult", nameof(result));

			return ((Task<IPAddress>)mappingResult.Task).GetAwaiter ().GetResult ();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		public override Mapping EndGetSpecificMapping(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			if (!(result is TaskAsyncResult mappingResult))
				throw new ArgumentException("Invalid AsyncResult", nameof(result));

			return ((Task<Mapping>)mappingResult.Task).GetAwaiter ().GetResult ();
		}

		public override bool Equals(object obj)
		{
			UpnpNatDevice device = obj as UpnpNatDevice;
			return (device == null) ? false : this.Equals((device));
		}

		public bool Equals(UpnpNatDevice other)
		{
			return (other == null) ? false : (this.hostEndPoint.Equals(other.hostEndPoint)
				//&& this.controlUrl == other.controlUrl
				&& this.serviceDescriptionUrl == other.serviceDescriptionUrl);
		}

		public override int GetHashCode()
		{
			return (this.hostEndPoint.GetHashCode() ^ this.controlUrl.GetHashCode() ^ this.serviceDescriptionUrl.GetHashCode());
		}

		MessageBase DecodeMessageFromResponse(Stream s, long length)
		{
			StringBuilder data = new StringBuilder();
			int bytesRead;
			int totalBytesRead = 0;
			byte[] buffer = new byte[10240];

			// Read out the content of the message, hopefully picking everything up in the case where we have no contentlength
			if (length != -1)
			{
				while (totalBytesRead < length)
				{
					bytesRead = s.Read(buffer, 0, buffer.Length);
					data.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
					totalBytesRead += bytesRead;
				}
			}
			else
			{
				while ((bytesRead = s.Read(buffer, 0, buffer.Length)) != 0)
					data.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
			}

			// Once we have our content, we need to see what kind of message it is. If we received
			// an error message we will immediately throw a MappingException.
			return MessageBase.Decode(this, data.ToString());
		}

		internal void GetServicesList(NatDeviceCallback callback)
		{
			// Save the callback so i can use it again later when i've finished parsing the services available
			this.callback = callback;

			// Create a HTTPWebRequest to download the list of services the device offers
			byte[] body;
			WebRequest request = new GetServicesMessage(this.serviceDescriptionUrl, this.hostEndPoint).Encode(out body);
			if (body.Length > 0)
				NatUtility.Log("Error: Services Message contained a body");
			request.BeginGetResponse(this.ServicesReceived, request);
		}

		private void ServicesReceived(IAsyncResult result)
		{
			HttpWebResponse response = null;
			try
			{
				int abortCount = 0;
				int bytesRead = 0;
				byte[] buffer = new byte[10240];
				StringBuilder servicesXml = new StringBuilder();
				XmlDocument xmldoc = new XmlDocument();
				HttpWebRequest request = result.AsyncState as HttpWebRequest;
				response = request.EndGetResponse(result) as HttpWebResponse;
				Stream s = response.GetResponseStream();

				if (response.StatusCode != HttpStatusCode.OK) {
					NatUtility.Log("{0}: Couldn't get services list: {1}", HostEndPoint, response.StatusCode);
					return; // FIXME: This the best thing to do??
				}

				while (true)
				{
					bytesRead = s.Read(buffer, 0, buffer.Length);
					servicesXml.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
					try
					{
						xmldoc.LoadXml(servicesXml.ToString());
						response.Close();
						break;
					}
					catch (XmlException)
					{
						// If we can't receive the entire XML within 500ms, then drop the connection
						// Unfortunately not all routers supply a valid ContentLength (mine doesn't)
						// so this hack is needed to keep testing our recieved data until it gets successfully
						// parsed by the xmldoc. Without this, the code will never pick up my router.
						if (abortCount++ > 50)
						{
							response.Close();
							return;
						}
						NatUtility.Log("{0}: Couldn't parse services list", HostEndPoint);
						System.Threading.Thread.Sleep(10);
					}
				}

				NatUtility.Log("{0}: Parsed services list", HostEndPoint);
				XmlNamespaceManager ns = new XmlNamespaceManager(xmldoc.NameTable);
				ns.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
				XmlNodeList nodes = xmldoc.SelectNodes("//*/ns:serviceList", ns);

				foreach (XmlNode node in nodes)
				{
					//Go through each service there
					foreach (XmlNode service in node.ChildNodes)
					{
						//If the service is a WANIPConnection, then we have what we want
						string type = service["serviceType"].InnerText;
						NatUtility.Log("{0}: Found service: {1}", HostEndPoint, type);
						StringComparison c = StringComparison.OrdinalIgnoreCase;
						// TODO: Add support for version 2 of UPnP.
						if (type.Equals("urn:schemas-upnp-org:service:WANPPPConnection:1", c) ||
							type.Equals("urn:schemas-upnp-org:service:WANIPConnection:1", c))
						{
							serviceType = type;
							this.controlUrl = service["controlURL"].InnerText;
							NatUtility.Log("{0}: Found upnp service at: {1}", HostEndPoint, controlUrl);
							try
							{
								Uri u = new Uri(controlUrl);
								if (u.IsAbsoluteUri)
								{
									EndPoint old = hostEndPoint;
									this.hostEndPoint = new IPEndPoint(IPAddress.Parse(u.Host), u.Port);
									NatUtility.Log("{0}: Absolute URI detected. Host address is now: {1}", old, HostEndPoint);
									this.controlUrl = controlUrl.Substring(u.GetLeftPart(UriPartial.Authority).Length);
									NatUtility.Log("{0}: New control url: {1}", HostEndPoint, controlUrl);
								}
							}
							catch
							{
								NatUtility.Log("{0}: Assuming control Uri is relative: {1}", HostEndPoint, controlUrl);
							}
							NatUtility.Log("{0}: Handshake Complete", HostEndPoint);
							this.callback(this);
							return;
						}
					}
				}

				//If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
				//So we don't invoke the callback, so this device is never added to our lists
			}
			catch (WebException ex)
			{
				// Just drop the connection, FIXME: Should i retry?
				NatUtility.Log("{0}: Device denied the connection attempt: {1}", HostEndPoint, ex);
			}
			finally
			{
				if (response != null)
					response.Close();
			}
		}

		/// <summary>
		/// Overridden.
		/// </summary>
		/// <returns></returns>
		public override string ToString( )
		{
			//GetExternalIP is blocking and can throw exceptions, can't use it here.
			return String.Format( 
				"UpnpNatDevice - EndPoint: {0}, External IP: {1}, Control Url: {2}, Service Description Url: {3}, Service Type: {4}, Last Seen: {5}",
				this.hostEndPoint, "Manually Check" /*this.GetExternalIP()*/, this.controlUrl, this.serviceDescriptionUrl, this.serviceType, this.LastSeen);
		}
	}
}
