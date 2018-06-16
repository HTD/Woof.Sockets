using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Woof.TextEx;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {
    
        /// <summary>
        /// Standard name.
        /// </summary>
        private const string _X690 = "X.690";

        /// <summary>
        /// Exception header for all X.690 exception messages.
        /// </summary>
        private const string _ExceptionHeader = _X690 + " Exception :: ";

        /// <summary>
        /// Abstract Syntax Notation Node.
        /// </summary>
        public class Node {

            #region Fields

            /// <summary>
            /// Maximum binary payload size allowed, default 128MB.
            /// </summary>
            public static int MaxPayloadSizeAllowed = 0x08000000;

            /// <summary>
            /// Node metadata.
            /// </summary>
            public Header Header;

            /// <summary>
            /// Number of bytes read since constructed from stream.
            /// </summary>
            internal int BytesRead;

            #endregion

            #region Properties
            
            /// <summary>
            /// Gets or sets X.690 node value as boxed CLR primitive.
            /// </summary>
            public object BoxedCLRValue {
                get {
                    if (this is Set) {
                        if (Nodes != null) {
                            if (Nodes.Count == 1) return Nodes[0].BoxedCLRValue;
                            else if (Nodes.Count > 1) {
                                if (Nodes.All(i => i is Text)) return Nodes.Select(i => (i as Text).Value).ToArray();
                                else return Nodes.Select(i => i.BoxedCLRValue).ToArray();
                            }
                        }
                    }
                    if (this is Boolean bn) return bn.Value;
                    if (this is Integer nn) return nn.Value;
                    if (this is Enumerated en) return en.Value;
                    if (this is Text tn) return tn.Value;
                    return Payload;
                }
                set {
                    if (this is Set) {
                        if (Nodes != null) {
                            if (Nodes.Count == 1) Nodes[0].BoxedCLRValue = value;
                            else if (Nodes.Count > 1) {
                                if (Nodes.All(i => i is Text) && value is string[] t && Nodes.Count == t.Length) {
                                    for (int i = 0, l = t.Length; i < l; i++) (Nodes[i] as Text).Value = t[i];
                                }
                                else if (value is object[] o && Nodes.Count == o.Length) {
                                    for (int i = 0, l = o.Length; i < l; i++) Nodes[i].BoxedCLRValue = o[i];
                                }
                            }
                        }
                    }
                    else if (this is Boolean bn) bn.Value = (bool)value;
                    else if (this is Integer nn) nn.Value = (long)value;
                    else if (this is Enumerated en) en.Value = (int)value;
                    else if (this is Text tn) tn.Value = (string)value;
                    else Payload = (byte[])value;
                }
            }
            
            /// <summary>
            /// Gets or sets node's tag class as one from <see cref="Class"/> enumeration.
            /// </summary>
            public virtual TagClass Class => Header.Class;

            /// <summary>
            /// Gets this and all descendant nodes of the node in depth-first order (branches first).
            /// </summary>
            public IEnumerable<Node> DFS => DFSR.Reverse();

            /// <summary>
            /// Gets this and all descendant nodes of the node in depth-first order reversed (root first).
            /// </summary>
            public IEnumerable<Node> DFSR {
                get {
                    if (!HasNodes) { yield return this; yield break; }
                    var s = new Stack<Node>();
                    s.Push(this);
                    while (s.Any()) {
                        var e = s.Pop();
                        if (e.HasNodes) foreach (var child in e.Nodes.Reverse()) s.Push(child);
                        yield return e;
                    }
                }
            }

            /// <summary>
            /// Gets all descendant primitive elements of the node.
            /// </summary>
            public IEnumerable<Node> Elements {
                get {
                    if (!HasNodes) yield break;
                    var s = new Stack<Node>();
                    foreach (var child in Nodes.Reverse()) s.Push(child);
                    while (s.Any()) {
                        var e = s.Pop();
                        if (e.HasNodes) foreach (var child in e.Nodes.Reverse()) s.Push(child);
                        else yield return e;
                    }
                }
            }

            /// <summary>
            /// Gets the first node contained in this node.
            /// </summary>
            public Node First => Nodes?.FirstOrDefault();

            /// <summary>
            /// Gets the value indicating whether the node has any payload.
            /// </summary>
            public bool HasContent => Elements.FirstOrDefault()?.Payload?.Length > 0;

            /// <summary>
            /// Gets the value indicating whether the type is primitive or constructed.
            /// </summary>
            public bool IsConstructed => Header.IsConstructed;

            /// <summary>
            /// Gets or sets a value indicating whether the payload of this node should be encoded with definite length.
            /// </summary>
            public bool IsDefiniteLength {
                get => Header.IsDefiniteLength; set => Header.IsDefiniteLength = value;
            }

            /// <summary>
            /// Gets a value indicating wheter the node is a special end-session message.
            /// </summary>
            public bool IsEndSessionMessage {
                get {
                    if (this is Sequence seq) {
                        var application = seq.Nodes.FirstOrDefault(i => i.Class == TagClass.Application);
                        return application != null && !application.HasNodes;
                    }
                    return false;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the node is the last child node of its parent.
            /// </summary>
            public bool IsLast => Parent == null ? true : ((object)this == (object)Parent.Last);

            /// <summary>
            /// Gets the last node contained in this node;
            /// </summary>
            public Node Last => Nodes?.LastOrDefault();

            /// <summary>
            /// Gets the node's nesting level.
            /// </summary>
            public int Level {
                get {
                    var node = this;
                    var level = 0;
                    while (node.Parent != null) {
                        node = node.Parent;
                        level++;
                    }
                    return level;
                }
            }

            /// <summary>
            /// Gets the child nodes of this node.
            /// </summary>
            public NodeCollection Nodes { get; }

            /// <summary>
            /// Gets the universal node type as a member of <see cref="UniversalNodeType1"/> enumeration.
            /// </summary>
            public virtual NodeType NodeType => Header.NodeType;

            /// <summary>
            /// Gets a value indicating whether this node has at least one child node.
            /// </summary>
            public bool HasNodes => Nodes != null && Nodes.Any();

            /// <summary>
            /// Gets parent <see cref="Node"/> of this node. Gets null for root nodes.
            /// </summary>
            public Node Parent { get; internal set; }

            /// <summary>
            /// Gets or sets raw binary content.
            /// </summary>
            public byte[] Payload { get; set; }

            /// <summary>
            /// Gets the root of this node.
            /// </summary>
            public Node Root {
                get {
                    var root = this;
                    while (root.Parent != null) root = root.Parent;
                    return root;
                }
            }

            /// <summary>
            /// Gets the node value as formatted string.
            /// </summary>
            public string StringValue {
                get {
                    if (this is Null) return "null";
                    if (this is Boolean _bool) return _bool.Value.ToString();
                    if (this is Integer _integer) {
                        return _integer.ValueBits <= 64
                            ? _integer.Value.ToString()
                            : string.Join("", Payload.Select(i => i.ToString("x2")));
                        
                    }
                    if (this is Enumerated _enum) {
                        return _enum.ValueBits <= 32
                            ? _enum.Value.ToString()
                            : string.Join("", Payload.Select(i => i.ToString("x2")));
                    }
                    if (this is Text _text) return '"' + _text.Value + '"';
                    if (IsConstructed) return '[' + (Nodes?.Count ?? 0).ToString() + ']';
                    return Payload.AsHexOrASCII();
                }
            }

            /// <summary>
            /// Gets the node type as string.
            /// </summary>
            public string StringType {
                get {
                    if (NodeType is RootType) return "Root";
                    if (Class == TagClass.Universal) return NodeType.ToString();
                    return NodeType.ToString() + ' ' + NodeType.TagNumber.ToString();
                }
            }

            /// <summary>
            /// Gets a description of the node as branch.
            /// </summary>
            public string StringBranch => IsConstructed ? $"[{StringType}]" : $"{StringValue} [{StringType}]";


            /// <summary>
            /// Gets a description of the node as text tree.
            /// </summary>
            public string StringTree {
                get {
                    var b = new StringBuilder();
                    var l = 0;
                    var h = new HashSet<int>();
                    var m = false;
                    foreach (var node in DFSR) {
                        l = node.Level - Level;
                        m = node.IsLast;
                        for (int i = 0; i < l; i++) b.Append(h.Contains(i) ? "  " : "│ ");
                        b.Append(m ? "└─" : "├─");
                        b.AppendLine((node.HasNodes ? "┬" : "─")  + node.StringBranch);
                        if (m) h.Add(l);
                    }
                    return b.ToString();
                }
            }

            #endregion

            #region Constructors

            /// <summary>
            /// Creates new node from parameters.
            /// </summary>
            /// <param name="class">
            /// <list type="number">
            /// <item><term>0</term> <description>Universal</description></item>
            /// <item><term>1</term> <description>Application</description></item>
            /// <item><term>2</term> <description>Context-specific</description></item>
            /// <item><term>3</term> <description>Private</description></item>
            /// </list>
            /// </param>
            /// <param name="tagNumber">Tag number as specified in X.690, application or private documentation.</param>
            /// <param name="isConstructed">True if the tag is constructed and thus contains other tags.</param>
            public Node(int @class, int tagNumber, bool isConstructed) {
                Header = new Header(new Identifier { Class = @class, TagNumber = tagNumber, IsConstructed = isConstructed });
                if (Header.IsConstructed) Nodes = new NodeCollection(this);
            }

            /// <summary>
            /// Constructs an empty ASN.1 node from constructed header.
            /// </summary>
            /// <param name="header">Node header.</param>
            internal Node(Header header) {
                Header = header;
                if (Header.IsConstructed) Nodes = new NodeCollection(this);
                if (!Header.IsDefiniteLength) Header.PayloadLength = -1;
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Gets all descendant nodes of the node of type TNode in DFS order, leaves only.
            /// </summary>
            /// <typeparam name="TNode">Node type.</typeparam>
            /// <returns>All leaves of type TNode.</returns>
            public IEnumerable<TNode> All<TNode>() where TNode : Node => DFSR.OfType<TNode>();

            /// <summary>
            /// Gets all descendant nodes of the node of type TNode in DFS order, leaves only.
            /// </summary>
            /// <typeparam name="TNode">Node type.</typeparam>
            /// <param name="predicate">A function to test each element for a condition.</param>
            /// <returns></returns>
            public IEnumerable<TNode> All<TNode>(Func<TNode, bool> predicate) where TNode : Node
                => DFSR.OfType<TNode>().Where(predicate);

            /// <summary>
            /// Calculates payload length of the specified node taking into accout all descendant nodes.
            /// </summary>
            /// <returns>Payload length as written in header.</returns>
            public int CalculateHeaderPayloadLength() => Header.CalculatePayloadLength(this);

            /// <summary>
            /// Determines if any of the text leaves matches the wildcard pattern specified.
            /// </summary>
            /// <param name="pattern">Wildcard pattern.</param>
            /// <returns>True if pattern was found.</returns>
            public bool ContainsPattern(string pattern) => All<Text>().Any(i => i.Value.MatchesPattern(pattern));

            /// <summary>
            /// Determines if any of the text leaves matches the wildcard pattern specified. If so, returns the first match, whole text.
            /// </summary>
            /// <param name="pattern">Wildcard pattern.</param>
            /// <param name="text">Text matched.</param>
            /// <returns></returns>
            public bool ContainsPattern(string pattern, out string text) {
                text = All<Text>().FirstOrDefault(i => i.Value.MatchesPattern(pattern))?.Value;
                return text != null;
            }

            /// <summary>
            /// Replaces all values of text leaves matching the <paramref name="search"/> pattern with the <paramref name="replacement"/> pattern.
            /// </summary>
            /// <param name="search">Search pattern.</param>
            /// <param name="replacement">Replacement pattern.</param>
            public void PatternReplace(string search, string replacement) {
                var pattern = new TextPattern(search);
                foreach (var node in All<Text>()) {
                    if (pattern.IsMatch(node.Value)) node.Value = pattern.Replace(node.Value, replacement);
                }
            }

            /// <summary>
            /// Returs debug string representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString() => StringValue;

            /// <summary>
            /// Writes only the header to the stream, use with <see cref="WriteContent(Stream)"/>.
            /// Buffered write.
            /// </summary>
            /// <param name="stream"></param>
            /// <returns>Number of bytes written.</returns>
            public int WriteHeader(Stream stream) {
                Header.CalculatePayloadLength(this);
                Header.Write(stream);
                return Header.Length;
            }

            /// <summary>
            /// Writes only the payload to the stream, use with <see cref="WriteHeader(Stream)"/>.
            /// Buffered write.
            /// </summary>
            /// <param name="stream"></param>
            /// <returns>Number of bytes written.</returns>
            public int WriteContent(Stream stream) {
                Header.CalculatePayloadLength(this);
                var buffer = new byte[Header.PayloadLength];
                var length = WritePayload(buffer, 0);
                stream.Write(buffer, 0, length);
                return length;
            }

            /// <summary>
            /// Writes ASN.1 node to a binary stream in one continuous block.
            /// </summary>
            /// <param name="stream">Output stream</param>
            /// <param name="buffer">Output buffer (optional)</param>
            /// <returns>Number of bytes written.</returns>
            public int WriteBuffered(Stream stream, byte[] buffer = null) {
                Header.CalculatePayloadLength(this);
                var maxlength = Header.MessageLength;
                if (buffer == null || buffer.Length < maxlength) buffer = new byte[maxlength];
                var length = Write(buffer, 0);
                stream.Write(buffer, 0, length);
                return length;
            }

            /// <summary>
            /// Implicitly converts the node into debug string representation.
            /// </summary>
            /// <param name="node"></param>
            public static implicit operator string(Node node) => node.StringValue;

            #endregion

            #region Non-public methods

            /// <summary>
            /// Reads the node's content from stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            /// <returns>Awaitable task.</returns>
            internal void Read(Stream stream) {
                BytesRead = Header.Length;
                if (IsConstructed) ReadChildNodes(stream);
                else ReadPayload(stream);
            }

            /// <summary>
            /// Reads the node's content from buffer.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of the node within the buffer.</param>
            /// <param name="length">Available data length.</param>
            internal void Read(byte[] buffer, int offset, int length) {
                BytesRead = Header.Length;
                if (IsConstructed) ReadChildNodes(buffer, offset, length);
                else ReadPayload(buffer, offset, length);
            }

            /// <summary>
            /// Writes ASN.1 node to a binary stream.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            /// <returns>Number of bytes written.</returns>
            internal int Write(Stream stream) {
                var index = 0;
                if (Parent == null) Header.CalculatePayloadLength(this);
                if (!(NodeType is RootType)) {
                    Header.Write(stream);
                    index += Header.Length;
                }
                index += WritePayload(stream);
                return index;
            }

            /// <summary>
            /// Writes ASN.1 node to an output buffer.
            /// </summary>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offset within the buffer.</param>
            /// <returns>Number of bytes written.</returns>
            internal int Write(byte[] buffer, int offset) {
                int index = 0;
                if (Parent == null) Header.CalculatePayloadLength(this);
                if (!(NodeType is RootType)) {
                    Header.Write(buffer, offset + index);
                    index += Header.Length;
                }
                var length = WritePayload(buffer, offset + index);
                index += length;
                return index;
            }

            /// <summary>
            /// Reads all nodes contained in this node from stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            internal void ReadChildNodes(Stream stream) {
                int position = 0;
                if (Header.IsDefiniteLength) {
                    while (position < Header.PayloadLength) {
                        var header = Header.Read(stream);
                        if (header == null) throw new InvalidDataException($"{_ExceptionHeader}Unexpected EndOfContent while reading children.");
                        position += header.Length;
                        BytesRead += header.Length;
                        var node = header.ReadDerivedNode(stream);
                        var payloadLength = node.BytesRead - node.Header.Length;
                        if (payloadLength <= 0) {
                            Nodes.Add(node);
                            continue;
                        }
                        position += payloadLength;
                        BytesRead += payloadLength;
                        Nodes.Add(node);
                    }
                }
                else {
                    Header header = null;
                    while ((header = Header.Read(stream)) != null) {
                        BytesRead += header.Length;
                        var node = header.ReadDerivedNode(stream);
                        if (node is EndOfContent) return;
                        var payloadLength = node.BytesRead - header.Length;
                        if (payloadLength <= 0) {
                            Nodes.Add(node);
                            continue;
                        }
                        BytesRead += payloadLength;
                        Nodes.Add(node);
                    }
                    throw new InvalidDataException($"{_ExceptionHeader}Unexpected EndOfContent while reading children.");

                }
            }

            /// <summary>
            /// Reads all nodes contained in this node from stream.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of the data within the buffer.</param>
            /// <param name="length">Available data length.</param>
            internal void ReadChildNodes(byte[] buffer, int offset, int length) {
                int position = 0;
                if (Header.IsDefiniteLength) {
                    while (position < Header.PayloadLength) {
                        var header = Header.Read(buffer, offset + position);
                        length -= header.Length;
                        position += header.Length;
                        BytesRead += header.Length;
                        if (header == null) throw new InvalidDataException($"{_ExceptionHeader}Unexpected EndOfContent while reading children.");
                        var node = header.ReadDerivedNode(buffer, offset + position, length);
                        var payloadLength = node.BytesRead - node.Header.Length;
                        if (payloadLength < 0) {
                            Nodes.Add(node);
                            continue;
                        }
                        length -= payloadLength;
                        position += payloadLength;
                        BytesRead += payloadLength;
                        Nodes.Add(node);
                    }
                }
                else {
                    while (length > 0) {
                        var header = Header.Read(buffer, offset);
                        offset += header.Length;
                        length -= header.Length;
                        BytesRead += header.Length;
                        var node = header.ReadDerivedNode(buffer, offset, length);
                        if (node is EndOfContent) return;
                        var payloadLength = node.BytesRead - header.Length;
                        if (payloadLength < 0) {
                            Nodes.Add(node);
                            continue;
                        }
                        offset += payloadLength;
                        length -= payloadLength;
                        BytesRead += payloadLength;
                        Nodes.Add(node);
                    }
                    throw new InvalidDataException($"{_ExceptionHeader}Unexpected EndOfContent while reading children.");
                }
            }

            /// <summary>
            /// Reads ASN.1 data node from binary stream.
            /// </summary>
            /// <param name="stream">Input stream.</param>
            protected virtual int ReadPayload(Stream stream) {
                int bytesRead = 0;
                if (IsConstructed) throw new InvalidDataException($"{_ExceptionHeader}Tried to get constructed node as binary data.");
                if (Header.PayloadLength == 0) { Payload = null; return 0; }
                if (Header.PayloadLength > MaxPayloadSizeAllowed)
                    throw new InvalidDataException($"{_ExceptionHeader}MaxPayloadSizeAllowed exceeded for definite length message. {Header.PayloadLength} bytes requested.");
                if (Header.IsDefiniteLength) {
                    if (Header.PayloadLength > 0) {
                        Payload = new byte[Header.PayloadLength];
                        bytesRead = stream.Read(Payload, 0, Header.PayloadLength);
                    }
                    BytesRead += Header.PayloadLength;
                    return Header.PayloadLength;
                }
                else {
                    int octet, lastOctet = 1;
                    var index = 0;
                    var payload = new List<byte>();
                    while ((octet = stream.ReadByte()) >= 0) {
                        payload.Add((byte)octet);
                        index++;
                        if (octet == 0 && lastOctet == 0) {
                            if (index > 2) Payload = payload.Take(index - 2).ToArray();
                            BytesRead += index;
                            return index;
                        }
                        lastOctet = octet;
                    }
                    throw new InvalidDataException($"{_ExceptionHeader}Terminating sequence not found in indefinite length block.");
                }
            }

            /// <summary>
            /// Reads ASN.1 data node from binary stream.
            /// </summary>
            /// <param name="buffer">Input buffer.</param>
            /// <param name="offset">The offset of payload data start.</param>
            /// <param name="length">Payload length.</param>
            /// <returns>Number of bytes read.</returns>
            protected int ReadPayload(byte[] buffer, int offset, int length) {
                if (IsConstructed) throw new InvalidDataException($"{_ExceptionHeader}Tried to get constructed node as binary data.");
                if (Header.PayloadLength == 0) { Payload = null; return 0; }
                if (Header.PayloadLength > MaxPayloadSizeAllowed)
                    throw new InvalidDataException($"{_ExceptionHeader}MaxPayloadSizeAllowed exceeded for definite length message. {Header.PayloadLength} bytes requested.");
                if (Header.IsDefiniteLength) {
                    if (Header.PayloadLength > 0) {
                        if (offset == 0 && length == Header.PayloadLength) Payload = buffer;
                        else {
                            Payload = new byte[Header.PayloadLength];
                            Buffer.BlockCopy(buffer, offset, Payload, 0, Header.PayloadLength);
                        }
                    }
                    BytesRead += Header.PayloadLength;
                    return Header.PayloadLength;
                }
                else {
                    byte octet, lastOctet = 1;
                    var index = 0;
                    while (index < length) {
                        octet = buffer[offset + index++];
                        if (octet == 0 && lastOctet == 0) {
                            if (index > 2) {
                                Payload = new byte[index - 2];
                                Buffer.BlockCopy(buffer, offset, Payload, 0, index - 2);
                            }
                            BytesRead += index;
                            return index;
                        }
                        lastOctet = octet;
                    }
                    throw new InvalidDataException($"{_ExceptionHeader}Terminating sequence not found in indefinite length block.");
                }
            }

            /// <summary>
            /// Writes ASN.1 node's payload to a binary stream.
            /// </summary>
            /// <param name="stream">Output stream.</param>
            protected virtual int WritePayload(Stream stream) {
                var index = 0;
                if (IsConstructed) foreach (var node in Nodes) index = node.Write(stream);
                else if (Payload != null && Payload.Length > 0) {
                    stream.Write(Payload, 0, Payload.Length);
                    index += Payload.Length;
                }
                if (!Header.IsDefiniteLength) {
                    stream.WriteByte(0);
                    stream.WriteByte(0);
                    index += 2;
                }
                return index;
            }

            /// <summary>
            /// Writes ASN.1 node's payload to an output buffer.
            /// </summary>
            /// <param name="buffer">Output buffer.</param>
            /// <param name="offset">Offest within the buffer.</param>
            /// <returns>Number of bytes written.</returns>
            protected virtual int WritePayload(byte[] buffer, int offset) {
                var index = 0;
                if (IsConstructed) foreach (var node in Nodes) index += node.Write(buffer, offset + index);
                else if (Payload != null && Payload.Length > 0) {
                    Buffer.BlockCopy(Payload, 0, buffer, offset + index, Payload.Length);
                    index += Payload.Length;
                    
                }
                if (!Header.IsDefiniteLength) {
                    buffer[offset + index++] = 0;
                    buffer[offset + index++] = 0;
                }
                return index;
            }

            

            ///// <summary>
            ///// Returns descriptive string representation for debugging purposes.
            ///// </summary>
            ///// <returns>X.690 node information as string.</returns>
            //private string ToDebugString() {
            //    if (IsConstructed) return $"{StringValue} [{StringType}]:";
            //    else return $"{StringValue} [{StringType}]";
            //}

            #endregion

            #region Equality

            public override int GetHashCode() => 31 * (31 * Header.GetHashCode() + Payload.GetHashCode()) + Nodes.GetHashCode();

            public override bool Equals(object obj) => (obj is Node n) &&
                n.Header == Header &&
                (n.Payload == null && Payload == null || (n.Payload != null && Payload != null && n.Payload.SequenceEqual(Payload))) &&
                n.Level == Level &&
                (n.Nodes == null && Nodes == null || (n.Nodes != null && Nodes != null && n.Nodes.SequenceEqual(Nodes)));

            #endregion

        }

        #region X.690 static methods

        /// <summary>
        /// Reads X.690 message from stream.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <returns>X.690 node.</returns>
        public static Node Read(Stream stream) {
            var header = Header.Read(stream);
            if (header == null) return null;
            return header.ReadDerivedNode(stream);
        }

        /// <summary>
        /// Reads X.690 message from buffer.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="offset">The offset of the message within the buffer.</param>
        /// <param name="length">Available data length.</param>
        /// <returns></returns>
        public static Node Read(byte[] buffer, int offset, int length) {
            var header = Header.Read(buffer, offset);
            offset += header.Length;
            length -= header.Length;
            if (header == null) return null;
            return header.ReadDerivedNode(buffer, offset, length);
        }

        #endregion

    }

}