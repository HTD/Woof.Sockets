using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Tools for simplifying operations on sockets.
    /// </summary>
    public static class SocketTools {

        /// <summary>
        /// Polling interval for sockets in microseconds. Lower values give faster disconnection detection at the cost of higher CPU usage.
        /// </summary>
        const int PollingInterval = 1000;

        /// <summary>
        /// Gets a random free port to use on local machine.
        /// </summary>
        public static int LocalFreePort {
            get {
                using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    return (socket.LocalEndPoint as IPEndPoint).Port;
                }
            }
        }

        /// <summary>
        /// Sets the Keep-Alive options of TCP connection.
        /// </summary>
        /// <param name="instance">Socket instance.</param>
        /// <param name="keepaliveTime">The timeout, in milliseconds, with no activity until the first keep-alive packet is sent.</param>
        /// <param name="keepaliveInterval">The interval, in milliseconds, between when successive keep-alive packets are sent if no acknowledgement is received.</param>
        public static void SetTcpKeepAlive(this Socket instance, uint keepaliveTime, uint keepaliveInterval) {
            const uint onOff = 1;
            byte[] inOptionValues = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(keepaliveTime).CopyTo(inOptionValues, 4);
            BitConverter.GetBytes(keepaliveInterval).CopyTo(inOptionValues, 8);
            instance.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        /// <summary>
        /// Waits until socket is in selected state or cancelled.
        /// </summary>
        /// <param name="instance">Socket instance.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the socket entered the selected state. False if it's closed.</returns>
        public static bool WaitAvailable(this Socket instance, CancellationToken token) {
            while (!instance.Poll(PollingInterval, SelectMode.SelectRead) && !token.IsCancellationRequested) ;
            return !token.IsCancellationRequested && instance.Connected && instance.Available > 0;
        }

        /// <summary>
        /// Waits until socket is in selected state or cancelled.
        /// </summary>
        /// <param name="instance">Socket instance.</param>
        /// <param name="selectMode">One of <see cref="SelectMode"/> options.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the socket entered the selected state.</returns>
        public static async Task<bool> WaitAvailableAsync(this Socket instance, CancellationToken token) => await Task.Run(() => {
            while (!instance.Poll(PollingInterval, SelectMode.SelectRead) && !token.IsCancellationRequested) ;
            return !token.IsCancellationRequested && instance.Connected && instance.Available > 0;
        });

    }

}