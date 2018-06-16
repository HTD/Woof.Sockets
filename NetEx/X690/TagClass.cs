namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Describes the tag class as byte enumeration.
        /// </summary>
        public enum TagClass {
            /// <summary>
            /// The type is native to ASN.1.
            /// </summary>
            Universal = 0,
            /// <summary>
            /// The type is only valid for one specific application.
            /// </summary>
            Application = 1,
            /// <summary>
            /// Meaning of this type depends on the context (such as within a sequence, set or choice).
            /// </summary>
            ContextSpecific = 2,
            /// <summary>
            /// Defined in private specifications.
            /// </summary>
            Private = 3
        }

    }

}