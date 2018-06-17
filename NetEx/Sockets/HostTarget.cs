using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Host target definition.
    /// </summary>
    public class HostTarget {

        /// <summary>
        /// Gets host end point.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Gets the data used for authentication, remote host name or <see cref="X509Certificate"/>.
        /// </summary>
        public object AuthenticationData { get; }

        /// <summary>
        /// Creates host target using local port number and optional SSL data.
        /// </summary>
        /// <param name="port">Local port.</param>
        /// <param name="ssl">Remote host name, <see cref="X509Certificate"/> or null for no cryptography.</param>
        public HostTarget(int port, object ssl = null) {
            EndPoint = new IPEndPoint(IPAddress.Loopback, port);
            AuthenticationData = ssl;
        }

        /// <summary>
        /// Creates host target using remote IP and port with optional SSL data.
        /// </summary>
        /// <param name="ip">Remote IP.</param>
        /// <param name="port">Remote port.</param>
        /// <param name="ssl">Remote host name, <see cref="X509Certificate"/> or null for no cryptography.</param>
        public HostTarget(IPAddress ip, int port, object ssl = null) {
            EndPoint = new IPEndPoint(ip, port);
            AuthenticationData = ssl;
        }

        /// <summary>
        /// Creates host target using <see cref="IPEndPoint"/> with optional SSL data.
        /// </summary>
        /// <param name="endPoint">End point.</param>
        /// <param name="ssl">Remote host name, <see cref="X509Certificate"/> or null for no cryptography.</param>
        public HostTarget(IPEndPoint endPoint, object ssl = null) {
            EndPoint = endPoint;
            AuthenticationData = ssl;
        }

    }

}