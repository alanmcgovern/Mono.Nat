//
// CreatePortMappingMessage.cs
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



using System.Net;
using System.IO;

namespace Nat.UPnPMessages
{
    internal class CreatePortMappingMessage
    {
        #region Member Variables
        public string PortMappingDescription
        {
            get { return this.portMappingDescription; }
        }
        private string portMappingDescription;


        public IPAddress LocalIpAddress
        {
            get { return this.localIpAddress; }
        }
        private IPAddress localIpAddress;


        public Mapping Mapping
        {
            get { return this.mapping; }
        }
        private Mapping mapping;


        private UPnPNatDevice device;
        #endregion


        #region Constructors
        public CreatePortMappingMessage(Mapping mapping, IPAddress localIpAddress,
                                    string portMappingDescription, UPnPNatDevice device)
        {
            this.mapping = mapping;
            this.localIpAddress = localIpAddress;
            this.portMappingDescription = portMappingDescription;
            this.device = device;
        }
        #endregion


        #region IMessage Members
        public HttpWebRequest Encode(bool useManHeader)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create("http://" + this.device.HostEndPoint.ToString() + this.device.ControlUrl);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#AddPortMapping\"");
            string tempVariable = false ? "192.168.0.111" : this.localIpAddress.ToString();

            string body = "<s:Envelope "
               + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
               + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
               + "<s:Body>"
               + "<u:AddPortMapping "
               + "xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"
               + "<NewRemoteHost></NewRemoteHost>"
               + "<NewExternalPort>" + this.mapping.Port.ToString() + "</NewExternalPort>"
               + "<NewProtocol>" + this.mapping.Protocol.ToString() + "</NewProtocol>"
               + "<NewInternalPort>" + this.mapping.Port.ToString() + "</NewInternalPort>"
               + "<NewInternalClient>" + tempVariable + "</NewInternalClient>"
               + "<NewEnabled>1</NewEnabled>"
               + "<NewPortMappingDescription>" + this.portMappingDescription + "</NewPortMappingDescription>"
               + "<NewLeaseDuration>0</NewLeaseDuration>"
               + "</u:AddPortMapping>"
               + "</s:Body>"
               + "</s:Envelope>\r\n\r\n";

            req.ContentLength = System.Text.Encoding.UTF8.GetByteCount(body);
            Stream s = req.GetRequestStream();

            s.Write(System.Text.Encoding.UTF8.GetBytes(body), 0, System.Text.Encoding.UTF8.GetByteCount(body));
            return req;
        }
        #endregion
    }
}
