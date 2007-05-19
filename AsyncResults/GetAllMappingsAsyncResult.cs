using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Nat
{
    internal class GetAllMappingsAsyncResult : PortMapAsyncResult
    {
        private List<Mapping> mappings;

        public List<Mapping> Mappings
        {
            get { return this.mappings; }
        }

        public GetAllMappingsAsyncResult(WebRequest request, AsyncCallback callback, object asyncState)
            : base(request, callback, asyncState)
        {
            mappings = new List<Mapping>();
        }
    }
}
