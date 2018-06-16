using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using Woof.SystemEx;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// A session object for <see cref="ActiveEndPoint{T}"/>.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class ActiveSession<T> : IDisposable {

        #region Events

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler MessageReceived;

        /// <summary>
        /// Occurs when the session is closed either explicitly or by remote end point.
        /// </summary>
        public event EventHandler End;

        /// <summary>
        /// Occurs when exception is thrown during communication.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionThrown;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets or sets the global session identifier used to distinguish this instance among sessions from multiple end points.
        /// </summary>
        public int GlobalId { get; set; }

        /// <summary>
        /// Gets or sets the route for multiple end point configurations.
        /// </summary>
        public int Route { get; set; }

        /// <summary>
        /// Gets the remote port associated with owner end point.
        /// </summary>
        public int Port => Owner.Host.EndPoint.Port;

        /// <summary>
        /// Gets the routed server session (if started).
        /// </summary>
        public ActiveSession<T> ServerSessionA => Remotes[Route].ClientSession;

        /// <summary>
        /// Gets the other server session (if started).
        /// </summary>
        public ActiveSession<T> ServerSessionB => Remotes[Route == 0 ? 1 : 0].ClientSession;

        /// <summary>
        /// Gets or sets a value indicating if broadcasting is enabled for this session.
        /// </summary>
        public bool IsBroadcast { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the session is established at the client side.
        /// CAUTION: This is set outside!
        /// </summary>
        public bool IsClientSide { get; set; }

        /// <summary>
        /// Gets the message received.
        /// Use with <see cref="MessageReceived"/> event.
        /// </summary>
        public T Message { get; private set; }

        /// <summary>
        /// Gets the local IP of the session socket.
        /// </summary>
        public IPAddress LocalIP => (Socket.LocalEndPoint as IPEndPoint).Address;

        /// <summary>
        /// Gets the remote IP of the session socket.
        /// </summary>
        public IPAddress RemoteIP => (Socket.RemoteEndPoint as IPEndPoint).Address;

        #endregion

        #region State and flags

        /// <summary>
        /// Owner active endpoint that spawned the session.
        /// </summary>
        public readonly ActiveEndPoint<T> Owner;

        /// <summary>
        /// Optional remote servers for proxies and alike.
        /// </summary>
        public ActiveEndPoint<T>[] Remotes;

        /// <summary>
        /// Transceiver module passed from end point.
        /// </summary>
        protected readonly ITransceiver<T> Transceiver;

        /// <summary>
        /// Associated socket.
        /// </summary>
        private readonly Socket Socket;

        /// <summary>
        /// Associated stream.
        /// </summary>
        private readonly NetworkStreamEx Stream;

        /// <summary>
        /// Cancellation source.
        /// </summary>
        private readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

        /// <summary>
        /// True if the session is active, within receiving loop.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// True if closing procedure has been initiated.
        /// </summary>
        private bool IsClosing;

        /// <summary>
        /// True if disposing procedure has ben initiated.
        /// </summary>
        private bool IsDisposing;

        #endregion

        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <param name="owner">Owner end point.</param>
        /// <param name="socket">Session socket.</param>
        /// <param name="id">Session identifier.</param>
        public ActiveSession(ActiveEndPoint<T> owner, Socket socket, int id) {
            Owner = owner;
            Id = id;
            Socket = socket;
            Stream = new NetworkStreamEx(socket, Owner.Host.AuthenticationData);
            Stream.Authenticate(); // TODO: make authentication optional for StartTLS support
            if (typeof(T) == typeof(byte[])) Transceiver = new BinaryTransceiver() as ITransceiver<T>;
            else if (typeof(T) == typeof(BinaryPacket)) Transceiver = new FastBinaryTransceiver() as ITransceiver<T>;
            else if (typeof(T) == typeof(X690.Message)) Transceiver = new X690Transceiver() as ITransceiver<T>;
            else if (typeof(T) == typeof(string)) Transceiver = new StringTransceiver() as ITransceiver<T>;
            else throw new InvalidDataException("Unsupported message type.");
        }

        /// <summary>
        /// Unique identifier for the managed thread that started the session.
        /// </summary>
        private int WorkerThreadId;

        /// <summary>
        /// Starts the session receiving loop. This exits when closed explicitly or by remote end point.
        /// </summary>
        public void Loop() {
            if (IsClosing || IsDisposing) return;
            var token = Cancellation.Token;
            WorkerThreadId = Thread.CurrentThread.ManagedThreadId;
            IsActive = true;
            if (Owner.IsServer) Owner.IsListening = true; else Owner.IsConnected = true;
            while (!token.IsCancellationRequested && Stream.WaitDataAvailable(token)) {
                try {
                    var received = Transceiver.Receive(Stream);
                    if (token.IsCancellationRequested) break;
                    switch (received.Key) {
                        case ReceiveStatus.Over: continue;
                        case ReceiveStatus.OverAndOut:
                            Message = received.Value;
                            MessageReceived?.Invoke(this, EventArgs.Empty);
                            if (((object)Message is X690.Message m) && m.IsEndSession) break;
                            continue;
                    }
                    break;
                }
                catch (Exception x) {
                    var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                    if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                    ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
                    break;
                }
            }
            IsActive = false;
            End?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sends a message to the session.
        /// </summary>
        /// <param name="message"></param>
        public void Send(T message) {
            try {
                Transceiver.Transmit(Stream, message);
            }
            catch (Exception x) {
                var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
            }
        }

        /// <summary>
        /// Closes the session by breaking the receiving loop. Disposes the object.
        /// </summary>
        public void Close() {
            if (IsClosing) return;
            IsClosing = true;
            if (IsActive) {
                Cancellation.Cancel();
                if (Thread.CurrentThread.ManagedThreadId != WorkerThreadId) while (IsActive) Thread.Sleep(1);
            }
            if (!IsDisposing) Dispose();
        }

        /// <summary>
        /// Disposes all used resources.
        /// </summary>
        public void Dispose() {
            if (IsActive) Close();
            if (IsDisposing) return;
            IsDisposing = true;
            Stream?.Dispose();
            Cancellation.Dispose();
        }

    }

}