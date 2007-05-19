//
// PortMapAsyncResult.cs
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
using System.Threading;

namespace Nat
{
    internal class PortMapAsyncResult : IAsyncResult
    {
        public bool IsCompleted
        {
            get { return this.isCompleted; }
            internal set { this.isCompleted = value; }
        }
        private bool isCompleted;


        public bool CompletedSynchronously
        {
            get { return this.completedSynchronously; }
            internal set { this.completedSynchronously = value; }
        }
        private bool completedSynchronously;


        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get { return this.waitHandle; }
        }
        internal ManualResetEvent AsyncWaitHandle
        {
            get { return this.waitHandle; }
        }
        private ManualResetEvent waitHandle;


        public object AsyncState
        {
            get { return this.asyncState; }
        }
        private object asyncState;


        public AsyncCallback CompletionCallback
        {
            get { return this.completionCallback; }
        }
        private AsyncCallback completionCallback;

// TODO: Not used
//        internal bool UsingManHeader
//        {
//            get { return this.usingManHeader; }
//        }
//        private bool usingManHeader;


        internal WebRequest Request
        {
            get { return this.request; }
            set { this.request = value; }
        }
        private WebRequest request;


        internal MessageBase SavedMessage
        {
            get { return this.savedMessage; }
            set { this.savedMessage = value; }
        }
        private MessageBase savedMessage;


        protected PortMapAsyncResult(WebRequest request, AsyncCallback callback, object asyncState)
        {
            this.asyncState = asyncState;
            this.completionCallback = callback;
            this.request = request;
            this.waitHandle = new ManualResetEvent(false);
        }

        internal static PortMapAsyncResult Create(MessageBase message, WebRequest request, AsyncCallback storedCallback, object asyncState)
        {
            if (message is GetGenericPortMappingEntry || message is GetSpecificPortMappingEntryMessage)
                return new GetAllMappingsAsyncResult(request, storedCallback, asyncState);

            return new PortMapAsyncResult(request, storedCallback, asyncState);
        }
    }
}
