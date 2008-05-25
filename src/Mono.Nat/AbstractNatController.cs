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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace Mono.Nat
{
	public abstract class AbstractNatController : INatController
	{
		public event EventHandler<DeviceEventArgs> DeviceFound;
		public event EventHandler<DeviceEventArgs> DeviceLost;
		
		public event EventHandler DiscoveryFinished;
		
		protected List<INatDevice> devices; // The list of all Internet Gateway Devices that support the specific port mapping protocol
		protected List<INatDevice> currentDevices; // The list of devices that are found in the current discovery
		protected bool discoveryRunning;
		
		protected Thread listenThread; // The thread used to listen for incoming messages
		protected UdpClient udpClient; // The UdpClient used to receive data
		
		protected IPAddress[] localAddresses; // The list of addresses that this computer has at this moment
		
		protected bool disposed;		
		
		protected AbstractNatController ()
		{
			this.localAddresses = Dns.GetHostAddresses (Dns.GetHostName ());
			
			Init ();
		}
		
		protected AbstractNatController (IPAddress[] localAddresses)
		{
			this.localAddresses = localAddresses;
			
			Init ();
		}
		
		public IPAddress[] LocalAddresses
		{
			get { return localAddresses; }
		}
		
		protected virtual void Init ()
		{
			this.devices = new List<INatDevice> ();
			this.udpClient = new UdpClient (new IPEndPoint(localAddresses[0], 0));
		}
		
		~AbstractNatController ()
		{
			Dispose (false);
		}
		
		public abstract string Name { get; }
		
		public IEnumerable<INatDevice> Devices
		{
			get { return devices; }
		}

		public bool IsDiscoveryRunning
		{
			get { return discoveryRunning; }
			protected internal set { discoveryRunning = value; }
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		public virtual void StartDiscovery ()
		{
			if (discoveryRunning)
				return;
			
			currentDevices = new List<INatDevice> ();
			
			StartThread ();
			Search ();
		}

		public virtual void StopDiscovery ()
		{
			StopThread ();
		}

		protected virtual void Dispose (bool disposeManagedResources)
		{
			if (disposed)
				return;
			if (!disposeManagedResources)
				return;

			IDisposable disposable;
			disposable = udpClient as IDisposable;
			if (disposable != null)
				disposable.Dispose ();
			
			disposed = true;
		}
		
		protected virtual void StartThread ()
		{
			if (discoveryRunning)
				return;

            discoveryRunning = true;
            ThreadPool.QueueUserWorkItem((WaitCallback)ThreadFunc);
		}

        private ManualResetEvent shouldStop = new ManualResetEvent(false);
        private ManualResetEvent hasStopped = new ManualResetEvent(false);
		protected virtual void StopThread ()
		{
			if (discoveryRunning) {
                shouldStop.Set();
                hasStopped.WaitOne();
                hasStopped.Reset();
			}
            this.currentDevices.Clear();
            this.devices.Clear();
			discoveryRunning = false;
		}
		
		protected virtual void ThreadFunc (object o)
        {
			IPEndPoint endPoint = null;
			ThreadInitialize (out endPoint);

			if (endPoint == null)
				endPoint = new IPEndPoint (localAddresses[0], 0);
			
			while (true)
            {
                while (udpClient.Available == 0)
                    if (shouldStop.WaitOne(1, true))
                        break;
                if (shouldStop.WaitOne(1, true))
                    break;

                byte[] data = udpClient.Receive(ref endPoint);
				ProcessData (endPoint, data);
			}
            shouldStop.Reset();
            hasStopped.Set();
		}
					
		protected abstract void ThreadInitialize (out IPEndPoint endPoint);
		
		protected abstract void ProcessData (IPEndPoint endPoint, byte[] data);
		
		protected abstract void Search ();
		
		protected virtual void DiscoveryComplete ()
		{
			StopDiscovery ();
			
			List<INatDevice> lostDevices = new List<INatDevice> ();
			
			foreach (INatDevice device in devices) {
				if (!currentDevices.Contains (device)) {
					//the device is lost
					OnDeviceLost (new DeviceEventArgs (device));
					lostDevices.Add (device);
				}
			}
			
			foreach (INatDevice device in lostDevices)
				devices.Remove (device);
			
			OnDiscoveryFinished (EventArgs.Empty);
		}
		
		protected virtual void OnDeviceFound (DeviceEventArgs args)
		{
			if (DeviceFound != null)
				DeviceFound (this, args);
		}
		
		protected virtual void OnDeviceLost (DeviceEventArgs args)
		{
			if (DeviceLost != null)
				DeviceLost (this, args);
		}
		
		protected virtual void OnDiscoveryFinished (EventArgs args)
		{
			if (DiscoveryFinished != null)
				DiscoveryFinished (this, args);
		}
	}
}