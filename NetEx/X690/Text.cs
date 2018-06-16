using System.Text;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Represents all ASN.1 string nodes.
        /// </summary>
        public class Text : Node {

            /// <summary>
            /// The default (UTF-8) encoding for octet streams.
            /// </summary>
            private static readonly Encoding Encoding = Encoding.UTF8;

            /// <summary>
            /// Creates an empty ASN.1 Utf8String node.
            /// </summary>
            /// <param name="type"><see cref="UniversalType"/> of the string.</param>
            public Text(int type = UniversalType.Utf8String) : base(new Header(new Identifier { TagNumber = type })) { }

            /// <summary>
            /// Creates new ASN.1 Utf8String node with a value.
            /// </summary>
            /// <param name="value">String value.</param>
            /// <param name="type"><see cref="UniversalType"/> of the string.</param>
            public Text(string value, int type = UniversalType.Utf8String) : this(type) => Value = value;
            
            /// <summary>
            /// Creates ASN.1 String from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Text(Header header) : base(header) { }

            /// <summary>
            /// Gets or sets CLR string value of the node.
            /// </summary>
            public string Value {
                get => Payload != null ? Encoding.GetString(Payload) : "";
                set => Payload = !string.IsNullOrEmpty(value) ? Encoding.GetBytes(value) : null;
            }

            /// <summary>
            /// Returns the node string representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString() => Value;

            /// <summary>
            /// Implicitly converts <see cref="Text"/> node to CLR <see cref="string"/>.
            /// </summary>
            /// <param name="node"></param>
            public static implicit operator string(Text node) => node.Value;

        }

    }

}