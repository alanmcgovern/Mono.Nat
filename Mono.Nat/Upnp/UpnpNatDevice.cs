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
using Mono.Nat.Enums;
using Mono.Nat.Exceptions;
using Mono.Nat.Upnp.AsyncResults;
using Mono.Nat.Upnp.Messages;
using Mono.Nat.Upnp.Messages.Requests;
using Mono.Nat.Upnp.Messages.Responses;

namespace Mono.Nat.Upnp
{
	public sealed class UpnpNatDevice : AbstractNatDevice, IEquatable<UpnpNatDevice> 
	{
		private EndPoint hostEndPoint;
		private readonly IPAddress localAddress;
		private readonly string serviceDescriptionUrl;
		private string controlUrl;
		private readonly string serviceType;

		public override IPAddress LocalAddress
		{
			get { return localAddress; }
		}
		
		/// <summary>
		/// The localCallback to invoke when we are finished setting up the device
		/// </summary>
		private NatDeviceCallback callback;
		
		internal UpnpNatDevice (IPAddress localAddress, string deviceDetails, string serviceType)
		{
			LastSeen = DateTime.Now;
			this.localAddress = localAddress;
            //Log.Debug("Mono.Nat", localAddress.ToString());  //FIXME: The ip address is registered as 0.0.0.0 need to fix

			// Split the string at the "location" section so i can extract the ipaddress and service description url
			string locationDetails = deviceDetails.Substring(deviceDetails.IndexOf("Location", StringComparison.InvariantCultureIgnoreCase) + 9).Split('\r')[0];
            this.serviceType = serviceType;

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
                    hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort.Remove(hostAddressAndPort.IndexOf(':'))),
                    Convert.ToUInt16(hostAddressAndPort.Substring(hostAddressAndPort.IndexOf(':') + 1), System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    // there is no port specified, use default port (80)
                    hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort), 80);
                }

				NatUtility.Log("Parsed device as: {0}", hostEndPoint.ToString());
				
				// The service description URL is the remainder of the "locationDetails" string. The bit that was originally after the ip
				// and port information
				serviceDescriptionUrl = locationDetails.Substring(locationDetails.IndexOf('/'));
			}
			else
			{
				//Trace.WriteLine("Couldn't decode address. Please send following string to the developer: ");
                //Log.Debug("Mono.Nat", "Couldn't decode address. Please send following string to the developer: ");
				//Trace.WriteLine(deviceDetails);
                //Log.Debug("Mono.Nat", deviceDetails);
			}
		}

		/// <summary>
		/// The EndPoint that the device is at
		/// </summary>
		internal EndPoint HostEndPoint
		{
			get { return hostEndPoint; }
		}

		/// <summary>
		/// The relative url of the xml file that describes the list of services is at
		/// </summary>
		internal string ServiceDescriptionUrl
		{
			get { return serviceDescriptionUrl; }
		}

		/// <summary>
		/// The relative url that we can use to control the port forwarding
		/// </summary>
		internal string ControlUrl
		{
			get { return controlUrl; }
		}

		/// <summary>
		/// The service type we're using on the device
		/// </summary>
		public string ServiceType
		{
			get { return serviceType; }
		}

		/// <summary>
		/// Begins an async call to get the external ip address of the router
		/// </summary>
		public override IAsyncResult BeginGetExternalIP(AsyncCallback localCallback, object asyncState)
		{
			// Create the port map message
			GetExternalIpAddressMessage message = new GetExternalIpAddressMessage(this);
			return BeginMessageInternal(message, localCallback, asyncState, EndGetExternalIPInternal);
		}

		/// <summary>
		///  Maps the specified port to this computer
		/// </summary>
        public override IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback localCallback, object asyncState)
		{
            CreatePortMappingMessage message = new CreatePortMappingMessage(mapping, localAddress, this);
            return BeginMessageInternal(message, localCallback, mapping, EndCreatePortMapInternal);
		}

		/// <summary>
		/// Removes a port mapping from this computer  
		/// </summary>
		public override IAsyncResult BeginDeletePortMap(Mapping mapping, AsyncCallback localCallback, object asyncState)
		{
			DeletePortMappingMessage message = new DeletePortMappingMessage(mapping, this);
			return BeginMessageInternal(message, localCallback, asyncState, EndDeletePortMapInternal);
		}


		public override IAsyncResult BeginGetAllMappings(AsyncCallback localCallback, object asyncState)
		{
			GetGenericPortMappingEntry message = new GetGenericPortMappingEntry(0, this);
			return BeginMessageInternal(message, localCallback, asyncState, EndGetAllMappingsInternal);
		}


		public override IAsyncResult BeginGetSpecificMapping (Protocol protocol, int port, AsyncCallback localCallback, object asyncState)
		{
			GetSpecificPortMappingEntryMessage message = new GetSpecificPortMappingEntryMessage(protocol, port, this);
			return BeginMessageInternal(message, localCallback, asyncState, EndGetSpecificMappingInternal);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		public override void EndCreatePortMap(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException("result");

			PortMapAsyncResult mappingResult = result as PortMapAsyncResult;
			if (mappingResult == null)
				throw new ArgumentException("Invalid AsyncResult", "result");

			// Check if we need to wait for the operation to finish
			if (!result.IsCompleted)
				result.AsyncWaitHandle.WaitOne();

			// If we have a saved exception, it means something went wrong during the mapping
			// so we just rethrow the exception and let the user figure out what they should do.
			if (mappingResult.SavedMessage is ErrorMessage)
			{
				ErrorMessage msg = mappingResult.SavedMessage as ErrorMessage;
				throw new MappingException(msg.ErrorCode, msg.Description);
			}

			//return result.AsyncState as Mapping;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="result"></param>
		public override void EndDeletePortMap(IAsyncResult result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			PortMapAsyncResult mappingResult = result as PortMapAsyncResult;
			if (mappingResult == null)
				throw new ArgumentException("Invalid AsyncResult", "result");

			// Check if we need to wait for the operation to finish
			if (!mappingResult.IsCompleted)
				mappingResult.AsyncWaitHandle.WaitOne();

			// If we have a saved exception, it means something went wrong during the mapping
			// so we just rethrow the exception and let the user figure out what they should do.
			if (mappingResult.SavedMessage is ErrorMessage)
			{
				ErrorMessage msg = mappingResult.SavedMessage as ErrorMessage;
				throw new MappingException(msg.ErrorCode, msg.Description);
			}

			// If all goes well, we just return
			//return true;
		}


		public override Mapping[] EndGetAllMappings(IAsyncResult result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			GetAllMappingsAsyncResult mappingResult = result as GetAllMappingsAsyncResult;
			if (mappingResult == null)
				throw new ArgumentException("Invalid AsyncResult", "result");

			if (!mappingResult.IsCompleted)
				mappingResult.AsyncWaitHandle.WaitOne();

			if (mappingResult.SavedMessage is ErrorMessage)
			{
				ErrorMessage msg = mappingResult.SavedMessage as ErrorMessage;
				if (msg.ErrorCode != 713)
					throw new MappingException(msg.ErrorCode, msg.Description);
			}

			return mappingResult.Mappings.ToArray();
		}


		/// <summary>
		/// Ends an async request to get the external ip address of the router
		/// </summary>
		public override IPAddress EndGetExternalIP(IAsyncResult result)
		{
			if (result == null) throw new ArgumentNullException("result");

			PortMapAsyncResult mappingResult = result as PortMapAsyncResult;
			if (mappingResult == null)
				throw new ArgumentException("Invalid AsyncResult", "result");

			if (!result.IsCompleted)
				result.AsyncWaitHandle.WaitOne();

			if (mappingResult.SavedMessage is ErrorMessage)
			{
				ErrorMessage msg = mappingResult.SavedMessage as ErrorMessage;
				throw new MappingException(msg.ErrorCode, msg.Description);
			}

			return mappingResult.SavedMessage == null ? null : ((GetExternalIpAddressResponseMessage)mappingResult.SavedMessage).ExternalIpAddress;
		}


		public override Mapping EndGetSpecificMapping(IAsyncResult result)
		{
			if (result == null)
				throw new ArgumentNullException("result");

			GetAllMappingsAsyncResult mappingResult = result as GetAllMappingsAsyncResult;
			if (mappingResult == null)
				throw new ArgumentException("Invalid AsyncResult", "result");

			if (!mappingResult.IsCompleted)
				mappingResult.AsyncWaitHandle.WaitOne();

			if (mappingResult.SavedMessage is ErrorMessage)
			{
				ErrorMessage message = mappingResult.SavedMessage as ErrorMessage;
				if (message.ErrorCode != 0x2ca)
				{
					throw new MappingException(message.ErrorCode, message.Description);
				}
			}
			return mappingResult.Mappings.Count == 0 ? new Mapping (Protocol.Tcp, -1, -1) : mappingResult.Mappings[0];
		}


		public override bool Equals(object obj)
		{
			UpnpNatDevice device = obj as UpnpNatDevice;
			return (device != null) && Equals((device));
		}


		public bool Equals(UpnpNatDevice other)
		{
			return (other != null) && (hostEndPoint.Equals(other.hostEndPoint)
			    //&& this.controlUrl == other.controlUrl
			    && serviceDescriptionUrl == other.serviceDescriptionUrl);
		}

		public override int GetHashCode()
		{
			return (hostEndPoint.GetHashCode() ^ controlUrl.GetHashCode() ^ serviceDescriptionUrl.GetHashCode());
		}

		private static IAsyncResult BeginMessageInternal(MessageBase message, AsyncCallback storedCallback, object asyncState, AsyncCallback callback)
		{
			byte[] body;
			WebRequest request = message.Encode(out body);
			PortMapAsyncResult mappingResult = PortMapAsyncResult.Create(message, request, storedCallback, asyncState);

			if (body.Length > 0)
			{
				request.ContentLength = body.Length;
				request.BeginGetRequestStream(delegate(IAsyncResult result) {
					try
					{
						Stream s = request.EndGetRequestStream(result);
						s.Write(body, 0, body.Length);
						request.BeginGetResponse(callback, mappingResult);
					}
					catch (Exception ex)
					{
						mappingResult.Complete(ex);
					}
				}, null);
			}
			else
			{
				request.BeginGetResponse(callback, mappingResult);
			}
			return mappingResult;
		}

		private static void CompleteMessage(IAsyncResult result)
		{
			PortMapAsyncResult mappingResult = result.AsyncState as PortMapAsyncResult;
		    if (mappingResult == null) return;
		    mappingResult.CompletedSynchronously = result.CompletedSynchronously;
		    mappingResult.Complete();
		}

		private MessageBase DecodeMessageFromResponse(Stream s, long length)
		{
			StringBuilder data = new StringBuilder();
			int bytesRead;
			int totalBytesRead = 0;
			byte[] buffer = new byte[10240];    //Whats so special about 10240?? -nick

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

			// Once we have our content, we need to see what kind of message it is. It'll either a an error
			// or a response based on the action we performed.
			return MessageBase.Decode(this, data.ToString());
		}

		private void EndCreatePortMapInternal(IAsyncResult result)
		{
			EndMessageInternal(result);
			CompleteMessage(result);
		}

		private void EndMessageInternal(IAsyncResult result)
		{
			HttpWebResponse response = null;
			PortMapAsyncResult mappingResult = result.AsyncState as PortMapAsyncResult;

			try
			{
				try
				{
				    if (mappingResult != null) response = (HttpWebResponse)mappingResult.Request.EndGetResponse(result);
				}
				catch (WebException ex)
				{
					// Even if the request "failed" i want to continue on to read out the response from the router
					response = ex.Response as HttpWebResponse;
					if (response == null)
					    if (mappingResult != null) mappingResult.SavedMessage = new ErrorMessage((int)ex.Status, ex.Message);
				}
			    if (response == null) return;
			    if (mappingResult != null)
			        mappingResult.SavedMessage = DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
			}

			finally
			{
				if (response != null)
					response.Close();
			}
		}

		private void EndDeletePortMapInternal(IAsyncResult result)
		{
			EndMessageInternal(result);
			CompleteMessage(result);
		}

		private void EndGetAllMappingsInternal(IAsyncResult result)
		{
			EndMessageInternal(result);

			GetAllMappingsAsyncResult mappingResult = result.AsyncState as GetAllMappingsAsyncResult;
		    if (mappingResult != null)
		    {
		        GetGenericPortMappingEntryResponseMessage message = mappingResult.SavedMessage as GetGenericPortMappingEntryResponseMessage;
		        if (message != null)
		        {
		            Mapping mapping = new Mapping (message.Protocol, message.InternalPort, message.ExternalPort, message.LeaseDuration)
		            {
		                Description = message.PortMappingDescription
		            };
		            mappingResult.Mappings.Add(mapping);
		            GetGenericPortMappingEntry next = new GetGenericPortMappingEntry(mappingResult.Mappings.Count, this);

		            // It's ok to do this synchronously because we should already be on anther thread
		            // and this won't block the user.
		            byte[] body;
		            WebRequest request = next.Encode(out body);
		            if (body.Length > 0)
		            {
		                request.ContentLength = body.Length;
		                request.GetRequestStream().Write(body, 0, body.Length);
		            }
		            mappingResult.Request = request;
		            request.BeginGetResponse(EndGetAllMappingsInternal, mappingResult);
		            return;
		        }
		    }

		    CompleteMessage(result);
		}

		private void EndGetExternalIPInternal(IAsyncResult result)
		{
			EndMessageInternal(result);
			CompleteMessage(result);
		}

		private void EndGetSpecificMappingInternal(IAsyncResult result)
		{
			EndMessageInternal(result);

			GetAllMappingsAsyncResult mappingResult = result.AsyncState as GetAllMappingsAsyncResult;
		    if (mappingResult != null)
		    {
		        GetGenericPortMappingEntryResponseMessage message = mappingResult.SavedMessage as GetGenericPortMappingEntryResponseMessage;
		        if (message != null) {
		            Mapping mapping = new Mapping(mappingResult.SpecificMapping.Protocol, message.InternalPort, mappingResult.SpecificMapping.PublicPort, message.LeaseDuration)
		            {
		                Description = mappingResult.SpecificMapping.Description
		            };
		            mappingResult.Mappings.Add(mapping);
		        }
		    }

		    CompleteMessage(result);
		}

		internal void GetServicesList(NatDeviceCallback localCallback)
		{
			// Save the localCallback so i can use it again later when i've finished parsing the services available
			callback = localCallback;

			// Create a HTTPWebRequest to download the list of services the device offers
			byte[] body;
			WebRequest request = new GetServicesMessage(serviceDescriptionUrl, hostEndPoint).Encode(out body);
			if (body.Length > 0)
				NatUtility.Log("Error: Services Message contained a body");
			request.BeginGetResponse(ServicesReceived, request);
		}

		private void ServicesReceived(IAsyncResult result)
		{
			HttpWebResponse response = null;
			try
			{
				int abortCount = 0;
			    byte[] buffer = new byte[10240];
				StringBuilder servicesXml = new StringBuilder();
				XmlDocument xmldoc = new XmlDocument();
				HttpWebRequest request = result.AsyncState as HttpWebRequest;
			    if (request != null) response = request.EndGetResponse(result) as HttpWebResponse;
			    if (response != null)
			    {
			        Stream s = response.GetResponseStream();

			        if (response.StatusCode != HttpStatusCode.OK) {
			            NatUtility.Log("{0}: Couldn't get services list: {1}", HostEndPoint, response.StatusCode);
			            return; // FIXME: This the best thing to do??
			        }

			        while (true)
			        {
			            int bytesRead = s.Read(buffer, 0, buffer.Length);
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
                        const StringComparison c = StringComparison.OrdinalIgnoreCase;
						// TODO: Add support for version 2 of UPnP.
					    if (!type.Equals("urn:schemas-upnp-org:service:WANPPPConnection:1", c) &&
					        !type.Equals("urn:schemas-upnp-org:service:WANIPConnection:1", c)) continue;
					    controlUrl = service["controlURL"].InnerText;
					    NatUtility.Log("{0}: Found upnp service at: {1}", HostEndPoint, controlUrl);
					    try
					    {
					        Uri u = new Uri(controlUrl);
					        if (u.IsAbsoluteUri)
					        {
					            EndPoint old = hostEndPoint;
					            hostEndPoint = new IPEndPoint(IPAddress.Parse(u.Host), u.Port);
					            NatUtility.Log("{0}: Absolute URI detected. Host address is now: {1}", old, HostEndPoint);
					            controlUrl = controlUrl.Substring(u.GetLeftPart(UriPartial.Authority).Length);
					            NatUtility.Log("{0}: New control url: {1}", HostEndPoint, controlUrl);
					        }
					    }
					    catch
					    {
					        NatUtility.Log("{0}: Assuming control Uri is relative: {1}", HostEndPoint, controlUrl);
					    }
					    NatUtility.Log("{0}: Handshake Complete", HostEndPoint);
					    callback(this);
					    return;
					}
				}

				//If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
				//So we don't invoke the localCallback, so this device is never added to our lists
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
                hostEndPoint, "Manually Check" /*this.GetExternalIP()*/, controlUrl, serviceDescriptionUrl, serviceType, LastSeen);
        }
	}
}