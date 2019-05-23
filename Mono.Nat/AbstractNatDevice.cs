//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//   Ben Motmans <ben.motmans@gmail.com>
//
// Copyright (C) 2006 Alan McGovern
// Copyright (C) 2007 Ben Motmans
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
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace Mono.Nat
{
	public abstract class AbstractNatDevice : INatDevice
	{
		public DateTime LastSeen { get; set; }

		public abstract IPAddress LocalAddress { get; }

		protected AbstractNatDevice ()
		{

		}

		#region Async APIs

		public virtual async Task CreatePortMapAsync (Mapping mapping)
		{
			await Task.Factory.FromAsync (BeginCreatePortMap (mapping, null, null), EndCreatePortMap);
		}

		public virtual async Task DeletePortMapAsync (Mapping mapping)
		{
			await Task.Factory.FromAsync (BeginDeletePortMap (mapping, null, null), EndDeletePortMap);
		}

		public virtual async Task<Mapping[]> GetAllMappingsAsync ()
		{
			return await Task.Factory.FromAsync (BeginGetAllMappings (null, null), EndGetAllMappings);
		}

		public async virtual Task<IPAddress> GetExternalIPAsync ()
		{
			return await Task.Factory.FromAsync (BeginGetExternalIP (null, null), EndGetExternalIP);
		}

		public async virtual Task<Mapping> GetSpecificMappingAsync (Protocol protocol, int port)
		{
			return await Task.Factory.FromAsync (BeginGetSpecificMapping (protocol, port, null, null), EndGetSpecificMapping);
		}

		#endregion

		public virtual void CreatePortMap (Mapping mapping)
		{
			CreatePortMapAsync(mapping).GetAwaiter ().GetResult ();
		}

		public virtual void DeletePortMap (Mapping mapping)
		{
			DeletePortMapAsync(mapping).GetAwaiter ().GetResult ();
		}

		public virtual Mapping[] GetAllMappings ()
		{
			return GetAllMappingsAsync().GetAwaiter ().GetResult ();
		}

		public virtual IPAddress GetExternalIP ()
		{
			return GetExternalIPAsync().GetAwaiter ().GetResult ();
		}

		public virtual Mapping GetSpecificMapping (Protocol protocol, int port)
		{
			return GetSpecificMappingAsync(protocol, port).GetAwaiter ().GetResult ();
		}


		public abstract IAsyncResult BeginCreatePortMap (Mapping mapping, AsyncCallback callback, object asyncState);
		public abstract void EndCreatePortMap (IAsyncResult result);

		public abstract IAsyncResult BeginDeletePortMap (Mapping mapping, AsyncCallback callback, object asyncState);
		public abstract void EndDeletePortMap (IAsyncResult result);

		public abstract IAsyncResult BeginGetAllMappings (AsyncCallback callback, object asyncState);
		public abstract Mapping[] EndGetAllMappings (IAsyncResult result);

		public abstract IAsyncResult BeginGetExternalIP (AsyncCallback callback, object asyncState);
		public abstract IPAddress EndGetExternalIP (IAsyncResult result);

		public abstract IAsyncResult BeginGetSpecificMapping (Protocol protocol, int externalPort, AsyncCallback callback, object asyncState);
		public abstract Mapping EndGetSpecificMapping (IAsyncResult result);
	}
}
