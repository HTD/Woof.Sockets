using System.Collections.Generic;
using System.IO;

namespace Woof.NetEx.Sockets {

    /// <summary>
    /// Transceiver module for transmitting X.690 messages.
    /// </summary>
    public class X690Transceiver : ITransceiver<X690.Message> {

        /// <summary>
        /// Incomplete message cache.
        /// </summary>
        private X690.Message MessageCompleted;

        /// <summary>
        /// Input buffer allocated on first couple of reads to fit maximum message length.
        /// </summary>
        private byte[] InputBuffer;

        /// <summary>
        /// Output buffer allocated on first couple of writes to fit maximum message length.
        /// </summary>
        private byte[] OutputBuffer;

        /// <summary>
        /// Receives a X.690 message from the network stream.
        /// </summary>
        /// <param name="stream">Readable network stream.</param>
        /// <returns>Pair of <see cref="ReceiveStatus"/> and received message.</returns>
        public KeyValuePair<ReceiveStatus, X690.Message> Receive(NetworkStreamEx stream) {
            if (InputBuffer == null) InputBuffer = new byte[NetworkStreamEx.ReceiveBufferLength];
            if (MessageCompleted != null) {
                MessageCompleted.ReadBufferedContinue(stream, InputBuffer);
                if (!MessageCompleted.IsIncomplete) {
                    try {
                        return new KeyValuePair<ReceiveStatus, X690.Message>(ReceiveStatus.OverAndOut, MessageCompleted);
                    } finally {
                        MessageCompleted = null;
                    }
                }
                return new KeyValuePair<ReceiveStatus, X690.Message>(ReceiveStatus.Over, MessageCompleted);
            }
            var message = X690.Message.ReadBuffered(stream, InputBuffer);
            if (message == null) return new KeyValuePair<ReceiveStatus, X690.Message>(ReceiveStatus.Fail, null);
            if (message.IsIncomplete) {
                MessageCompleted = message;
                return new KeyValuePair<ReceiveStatus, X690.Message>(ReceiveStatus.Over, MessageCompleted);
            }
            return new KeyValuePair<ReceiveStatus, X690.Message>(ReceiveStatus.OverAndOut, message);
        }

        /// <summary>
        /// Sends a X.690 message to the stream.
        /// </summary>
        /// <param name="stream">Writeable stream.</param>
        /// <param name="message">X.690 message to send.</param>
        public void Transmit(NetworkStreamEx stream, X690.Message message) {
            if (message == null) return;
            if (OutputBuffer == null || OutputBuffer.Length < message.Data.Header.MessageLength)
                OutputBuffer = new byte[message.Data.Header.MessageLength];
            message.WriteBuffered(stream, OutputBuffer);
        }

    }

}