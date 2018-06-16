using System.Collections.Generic;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// ASN.1 Integer type node.
        /// </summary>
        public class Integer : Node {

            /// <summary>
            /// Gets or sets the value as Int64. If <see cref="ValueBits"/> is greater than 64 the overflow value of -1 is returned.
            /// </summary>
            public long Value {
                get {
                    if (ValueBits <= 64) return DecodeInt64(Payload);
                    return -1;
                }
                set => Payload = EncodeInt64(value);
            }

            /// <summary>
            /// Gets the number of bits used by the data. The value has any meaning if this is less or equal 64.
            /// </summary>
            public int ValueBits => Payload.Length << 3;

            /// <summary>
            /// Creates a new ASN.1 <see cref="Integer"/> node with default value of zero.
            /// </summary>
            public Integer() : base(new Header(new Identifier { TagNumber = UniversalType.Integer })) => Value = 0;
            /// <summary>
            /// Creates a new ASN.1 <see cref="Integer"/> node with a value.
            /// </summary>
            /// <param name="value">Integer value.</param>
            public Integer(long value) : this() => Value = value;
            /// <summary>
            /// Creates a new ASN.1 <see cref="Integer"/> node from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Integer(Header header) : base(header) { }

            /// <summary>
            /// Encodes integer using ASN.1 BER.
            /// </summary>
            /// <param name="x">Integer value.</param>
            /// <returns>Encoded octets.</returns>
            private static byte[] EncodeInt64(long x) {
                var bytes = new List<byte>();
                long b;
                int i;
                if (x >= 0) {
                    for (i = 7, b = 0; i >= 0 && b == 0; i--) b = x >> (i << 3);
                    if ((b & 0x80) > 0) bytes.Add(0);
                    bytes.Add((byte)b);
                    for (; i >= 0; i--) bytes.Add((byte)(x >> (i << 3)));
                }
                else {
                    x = -x - 1;
                    for (i = 7, b = 0; i >= 0 && b == 0; i--) b = x >> (i << 3);
                    if ((b & 0x80) > 0) bytes.Add(0xff);
                    bytes.Add((byte)(b ^ 0xff));
                    for (; i >= 0; i--) bytes.Add((byte)((x >> (i << 3)) ^ 0xff));
                }
                return bytes.ToArray();
            }

            /// <summary>
            /// Decodes integer from ASN.1 BER.
            /// </summary>
            /// <param name="x">Encoded octets.</param>
            /// <returns>Decoded value.</returns>
            private static long DecodeInt64(byte[] x) {
                long y = 0;
                if ((x[0] & 0x80) < 1) {
                    for (int i = 0, n = x.Length; i < n; i++) y |= (long)x[i] << ((n - i - 1) << 3);
                    return y;
                }
                else {
                    for (int i = 0, n = x.Length; i < n; i++) y |= (long)(x[i] ^ 0xff) << ((n - i - 1) << 3);
                    return -y - 1;
                }
            }

            /// <summary>
            /// Implicitly converts <see cref="Integer"/> to <see cref="Int64"/>.
            /// </summary>
            /// <param name="node"></param>
            public static implicit operator long(Integer node) => node.Value;

        }

    }

}