using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	public static class SemaphoreSlimExtensions
	{
		class SemaphoreSlimDisposable : IDisposable
		{
			SemaphoreSlim Semaphore;

			public SemaphoreSlimDisposable (SemaphoreSlim semaphore)
			{
				Semaphore = semaphore;
			}

			public void Dispose ()
			{
				Semaphore?.Release ();
				Semaphore = null;
			}
		}

		public static async Task<IDisposable> DisposableWaitAsync (this SemaphoreSlim semaphore)
		{
			await semaphore.WaitAsync ();
			return new SemaphoreSlimDisposable (semaphore);
		}
	}
}
