using System;
using System.Collections.Generic;
using System.Text;
using Nat;
using System.Net;

namespace Nat
{
    public interface INatDevice
    {
        void CreatePortMap(Mapping mapping);
        void CreatePortMap(Mapping mapping, string portMapDescription);
        void DeletePortMap(Mapping mapping);
        Mapping[] GetAllMappings();
        IPAddress GetExternalIP();
		Mapping GetSpecificMapping(int port, Protocol protocol);
        
        IAsyncResult BeginCreatePortMap(Mapping mapping, AsyncCallback callback, object asyncState);
        IAsyncResult BeginCreatePortMap(Mapping mapping, string portMapDescription, AsyncCallback callback, object asyncState);
        IAsyncResult BeginDeletePortMap(Mapping mapping, AsyncCallback callback, object asyncState);
        IAsyncResult BeginGetAllMappings(AsyncCallback callback, object asyncState);
        IAsyncResult BeginGetExternalIP(AsyncCallback callback, object asyncState);
		IAsyncResult BeginGetSpecificMapping(int port, Protocol protocol, AsyncCallback callback, object asyncState);

        void EndCreatePortMap(IAsyncResult result);
        void EndDeletePortMap(IAsyncResult result);
        Mapping[] EndGetAllMappings(IAsyncResult result);
        IPAddress EndGetExternalIP(IAsyncResult result);
		Mapping EndGetSpecificMapping(IAsyncResult result);
        DateTime LastSeen { get; set; }
	}
}
