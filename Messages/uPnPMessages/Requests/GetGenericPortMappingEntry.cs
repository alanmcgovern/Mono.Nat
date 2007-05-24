using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Nat
{
    internal class GetGenericPortMappingEntry : MessageBase
    {
        private int index;

        public GetGenericPortMappingEntry(int index, UpnpNatDevice device)
            :base(device)
        {
            this.index = index;
        }

        public override System.Net.WebRequest Encode()
        {
            StringBuilder sb = new StringBuilder(128);
            XmlWriter writer = CreateWriter(sb);

            WriteFullElement(writer, "NewPortMappingIndex", index.ToString());

            writer.Flush();
            return CreateRequest("GetGenericPortMappingEntry", sb.ToString(), "POST");
        }
    }
}
