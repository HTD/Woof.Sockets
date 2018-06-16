using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Woof.SystemEx;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Universal active end point capable of receiving and sending messages of a supported type.
    /// This kind of end point can be both client and server.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class ActiveEndPoint<T> : IDisposable {

        #region Events

        /// <summary>
        /// Occurs when this endpoint has received a message from any session.
        /// </summary>
        public event EventHandler MessageReceived;

        /// <summary>
        /// Occurs when a connection is made. A session object is passed as sender.
        /// </summary>
        public event EventHandler SessionSpawned;

        /// <summary>
        /// Occurs when a session is closed.
        /// </summary>
        public event EventHandler SessionClosed;

        /// <summary>
        /// Occurs when exception is thrown during communication.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionThrown;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the protocol type used.
        /// </summary>
        public ProtocolType ProtocolType { get; }

        /// <summary>
        /// Host definition associated with this active end point.
        /// </summary>
        public HostTarget Host { get; }

        /// <summary>
        /// Gets or sets general purpose index for multi-endpoint configurations.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a server.
        /// </summary>
        public bool IsServer { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a client and it's connected to the server.
        /// </summary>
        public bool IsConnected { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a server and is currently listening to incoming connections.
        /// </summary>
        public bool IsListening { get; internal set; }


        /// <summary>
        /// Gets the server session if client. 
        /// </summary>
        public ActiveSession<T> ClientSession => Connector != null ? Sessions.FirstOrDefault().Value : null;

        /// <summary>
        /// Gets or sets the endpoint connect timeout in milliseconds (default 5000).
        /// </summary>
        public int ConnectTimeout { get; set; } = 5000;

        #endregion

        #region State and flags

        /// <summary>
        /// Active sessions (many if server, single if client).
        /// </summary>
        protected readonly ConcurrentDictionary<int, ActiveSession<T>> Sessions = new ConcurrentDictionary<int, ActiveSession<T>>();

        /// <summary>
        /// Originating session (if started from sesion).
        /// </summary>
        internal ActiveSession<T> Origin;

        /// <summary>
        /// Listener socket (if server).
        /// </summary>
        private Socket Listener;

        /// <summary>
        /// Connector socket (if client).
        /// </summary>
        private Socket Connector;

        /// <summary>
        /// True if closing procedure is started.
        /// </summary>
        private bool IsClosing;

        /// <summary>
        /// True if dispose procedure is started.
        /// </summary>
        private bool IsDisposing;

        /// <summary>
        /// Session counter.
        /// </summary>
        private int SessionId;

        #endregion

        /// <summary>
        /// Creates client or server instance for specified host target.
        /// </summary>
        /// <param name="host">Host target.</param>
        /// <param name="protocolType">TCP (default), or UDP (not tested).</param>
        public ActiveEndPoint(HostTarget host, ProtocolType protocolType = ProtocolType.Tcp) => Host = host;

        /// <summary>
        /// Creates a remote host for a session instance.
        /// </summary>
        /// <param name="host">Remote host.</param>
        /// <param name="origin">Originating session.</param>
        /// <param name="protocolType">TCP (default), or UDP (not tested).</param>
        public ActiveEndPoint(HostTarget host, ActiveSession<T> origin, ProtocolType protocolType = ProtocolType.Tcp) {
            Host = host;
            Origin = origin;
        }

        /// <summary>
        /// Starts listening as server.
        /// ASYNCHRONOUSLY. Does not block the thread.
        /// </summary>
        public bool Listen() {
            IsServer = true;
            try {
                Listener = new Socket(Host.EndPoint.AddressFamily, SocketType.Stream, ProtocolType);
                Listener.Bind(Host.EndPoint);
                Listener.Listen(128);
                (new Task(AcceptLoop, null, TaskCreationOptions.LongRunning)).Start();
                return true;
            } catch (Exception x) {
                var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
                return false;
            }
        }

        /// <summary>
        /// Connects as client.
        /// ASYNCHRONOUSLY. Does not block the thread.
        /// </summary>
        public bool Connect() {
            Connector = new Socket(Host.EndPoint.AddressFamily, SocketType.Stream, ProtocolType);
            try {
                const string timeoutMessage = "ActiveEndPoint connect timed out.";
                var connectAsyncResult = Connector.BeginConnect(Host.EndPoint, null, null);
                connectAsyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout, true);
                if (Connector.Connected) {
                    Connector.EndConnect(connectAsyncResult);
                }
                else {
                    Connector.Close();
                    throw new TimeoutException(timeoutMessage);
                }
                SessionStart(Connector);
                var deadline = Environment.TickCount + ConnectTimeout;
                while (!IsConnected) {
                    if (Environment.TickCount > deadline) throw new TimeoutException(timeoutMessage);
                    Thread.Sleep(1);
                }
                return true;
            } catch (Exception x) {
                if (IsClosing || IsDisposing) return false;
                var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
                return false;
            }
        }

        /// <summary>
        /// Waits for incomming connections.
        /// </summary>
        /// <param name="state"></param>
        private void AcceptLoop(object state) {
            while (!IsClosing && !IsDisposing && Listener.IsBound)
                try {
                    SessionStart(Listener.Accept());
                } catch (Exception x) {
                    if (IsClosing || IsDisposing) return;
                    var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                    if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                    ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
                }
        }

        /// <summary>
        /// Creates session,
        /// assigns a new id for it, attaches <see cref="MessageReceived"/> event,
        /// adds to session collection,
        /// returns the session.
        /// </summary>
        /// <param name="socket">A socket to create session for.</param>
        /// <returns>Added session with id set, not yet run.</returns>
        private ActiveSession<T> SessionGet(Socket socket) {
            var id = ++SessionId;
            var session = new ActiveSession<T>(this, socket, id);
            session.MessageReceived += (sender, e) => {
                try { // workaround for missing exception events when exceptions are thrown in MessageReceived handler:
                    MessageReceived?.Invoke(sender, e);
                }
                catch (Exception x) { // this is not actually caught elsewhere...
                    var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                    if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                    ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
                    throw; // ...or is it? still closes the connection correctly.
                }
            };
            Sessions.TryAdd(id, session);
            return session;
        }

        /// <summary>
        /// Starts the session loop,
        /// closes the session when ended,
        /// removes the ended session from session collection.
        /// </summary>
        /// <param name="box">A state box containing the session.</param>
        private void SessionLoop(object box) {
            var session = box as ActiveSession<T>;
            SessionSpawned?.Invoke(session, EventArgs.Empty);
            try {
                session.Loop();
            } catch (Exception x) {
                var source = $"{GetType().Name}.{MethodBase.GetCurrentMethod().Name}";
                if (String.IsNullOrEmpty(x.Source)) x.Source = source; else x.Source += $"; {source}";
                ExceptionThrown?.Invoke(this, new ExceptionEventArgs(x));
            }
            if (IsConnected && Connector != null) {
                Connector.Dispose();
                Connector = null;
                IsConnected = false;
            }
            SessionClosed?.Invoke(session, EventArgs.Empty);
            if (Sessions.TryRemove(session.Id, out session)) session.Close();
        }

        /// <summary>
        /// Starts the session, exists when it's started.
        /// Does not block the thread.
        /// </summary>
        /// <param name="socket">A socket to create session for.</param>
        private void SessionStart(Socket socket) => (new Task(SessionLoop, SessionGet(socket), TaskCreationOptions.LongRunning)).Start();

        /// <summary>
        /// If client - just sends the message to the server.
        /// If server - broadcasts the message to all active sessions.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void Send(T message) {
            foreach (var session in Sessions) session.Value.Send(message);
        }

        /// <summary>
        /// Closes the session, disposes the resources.
        /// </summary>
        public void Close() {
            if (IsClosing) return;
            IsListening = false;
            IsConnected = false;
            IsClosing = true;
            foreach (var session in Sessions) session.Value.Close();
            Dispose();
        }

        /// <summary>
        /// Disposes all used resources.
        /// </summary>
        public void Dispose() {
            if (IsDisposing) return;
            IsDisposing = true;
            if (Sessions.Any()) Close();
            Listener?.Dispose();
            Connector?.Dispose();
        }

    }

}