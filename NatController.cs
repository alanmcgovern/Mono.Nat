//
// NatController.cs
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
using System.Net;
using System.Timers;
using System.Net.Sockets;
using System.Collections.Generic;
using Nat.UPnPMessages;
using System.Threading;

namespace Nat
{
    public delegate void NatDeviceFoundCallback(NatDevice d);

    public class NatController
    {
        #region Events
        public event EventHandler<EventArgs> DeviceFound;
        public event EventHandler<EventArgs> DeviceLost;
        #endregion


        #region Member Variables
        /// <summary>
        /// The list of all Internet Gateway Devices that support uPnP port forwarding
        /// </summary>
        public List<NatDevice> Devices
        {
            get { return this.devices; }
        }
        private List<NatDevice> devices;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        //internal delegate void NatDeviceFoundCallback(NatDevice d);


        /// <summary>
        /// The IPAddresses that this computer has at the moment
        /// </summary>
        internal static IPAddress[] localAddresses = Dns.GetHostAddresses(Dns.GetHostName());


        /// <summary>
        /// The time in seconds between each search
        /// </summary>
        private const int SearchPeriod = 120 * 1000; // search once every 2 minutes


        /// <summary>
        /// The timer used to control the update period
        /// </summary>
        private System.Timers.Timer searchTimer;


        /// <summary>
        /// The UDPClient to send search requests over
        /// </summary>
        private UdpClient udpClient;


        /// <summary>
        /// The local endpoint that search requests are sent to
        /// </summary>
        private IPEndPoint searchEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

        private Thread listenThread;
        #endregion


        #region Constructors
        public NatController()
        {
            this.devices = new List<NatDevice>();
            this.searchTimer = new System.Timers.Timer(NatController.SearchPeriod);
            this.udpClient = new UdpClient();
            this.listenThread = new Thread(new ThreadStart(ListenThread));
            this.searchTimer.Elapsed += new ElapsedEventHandler(timerTick);
        }
        #endregion


        #region Public Methods
        /// <summary>
        /// True if the controller is actively searching for new devices
        /// </summary>
        public bool IsSearching
        {
            get { return this.searchTimer.Enabled; }
        }


        /// <summary>
        /// Makes the controller start actively searching for new devices
        /// </summary>
        public void StartSearching()
        {
            if (this.IsSearching)
                return;

            this.Search();
            this.searchTimer.Start();
            this.listenThread.IsBackground = true;
            this.listenThread.Start();
        }


        /// <summary>
        /// Makes the controller stop actively searching for new devices
        /// </summary>
        public void StopSearching()
        {
            if (!this.IsSearching)
                return;

            this.searchTimer.Stop();
        }
        #endregion


        #region Private/Internal Methods
        /// <summary>
        /// 
        /// </summary>
        private void Search()
        {
            DiscoverDeviceMessage message = new DiscoverDeviceMessage();
            byte[] data = message.Encode();

            // UDP is a bit unreliable, so send 5 requests at a time
            for (int i = 0; i < 5; i++)
                this.udpClient.Send(data, data.Length, this.searchEndPoint);
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timerTick(object sender, ElapsedEventArgs e)
        {
            this.Search();
        }


        /// <summary>
        /// 
        /// </summary>
        private void ListenThread()
        {
#warning Get a nicer way to signal the thread to die. Also stop the blocking on receive(). Can be done when mono has full support of UPnP client
            while (true)
            {
                Console.WriteLine("listening started");
                byte[] data = this.udpClient.Receive(ref this.searchEndPoint);
                this.ReplyReceived(data);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        private void ReplyReceived(byte[] data)
        {
            try
            {
                // Convert it to a string for easy parsing
                string dataString = System.Text.UTF8Encoding.UTF8.GetString(data);

                // No matter what reply we receive, we only want it if the device has a WANIPConnection service
                // We don't care about *anything* else.
                if (dataString.IndexOf("schemas-upnp-org:service:WANIPConnection:1", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    // We have an internet gateway device now
                    NatDevice d = new NatDevice(dataString);

                    if (this.devices.Contains(d))
                    {
                        // We already have found this device, so we just refresh it to let people know it's
                        // Still alive. If a device doesn't respond to a search, we dump it.
                        this.devices[this.devices.IndexOf(d)].LastSeen = DateTime.Now;
                    }

                    else
                    {
                        // Once we've parsed the information we need, we tell the device to retrieve it's service list
                        // Once we successfully receive the service list, the callback provided will be invoked.
                        d.GetServicesList(new NatDeviceFoundCallback(DeviceSetupComplete));
                    }
                }
            }
            finally
            { }
        }


        /// <summary>
        /// This method is invoked when an InternetGatewayDevice has finished setting itself up and is now ready to be used
        /// to map ports.
        /// </summary>
        /// <param name="d">The device which has just finished setting up</param>
        private void DeviceSetupComplete(NatDevice d)
        {
            lock (this.devices)
            {
                // We don't want the same device in there twice
                if (this.devices.Contains(d))
                    return;

                this.devices.Add(d);
                if (this.DeviceFound != null)
                    this.DeviceFound(d, new EventArgs());
            }
        }
        #endregion
    }
}