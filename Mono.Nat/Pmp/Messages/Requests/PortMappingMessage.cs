//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2019 Alan McGovern
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
using System.Net;

namespace Mono.Nat.Pmp
{
	abstract class PortMappingMessage
	{
		bool Create { get; }
		public Mapping Mapping { get; }

		public PortMappingMessage (Mapping mapping, bool create)
		{
			Mapping = mapping;
			Create = create;
		}

		public byte [] Encode ()
		{
			var package = new List<byte> ();

			package.Add (PmpConstants.Version);
			package.Add (Mapping.Protocol == Protocol.Tcp ? PmpConstants.OperationCodeTcp : PmpConstants.OperationCodeUdp);
			package.Add ((byte) 0); //reserved
			package.Add ((byte) 0); //reserved
			package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder ((short) Mapping.PrivatePort)));
			if (Create) {
				package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder ((short) Mapping.PublicPort)));
				package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder (Mapping.Lifetime == 0 ? 7200 : Mapping.Lifetime)));
			} else {
				package.AddRange (BitConverter.GetBytes ((short) 0));
				package.AddRange (BitConverter.GetBytes ((int) 0));
			}

			return package.ToArray ();
		}
	}
}
