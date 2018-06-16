namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// ASN.1 Null node.
        /// </summary>
        public class Null : Node {

            /// <summary>
            /// Creates new ASN.1 Null node.
            /// </summary>
            public Null() : this(new Header(new Identifier { TagNumber = UniversalType.Null })) { }

            /// <summary>
            /// Creates new ASN.1 Null node from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Null(Header header) : base(header) { }

        }

    }

}