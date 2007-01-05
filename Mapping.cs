//
// Mapping.cs
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



namespace Nat
{
    public struct Mapping
    {
        public ushort Port
        {
            get { return this.port; }
        }
        private ushort port;


        public Protocol Protocol
        {
            get { return this.protocol; }
        }
        private Protocol protocol;


        public Mapping(ushort port, Protocol protocol)
        {
            this.port = port;
            this.protocol = protocol;
        }
        
		public override bool Equals(object obj)
		{
            if (!(obj is Mapping))
                return false;

			Mapping instance = (Mapping)obj;
			return this.port == instance.port && this.protocol == instance.protocol;
		}
		
		public override int GetHashCode()
		{
			return this.port.GetHashCode() ^ this.protocol.GetHashCode();
		}

		public static bool operator ==(Mapping instanceA, Mapping instanceB) 
		{
			return instanceA.Equals(instanceB);
		}
		
		public static bool operator !=(Mapping instanceA, Mapping instanceB) 
		{
		  return !instanceA.Equals(instanceB);
		}
    }
}
