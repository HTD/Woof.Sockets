namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Message type for FastBinaryTransceiver.
    /// </summary>
    public class BinaryPacket {

        /// <summary>
        /// Gets message buffer reference.
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Gets message length.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Creates new binary message from buffer. Whole buffer length is considered the message length.
        /// </summary>
        /// <param name="buffer">Buffer reference.</param>
        public BinaryPacket(byte[] buffer) {
            Bytes = buffer;
            Length = buffer.Length;
        }

        /// <summary>
        /// Creates new binary message form buffer and message length.
        /// </summary>
        /// <param name="buffer">Buffer reference.</param>
        /// <param name="length">Message length.</param>
        public BinaryPacket(byte[] buffer, int length) {
            Bytes = buffer;
            Length = length;
        }

        /// <summary>
        /// Implicitly converts <see cref="BinaryPacket"/> to <see cref="byte[]"/>.
        /// </summary>
        /// <param name="m"></param>
        public static implicit operator byte[](BinaryPacket m) => m.Bytes;

    }

}
