using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Nat
{
    internal class GetAllMappingsAsyncResult : PortMapAsyncResult
    {
        private List<Mapping> mappings;
        private Mapping specificMapping;

        public List<Mapping> Mappings
        {
            get { return this.mappings; }
        }
        public Mapping SpecificMapping
        {
            get { return this.specificMapping; }
            set { this.specificMapping = value; }
        }

        public GetAllMappingsAsyncResult(WebRequest request, AsyncCallback callback, object asyncState)
            : base(request, callback, asyncState)
        {
            mappings = new List<Mapping>();
        }
    }
}
