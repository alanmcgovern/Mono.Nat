/* To run the program, just compile it as an exe using GMCS */

using System;
using System.Collections.Generic;
using System.Text;
using Nat;
using System.Net;
using Nat;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;
            NatController c = new NatController();

            c.StartSearching();
            c.DeviceFound += new EventHandler<DeviceEventArgs>(c_DeviceFound);
            c.DeviceLost += new EventHandler<DeviceEventArgs>(c_DeviceLost);
            while (i++ < 15)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("If you haven't seen a port map/unmap by now, no router has been found.");
            Console.WriteLine();
            Console.WriteLine("Press a key to exit...");
            Console.ReadLine();
        }

        static void c_DeviceLost(object sender, EventArgs e)
        {

            throw new Exception("The method or operation is not implemented.");
        }

        private static INatDevice d;
        static void c_DeviceFound(object sender, DeviceEventArgs e)
        {
            Console.WriteLine("Found a uPnP device...");
            d = e.Device;
            Thread t = new Thread(new ThreadStart(Mapper));
            t.Start();
            while (t.IsAlive)
            {
                System.Threading.Thread.Sleep(150);
            }
        }


        private static void Mapper()
        {
            IAsyncResult result;
            Mapping m;

            m = new Mapping(15661, Protocol.TCP);
            Console.WriteLine();
            Console.WriteLine("Trying to map port: " + m.Port + " " + m.Protocol.ToString());
            result = d.BeginCreatePortMap(m, "MonoTorrent", null, m);
            result.AsyncWaitHandle.WaitOne();
            try
            {
                d.EndCreatePortMap(result);
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Mapping created: " + m.Port + " " + m.Protocol.ToString());
            }
            catch (MappingException ex)
            {
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Failed to create map: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }

            Console.WriteLine();
            m = new Mapping(15661, Protocol.TCP);
            Console.WriteLine("Trying to delete mapping: " + m.Port + " " + m.Protocol.ToString());
            result = d.BeginDeletePortMap(m, null, m);
            result.AsyncWaitHandle.WaitOne();

            try
            {
                d.EndDeletePortMap(result);
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Mapping deleted: " + m.Port + " " + m.Protocol.ToString());
            }
            catch(MappingException ex)
            {
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Failed to delete map: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }

            Console.WriteLine();
            m = new Mapping(15661, Protocol.UDP);
            Console.WriteLine("Trying to map port: " + m.Port + " " + m.Protocol.ToString());
            result = d.BeginCreatePortMap(m, "MonoTorrent", null, m);
            result.AsyncWaitHandle.WaitOne();
            try
            {
                d.EndCreatePortMap(result);
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Mapping created: " + m.Port + " " + m.Protocol.ToString());
            }
            catch (MappingException ex)
            {
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Failed to create map: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }

            Console.WriteLine();
            Console.WriteLine("Trying to unmap port 15661, UDP");
            result = d.BeginDeletePortMap(new Mapping(15661, Protocol.UDP), null, new Mapping(15661, Protocol.UDP));
            result.AsyncWaitHandle.WaitOne();
            try
            {
                d.EndDeletePortMap(result);
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Mapping deleted: " + m.Port.ToString() + " " + m.Protocol.ToString());
            }
            catch (MappingException ex)
            {
                m = (Mapping)result.AsyncState;
                Console.WriteLine("Failed to delete mapping: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }
        }


        static void d_PortMapEvent(object sender, EventArgs e)
        {
            System.Console.WriteLine(sender.ToString());
        }
    }
}
