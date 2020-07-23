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
using System.Buffers.Binary;

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
			var package = new byte [12];

			package[0] = PmpConstants.Version;
			package[1] = Mapping.Protocol == Protocol.Tcp ? PmpConstants.OperationCodeTcp : PmpConstants.OperationCodeUdp;
			package[2] = 0; //reserved
			package[3] = 0; //reserved

			BinaryPrimitives.WriteInt16BigEndian (package.AsSpan (4), (short)Mapping.PrivatePort);

			if (Create) {
				BinaryPrimitives.WriteInt16BigEndian (package.AsSpan (6), (short) Mapping.PublicPort);
				BinaryPrimitives.WriteInt32BigEndian (package.AsSpan (8), Mapping.Lifetime == 0 ? 7200 : Mapping.Lifetime);
			} else {
				BinaryPrimitives.WriteInt16BigEndian (package.AsSpan (6), 0);
				BinaryPrimitives.WriteInt32BigEndian (package.AsSpan (8), 0);
			}

			return package;
		}
	}
}
