using System;
using System.Threading;

namespace Mono.Nat.AsyncResults
{
    internal class AsyncResult : IAsyncResult
    {
        private readonly object asyncState;
        private readonly AsyncCallback callback;
        private bool isCompleted;
        private Exception storedException;
        private readonly ManualResetEvent waitHandle;

        public AsyncResult(AsyncCallback callback, object asyncState)
        {
            this.callback = callback;
            this.asyncState = asyncState;
            waitHandle = new ManualResetEvent(false);
        }

        public object AsyncState
        {
            get { return asyncState; }
        }

        public ManualResetEvent AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get { return waitHandle; }
        }

        public bool CompletedSynchronously { get; protected internal set; }

        public bool IsCompleted
        {
            get { return isCompleted; }
            protected internal set { isCompleted = value; }
        }

        public Exception StoredException
        {
            get { return storedException; }
        }

        public void Complete()
        {
            Complete(storedException);
        }

        public void Complete(Exception ex)
        {
            storedException = ex;
            isCompleted = true;
            waitHandle.Set();

            if (callback != null)
                callback(this);
        }
    }
}
