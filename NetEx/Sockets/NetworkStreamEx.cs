using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Extended netwok communication stream.
    /// </summary>
    public class NetworkStreamEx : Stream {

        /// <summary>
        /// Microseconds to wait for the socket to enter requested state.
        /// Defines minimal time slice the polling can be canceled.
        /// </summary>
        internal const int PollingInterval = 1024;

        /// <summary>
        /// Receive buffer size.
        /// </summary>
        public const int ReceiveBufferLength = 128 * 1024;

        /// <summary>
        /// If true keep alive values will be configured for all sockets.
        /// </summary>
        public const bool IsKeepAliveEnabled = true;

        /// <summary>
        /// The timeout, in milliseconds, with no activity until the first keep-alive packet is sent.
        /// </summary>
        public const uint KeepAliveTime = 14 * 60 * 1000;

        /// <summary>
        /// The interval, in milliseconds, between when successive keep-alive packets are sent if no acknowledgement is received.
        /// </summary>
        public const uint KeepAliveInterval = 7 * 60 * 1000;

        #region Properties

        /// <summary>
        /// Gets a <see cref="System.Boolean"/> value that indicates whether authentication was successful.
        /// </summary>
        public bool IsAuthenticated => (InnerStream is SslStream s) && s.IsAuthenticated;

        ///// <summary>
        ///// Gets a value that indicates whether data is available on <see cref="NetworkStreamEx"/> to be read.
        ///// </summary>
        //public bool IsDataAvailable => (InnerStream is NetworkStream n) ? n.DataAvailable : SslDataAvailable;

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        public int Available => (InnerStream is SslStream && BufferedLength > 0) ? BufferedLength : Socket.Available;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkStreamEx"/> for the specified <see cref="System.Net.Sockets.Socket"/> and optional authentication data.
        /// </summary>
        /// <param name="socket">The <see cref="System.Net.Sockets.Socket"/> that the <see cref="NetworkStreamEx"/> will use to send and receive data.</param>
        /// <param name="ssl">Target host name to authenticate as client, server certificate to authenticate as sever, null for no authentication.</param>
        /// <remarks>To actually authenticate the stream <see cref="Authenticate"/> mehtod must be called explicitely.</remarks>
        public NetworkStreamEx(Socket socket, object ssl = null) {
            Socket = socket;
            Socket.ReceiveBufferSize = ReceiveBufferLength;
            if (IsKeepAliveEnabled) {
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                Socket.SetTcpKeepAlive(KeepAliveTime, KeepAliveInterval);
            }
            AuthenticationData = ssl;
            InnerStream = new NetworkStream(Socket, true);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authenticates the stream if target host name or server certificate was provided when the <see cref="NetworkStreamEx"/> was created.
        /// </summary>
        public void Authenticate() {
            if (AuthenticationData is X509Certificate certificate) {
                InnerNetworkStream = InnerStream as NetworkStream;
                InnerStream = new SslStream(InnerNetworkStream, false);
                (InnerStream as SslStream).AuthenticateAsServer(certificate);
            }
            else if (AuthenticationData is string targetHost) {
                InnerNetworkStream = InnerStream as NetworkStream;
                InnerStream = new SslStream(InnerNetworkStream, false);
                (InnerStream as SslStream).AuthenticateAsClient(targetHost);
            }
        }

        /// <summary>
        /// Blocks the current thread until this stream has data available for reading or the token is canceled.
        /// </summary>
        /// <param name="token">The object that allows this operation to be canceled.</param>
        /// <returns>True if data is available to be read, false if canceled.</returns>
        public bool WaitDataAvailable(CancellationToken token) {
            if (BufferedLength > 0 || Socket.Available > 0) return !token.IsCancellationRequested && Socket.Connected;
            while (!token.IsCancellationRequested && Socket.Available < 1 && Socket.Connected) {
                if (Socket.Poll(PollingInterval, SelectMode.SelectRead) && Socket.Available < 1) return false;
                Thread.Sleep(1);
            }
            return !token.IsCancellationRequested && Socket.Connected;
        }

        /// <summary>
        /// Disposes all disposable data for this stream.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged.</param>
        protected override void Dispose(bool disposing) {
            InnerStream?.Dispose();
            InnerNetworkStream?.Dispose();
            Socket.Dispose();
            base.Dispose(disposing);
        }

        #endregion

        #region Private data

        

        /// <summary>
        /// Berkeley <see cref="System.Net.Sockets.Socket"/> this stream is connected to.
        /// </summary>
        private readonly Socket Socket;

        /// <summary>
        /// Server <see cref="X509Certificate"/> or target host name.
        /// </summary>
        private readonly object AuthenticationData;

        /// <summary>
        /// Either a <see cref="NetworkStream"/> or a <see cref="SslStream"/>.
        /// </summary>
        private Stream InnerStream;

        /// <summary>
        /// Inner <see cref="NetworkStream"/> reference for the <see cref="SslStream"/>.
        /// </summary>
        private NetworkStream InnerNetworkStream;

        /// <summary>
        /// Buffer used to read SSL streams in chunks.
        /// </summary>
        private readonly byte[] ReceiveBuffer = new byte[ReceiveBufferLength];

        /// <summary>
        /// Current receive buffer offset.
        /// </summary>
        private int Offset = 0;

        /// <summary>
        /// Number of bytes read to receive buffer, but not yet read from it.
        /// </summary>
        private int BufferedLength = 0;

        #endregion

        #region Stream abstract class implementation

        /// <summary>
        /// Gets a value indicating whether the inner stream supports reading.
        /// </summary>
        public override bool CanRead => InnerStream.CanRead;

        /// <summary>
        /// Gets a value indicating whether the inner stream supports seeking.
        /// </summary>
        public override bool CanSeek => InnerStream.CanSeek;

        /// <summary>
        /// Gets a value indicating whether the inner stream supports writing.
        /// </summary>
        public override bool CanWrite => InnerStream.CanWrite;

        /// <summary>
        /// Gets the length in bytes of the inner stream.
        /// </summary>
        public override long Length => InnerStream.Length;

        /// <summary>
        /// Gets or sets the position within the inner stream.
        /// </summary>
        public override long Position { get => InnerStream.Position; set => InnerStream.Position = value; }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush() => InnerStream.Flush();

        /// <summary>
        /// Reads a sequence of bytes from the inner stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            if (BufferedLength < 1 && Socket.Available > 0)
                BufferedLength = InnerStream.Read(ReceiveBuffer, Offset = 0, ReceiveBufferLength);
            var read = count < BufferedLength ? count : BufferedLength;
            if (read < 1) return read;
            Buffer.BlockCopy(ReceiveBuffer, Offset, buffer, offset, read);
            BufferedLength -= read;
            Offset += read;
            return read;
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset, origin);

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value) => InnerStream.SetLength(value);

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count) => InnerStream.Write(buffer, offset, count);

        #endregion

    }

}