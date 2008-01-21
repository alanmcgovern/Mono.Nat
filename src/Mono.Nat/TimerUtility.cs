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
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Threading;
using System.Collections.Generic;

namespace Mono.Nat
{
	internal static class TimerUtility
	{
		private static System.Timers.Timer timer;
		private static int gcd;
		
		private static List<NatControllerContainer> containers;
		
		static TimerUtility ()
		{
			containers = new List<NatControllerContainer> ();
			
			timer = new System.Timers.Timer ();
			timer.Elapsed += TimerElapsed;
		}
		
		public static void RegisterDelayedDiscovery (INatController controller, int delay)
		{
			NatControllerContainer container = new NatControllerContainer (controller, delay);
			containers.Add (container);

			CalculateGcd (); //calculate the greatest common divisor to optimize the timer interval
			timer.Interval = gcd;

			if (!timer.Enabled)
				timer.Start ();
		}
		
		private static void TimerElapsed (object sender, ElapsedEventArgs args)
		{
			foreach (NatControllerContainer container in containers) {
				container.CurrentDelay += gcd;
				
				if (container.CurrentDelay >= container.Delay) {
					container.Reset ();

					container.Controller.StartDiscovery ();
				}
			}
		}
			
		private static void CalculateGcd ()
		{
			int gcd = -1;
			foreach (NatControllerContainer container in containers) {
				if (gcd < 0)
					gcd = container.Delay;
				else
					gcd = CalculateGcd (gcd, container.Delay);
			}

			TimerUtility.gcd = gcd;
		}
		
		private static int CalculateGcd (int a, int b)
		{
			while (b != 0) {
				int t = b;
				b = a % b;
				a = t;
			}
			return a;
		}
		
		internal class NatControllerContainer
		{
			public INatController Controller;
			public int Delay;
			public int CurrentDelay;
			
			public NatControllerContainer (INatController controller, int delay)
			{
				Controller = controller;
				Delay = delay;
				CurrentDelay = 0;
			}
			
			public void Reset ()
			{
				CurrentDelay = 0;
			}
		}
	}
}