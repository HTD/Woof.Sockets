namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        public class Sequence : Node {

            /// <summary>
            /// Creates new empty ASN.1 Sequence.
            /// </summary>
            public  Sequence() : base(new Header(new Identifier { IsConstructed = true, TagNumber = UniversalType.Sequence })) { }

            /// <summary>
            /// Creates new empty ASN.1 Sequence from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Sequence(Header header) : base(header) { }

        }

    }

}