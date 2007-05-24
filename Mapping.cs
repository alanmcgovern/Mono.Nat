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
    public class Mapping
    {
        #region Private Fields

        private int port;
        private Protocol protocol;

        #endregion Private Fields


        #region Properties

        public int Port
        {
            get { return this.port; }
        }

        public Protocol Protocol
        {
            get { return this.protocol; }
        }

        #endregion Properties


        #region Constructors

        public Mapping(int port, Protocol protocol)
        {
            this.port = port;
            this.protocol = protocol;
        }

        #endregion Constructors


        #region Methods

        public override bool Equals(object obj)
        {
            Mapping other = obj as Mapping;
            return other == null ? false : this.port == other.port
                                        && this.protocol == other.protocol;
        }
        
        public override int GetHashCode()
        {
            return this.port.GetHashCode() ^ this.protocol.GetHashCode();
        }

        #endregion Methods
    }
}
