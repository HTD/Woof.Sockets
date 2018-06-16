using System.Collections.Generic;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Transceiver module interface.
    /// </summary>
    /// <typeparam name="T">Packet type.</typeparam>
    public interface ITransceiver<T> {

        /// <summary>
        /// Receives message of type <see cref="T"/> from the stream.
        /// </summary>
        /// <param name="stream">Readable network stream.</param>
        /// <returns>Pair of <see cref="ReceiveStatus"/> and received packet.</returns>
        KeyValuePair<ReceiveStatus, T> Receive(NetworkStreamEx stream);

        /// <summary>
        /// Sends a message of type <see cref="T"/> to the stream.
        /// </summary>
        /// <param name="stream">Writeable stream.</param>
        /// <param name="packet">Packet to send.</param>
        void Transmit(NetworkStreamEx stream, T packet);

    }

    /// <summary>
    /// Receive status.
    /// </summary>
    public enum ReceiveStatus {

        /// <summary>
        /// A part of the message is received, wait for the next part.
        /// </summary>
        Over,

        /// <summary>
        /// A complete message is received, wait for the next message.
        /// </summary>
        OverAndOut,

        /// <summary>
        /// Receive failed. Disconnect.
        /// </summary>
        Fail,

    }

}