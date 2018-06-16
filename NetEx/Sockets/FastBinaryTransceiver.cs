using System.Collections.Generic;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Transceiver module for transmitting binary messages, with shared input buffer optimization.
    /// </summary>
    public class FastBinaryTransceiver : ITransceiver<BinaryPacket> {

        /// <summary>
        /// Session input buffer to save allocations on receive.
        /// </summary>
        private byte[] InputBuffer;

        /// <summary>
        /// Receives a message of binary data from the stream.
        /// </summary>
        /// <param name="stream">Readable network stream.</param>
        /// <returns>Pair of <see cref="ReceiveStatus"/> and received packet.</returns>
        public KeyValuePair<ReceiveStatus, BinaryPacket> Receive(NetworkStreamEx stream) {
            if (InputBuffer == null) InputBuffer = new byte[NetworkStreamEx.ReceiveBufferLength];
            var length = stream.Read(InputBuffer, 0, NetworkStreamEx.ReceiveBufferLength);
            if (length < 1) return new KeyValuePair<ReceiveStatus, BinaryPacket>(ReceiveStatus.Fail, null);
            return new KeyValuePair<ReceiveStatus, BinaryPacket>(ReceiveStatus.OverAndOut, new BinaryPacket(InputBuffer, length));
        }

        /// <summary>
        /// Sends a binary message to the stream.
        /// </summary>
        /// <param name="stream">Writeable network stream.</param>
        /// <param name="packet">Binary packet to send.</param>
        public void Transmit(NetworkStreamEx stream, BinaryPacket packet) => stream.Write(packet, 0, packet.Length);

    }

}