using System.IO;
using System.Linq;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// Represents X.690 data header.
        /// </summary>
        public sealed class Header {

            #region Properties

            /// <summary>
            /// Gets the tag class as one from <see cref="Class"/> enumeration.
            /// </summary>
            public TagClass Class => (TagClass)Identifier.Class;

            /// <summary>
            /// Gets the node identifier used to create the node.
            /// </summary>
            internal Identifier Identifier { get; }

            /// <summary>
            /// Gets the header length in bytes.
            /// </summary>
            public int Length { get; internal set; }

            /// <summary>
            /// Gets the value indicating whether the type is primitive or constructed.
            /// </summary>
            public bool IsConstructed => Identifier.IsConstructed;

            /// <summary>
            /// Gets or sets a value indicating whether the payload length is encoded as definite.
            /// </summary>
            public bool IsDefiniteLength {
                get => _IsDefiniteLength;
                internal set {
                    if (!value) PayloadLength = -1;
                    _IsDefiniteLength = value;
                }
            }

            /// <summary>
            /// Gets the node type.
            /// </summary>
            public NodeType NodeType { get; }

            /// <summary>
            /// Gets the payload length in bytes, negative values indicate indefinite length.
            /// </summary>
            public int PayloadLength { get; internal set; }

            /// <summary>
            /// Gets the full message length if available, -1 otherwise.
            /// </summary>
            public int MessageLength => IsDefiniteLength ? Length + PayloadLength : -1;

            #endregion

            #region Data

            private bool _IsDefiniteLength = true;

            #endregion

            #region Constructors

            /// <summary>
            /// Creates header from identifier.
            /// </summary>
            /// <param name="identifier">Tag identifier.</param>
            internal Header(Identifier identifier) {
                Identifier = identifier;
                NodeType = NodeType.Get(identifier.Class, identifier.TagNumber);
                IsDefiniteLength = true;
            }

            /// <summary>
            /// Creates ASN.1 header object from read identifier and payload length.
            /// </summary>
            /// <param name="identifier">Tag identifier.</param>
            /// <param name="payloadLength">Payload length in bytes, negative values indicate indefinite length.</param>
            internal Header(Identifier identifier, int payloadLength) : this(identifier) {
                PayloadLength = payloadLength;
                IsDefiniteLength = payloadLength >= 0;
                Length += OctetCount(PayloadLength);
            }

            /// <summary>
            /// Creates ASN.1 header for a root node.
            /// </summary>
            /// <param name="rootTypeInstance">Root type instance.</param>
            internal Header(RootType rootTypeInstance) {
                Identifier = new Identifier { IsConstructed = true };
                NodeType = rootTypeInstance;
                IsDefiniteLength = true;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Calculates payload length of the specified node taking into accout all descendant nodes.
            /// </summary>
            /// <param name="node">Target node.</param>
            /// <returns>Node payload length as specified in header.</returns>
            internal int CalculatePayloadLength(Node node) {
                foreach (var e in node.DFS) {
                    e.Header.PayloadLength = e.Payload?.Length ?? 0;
                    if (e.HasNodes)
                        e.Header.PayloadLength +=
                            e.Nodes.Sum(i => i.Header.Length + i.Header.PayloadLength + (i.Header.IsDefiniteLength ? 0 : 2));
                    e.Header.Length =
                        e.Header.IsDefiniteLength
                            ? e.Header.OctetCount(e.Header.PayloadLength)
                            : e.Header.OctetCount(-1);
                }
                foreach (var e in node.DFSR) if (!e.Header.IsDefiniteLength) e.Header.PayloadLength = -1;
                return node.Header.PayloadLength;
            }

            /// <summary>
            /// Calculates total header octet count for specified payload length.
            /// </summary>
            /// <param name="payloadLength">Payload length.</param>
            /// <returns>Header octet count consisting of identifier and length octets.</returns>
            internal int OctetCount(int payloadLength) => Identifier.Length + LengthOctets.OctetCount(payloadLength);

            /// <summary>
            /// Reads ASN.1 header.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Message header.</returns>
            internal static Header Read(Stream stream) {
                var identifier = Identifier.Read(stream);
                if (!identifier.IsRead) return null;
                var lengthOctets = LengthOctets.Read(stream);
                if (lengthOctets.ReadLength < 1) throw new InvalidDataException("Unexpected end of content in header.");
                return new Header(identifier, lengthOctets.Value) { Length = identifier.ReadLength + lengthOctets.ReadLength };
            }

            /// <summary>
            /// Reads ASN.1 header.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Message header.</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown when the buffer is incomplete.</exception>
            /// <exception cref="InvalidDataException">Thrown when the length octets indicated unrealistic data length.</exception>
            internal static Header Read(byte[] buffer, int offset) {
                var identifier = Identifier.Read(buffer, offset);
                offset += identifier.ReadLength;
                if (!identifier.IsRead) return null;
                var lengthOctets = LengthOctets.Read(buffer, offset);
                if (lengthOctets.ReadLength < 1) throw new InvalidDataException("Unexpected end of content in header.");
                return new Header(identifier, lengthOctets.Value) { Length = identifier.ReadLength + lengthOctets.ReadLength };
            }

            /// <summary>
            /// Reads derived node from stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Derived node.</returns>
            internal Node ReadDerivedNode(Stream stream) {
                Node node = null;
                if (Class == TagClass.Universal) {
                    switch (NodeType.TagNumber) {
                        case UniversalType.EndOfContent: node = new EndOfContent(this); break;
                        case UniversalType.Boolean: node = new Boolean(this); node.Read(stream); break;
                        case UniversalType.Integer: node = new Integer(this); node.Read(stream); break;
                        case UniversalType.Null: node = new Null(this); break;
                        case UniversalType.Enumerated: node = new Enumerated(this); break;
                        case UniversalType.Sequence: node = new Sequence(this); node.Read(stream); break;
                        case UniversalType.Set: node = new Set(this); node.Read(stream); break;
                        case UniversalType.BmpString:
                        case UniversalType.GeneralString:
                        case UniversalType.GraphicString:
                        case UniversalType.VisibleString:
                        case UniversalType.OctetString:
                        case UniversalType.Utf8String:
                        case UniversalType.NumericString:
                        case UniversalType.PrintableString:
                        case UniversalType.T61String:
                        case UniversalType.VideotexString:
                        case UniversalType.IA5String: node = new Text(this); node.Read(stream); break;
                        default: node = new Node(this); node.Read(stream); break;
                    }
                }
                else { node = new Node(this); node.Read(stream); }
                return node;
            }

            /// <summary>
            /// Reads derived node from buffer.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of the node within the buffer.</param>
            /// <param name="length">Available data length.</param>
            /// <returns>Derived node.</returns>
            internal Node ReadDerivedNode(byte[] buffer, int offset, int length) {
                Node node = null;
                if (Class == TagClass.Universal) {
                    switch (NodeType.TagNumber) {
                        case UniversalType.EndOfContent: node = new EndOfContent(this); break;
                        case UniversalType.Boolean: node = new Boolean(this); node.Read(buffer, offset, length); break;
                        case UniversalType.Integer: node = new Integer(this); node.Read(buffer, offset, length); break;
                        case UniversalType.Null: node = new Null(this); break;
                        case UniversalType.Enumerated: node = new Enumerated(this); node.Read(buffer, offset, length); break;
                        case UniversalType.Sequence: node = new Sequence(this); node.Read(buffer, offset, length); break;
                        case UniversalType.Set: node = new Set(this); node.Read(buffer, offset, length); break;
                        case UniversalType.BmpString:
                        case UniversalType.GeneralString:
                        case UniversalType.GraphicString:
                        case UniversalType.VisibleString:
                        case UniversalType.OctetString:
                        case UniversalType.Utf8String:
                        case UniversalType.NumericString:
                        case UniversalType.PrintableString:
                        case UniversalType.T61String:
                        case UniversalType.VideotexString:
                        case UniversalType.IA5String: node = new Text(this); node.Read(buffer, offset, length); break;
                        default: node = new Node(this); node.Read(buffer, offset, length); break;
                    }
                }
                else { node = new Node(this); node.Read(buffer, offset, length); }
                return node;
            }

            /// <summary>
            /// Writes the ASN.1 header to the output stream.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            internal void Write(Stream stream) {
                var identifier = new Identifier { Class = (int)Class, IsConstructed = IsConstructed, TagNumber = NodeType.TagNumber };
                var length = OctetCount(PayloadLength);
                var buffer = new byte[length];
                identifier.Write(buffer, 0);
                LengthOctets.Write(IsDefiniteLength ? PayloadLength : -1, buffer, identifier.Length);
                Length = length;
                stream.Write(buffer, 0, length);
            }

            /// <summary>
            /// Writes the ASN.1 header to the output buffer.
            /// </summary>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset in the target buffer.</param>
            internal void Write(byte[] buffer, int offset) {
                var identifier = new Identifier { Class = (int)Class, IsConstructed = IsConstructed, TagNumber = NodeType.TagNumber };
                int headerLength = identifier.Write(buffer, offset);
                offset += headerLength;
                headerLength += LengthOctets.Write(IsDefiniteLength ? PayloadLength : -1, buffer, offset);
                Length = headerLength;
            }

            #endregion

            #region Equality

            public override int GetHashCode()
                => 31 * (31 * (31 * Identifier.GetHashCode() + Length.GetHashCode()) + NodeType.GetHashCode()) + PayloadLength.GetHashCode();

            public override bool Equals(object obj) =>
                (obj is Header h) && h.Identifier == Identifier && h.Length == Length && h.NodeType == NodeType && h.PayloadLength == PayloadLength;

            public static bool operator ==(Header a, Header b) =>
                ((object)a == null && (object)b == null) ||
                (
                    (object)a != null && (object)b != null &&
                    a.Identifier == b.Identifier && a.Length == b.Length && a.NodeType == b.NodeType && a.PayloadLength == b.PayloadLength
                );

            public static bool operator !=(Header a, Header b) => !(a == b);

            #endregion

        }

    }

}