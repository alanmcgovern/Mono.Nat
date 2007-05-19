//
// NatDevice.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Nat.UpnpMessages;
using Nat;

namespace Nat
{
    public class UpnpNatDevice : IEquatable<UpnpNatDevice>, INatDevice
    {
        #region Member Variables
        /// <summary>
        /// The time that this device was last seen
        /// </summary>
        public DateTime LastSeen
        {
            get { return this.lastSeen; }
            set { this.lastSeen = value; }
        }
        private DateTime lastSeen;


        /// <summary>
        /// The EndPoint that the device is at
        /// </summary>
        internal EndPoint HostEndPoint
        {
            get { return this.hostEndPoint; }
        }
        private EndPoint hostEndPoint;


        /// <summary>
        /// The relative url of the xml file that describes the list of services is at
        /// </summary>
        //        internal string ServiceDescriptionUrl
        //        {
        //            get { return this.serviceDescriptionUrl; }
        //        }
        private string serviceDescriptionUrl;


        /// <summary>
        /// The relative url that we can use to control the port forwarding
        /// </summary>
        internal string ControlUrl
        {
            get { return this.controlUrl; }
        }
        private string controlUrl;


        /// <summary>
        /// The callback to invoke when we are finished setting up the device
        /// </summary>
        private NatDeviceFoundCallback callback;
        #endregion


