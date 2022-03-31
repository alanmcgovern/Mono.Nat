using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mono.Nat
{
    class SocketGroup : IDisposable
    {
        public ICollection<UdpClient> Clients => Sockets.Keys;

        Dictionary<UdpClient, List<IPAddress>> Sockets { get; }
        SemaphoreSlim SocketSendLocker { get; }

        int DefaultPort { get; }

        public SocketGroup (Dictionary<UdpClient, List<IPAddress>> sockets, int defaultPort)
        {
            Sockets = sockets;
            DefaultPort = defaultPort;
            SocketSendLocker = new SemaphoreSlim (1, 1);
        }

        public void Dispose ()
        {
            foreach (var s in Sockets)
                s.Key.Dispose ();
        }

        public async Task SendAsync (byte[] buffer, IPAddress gatewayAddress, CancellationToken token)
        {
            using (await SocketSendLocker.EnterAsync (token)) {
                foreach (var keypair in Sockets) {
                    token.ThrowIfCancellationRequested ();
                    try {
                        if (gatewayAddress == null) {
                            foreach (var defaultGateway in keypair.Value)
                                await keypair.Key.SendAsync (buffer, buffer.Length, new IPEndPoint (defaultGateway, DefaultPort)).ConfigureAwait (false);
                        } else {
                            await keypair.Key.SendAsync (buffer, buffer.Length, new IPEndPoint (gatewayAddress, DefaultPort)).ConfigureAwait (false);
                        }
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception) {

                    }
                }
            }
        }
    }
}
