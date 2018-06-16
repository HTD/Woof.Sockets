using System;
using System.Linq;
using System.Reflection;

namespace Woof.NetEx {

    public static partial class X690 {

        /// <summary>
        /// Represents ASN.1 node type.
        /// </summary>
        public abstract class NodeType {

            /// <summary>
            /// Gets X.690 tag number.
            /// </summary>
            public int TagNumber { get; }

            /// <summary>
            /// Creates ASN.1 node type from tag number.
            /// </summary>
            /// <param name="tagNumber">X.690 tag number.</param>
            protected NodeType(int tagNumber) => TagNumber = tagNumber;
            /// <summary>
            /// Creates ASN.1 node type from tag X.690 tag class and number.
            /// </summary>
            /// <param name="tagClass">Tag class byte signature.</param>
            /// <param name="tagNumber">Tag number as defined in X.690 or extending specs.</param>
            /// <returns></returns>
            public static NodeType Get(int tagClass, int tagNumber) {
                switch (tagClass) {
                    case 0: return new UniversalType(tagNumber);
                    case 1: return new ApplicationType(tagNumber);
                    case 2: return new ContextSpecificType(tagNumber);
                    case 3: return new PrivateType(tagNumber);
                    default: throw new ArgumentException("Unsupported tag class", "tagClass");
                }
            }

            /// <summary>
            /// Returns the node type string representation for debugging purpose.
            /// </summary>
            /// <returns>String node type representation.</returns>
            public override string ToString() {
                var type = GetType();
                string name;
                try {
                    if (this is UniversalType)
                        return type
                            .GetFields(BindingFlags.Public | BindingFlags.Static)
                            .Single(i => i.IsLiteral && !i.IsInitOnly && (int)i.GetValue(this) == TagNumber).Name;
                }
                catch (Exception) {
                    return "Unknown";
                }
                name = type.Name;
                if (name.EndsWith("Type")) name = name.Substring(0, name.Length - 4);
                return name;
            }

            #region Equality

            public override int GetHashCode() => TagNumber.GetHashCode();

            public override bool Equals(object obj) => (obj is NodeType t) && t.TagNumber == TagNumber;

            public static bool operator ==(NodeType a, NodeType b) => a.TagNumber == b.TagNumber;

            public static bool operator !=(NodeType a, NodeType b) => a.TagNumber != b.TagNumber;

            #endregion

            #region Implicit conversions

            public static implicit operator int(NodeType type) => type.TagNumber;

            public static implicit operator string(NodeType type) => type.ToString();

            #endregion

        }

        /// <summary>
        /// ASN.1 root node type. Nodes of this type contains subnodes and are written sequentially without common header.
        /// </summary>
        public sealed class RootType : NodeType {
            /// <summary>
            /// Creates <see cref="RootType"/> instance.
            /// </summary>
            internal RootType() : base(0) { }
        }

        /// <summary>
        /// ASN.1 universal node type.
        /// </summary>
        public sealed class UniversalType : NodeType {

            #region X.690 universal node types
            public const int EndOfContent = 0x00;
            public const int Boolean = 0x01;
            public const int Integer = 0x02;
            public const int BitString = 0x03;
            public const int OctetString = 0x04;
            public const int Null = 0x05;
            public const int ObjectId = 0x06;
            public const int ObjectDescription = 0x07;
            public const int External = 0x08;
            public const int Real = 0x09;
            public const int Enumerated = 0x0a;
            public const int EmbeddedPdv = 0x0b;
            public const int Utf8String = 0x0c;
            public const int RelativeOId = 0x0d;
            public const int Reserved1 = 0x0e;
            public const int Reserved2 = 0x0f;
            public const int Sequence = 0x10;
            public const int Set = 0x11;
            public const int NumericString = 0x12;
            public const int PrintableString = 0x13;
            public const int T61String = 0x14;
            public const int VideotexString = 0x15;
            public const int IA5String = 0x16;
            public const int UtcTime = 0x17;
            public const int GeneralizedTime = 0x18;
            public const int GraphicString = 0x19;
            public const int VisibleString = 0x1a;
            public const int GeneralString = 0x1b;
            public const int UniversalString = 0x1c;
            public const int CharacterString = 0x1d;
            public const int BmpString = 0x1e;
            #endregion

            /// <summary>
            /// Creates a universal node type from tag number.
            /// </summary>
            /// <param name="tagNumber">X.690 tag number.</param>
            internal UniversalType(int tagNumber) : base(tagNumber) { }

        }

        /// <summary>
        /// ASN.1 application node type.
        /// </summary>
        public sealed class ApplicationType : NodeType {

            /// <summary>
            /// Creates application node type instance from tag number.
            /// </summary>
            /// <param name="tagNumber">X.690 tag number.</param>
            internal ApplicationType(int tagNumber) : base(tagNumber) { }
        }

        /// <summary>
        /// ASN.1 content-specific node type.
        /// </summary>
        public sealed class ContextSpecificType : NodeType {

            /// <summary>
            /// Creates context-specific node type from tag number.
            /// </summary>
            /// <param name="tagNumber">X.690 tag number.</param>
            internal ContextSpecificType(int tagNumber) : base(tagNumber) { }
        }

        /// <summary>
        /// ASN.1 private node type.
        /// </summary>
        public sealed class PrivateType : NodeType {

            /// <summary>
            /// Creates private node type from tag number.
            /// </summary>
            /// <param name="tagNumber">X.690 tag number.</param>
            internal PrivateType(int tagNumber) : base(tagNumber) { }
        }

    }

}