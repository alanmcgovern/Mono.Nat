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
using System.Xml;
using Mono.Nat.Enums;

namespace Mono.Nat.Upnp.Messages.Responses
{
    internal class GetGenericPortMappingEntryResponseMessage : MessageBase
    {
        private readonly string remoteHost;
        private readonly int externalPort;
        private readonly Protocol protocol;
        private readonly int internalPort;
        private readonly string internalClient;
        private readonly bool enabled;
        private readonly string portMappingDescription;
        private readonly int leaseDuration;

        public string RemoteHost
        {
            get { return remoteHost; }
        }

        public int ExternalPort
        {
            get { return externalPort; }
        }

        public Protocol Protocol
        {
            get { return protocol; }
        }

        public int InternalPort
        {
            get { return internalPort; }
        }

        public string InternalClient
        {
            get { return internalClient; }
        }

        public bool Enabled
        {
            get { return enabled; }
        }

        public string PortMappingDescription
        {
            get { return portMappingDescription; }
        }

        public int LeaseDuration
        {
            get { return leaseDuration; }
        }


        public GetGenericPortMappingEntryResponseMessage(XmlNode data, bool genericMapping)
            : base(null)
        {
            var xmlNewRemoteHost = data["NewRemoteHost"];
            if (xmlNewRemoteHost != null) remoteHost = (genericMapping) ? xmlNewRemoteHost.InnerText : string.Empty;
            
            var xmlNewExternalPort = data["NewExternalPort"];
            if (xmlNewExternalPort != null) externalPort = (genericMapping) ? Convert.ToInt32(xmlNewExternalPort.InnerText) : -1;
            
            if (genericMapping)
            {
                var xmlNewProtocol = data["NewProtocol"];
                protocol = xmlNewProtocol != null && xmlNewProtocol.InnerText.Equals("TCP", StringComparison.InvariantCultureIgnoreCase) ? Protocol.Tcp : Protocol.Udp;
            }
            else
                protocol = Protocol.Udp;

            var xmlNewInternalPort = data["NewInternalPort"];
            if (xmlNewInternalPort != null) internalPort = Convert.ToInt32(xmlNewInternalPort.InnerText);
            
            var xmlNewInternalCLient = data["NewInternalClient"];
            if (xmlNewInternalCLient != null) internalClient = xmlNewInternalCLient.InnerText;
            
            var xmlNewEnabled = data["NewEnabled"];
            if (xmlNewEnabled != null) enabled = xmlNewEnabled.InnerText == "1";
            
            var xmlNewPortMappingDescription = data["NewPortMappingDescription"];
            if (xmlNewPortMappingDescription != null) portMappingDescription = xmlNewPortMappingDescription.InnerText;
            leaseDuration = Convert.ToInt32(data["NewLeaseDuration"].InnerText);
        }

        public override System.Net.WebRequest Encode(out byte[] body)
        {
            throw new NotImplementedException();
        }
    }
}
