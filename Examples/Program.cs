/* To run the program, just compile it as an exe using GMCS */

using System;
using System.Collections.Generic;
using System.Text;
using Nat;
using System.Net;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;
            NatController c = new NatController();

            c.Start();
            c.DeviceFound += new EventHandler<DeviceEventArgs>(c_DeviceFound);
            //c.DeviceLost += new EventHandler<DeviceEventArgs>(c_DeviceLost);
            while (i++ < 15)
            {
                System.Threading.Thread.Sleep(1000);
            }

            Console.WriteLine("If you haven't seen a port map/unmap by now, no router has been found.");
            Console.WriteLine();
            Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
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
            GetExternalIP();
            MapPort();
            GetAllMappings();
			GetSpecificMapping();

			Console.WriteLine("Finished!");
        }

        private static void MapPort()
        {
            Mapping m;
            m = new Mapping(15661, Protocol.Tcp);

            Console.WriteLine();
            
            Console.WriteLine("Trying to map port: " + m.Port + " " + m.Protocol.ToString());
            try
            {
                d.CreatePortMap(m, "MonoTorrent");
				System.Threading.Thread.Sleep(100);

                Console.WriteLine("Mapping created: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine();
                Console.WriteLine("Trying to delete mapping: " + m.Port + " " + m.Protocol.ToString());
                d.DeletePortMap(m);
				System.Threading.Thread.Sleep(100);

                Console.WriteLine("Mapping deleted: " + m.Port + " " + m.Protocol.ToString());

            }
            catch (MappingException ex)
            {
                Console.WriteLine("Failed to create/delete map: " + m.Port + " " + m.Protocol.ToString());
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }

        }

        private static void GetExternalIP()
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine("Trying to get external IP address");
                IPAddress address = d.GetExternalIP();
				System.Threading.Thread.Sleep(100);
                Console.WriteLine("External IP address is: " + address.ToString());
            }
            catch (MappingException ex)
            {
                Console.WriteLine("Failed to get external IP address");
                Console.WriteLine(ex.ErrorCode.ToString() + " " + ex.ErrorText);
            }
        }

        private static void GetAllMappings()
        {
            Console.WriteLine();
            Console.WriteLine("Trying to get all the mappings");

            Mapping[] mappings = d.GetAllMappings();
			System.Threading.Thread.Sleep(100);

            Console.WriteLine("Got all the mappings...");
            for (int i = 0; i < mappings.Length; i++)
            {
                Console.Write("Port: " + mappings[i].Port.ToString());
                Console.WriteLine(" Protocol: " + mappings[i].Protocol.ToString());
            }

            Console.WriteLine();

        }

		private static void GetSpecificMapping()
		{
				Mapping m = new Mapping(54321, Protocol.Tcp);
			try
			{
				d.CreatePortMap(m, "Test");
				System.Threading.Thread.Sleep(100);
				Console.WriteLine("Getting specific mapping...");
				Mapping routerMapping = d.GetSpecificMapping(m.Port, m.Protocol);
				System.Threading.Thread.Sleep(100);

				Console.WriteLine("Got specific mapping. Port: " + routerMapping.Port + " Protocol: " + routerMapping.Protocol); 
				d.DeletePortMap(m);
				System.Threading.Thread.Sleep(100);

			}
			catch
			{
				Console.WriteLine("Failed to get a specific mapping");
			}
			finally
			{
				try
				{
					d.DeletePortMap(m);
					System.Threading.Thread.Sleep(100);

				}
				catch
				{
				}
			}
		}

        static void d_PortMapEvent(object sender, EventArgs e)
        {
            System.Console.WriteLine(sender.ToString());
        }
    }
}
