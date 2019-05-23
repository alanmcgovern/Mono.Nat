//
// Authors:
//   Ben Motmans <ben.motmans@gmail.com>
//
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mono.Nat.Pmp
{
	internal sealed class PmpNatDevice : AbstractNatDevice, IEquatable<PmpNatDevice> 
	{
		readonly IPAddress localAddress;
		readonly IPAddress publicAddress;

		public override IPAddress LocalAddress => localAddress;
		
		internal PmpNatDevice (IPAddress localAddress, IPAddress publicAddress)
		{
			this.localAddress = localAddress;
			this.publicAddress = publicAddress;
		}

		public override async Task CreatePortMapAsync(Mapping mapping)
		{
			var message = CreatePortMapMessage (mapping, true);
			mapping.Lifetime = await SendMessageAsync (localAddress, message);
		}

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
			var message = CreatePortMapMessage (mapping, false);
			mapping.Lifetime = await SendMessageAsync (localAddress, message);
		}

		public override Task<IPAddress> GetExternalIPAsync()
		{
			return Task.FromResult (publicAddress);
		}

		public override IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (CreatePortMapAsync (mapping), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		public override IAsyncResult BeginDeletePortMap (Mapping mapping, AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (DeletePortMapAsync (mapping), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		public override void EndCreatePortMap (IAsyncResult result)
		{
			((TaskAsyncResult)result).Task.GetAwaiter ().GetResult ();
		}

		public override void EndDeletePortMap (IAsyncResult result)
		{
			((TaskAsyncResult)result).Task.GetAwaiter ().GetResult ();
		}
		
		public override IAsyncResult BeginGetAllMappings (AsyncCallback callback, object asyncState)
		{
			//NAT-PMP does not specify a way to get all port mappings
			throw new NotSupportedException ();
		}

		public override IAsyncResult BeginGetExternalIP (AsyncCallback callback, object asyncState)
		{
			var result = new TaskAsyncResult (GetExternalIPAsync (), callback, asyncState);
			result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
			return result;
		}

		public override IAsyncResult BeginGetSpecificMapping (Protocol protocol, int port, AsyncCallback callback, object asyncState)
		{
			//NAT-PMP does not specify a way to get a specific port map
			throw new NotSupportedException ();
		}
		
		public override Mapping[] EndGetAllMappings (IAsyncResult result)
		{
			//NAT-PMP does not specify a way to get all port mappings
			throw new NotSupportedException ();
		}

		public override IPAddress EndGetExternalIP (IAsyncResult result)
		{
			var tar = (TaskAsyncResult)result;
			return ((Task<IPAddress>)tar.Task).GetAwaiter ().GetResult ();
		}

		public override Mapping EndGetSpecificMapping (IAsyncResult result)
		{
			//NAT-PMP does not specify a way to get a specific port map
			throw new NotSupportedException ();
		}
		
		public override bool Equals(object obj)
		{
			PmpNatDevice device = obj as PmpNatDevice;
			return (device == null) ? false : this.Equals(device);
		}
		
		public override int GetHashCode ()
		{
			return this.publicAddress.GetHashCode();
		}

		public bool Equals (PmpNatDevice other)
		{
			return (other == null) ? false : this.publicAddress.Equals(other.publicAddress);
		}

		static byte[] CreatePortMapMessage (Mapping mapping, bool create)
		{
			List<byte> package = new List<byte> ();
			
			package.Add (PmpConstants.Version);
			package.Add (mapping.Protocol == Protocol.Tcp ? PmpConstants.OperationCodeTcp : PmpConstants.OperationCodeUdp);
			package.Add ((byte)0); //reserved
			package.Add ((byte)0); //reserved
			package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder((short)mapping.PrivatePort)));
			package.AddRange (BitConverter.GetBytes (create ? IPAddress.HostToNetworkOrder((short)mapping.PublicPort) : (short)0));
			package.AddRange (BitConverter.GetBytes (IPAddress.HostToNetworkOrder(mapping.Lifetime)));

			return package.ToArray ();
		}

		
		static async Task<int> SendMessageAsync (IPAddress localAddress, byte[] message)
		{
			int delay = PmpConstants.RetryDelay;
			UdpClient udpClient = new UdpClient ();
			CancellationTokenSource tcs = new CancellationTokenSource ();
			tcs.Token.Register (() => udpClient.Dispose ());

			await udpClient.SendAsync (message, message.Length, new IPEndPoint (localAddress, PmpConstants.ServerPort)).ConfigureAwait (false);
			var receiveTask = ReceiveMessageAsync (udpClient);

			await Task.Delay (delay);
			for (int i = 0; i < PmpConstants.RetryAttempts && !receiveTask.IsCompleted; i ++) {
				delay *= 2;
				await Task.Delay (delay);
				await udpClient.SendAsync (message, message.Length, new IPEndPoint (localAddress, PmpConstants.ServerPort)).ConfigureAwait (false);
			}

			tcs.Dispose ();
			return await receiveTask;
		}
		
		static async Task<int> ReceiveMessageAsync (UdpClient udpClient)
		{
			while (true)
			{
				var receiveResult = await udpClient.ReceiveAsync ();
				var data = receiveResult.Buffer;

				if (data.Length < 16)
					continue;

				if (data[0] != PmpConstants.Version)
					continue;

				byte opCode = (byte)(data[1] & (byte)127);

				Protocol protocol = Protocol.Tcp;
				if (opCode == PmpConstants.OperationCodeUdp)
					protocol = Protocol.Udp;

				short resultCode = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 2));
				uint epoch = (uint)IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 4));

				int privatePort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 8));
				int publicPort = IPAddress.NetworkToHostOrder (BitConverter.ToInt16 (data, 10));

				uint lifetime = (uint)IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (data, 12));

				if (publicPort < 0 || privatePort < 0 || resultCode != PmpConstants.ResultCodeSuccess)
					throw new MappingException (resultCode, "Could not modify the port map");

				return (int)lifetime;
			}
		}

		/// <summary>
		/// Overridden.
		/// </summary>
		/// <returns></returns>
		public override string ToString( )
		{
			return String.Format( "PmpNatDevice - Local Address: {0}, Public IP: {1}, Last Seen: {2}",
				this.localAddress, this.publicAddress, this.LastSeen );
		}
	}
}