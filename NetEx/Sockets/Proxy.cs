using System;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Universal network message proxy.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class Proxy<T> : IDisposable {

        #region Events

        /// <summary>
        /// Occurs when a new local session is started.
        /// </summary>
        public event EventHandler SessionStarted;

        /// <summary>
        /// Occurs when a local session is closed.
        /// </summary>
        public event EventHandler SessionClosed;

        /// <summary>
        /// Occurs when a client message is received, before sending it to host designated by the session Route property.
        /// </summary>
        public event EventHandler<MessageEventArgs<T>> ClientMessageReceived;

        /// <summary>
        /// Occurs directly after <see cref="ClientMessageReceived"/> before the message is sent to each broadcast or selected target.
        /// </summary>
        public event EventHandler<BroadcastMessageArgs<T>> ClientBeforeSend;

        /// <summary>
        /// Occurs when a server message is received, before sending it to the local session.
        /// </summary>
        public event EventHandler<MessageEventArgs<T>> ServerMessageReceived;

        /// <summary>
        /// Occurs when an exception is thrown during communication.
        /// </summary>
        public event EventHandler ExceptionThrown;

        #endregion

        /// <summary>
        /// Gets the value indicating if this instance is active (started).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Target A
        /// </summary>
        private readonly HostTarget A;

        /// <summary>
        /// Target B
        /// </summary>
        private readonly HostTarget B;

        /// <summary>
        /// Proxy server instance.
        /// </summary>
        private readonly ActiveEndPoint<T> ProxyServer;

        /// <summary>
        /// True if this instance has started stopping sequence.
        /// </summary>
        private bool IsStopping;

        /// <summary>
        /// True if this instance has started disposing sequence.
        /// </summary>
        private bool IsDisposing;

        /// <summary>
        /// Creates a new proxy between 2 targets.
        /// </summary>
        /// <param name="a">Point A.</param>
        /// <param name="b">Point B.</param>
        public Proxy(HostTarget a, HostTarget b) {
            A = a;
            B = b;
            ProxyServer = new ActiveEndPoint<T>(B);
            ProxyServer.SessionSpawned += ProxyServer_SessionSpawned;
        }

        /// <summary>
        /// Stars the proxy.
        /// </summary>
        public void Start() {
            ProxyServer.Listen();
            IsActive = true;
        }

        /// <summary>
        /// Stops the proxy. Doees not dispose!
        /// </summary>
        public void Stop() {
            if (!IsActive || IsStopping) return;
            IsStopping = true;
            ProxyServer.Close();
            IsActive = false;
        }

        /// <summary>
        /// Disposing the proxy. If it's active, it will be stopped first.
        /// </summary>
        public void Dispose() {
            if (IsDisposing) return;
            IsDisposing = true;
            if (IsActive && !IsStopping) Stop();
            ProxyServer.Dispose();
        }

        /// <summary>
        /// Handles this server sessions spawn.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void ProxyServer_SessionSpawned(object sender, EventArgs e) {
            var session = sender as ActiveSession<T>;
            session.End += Session_End;
            session.IsClientSide = true;
            session.ExceptionThrown += Session_ExceptionThrown;
            session.MessageReceived += Session_MessageReceived;
            var remote = new ActiveEndPoint<T>(A, session);
            remote.MessageReceived += Remote_MessageReceived;
            remote.ExceptionThrown += Session_ExceptionThrown;
            session.Remotes = new[] { remote };
            SessionStarted?.Invoke(this, EventArgs.Empty);
            remote.Connect();
            if (!remote.IsConnected) session.Close();
        }

        /// <summary>
        /// Handles exceptions from all sessions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Session_ExceptionThrown(object sender, SystemEx.ExceptionEventArgs e) => ExceptionThrown?.Invoke(sender, e);

        /// <summary>
        /// Handles incoming messages from local sessions.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void Session_MessageReceived(object sender, EventArgs e) {
            var session = sender as ActiveSession<T>;
            if (session.Message == null || (object)session.Message is X690.Message m && m.IsEndSession) {
                session.Remotes[0].Close();
                return;
            }
            ClientMessageReceived?.Invoke(sender, new MessageEventArgs<T>(session, session.Message));
            ClientBeforeSend?.Invoke(session, new BroadcastMessageArgs<T>(session.Message, 0));
            if (session.Remotes[0].IsConnected) session.Remotes[0].Send(session.Message);
        }

        /// <summary>
        /// Handles incoming messages from remote sessions. 
        /// </summary>
        /// <param name="sender">Remote session.</param>
        /// <param name="e">Formal junk.</param>
        private void Remote_MessageReceived(object sender, EventArgs e) {
            var remote = sender as ActiveSession<T>;
            var session = remote.Owner.Origin;
            if (remote.Message == null || ((object)remote.Message is X690.Message m && m.IsEndSession)) { session.Close(); return; }
            ServerMessageReceived?.Invoke(session, new MessageEventArgs<T>(remote, remote.Message));
            if (session.IsActive) session.Send(remote.Message);
        }

        /// <summary>
        /// Handles session termination.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void Session_End(object sender, EventArgs e) {
            (sender as ActiveSession<T>).Remotes[0].Close();
            SessionClosed?.Invoke(sender, EventArgs.Empty);
        }

    }

}