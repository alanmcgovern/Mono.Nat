//
// GetServicesMessage.cs
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
using System.Net;

namespace Nat.UpnpMessages
{
   internal class GetServicesMessage
    {
       private string servicesDescriptionUrl;
       private EndPoint hostAddress;

       public GetServicesMessage(string description, EndPoint hostAddress)
       {
           if (string.IsNullOrEmpty(description))
               Trace.WriteLine("Description is null");

           if (hostAddress == null)
               Trace.WriteLine("hostaddress is null");

           this.servicesDescriptionUrl = description;
           this.hostAddress = hostAddress;
       }


        #region IMessage Members

       public HttpWebRequest Encode()
       {
           HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://" + this.hostAddress.ToString() + this.servicesDescriptionUrl);
           req.Headers.Add("ACCEPT-LANGUAGE", "en");
           req.Method = "GET";
           
           //string s = "GET " + this.servicesDescriptionUrl + " HTTP/1.1\r\n"
           //             + "HOST: " + this.hostAddress + "\r\n"
           //             + "ACCEPT-LANGUAGE: en\r\n\r\n";

           return req;
       }

//        public static void Decode(byte[] response)
//        {
//            // I don't decode these
//            throw new NotSupportedException();
//        }

        #endregion
    }
}
