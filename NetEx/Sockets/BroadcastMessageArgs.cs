namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Event arguments for transmitting messages to one of designated targets.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class BroadcastMessageArgs<T> : MessageEventArgs<T> {

        /// <summary>
        /// Gets the designated target route.
        /// </summary>
        public int Target { get; }

        /// <summary>
        /// Creates new event arguments for transmitting a message to one of designated targets.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="target">Designated target route.</param>
        public BroadcastMessageArgs(T message, int target) : base(message) => Target = target;

    }

}