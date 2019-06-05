using System;
using System.Net;

namespace Mono.Nat.Upnp
{
	interface IRequestMessage
	{
		WebRequest Encode (out byte [] body);
	}
}
