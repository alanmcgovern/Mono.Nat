using System;
using System.Xml;

namespace Mono.Nat.Upnp
{
	class ResponseMessage
	{
		public static ResponseMessage Decode (UpnpNatDevice device, string message)
		{
			XmlNode node;
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (message);

			XmlNamespaceManager nsm = new XmlNamespaceManager (doc.NameTable);

			// Error messages should be found under this namespace
			nsm.AddNamespace ("errorNs", "urn:schemas-upnp-org:control-1-0");
			nsm.AddNamespace ("responseNs", device.ServiceType);

			// Check to see if we have a fault code message.
			if ((node = doc.SelectSingleNode ("//errorNs:UPnPError", nsm)) != null) {
				string errorCode = node ["errorCode"] != null ? node ["errorCode"].InnerText : "";
				string errorDescription = node ["errorDescription"] != null ? node ["errorDescription"].InnerText : "";

				throw new MappingException ((ErrorCode) int.Parse (errorCode), errorDescription);
			}

			if ((doc.SelectSingleNode ("//responseNs:AddPortMappingResponse", nsm)) != null)
				return new CreatePortMappingResponseMessage ();

			if ((doc.SelectSingleNode ("//responseNs:DeletePortMappingResponse", nsm)) != null)
				return new DeletePortMapResponseMessage ();

			if ((node = doc.SelectSingleNode ("//responseNs:GetExternalIPAddressResponse", nsm)) != null)
				return new GetExternalIPAddressResponseMessage (node);

			if ((node = doc.SelectSingleNode ("//responseNs:GetGenericPortMappingEntryResponse", nsm)) != null)
				return new GetGenericPortMappingEntryResponseMessage (node);

			if ((node = doc.SelectSingleNode ("//responseNs:GetSpecificPortMappingEntryResponse", nsm)) != null)
				return new GetSpecificPortMappingEntryResponseMessage (node);

			NatUtility.Log ("Unknown message returned. Please send me back the following XML:");
			NatUtility.Log (message);
			return null;
		}
	}
}
