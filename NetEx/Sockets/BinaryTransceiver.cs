using System;
using System.Collections.Generic;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Transceiver module for transmitting binary packets.
    /// </summary>
    public class BinaryTransceiver : ITransceiver<byte[]> {

        /// <summary>
        /// Session input buffer to save allocations on receive.
        /// </summary>
        private byte[] InputBuffer;

        /// <summary>
        /// Receives a message of binary data from the stream.
        /// </summary>
        /// <param name="stream">Readable network stream.</param>
        /// <returns>Pair of <see cref="ReceiveStatus"/> and received packet.</returns>
        public KeyValuePair<ReceiveStatus, byte[]> Receive(NetworkStreamEx stream) {
            if (InputBuffer == null) InputBuffer = new byte[NetworkStreamEx.ReceiveBufferLength];
            var length = stream.Read(InputBuffer, 0, NetworkStreamEx.ReceiveBufferLength);
            if (length < 1) return new KeyValuePair<ReceiveStatus, byte[]>(ReceiveStatus.Fail, null);
            var packet = new byte[length];
            Buffer.BlockCopy(InputBuffer, 0, packet, 0, length);
            return new KeyValuePair<ReceiveStatus, byte[]>(ReceiveStatus.OverAndOut, packet);
        }

        /// <summary>
        /// Sends a binary message to the stream.
        /// </summary>
        /// <param name="stream">Writeable network stream.</param>
        /// <param name="packet">Binary packet to send.</param>
        public void Transmit(NetworkStreamEx stream, byte[] packet) => stream.Write(packet, 0, packet.Length);

    }

}