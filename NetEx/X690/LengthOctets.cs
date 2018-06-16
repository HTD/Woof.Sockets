using System;
using System.IO;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Provides methods of handling X.690 length octets.
        /// </summary>
        internal struct LengthOctets {

            /// <summary>
            /// Gets the value indicating how many octets was read as length octets.
            /// </summary>
            public int ReadLength { get; }

            /// <summary>
            /// Gets the direct value of decoded payload length.
            /// </summary>
            public int Value { get; }

            /// <summary>
            /// Creates new <see cref="LengthOctets"/> structure for a value.
            /// </summary>
            /// <param name="value">A value to create <see cref="LengthOctets"/> struct from.</param>
            public LengthOctets(int value) { ReadLength = OctetCount(value); Value = value; }

            /// <summary>
            /// Creates new <see cref="LengthOctets"/> structure from header data.
            /// </summary>
            /// <param name="length">Read object's length.</param>
            /// <param name="value">Direct value of decoded payload length.</param>
            private LengthOctets(int length, int value) { ReadLength = length; Value = value; }

            /// <summary>
            /// Returns minimum octet count in bytes.
            /// </summary>
            /// <param name="length">Integer length, -1 for indefinite length.</param>
            /// <returns>Calculated octet count for specified length.</returns>
            internal static int OctetCount(int length) {
                if (length < 0x80) return 1;
                if (length < 0x100) return 2;
                if (length < 0x10000) return 3;
                if (length < 0x1000000) return 4;
                return 5;
            }

            /// <summary>
            /// Reads integer length from ITU-T X.690 length octet.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns><see cref="LengthOctets"/> structure.</returns>
            public static LengthOctets Read(Stream stream) {
                int x = stream.ReadByte();
                if (x < 0) return new LengthOctets();
                if (x < 0x80) return new LengthOctets(1, x);
                if (x == 0x80) return new LengthOctets(1, -1);
                int bytesRead = (x & 0x7f) + 1;
                if (bytesRead > 5) throw new InvalidDataException($"{_ExceptionHeader}Header length octet count exceeded. Length appears to be {bytesRead * 8}-bit.");
                int value = 0;
                for (int i = bytesRead - 2; i > 0; i--) {
                    x = stream.ReadByte();
                    if (x < 0) throw new InvalidDataException($"{_ExceptionHeader}Unexpected end of stream when reading message length octets.");
                    value |= x << (8 * i);
                }
                x = stream.ReadByte();
                if (x < 0) throw new InvalidDataException($"{_ExceptionHeader}Unexpected end of stream when reading message length octets.");
                value |= x;
                return new LengthOctets(bytesRead, value);
            }

            /// <summary>
            /// Reads integer length from ITU-T X.690 length octet.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of the length octets start.</param>
            /// <returns><see cref="LengthOctets"/> structure.</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown when the buffer is incomplete.</exception>
            /// <exception cref="InvalidDataException">Thrown when the length octets indicated unrealistic data length.</exception>
            public static LengthOctets Read(byte[] buffer, int offset) {
                int index = 0;
                int x = buffer[offset + index++];
                if (x < 0x80) return new LengthOctets(1, x);
                if (x == 0x80) return new LengthOctets(1, -1);
                if ((x & 0x7f) > 5) throw new InvalidDataException($"{_ExceptionHeader}Header length octet count exceeded. Length appears to be {(x & 0x7f) * 8}-bit.");
                int value = 0;
                for (int i = (x & 0x7f) - 1; i > 0; i--) {
                    x = buffer[offset + index++];
                    value |= x << (8 * i);
                }
                x = buffer[offset + index++];
                value |= x;
                return new LengthOctets(index, value);
            }

            /// <summary>
            /// Tries to write integer length in the short 1-octed form if applicable.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="stream">Output stream.</param>
            /// <returns>1 if length was in 0..127 range, 0 otherwise.</returns>
            public static int WriteShort(int length, Stream stream) {
                if (length >= 0 && length < 128) {
                    stream.WriteByte((byte)length);
                    return 1;
                }
                return 0;
            }

            /// <summary>
            /// Tries to write integer length in the short 1-octed form if applicable.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>1 if length was in 0..127 range, 0 otherwise.</returns>
            public static int WriteShort(int length, byte[] buffer, int offset) {
                if (length >= 0 && length < 128) {
                    buffer[offset] = (byte)length;
                    return 1;
                }
                return 0;
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 length octet long form.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            public static int WriteLong(int length, Stream stream) {
                if (length < 0) throw new ArgumentException();
                if (length < 0x100) {
                    stream.WriteByte(0x81);
                    stream.WriteByte((byte)length);
                    return 2;
                }
                if (length < 0x10000) {
                    stream.WriteByte(0x82);
                    stream.WriteByte((byte)(length >> 0x08 & 0xff));
                    stream.WriteByte((byte)(length & 0xff));
                    return 3;
                }
                if (length < 0x1000000) {
                    stream.WriteByte(0x83);
                    stream.WriteByte((byte)(length >> 0x10 & 0xff));
                    stream.WriteByte((byte)(length >> 0x08 & 0xff));
                    stream.WriteByte((byte)(length & 0xff));
                    return 4;
                }
                stream.WriteByte(0x84);
                stream.WriteByte((byte)(length >> 0x18 & 0xff));
                stream.WriteByte((byte)(length >> 0x10 & 0xff));
                stream.WriteByte((byte)(length >> 0x08 & 0xff));
                stream.WriteByte((byte)(length & 0xff));
                return 5;
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 length octet long form.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>Number of bytes written.</returns>
            public static int WriteLong(int length, byte[] buffer, int offset) {
                if (length < 0) throw new ArgumentException();
                if (length < 0x100) {
                    buffer[offset] = 0x81;
                    buffer[offset + 1] = (byte)length;
                    return 2;
                }
                if (length < 0x10000) {
                    buffer[offset] = 0x82;
                    buffer[offset + 1] = (byte)(length >> 0x08 & 0xff);
                    buffer[offset + 2] = (byte)(length & 0xff);
                    return 3;
                }
                if (length < 0x1000000) {
                    buffer[offset] = 0x83;
                    buffer[offset + 1] = (byte)(length >> 0x10 & 0xff);
                    buffer[offset + 2] = (byte)(length >> 0x08 & 0xff);
                    buffer[offset + 3] = (byte)(length & 0xff);
                    return 4;
                }
                buffer[offset] = 0x84;
                buffer[offset + 1] = (byte)(length >> 0x18 & 0xff);
                buffer[offset + 2] = (byte)(length >> 0x10 & 0xff);
                buffer[offset + 3] = (byte)(length >> 0x08 & 0xff);
                buffer[offset + 4] = (byte)(length & 0xff);
                return 5;
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 32-bit length octet.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            public static int Write32(int length, Stream stream) {
                stream.WriteByte(0x84);
                stream.WriteByte((byte)(length >> 0x18 & 0xff));
                stream.WriteByte((byte)(length >> 0x10 & 0xff));
                stream.WriteByte((byte)(length >> 0x08 & 0xff));
                stream.WriteByte((byte)(length & 0xff));
                return 5;
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 32-bit length octet.
            /// </summary>
            /// <param name="length">Integer length.</param>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>Number of bytes written.</returns>
            public static int Write32(int length, byte[] buffer, int offset) {
                buffer[offset] = 0x84;
                buffer[offset + 1] = (byte)(length >> 0x18 & 0xff);
                buffer[offset + 2] = (byte)(length >> 0x10 & 0xff);
                buffer[offset + 3] = (byte)(length >> 0x08 & 0xff);
                buffer[offset + 4] = (byte)(length & 0xff);
                return 5;
            }

            /// <summary>
            /// Writes length octet indicating ITU-T X.690 indefinite length value.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            public static int WriteIndefinite(Stream stream) {
                stream.WriteByte(0x80);
                return 1;
            }

            /// <summary>
            /// Writes length octet indicating ITU-T X.690 indefinite length value.
            /// </summary>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>Number of bytes written.</returns>
            public static int WriteIndefinite(byte[] buffer, int offset) {
                buffer[offset] = 0x80;
                return 1;
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 length octet.
            /// </summary>
            /// <param name="length">Integer length, -1 means indefinite length.</param>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            public static int Write(int length, Stream stream) {
                if (length < 0) return WriteIndefinite(stream);
                if (length < 0x80) return WriteShort(length, stream);
                return WriteLong(length, stream);
            }

            /// <summary>
            /// Writes integer length as ITU-T X.690 length octet.
            /// </summary>
            /// <param name="length">Integer length, -1 means indefinite length.</param>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>Number of bytes written.</returns>
            public static int Write(int length, byte[] buffer, int offset) {
                if (length < 0) return WriteIndefinite(buffer, offset);
                if (length < 0x80) return WriteShort(length, buffer, offset);
                return WriteLong(length, buffer, offset);
            }

            #region Equality

            public override int GetHashCode() => 257 * ReadLength.GetHashCode() + Value.GetHashCode();

            public override bool Equals(object obj) => (obj is LengthOctets l) && l.ReadLength == ReadLength && l.Value == Value;

            public static bool operator ==(LengthOctets a, LengthOctets b) => a.ReadLength == b.ReadLength && a.Value == b.Value;

            public static bool operator !=(LengthOctets a, LengthOctets b) => !(a == b);

            #endregion

        }

    }

}