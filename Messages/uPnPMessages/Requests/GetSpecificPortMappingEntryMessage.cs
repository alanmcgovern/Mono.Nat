using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;

namespace Nat
{
	internal class GetSpecificPortMappingEntryMessage : MessageBase
	{
		internal int externalPort;
        internal Protocol protocol;

		public GetSpecificPortMappingEntryMessage(int externalPort, Protocol protocol, UpnpNatDevice device)
			: base(device)
		{
			this.externalPort = externalPort;
			this.protocol = protocol;
		}

		public override WebRequest Encode()
		{
			StringBuilder sb = new StringBuilder(64);
			XmlWriter writer = CreateWriter(sb);

			WriteFullElement(writer, "NewRemoteHost", string.Empty);
			WriteFullElement(writer, "NewExternalPort", externalPort.ToString());
			WriteFullElement(writer, "NewProtocol", protocol == Protocol.Tcp ? "TCP" : "UDP");
			writer.Flush();

			return CreateRequest("GetSpecificPortMappingEntry", sb.ToString(), "POST");
		}
	}
}
