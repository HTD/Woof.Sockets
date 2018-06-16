using System;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Generic event arguments for transmitting messages.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class MessageEventArgs<T> : EventArgs {

        /// <summary>
        /// Gets or sets the message to be inspected / modified.
        /// </summary>
        public T Message { get; set; }

        /// <summary>
        /// Target session for remote messages received.
        /// </summary>
        public ActiveSession<T> TargetSession { get; private set; }

        /// <summary>
        /// Creates new event arguments containing a message received.
        /// </summary>
        /// <param name="message">Message received.</param>
        public MessageEventArgs(T message) => Message = message;

        /// <summary>
        /// Cretes new event arguments for target session containing a message received.
        /// </summary>
        /// <param name="targetSession"></param>
        /// <param name="message"></param>
        public MessageEventArgs(ActiveSession<T> targetSession, T message) {
            TargetSession = targetSession;
            Message = message;
        }

    }

}
