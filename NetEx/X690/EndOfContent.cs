namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// ASN.1 EndOfContent node indicating the end of the indefinite length content.
        /// </summary>
        public class EndOfContent : Node {

            /// <summary>
            /// Creates new ASN.1 <see cref="EndOfContent"/> node.
            /// </summary>
            public EndOfContent() : base(new Header(new Identifier { TagNumber = UniversalType.EndOfContent })) { }

            /// <summary>
            /// Creates new ASN.1 <see cref="EndOfContent"/> node form header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal EndOfContent(Header header) : base(header) { }

        }

    }

}