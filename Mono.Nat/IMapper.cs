using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Mono.Nat
{
    internal interface IMapper
    {
        void Map();
        void Handle(byte[] response, IPEndPoint endpoint);
    }
}
