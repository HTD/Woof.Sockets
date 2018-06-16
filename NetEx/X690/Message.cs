using System;
using System.IO;
using Woof.NetEx.Sockets;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Contains X.690 encoded message or raw data message.
        /// This allows codec to be an optional part.
        /// </summary>
        public class Message {

            /// <summary>
            /// Gets a value indicating this message is incomplete.
            /// </summary>
            public bool IsIncomplete { get; private set; }

            /// <summary>
            /// Gets a value indicating this message is continued from an incomplete message.
            /// </summary>
            public bool IsContinued { get; private set; }

            /// <summary>
            /// Gets the message identifier.
            /// </summary>
            public long Id { get; private set; }

            /// <summary>
            /// Gets the message length.
            /// </summary>
            public int Length { get; private set; }

            /// <summary>
            /// Gets a value indication whether this message is an end session message.
            /// </summary>
            public bool IsEndSession { get; private set; }

            /// <summary>
            /// Gets or sets X.690 message payload.
            /// </summary>
            public Node Data { get; set; }
            
            /// <summary>
            /// Gets a header of the message.
            /// </summary>
            public Header Header { get; }

            /// <summary>
            /// A buffer used to complete the message from incomplete reads.
            /// </summary>
            private byte[] CompletionBuffer;

            /// <summary>
            /// Creates a new complete message from X.690 node.
            /// </summary>
            /// <param name="node">X.690 node.</param>
            public Message(Node node) {
                IsIncomplete = false;
                Header = node.Header;
                Data = node;
                Length = Data.Header.Length + Data.Header.PayloadLength;
                if (Data is Sequence seq && seq?.First is Integer dataId) Id = dataId; else Id = -1;
                IsEndSession = Data.IsEndSessionMessage;
            }

            /// <summary>
            /// Creates a new incomplete message from X.690 header and incomplete buffer.
            /// </summary>
            /// <param name="header">Header read.</param>
            /// <param name="buffer">Incomplete data buffer.</param>
            /// <param name="length">The number of bytes read.</param>
            private Message(Header header, byte[] buffer, int length) {
                IsIncomplete = true;
                Length = length;
                Header = header;
                CompletionBuffer = new byte[length];
                Buffer.BlockCopy(buffer, 0, CompletionBuffer, 0, length);
            }

            /// <summary>
            /// Reads the complete message from the stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Message read.</returns>
            public static Message Read(Stream stream) {
                var header = Header.Read(stream);
                return new Message(header.ReadDerivedNode(stream));
            }

            /// <summary>
            /// Performs a buffered read from a network stream. Returns either complete or incomplete message.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <param name="buffer">Input buffer.</param>
            /// <returns>Complete or incomplete message.</returns>
            public static Message ReadBuffered(NetworkStreamEx stream, byte[] buffer = null) {
                var header = Header.Read(stream); // careful here, header length is unknown until read.
                if (header == null) return null; // edge case, stream closed while reading.
                if (buffer == null || buffer.Length < header.PayloadLength) buffer = new byte[header.PayloadLength];
                // now we should request amount of bytes stated in the header, unless our buffer is smaller:
                var requested = header.PayloadLength <= buffer.Length ? header.PayloadLength : buffer.Length;
                var length = stream.Read(buffer, 0, requested);
                return header.PayloadLength <= length
                    ? new Message(header.ReadDerivedNode(buffer, 0, length)) // complete message
                    : new Message(header, buffer, length); // incomplete message
            }

            /// <summary>
            /// Continues reading the current message from the network stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <param name="buffer">Input buffer.</param>
            internal void ReadBufferedContinue(NetworkStreamEx stream, byte[] buffer = null) {
                if (!IsIncomplete || Header == null || CompletionBuffer == null) throw new InvalidOperationException();
                IsContinued = true;
                var bytesMissing = Header.PayloadLength - CompletionBuffer.Length;
                if (buffer == null || buffer.Length < bytesMissing) buffer = new byte[bytesMissing];
                var length = stream.Read(buffer, 0, bytesMissing);
                var merged = new byte[CompletionBuffer.Length + length];
                Buffer.BlockCopy(CompletionBuffer, 0, merged, 0, CompletionBuffer.Length);
                Buffer.BlockCopy(buffer, 0, merged, CompletionBuffer.Length, length);
                IsIncomplete = merged.Length < Header.PayloadLength;
                if (!IsIncomplete) {
                    Data = Header.ReadDerivedNode(merged, 0, merged.Length);
                    Length = Data.Header.Length + merged.Length;
                    if (Data is Sequence seq && seq?.First is Integer dataId) Id = dataId; else Id = -1;
                    IsEndSession = Data.IsEndSessionMessage;
                }
                else CompletionBuffer = merged;
            }

            /// <summary>
            /// Writes the message to the stream, no buffering.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            public void Write(Stream stream) => Data.Write(stream);

            /// <summary>
            /// Performs buffered write to the output stream to prevent fragmentation.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            /// <param name="buffer">Output buffer.</param>
            public void WriteBuffered(Stream stream, byte[] buffer = null) => Data.WriteBuffered(stream, buffer);

        }

    }

}