using System;
using System.Collections.Generic;
using System.Linq;
using Woof.SystemEx;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Universal network message router.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class Router<T> : IDisposable {

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
        /// Occurs when an other server message is received, the one which is NOT routed to the client.
        /// </summary>
        public event EventHandler<MessageEventArgs<T>> OtherServerMessageReceived;

        /// <summary>
        /// Occurs when an exception is thrown during communication.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionThrown;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a default route for the new sessions.
        /// </summary>
        public int RouteDefault { get; set; }

        /// <summary>
        /// Gets the value indicating whether this instance is active (started).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets or sets the value indicating broadcast mode. In this mode messages from common will be transmitted to all targets.
        /// </summary>
        public bool IsBroadcast { get; set; }

        #endregion

        /// <summary>
        /// Common target host.
        /// </summary>
        private readonly HostTarget Common;

        /// <summary>
        /// Remote targets the new sessions will be connected to.
        /// </summary>
        private readonly HostTarget[] Targets;

        /// <summary>
        /// This server.
        /// </summary>
        private readonly ActiveEndPoint<T> RoutingServer;

        /// <summary>
        /// True if this instance has started stopping sequence.
        /// </summary>
        private bool IsStopping;

        /// <summary>
        /// True if this instance has started disposing sequence.
        /// </summary>
        private bool IsDisposing;

        /// <summary>
        /// Creates a new end point router from common target to multiple remote targets.
        /// </summary>
        /// <param name="common">Common end.</param>
        /// <param name="targets">Remote targets.</param>
        public Router(HostTarget common, params HostTarget[] targets) {
            Common = common;
            Targets = targets;
            RoutingServer = new ActiveEndPoint<T>(Common) { Index = -1 };
            RoutingServer.ExceptionThrown += AllTargets_ExceptionThrown;
            RoutingServer.SessionSpawned += RoutingServer_SessionSpawned;
        }

        /// <summary>
        /// Starts the routing server.
        /// </summary>
        public void Start() {
            RoutingServer.Listen();
            IsActive = true;
        }

        /// <summary>
        /// Stops the routing server. Does not dispose!
        /// </summary>
        public void Stop() {
            if (!IsActive || IsStopping) return;
            IsStopping = true;
            RoutingServer.Close();
            IsActive = false;
        }

        /// <summary>
        /// Disposing the routing server. If it's active, it will be stopped first.
        /// </summary>
        public void Dispose() {
            if (IsDisposing) return;
            IsDisposing = true;
            if (IsActive && !IsStopping) Stop();
            RoutingServer.Dispose();
        }

        /// <summary>
        /// Handles exceptions from all sessions.
        /// </summary>
        /// <param name="sender"><see cref="ActiveEndPoint{T}"/> instance.</param>
        /// <param name="e"></param>
        private void AllTargets_ExceptionThrown(object sender, ExceptionEventArgs e) => ExceptionThrown?.Invoke(sender, e);

        /// <summary>
        /// Handles this server sessions spawn.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void RoutingServer_SessionSpawned(object sender, EventArgs e) {
            var session = sender as ActiveSession<T>;
            session.End += Session_End;
            session.IsClientSide = true;
            session.ExceptionThrown += AllTargets_ExceptionThrown;
            session.MessageReceived += Session_MessageReceived;
            var servers = new List<ActiveEndPoint<T>>();
            for (int i = 0, n = Targets.Length; i < n; i++) {
                var remote = new ActiveEndPoint<T>(Targets[i], session) {
                    Index = i
                };
                remote.ExceptionThrown += AllTargets_ExceptionThrown;
                remote.MessageReceived += Remote_MessageReceived;
                remote.SessionClosed += Remote_SessionClosed;
                servers.Add(remote);
            }
            session.Remotes = servers.ToArray();
            session.Route = RouteDefault;
            session.IsBroadcast = IsBroadcast;
            SessionStarted?.Invoke(session, EventArgs.Empty);
            foreach (var server in session.Remotes) server.Connect();
            if (!session.Remotes.All(i => i.IsConnected)) session.Close();
        }
        /// <summary>
        /// Handles incoming messages from local sessions.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void Session_MessageReceived(object sender, EventArgs e) {
            var session = sender as ActiveSession<T>;
            if (session.Message == null || ((object)session.Message is X690.Message m && m.IsEndSession)) { // disconnect requested
                foreach (var remote in session.Remotes) remote.Close();
                return;
            }
            ClientMessageReceived?.Invoke(session, new MessageEventArgs<T>(session.Message));
            if (session.IsBroadcast) {
                foreach (var remote in session.Route == 0 ? session.Remotes : session.Remotes.Reverse()) {
                    var toRemoteMessageArgs = new BroadcastMessageArgs<T>(session.Message, remote.Index);
                    ClientBeforeSend?.Invoke(session, toRemoteMessageArgs);
                    if (toRemoteMessageArgs.Message != null) remote.Send(toRemoteMessageArgs.Message);
                }
                //if (session.Route == 1)
                //    for (int i = 0, n = session.Remotes.Length; i < n; i++) {
                //        var toRemoteMessageArgs = new BroadcastMessageArgs<T>(session.Message, session.Remotes[i].Index);
                //        ClientBeforeSend?.Invoke(session, toRemoteMessageArgs);
                //        if (toRemoteMessageArgs.Message != null) session.Remotes[i].Send(toRemoteMessageArgs.Message);
                //    }
                //else
                //    for (int i = session.Remotes.Length - 1; i >= 0; i--) {
                //        var toRemoteMessageArgs = new BroadcastMessageArgs<T>(session.Message, session.Remotes[i].Index);
                //        ClientBeforeSend?.Invoke(session, toRemoteMessageArgs);
                //        if (toRemoteMessageArgs.Message != null) session.Remotes[i].Send(toRemoteMessageArgs.Message);
                //    }
            }
            else {
                var toRemoteMessageArgs = new BroadcastMessageArgs<T>(session.Message, session.Route);
                ClientBeforeSend?.Invoke(session, toRemoteMessageArgs);
                if (toRemoteMessageArgs.Message != null) session.Remotes[session.Route].Send(toRemoteMessageArgs.Message);
            }
        }

        /// <summary>
        /// Handles incoming messages from remote sessions. 
        /// </summary>
        /// <param name="sender">Remote session.</param>
        /// <param name="e">Formal junk.</param>
        private void Remote_MessageReceived(object sender, EventArgs e) {
            var remoteSession = sender as ActiveSession<T>;
            var remoteHost = remoteSession.Owner;
            var commonSession = remoteHost.Origin;
            if (remoteSession.Message == null || ((object)remoteSession.Message is X690.Message m && m.IsEndSession)) {
                commonSession.Close();
                return;
            }
            if (remoteHost.Index == commonSession.Route) {
                if (ServerMessageReceived != null) {
                    var args = new MessageEventArgs<T>(remoteSession, remoteSession.Message);
                    ServerMessageReceived(commonSession, args);
                    if (args.Message != null) commonSession.Send(args.Message);
                }
                else commonSession.Send(remoteSession.Message);
            } else {
                if (OtherServerMessageReceived != null) {
                    var args = new MessageEventArgs<T>(remoteSession, remoteSession.Message);
                    OtherServerMessageReceived(commonSession, args);
                }
            }
        }

        /// <summary>
        /// Handles remote (server) session termination.
        /// </summary>
        /// <param name="sender">Remote session.</param>
        /// <param name="e">Formal junk.</param>
        private void Remote_SessionClosed(object sender, EventArgs e) {
            var remoteSession = sender as ActiveSession<T>;
            remoteSession.Owner.Origin.Close();
        }

        /// <summary>
        /// Handles session termination.
        /// </summary>
        /// <param name="sender">Local session.</param>
        /// <param name="e">Formal junk.</param>
        private void Session_End(object sender, EventArgs e) {
            foreach (var remote in (sender as ActiveSession<T>).Remotes) remote.Close();
            SessionClosed?.Invoke(sender, EventArgs.Empty);
        }

    }

}