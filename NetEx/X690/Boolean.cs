namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// ASN.1 Boolean type node.
        /// </summary>
        public class Boolean : Node {

            /// <summary>
            /// Gets or sets node's <see cref="bool"/> value.
            /// </summary>
            public bool Value {
                get => Payload[0] > 0; set => Payload = new byte[1] { (byte)(value ? 0xff : 0x00) };
            }

            /// <summary>
            /// Creates a new empty ASN.1 <see cref="Boolean"/> node with default value of false.
            /// </summary>
            public Boolean() : base(new Header(new Identifier { TagNumber = UniversalType.Boolean })) => Value = false;
            /// <summary>
            /// Creates a new ASN.1 <see cref="Boolean"/> node with a value.
            /// </summary>
            /// <param name="value">True or false.</param>
            public Boolean(bool value) : this() => Value = value;
            /// <summary>
            /// Creates an empty ASN.1 <see cref="Boolean"/> node from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Boolean(Header header) : base(header) { }

            /// <summary>
            /// Implicitly converts ASN.1 <see cref="Boolean"/> nodes to CLR <see cref="bool"/>.
            /// </summary>
            /// <param name="node"></param>
            public static implicit operator bool(Boolean node) => node.Value;

        }

    }

}