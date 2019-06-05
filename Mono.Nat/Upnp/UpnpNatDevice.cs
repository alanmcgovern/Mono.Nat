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
	sealed class UpnpNatDevice : NatDevice, IEquatable<UpnpNatDevice>
	{
		/// <summary>
		/// The url we can use to control the port forwarding
		/// </summary>
		internal Uri DeviceControlUri { get; private set; }

		/// <summary>
		/// The local IP address
		/// </summary>
		IPAddress LocalAddress { get; }

		/// <summary>
		/// The service type we're using on the device
		/// </summary>
		public string ServiceType { get; private set; }

		internal UpnpNatDevice (IPAddress localAddress, IPEndPoint deviceEndpoint, Uri deviceControlUri, string serviceType)
			: base (deviceEndpoint, NatProtocol.Upnp)
		{
			LocalAddress = localAddress;
			DeviceControlUri = deviceControlUri;
			ServiceType = serviceType;
		}

		public override async Task<Mapping> CreatePortMapAsync (Mapping mapping)
		{
			var message = new CreatePortMappingMessage (mapping, LocalAddress, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is CreatePortMappingResponseMessage))
				throw new MappingException (ErrorCode.Unknown, "Invalid response received when creating the port map");
			return mapping;
		}

		public override async Task<Mapping> DeletePortMapAsync (Mapping mapping)
		{
			var message = new DeletePortMappingMessage (mapping, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is DeletePortMapResponseMessage))
				throw new MappingException (ErrorCode.Unknown, "Invalid response received when deleting the port map");
			return mapping;
		}

		public override async Task<Mapping []> GetAllMappingsAsync ()
		{
			var mappings = new List<Mapping> ();

			// Is it OK to hardcode 1000 mappings as the maximum? Probably better than an infinite loop
			// which would rely on routers correctly reporting all the mappings have been retrieved...
			try {
				for (int i = 0; i < 1000; i++) {
					var message = new GetGenericPortMappingEntry (i, this);
					// If we get a null response, or it's the wrong type, bail out.
					// It means we've iterated over the entire array.
					var resp = await SendMessageAsync (message).ConfigureAwait (false);
					if (!(resp is GetGenericPortMappingEntryResponseMessage response))
						break;

					mappings.Add (new Mapping (response.Protocol, response.InternalPort, response.ExternalPort, response.LeaseDuration, response.PortMappingDescription));
				}
			} catch (MappingException ex) {
				// Error code 713 means we successfully iterated to the end of the array and have all the mappings.
				// Exception driven code flow ftw!
				if (ex.ErrorCode != ErrorCode.SpecifiedArrayIndexInvalid)
					throw;
			}

			return mappings.ToArray ();
		}

		public override async Task<IPAddress> GetExternalIPAsync ()
		{
			var message = new GetExternalIPAddressMessage (this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is GetExternalIPAddressResponseMessage msg))
				throw new MappingException (ErrorCode.Unknown, "Invalid response received when getting the external IP address");
			return msg.ExternalIPAddress;
		}

		public override async Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int publicPort)
		{
			var message = new GetSpecificPortMappingEntryMessage (protocol, publicPort, this);
			var response = await SendMessageAsync (message).ConfigureAwait (false);
			if (!(response is GetSpecificPortMappingEntryResponseMessage msg))
				throw new MappingException (ErrorCode.Unknown, "Invalid response received when getting the specific mapping");
			return new Mapping (protocol, msg.InternalPort, publicPort, msg.LeaseDuration, msg.PortMappingDescription);
		}

		async Task<ResponseMessage> SendMessageAsync (RequestMessage message)
		{
			WebRequest request = message.Encode (out byte [] body);
			if (body.Length > 0) {
				request.ContentLength = body.Length;
				using (var stream = await request.GetRequestStreamAsync ().ConfigureAwait (false))
					stream.Write (body, 0, body.Length);
			}

			try {
				using (var response = await request.GetResponseAsync ().ConfigureAwait (false))
					return DecodeMessageFromResponse (response.GetResponseStream (), response.ContentLength);
			} catch (WebException ex) {
				// Even if the request "failed" i want to continue on to read out the response from the router
				using (var response = ex.Response as HttpWebResponse) {
					if (response == null)
						throw new MappingException ("Unexpected error sending a message to the device", ex);
					else
						return DecodeMessageFromResponse (response.GetResponseStream (), response.ContentLength);
				}
			}
		}

		public override bool Equals (object obj)
		{
			UpnpNatDevice device = obj as UpnpNatDevice;
			return device != null && Equals ((device));
		}

		public bool Equals (UpnpNatDevice other)
		{
			return other != null
				&& DeviceEndpoint.Equals (other.DeviceEndpoint)
				&& DeviceControlUri == other.DeviceControlUri;
		}

		public override int GetHashCode ()
		{
			return DeviceEndpoint.GetHashCode () ^ DeviceControlUri.GetHashCode ();
		}

		ResponseMessage DecodeMessageFromResponse (Stream s, long length)
		{
			StringBuilder data = new StringBuilder ();
			int bytesRead;
			int totalBytesRead = 0;
			byte [] buffer = new byte [10240];

			// Read out the content of the message, hopefully picking everything up in the case where we have no contentlength
			if (length != -1) {
				while (totalBytesRead < length) {
					bytesRead = s.Read (buffer, 0, buffer.Length);
					data.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
					totalBytesRead += bytesRead;
				}
			} else {
				while ((bytesRead = s.Read (buffer, 0, buffer.Length)) != 0)
					data.Append (Encoding.UTF8.GetString (buffer, 0, bytesRead));
			}

			// Once we have our content, we need to see what kind of message it is. If we received
			// an error message we will immediately throw a MappingException.
			return ResponseMessage.Decode (this, data.ToString ());
		}

		public override string ToString ()
		{
			return $"UpnpNatDevice - EndPoint: {DeviceEndpoint}, External IP: Manually Check, Control Url: {DeviceControlUri}, Service Type: {ServiceType}, Last Seen: {LastSeen}";
		}
	}
}
