using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	static class AsyncExtensions
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

		public static async Task<IDisposable> DisposableWaitAsync (this SemaphoreSlim semaphore, CancellationToken token)
		{
			await semaphore.WaitAsync (token);
			return new SemaphoreSlimDisposable (semaphore);
		}

		public static async Task CatchExceptions (this Task task)
		{
			try {
				await task.ConfigureAwait (false);
			} catch (OperationCanceledException) {
				// If we cancel the task then we don't need to log anything.
			} catch (Exception ex) {
				NatUtility.Log ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
			}
		}

		public static async void FireAndForget (this Task task)
		{
			try {
				await task.ConfigureAwait (false);
			} catch (OperationCanceledException) {
				// If we cancel the task then we don't need to log anything.
			} catch (Exception ex) {
				NatUtility.Log ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
			}
		}

		public static void WaitAndForget (this Task task)
		{
			try {
				task.GetAwaiter ().GetResult ();
			} catch (OperationCanceledException) {
				// If we cancel the task then we don't need to log anything.
			} catch (Exception ex) {
				NatUtility.Log ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
			}
		}
	}
}
