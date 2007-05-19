using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nat
{
    internal class GetGenericPortMappingEntryResponseMessage : MessageBase
    {
        private string remoteHost;
        private int externalPort;
        private Protocol protocol;
        private int internalPort;
        private string internalClient;
        private bool enabled;
        private string portMappingDescription;
        private int leaseDuration;

        public string RemoteHost
        {
            get { return this.remoteHost; }
        }

        public int ExternalPort
        {
            get { return this.externalPort; }
        }

        public Protocol Protocol
        {
            get { return this.protocol; }
        }

        public int InternalPort
        {
            get { return this.internalPort; }
        }

        public string InternalClient
        {
            get { return this.internalClient; }
        }

        public bool Enabled
        {
            get { return this.enabled; }
        }

        public string PortMappingDescription
        {
            get { return this.portMappingDescription; }
        }

        public int LeaseDuration
        {
            get { return this.leaseDuration; }
        }


        public GetGenericPortMappingEntryResponseMessage(XmlNode data)
            :base(null)
        {
            remoteHost = data["NewRemoteHost"].InnerText;
            externalPort = Convert.ToInt32(data["NewExternalPort"].InnerText);
            protocol = data["NewProtocol"].InnerText.Equals("TCP", StringComparison.InvariantCultureIgnoreCase) ? Protocol.Tcp : Protocol.Udp;
            internalPort = Convert.ToInt32(data["NewInternalPort"].InnerText);
            internalClient = data["NewInternalClient"].InnerText;
            enabled = data["NewEnabled"].InnerText == "1" ? true : false;
            portMappingDescription = data["NewPortMappingDescription"].InnerText;
            leaseDuration = Convert.ToInt32(data["NewLeaseDuration"].InnerText);
        }
        public override System.Net.WebRequest Encode()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
