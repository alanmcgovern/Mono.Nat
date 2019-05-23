using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
	class TaskAsyncResult : IAsyncResult
	{
		public object AsyncState { get; }

		public AsyncCallback Callback { get; }

		public WaitHandle AsyncWaitHandle => WaitHandle;

		public bool CompletedSynchronously { get; }

		public bool IsCompleted { get; private set; }

		public Task Task { get; }

		ManualResetEvent WaitHandle { get; }

		public TaskAsyncResult (Task task, AsyncCallback callback, object asyncState)
		{
			AsyncState = asyncState;
			Callback = callback;
			Task = task;
			WaitHandle = new ManualResetEvent(false);
		}

		public void Complete ()
		{
			IsCompleted = true;
			WaitHandle.Set ();
			Callback (this);
		}
	}
}
