using System;
using System.Threading;
using System.Threading.Tasks;

using Mono.Nat.Logging;

namespace Mono.Nat
{
    static class AsyncExtensions
    {
        static Logger Log { get; } = Logger.Create ();

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
                Log.ErrorFormatted ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }

        public static async void FireAndForget (this Task task)
        {
            try {
                await task.ConfigureAwait (false);
            } catch (OperationCanceledException) {
                // If we cancel the task then we don't need to log anything.
            } catch (Exception ex) {
                Log.ErrorFormatted ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }

        public static void WaitAndForget (this Task task)
        {
            try {
                task.ConfigureAwait (false).GetAwaiter().GetResult();
            } catch (OperationCanceledException) {
                // If we cancel the task then we don't need to log anything.
            } catch (Exception ex) {
                Log.ErrorFormatted ("Unhandled exception: {0}{1}", Environment.NewLine, ex);
            }
        }
        /// <summary>
        /// Adds cancellation functionality to a task that does not accept a CancellationToken otherwise.
        /// https://stackoverflow.com/questions/19404199/how-to-to-make-udpclient-receiveasync-cancelable#:~:text=There's%20no%20built%2Din%20way,Delay()%20to%20implement%20timeouts).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T> WithCancellation<T> (this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool> ();
            using (cancellationToken.Register (s => ((TaskCompletionSource<bool>) s).TrySetResult (true), tcs)) {
                if (task != await Task.WhenAny (task, tcs.Task).ConfigureAwait (false)) {
                    throw new OperationCanceledException (cancellationToken);
                }
            }

            return task.Result;
        }
    }
}