        #region Constructors
        internal UpnpNatDevice(string deviceDetails)
        {
            this.lastSeen = DateTime.Now;

            // Split the string at the "location" section so i can extract the ipaddress and service description url
            string locationDetails = deviceDetails.Substring(deviceDetails.IndexOf("Location", StringComparison.InvariantCultureIgnoreCase) + 9).Split('\r')[0];

            // Make sure we have no excess whitespace
            locationDetails = locationDetails.Trim();

            // FIXME: Is this reliable enough. What if we get a hostname as opposed to a proper http address
            // Are we going to get addresses with the "http://" attached?
            if (locationDetails.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                // This bit strings out the "http://" from the string
                locationDetails = locationDetails.Substring(7);

                // We then split off the end of the string to get something like: 192.168.0.3:241 in our string
                string hostAddressAndPort = locationDetails.Remove(locationDetails.IndexOf('/'));

                // From this we parse out the IP address and Port
                this.hostEndPoint = new IPEndPoint(IPAddress.Parse(hostAddressAndPort.Remove(hostAddressAndPort.IndexOf(':'))),
                                                   Convert.ToUInt16(hostAddressAndPort.Substring(hostAddressAndPort.IndexOf(':') + 1), System.Globalization.CultureInfo.InvariantCulture));

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
        #endregion


        #region Public Methods
        /// <summary>
        /// Begins an async call to get the external ip address of the router
        /// </summary>
        public IAsyncResult BeginGetExternalIP(AsyncCallback callback, object asyncState)
        {
            return this.BeginGetExternalIPInternal(callback, asyncState);
        }


        /// <summary>
        ///  Maps the specified port to this computer
        /// </summary>
        public IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
        {
            // Initially try without the M- header
            return this.BeginCreatePortMapInternal(mapping, string.Empty, callback, asyncState);
        }


        /// <summary>
        /// Automatically fowards the specified port to this computer
        /// </summary>
        /// <param name="portMapDescription">The description to use for the mapped port</param>
        public IAsyncResult BeginCreatePortMap(Mapping mapping, string portMapDescription, AsyncCallback callback, object asyncState)
        {
            // Initially try without the M- header
            return this.BeginCreatePortMapInternal(mapping, portMapDescription, callback, asyncState);
        }


        /// <summary>
        /// Removes a port mapping from this computer  
        /// </summary>
        public IAsyncResult BeginDeletePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
        {
            return this.BeginDeletePortMapInternal(mapping, callback, asyncState);
        }


        public IAsyncResult BeginGetAllMappings(AsyncCallback callback, object asyncState)
        {
            return this.BeginGetAllMappingsInternal(callback, asyncState);
        }


        /// <summary>
        /// Creates a port mapping on the upnp router.
        /// </summary>
        /// <param name="mapping">Port details</param>
        public void CreatePortMap(Mapping mapping)
        {
            IAsyncResult result = BeginCreatePortMap(mapping, null, mapping);
            EndCreatePortMap(result);
        }


        /// <summary>
        /// Creates a port mapping on the upnp router.
        /// </summary>
        /// <param name="mapping">Port details</param>
        /// <param name="portMapDescription">Description that identifies this mapping.</param>
        public void CreatePortMap(Mapping mapping, string portMapDescription)
        {
            IAsyncResult result = BeginCreatePortMap(mapping, portMapDescription, null, mapping);
            EndCreatePortMap(result);
        }


        /// <summary>
        /// Removes a port mapping on the upnp router
        /// </summary>
        /// <param name="mapping">Port details</param>
        public void DeletePortMap(Mapping mapping)
        {
            IAsyncResult result = BeginDeletePortMap(mapping, null, mapping);
            EndDeletePortMap(result);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void EndCreatePortMap(IAsyncResult result)
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

            // If all goes well, we just return
            return;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void EndDeletePortMap(IAsyncResult result)
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
            return;
        }


        public Mapping[] EndGetAllMappings(IAsyncResult result)
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
        public IPAddress EndGetExternalIP(IAsyncResult result)
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

            return ((ExternalIPAddressMessage)mappingResult.SavedMessage).ExternalIPAddress;
        }


        public override bool Equals(object obj)
        {
            UpnpNatDevice device = obj as UpnpNatDevice;
            return (device == null) ? false : this.Equals((device));
        }


        public bool Equals(UpnpNatDevice other)
        {
            return (other == null) ? false : (this.hostEndPoint.Equals( other.hostEndPoint)
                                           //&& this.controlUrl == other.controlUrl
                                           && this.serviceDescriptionUrl == other.serviceDescriptionUrl);
        }


        /// <summary>
        /// Retrieves all portmappings currently configured on the upnp router
        /// </summary>
        /// <returns></returns>
        public Mapping[] GetAllMappings()
        {
            IAsyncResult result = BeginGetAllMappings(null, null);
            return EndGetAllMappings(result);
        }


        public override int GetHashCode()
        {
            return (this.hostEndPoint.GetHashCode() ^ this.controlUrl.GetHashCode() ^ this.serviceDescriptionUrl.GetHashCode());
        }


        /// <summary>
        /// Returns the IP address of the upnp router that is currently used.
        /// </summary>
        /// <returns>Address of the upnp router.</returns>
        public IPAddress GetExternalIP()
        {
            IAsyncResult result = BeginGetExternalIP(null, null);
            return EndGetExternalIP(result);
        }
        #endregion


        #region Private and Internal methods

        private IAsyncResult BeginMessageInternal(MessageBase message, AsyncCallback storedCallback, object asyncState, AsyncCallback callback)
        {
            WebRequest request = message.Encode();
            PortMapAsyncResult mappingResult = PortMapAsyncResult.Create(message, request, storedCallback, asyncState);
            request.BeginGetResponse(callback, mappingResult);
            return mappingResult;
        }

        private IAsyncResult BeginCreatePortMapInternal(Mapping mapping, string portMapDescription, AsyncCallback callback, object asyncState)
        {
            // Create the port map message
            CreatePortMappingMessage message = new CreatePortMappingMessage(mapping, NatController.localAddresses[0], portMapDescription, this);
            return BeginMessageInternal(message, callback, asyncState, EndCreatePortMapInternal);
        }

        private IAsyncResult BeginDeletePortMapInternal(Mapping mapping, AsyncCallback callback, object asyncState)
        {
            DeletePortMappingMessage message = new DeletePortMappingMessage(mapping, this);
            return BeginMessageInternal(message, callback, asyncState, EndDeletePortMapInternal);
        }

        private IAsyncResult BeginGetAllMappingsInternal(AsyncCallback callback, object asyncState)
        {
            GetGenericPortMappingEntry message = new GetGenericPortMappingEntry(0, this);
            return BeginMessageInternal(message, callback, asyncState, EndGetAllMappingsInternal);
        }

        private IAsyncResult BeginGetExternalIPInternal(AsyncCallback callback, object asyncState)
        {
            // Create the port map message
            GetExternalIPAddressMessage message = new GetExternalIPAddressMessage(this);
            return BeginMessageInternal(message, callback, asyncState, EndGetExternalIPInternal);
        }

        private static MessageBase DecodeMessageFromResponse(Stream s, long length)
        {
            StringBuilder data = new StringBuilder();
            int bytesRead = 0;
            int totalBytesRead = 0;
            byte[] buffer = new byte[10240];

            // Read out the content of the message, hopefully picking everything up in the case where we have no contentlength
            if (length != -1)
            {
                while (totalBytesRead != length)
                {
                    bytesRead += s.Read(buffer, 0, buffer.Length);
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
            return MessageBase.Decode(data.ToString());
        }

        private void EndCreatePortMapInternal(IAsyncResult result)
        {
            HttpWebResponse response = null;
            PortMapAsyncResult mappingResult = result.AsyncState as PortMapAsyncResult;

            try
            {
                try
                {
                    response = (HttpWebResponse)mappingResult.Request.EndGetResponse(result);
                }
                catch (WebException ex)
                {
                    // Even if the request "failed" i want to continue on to read out the response from the router
                    response = ex.Response as HttpWebResponse;
                }

                MessageBase message = DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
                mappingResult.SavedMessage = message;

                mappingResult.CompletedSynchronously = result.CompletedSynchronously;
                mappingResult.IsCompleted = true;
                mappingResult.AsyncWaitHandle.Set();

                // Invoke the callback if one was supplied
                if (mappingResult.CompletionCallback != null)
                    mappingResult.CompletionCallback(mappingResult);
            }

            finally
            {
                if (response != null)
                    response.Close();
            }
        }

        private void EndDeletePortMapInternal(IAsyncResult result)
        {
            MessageBase message;
            HttpWebResponse response = null;
            PortMapAsyncResult mappingResult = result.AsyncState as PortMapAsyncResult;
            try
            {
                try
                {
                    response = mappingResult.Request.EndGetResponse(result) as HttpWebResponse;
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                    if (response == null)
                        message = new ErrorMessage((int)ex.Status, ex.Message);
                }

                if (response != null)
                {
                    message = DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
                    mappingResult.SavedMessage = message;
                }

                mappingResult.IsCompleted = true;
                mappingResult.CompletedSynchronously = result.CompletedSynchronously;
                mappingResult.AsyncWaitHandle.Set();

                // Invoke the callback if one was supplied
                if (mappingResult.CompletionCallback != null)
                    mappingResult.CompletionCallback(mappingResult);
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
        }

        private void EndGetAllMappingsInternal(IAsyncResult result)
        {
            MessageBase message;
            WebResponse response = null;
            GetAllMappingsAsyncResult mappingResult = result.AsyncState as GetAllMappingsAsyncResult;
            try
            {
                try
                {
                    response = mappingResult.Request.EndGetResponse(result);
                }
                catch (WebException ex)
                {
                    response = ex.Response as HttpWebResponse;
                    if (response == null)
                        message = new ErrorMessage((int)ex.Status, ex.Message);
                }

                if (response != null)
                {
                    message = DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
                    mappingResult.SavedMessage = message;
                    GetGenericPortMappingEntryResponseMessage responseMessage = message as GetGenericPortMappingEntryResponseMessage;
                    if (responseMessage != null)
                    {
                        mappingResult.Mappings.Add(new Mapping(responseMessage.ExternalPort, responseMessage.Protocol));
                        GetGenericPortMappingEntry next = new GetGenericPortMappingEntry(mappingResult.Mappings.Count, this);

                        WebRequest request = next.Encode();
                        mappingResult.Request = request;
                        request.BeginGetResponse(EndGetAllMappingsInternal, mappingResult);
                        return;
                    }
                }

                mappingResult.IsCompleted = true;
                mappingResult.CompletedSynchronously = result.CompletedSynchronously;
                mappingResult.AsyncWaitHandle.Set();

                // Invoke the callback if one was supplied
                if (mappingResult.CompletionCallback != null)
                    mappingResult.CompletionCallback(mappingResult);
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
        }

        private void EndGetExternalIPInternal(IAsyncResult result)
        {
            HttpWebResponse response = null;
            PortMapAsyncResult mappingResult = result.AsyncState as PortMapAsyncResult;

            try
            {
                try
                {
                    response = (HttpWebResponse)mappingResult.Request.EndGetResponse(result);
                }
                catch (WebException ex)
                {
                    // Even if the request "failed" i want to continue on to read out the response from the router
                    response = ex.Response as HttpWebResponse;
                }

                MessageBase message = DecodeMessageFromResponse(response.GetResponseStream(), response.ContentLength);
                mappingResult.SavedMessage = message;

                mappingResult.CompletedSynchronously = result.CompletedSynchronously;
                mappingResult.IsCompleted = true;
                mappingResult.AsyncWaitHandle.Set();

                // Invoke the callback if one was supplied
                if (mappingResult.CompletionCallback != null)
                    mappingResult.CompletionCallback(mappingResult);
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
        }

        internal void GetServicesList(NatDeviceFoundCallback callback)
        {
            // Save the callback so i can use it again later when i've finished parsing the services available
            this.callback = callback;

            // Create a HTTPWebRequest to download the list of services the device offers
            WebRequest request = new GetServicesMessage(this.serviceDescriptionUrl, this.hostEndPoint).Encode();
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

                if (response.StatusCode != HttpStatusCode.OK)
                {
#warning Handle this how exactly?
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
                        System.Threading.Thread.Sleep(10);
                    }
                }

                XmlNamespaceManager ns = new XmlNamespaceManager(xmldoc.NameTable);
                ns.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
                XmlNodeList nodes = xmldoc.SelectNodes("//*/ns:serviceList", ns);

                foreach (XmlNode node in nodes)
                {
                    //Go through each service there
                    foreach (XmlNode service in node.ChildNodes)
                    {
                        //If the service is a WANIPConnection, then we have what we want
                        if (service["serviceType"].InnerText == "urn:schemas-upnp-org:service:WANIPConnection:1")
                        {
                            this.controlUrl = service["controlURL"].InnerText;
                            this.callback(this);
                            return;
                        }
                    }
                }

                //If we get here, it means that we didn't get WANIPConnection service, which means no uPnP forwarding
                //So we don't invoke the callback, so this device is never added to our lists
            }
            catch (WebException)
            {
#warning At the moment i just drop the connection. Should i retry once more?
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
        }
        #endregion
    }
}