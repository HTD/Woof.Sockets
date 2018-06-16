namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        public class Set : Node {

            /// <summary>
            /// Creates new empty ASN.1 Set.
            /// </summary>
            public Set() : base(new Header(new Identifier { IsConstructed = true, TagNumber = UniversalType.Set })) { }

            /// <summary>
            /// Creates new empty ASN.1 Set from header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Set(Header header) : base(header) { }

        }

    }

}