using System;
using System.Collections.Generic;
using System.Text;
using Nat;
using System.Net;
using System.IO;

namespace Nat.UpnpMessages
{
    public class GetExternalIPAddressMessage
    {
        #region Member Variables
        private UpnpNatDevice device;
        #endregion


        #region Constructors
        public GetExternalIPAddressMessage(UpnpNatDevice device)
        {
            this.device = device;
        }
        #endregion


        #region IMessage Members
        public HttpWebRequest Encode()
        {
        	// TODO: Make use of the proper constructor.
        	Uri location = new Uri("http://" + this.device.HostEndPoint.ToString() + this.device.ControlUrl);
        	
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(location);
            req.Method = "POST";
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#GetExternalIPAddress\"");

            string body = "<s:Envelope "
               + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
               + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
               + "<s:Body>"
               + "<u:GetExternalIPAddress "
               + "xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"
               + "</u:GetExternalIPAddress>"
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
