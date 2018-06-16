using System.IO;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Tag identifier as defined in X.690 p. 8.1.2.
        /// </summary>
        internal struct Identifier {

            /// <summary>
            /// <list type="number">
            /// <item><term>0</term> <description>Universal</description></item>
            /// <item><term>1</term> <description>Application</description></item>
            /// <item><term>2</term> <description>Context-specific</description></item>
            /// <item><term>3</term> <description>Private</description></item>
            /// </list>
            /// </summary>
            public int Class;

            /// <summary>
            /// Tag number as specified in X.690, application or private documentation.
            /// </summary>
            public int TagNumber;

            /// <summary>
            /// True if the tag is constructed and thus contains other tags.
            /// </summary>
            public bool IsConstructed;


            /// <summary>
            /// Value indicating the structure was read from a stream.
            /// </summary>
            internal readonly bool IsRead;

            /// <summary>
            /// The lenght in bytes read from input stream.
            /// </summary>
            internal int ReadLength;

            /// <summary>
            /// Gets the calculated octet count.
            /// </summary>
            internal int Length {
                get {
                    if (TagNumber < 31) return 1;
                    else {
                        var length = 2;
                        var r = TagNumber;
                        while (r > 0x7f) {
                            r -= 0x7f;
                            length++;
                        }
                        return length;
                    }
                }
            }

            /// <summary>
            /// Creates identifier structure.
            /// </summary>
            /// <param name="class"><see cref="Class"/>.</param>
            /// <param name="tagNumber"><see cref="TagNumber"/>.</param>
            /// <param name="isConstructed"><see cref="IsConstructed"/>.</param>
            /// <param name="readLength"><see cref="ReadLength"/>.</param>
            private Identifier(int @class, int tagNumber, bool isConstructed, int readLength) {
                IsRead = true;
                Class = @class;
                TagNumber = tagNumber;
                IsConstructed = isConstructed;
                ReadLength = readLength;
            }

            /// <summary>
            /// Reads X.690 identifier octets from input stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Tag identifier as defined in X.690 p. 8.1.2.</returns>
            /// <exception cref="IOException">Thrown when identifier data cannot be read from the stream.</exception>
            /// <exception cref="InvalidDataException">Thrown when unexpected message end is met.</exception>
            public static Identifier Read(Stream stream) {
                var length = 0;
                int octet = stream.ReadByte();
                if (octet < 0) return default(Identifier);
                length++;
                var tagClass = octet >> 6;
                var isContructed = (octet >> 5 & 1) > 0;
                var tagNumber = octet & 0x1f;
                if (tagNumber == 0x1f) {
                    tagNumber = 0;
                    do {
                        octet = stream.ReadByte();
                        if (octet < 0) throw new InvalidDataException($"{_ExceptionHeader}Unexpected end of message when reading continuation of the identifier.");
                        length++;
                        tagNumber += octet & 0x7f;
                    } while ((octet & 0x80) > 0);
                }
                return new Identifier(tagClass, tagNumber, isContructed, length);
            }

            /// <summary>
            /// Reads X.690 identifier octets from input buffer.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of the identifier data start.</param>
            /// <returns>Tag identifier as defined in X.690 p. 8.1.2.</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown when the buffer is incomplete.</exception>
            public static Identifier Read(byte[] buffer, int offset) {
                var index = 0;
                var octet = buffer[offset + index++];
                var tagClass = octet >> 6;
                var isContructed = (octet >> 5 & 1) > 0;
                var tagNumber = octet & 0x1f;
                if (tagNumber == 0x1f) {
                    tagNumber = 0;
                    do {
                        octet = buffer[offset + index++];
                        tagNumber += octet & 0x7f;
                    } while ((octet & 0x80) > 0);
                }
                return new Identifier(tagClass, tagNumber, isContructed, index);
            }

            /// <summary>
            /// Writes X.690 identifier to output stream.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            public int Write(Stream stream) {
                var length = 1;
                var octet = Class << 6;
                if (IsConstructed) octet |= 0x20;
                if (TagNumber < 31) {
                    octet |= TagNumber;
                    stream.WriteByte((byte)octet);
                }
                else {
                    stream.WriteByte((byte)(octet | 0x1f));
                    var r = TagNumber;
                    while (r > 0x7f) {
                        stream.WriteByte(0xff);
                        r -= 0x7f;
                        length++;
                    }
                    stream.WriteByte((byte)r);
                    length++;
                }
                return length;
            }

            /// <summary>
            /// Writes X.690 identifier to output buffer.
            /// </summary>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            /// <returns>Number of bytes written.</returns>
            public int Write(byte[] buffer, int offset) {
                var index = 0;
                var octet = Class << 6;
                if (IsConstructed) octet |= 0x20;
                if (TagNumber < 31) {
                    octet |= TagNumber;
                    buffer[offset + index++] = (byte)octet;
                }
                else {
                    buffer[offset + index++] = (byte)(octet | 0x1f);
                    var r = TagNumber;
                    while (r > 0x7f) {
                        buffer[offset + index++] = 0xff;
                        r -= 0x7f;
                    }
                    buffer[offset + index++] = (byte)r;
                }
                return index;
            }

            #region Equality

            public override int GetHashCode() =>
                31 * (31 * (31 * Class.GetHashCode() + TagNumber.GetHashCode()) + IsConstructed.GetHashCode()) + Length.GetHashCode();


            public override bool Equals(object obj) =>
                (obj is Identifier i) && i.Class == Class && i.TagNumber == TagNumber && i.IsConstructed == IsConstructed && i.Length == Length;

            public static bool operator ==(Identifier a, Identifier b) =>
                a.Class == b.Class && a.TagNumber == b.TagNumber && a.IsConstructed == b.IsConstructed && a.Length == b.Length;

            public static bool operator !=(Identifier a, Identifier b) => !(a == b);

            #endregion

        }

    }

}