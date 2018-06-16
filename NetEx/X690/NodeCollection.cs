using System;
using System.Collections;
using System.Collections.Generic;

namespace Woof.NetEx {

    /// <summary>
    /// <see href="https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf">ITU-T X.690-0207</see>
    /// </summary>
    public static partial class X690 {

        /// <summary>
        /// A node collection for ASN.1 sequences and sets.
        /// </summary>
        public class NodeCollection : IEnumerable<Node> {

            /// <summary>
            /// Internal list of items.
            /// </summary>
            private readonly List<Node> Items = new List<Node>();

            /// <summary>
            /// Parent node for all new nodes.
            /// </summary>
            private readonly Node Parent;

            /// <summary>
            /// Gets the contained nodes count.
            /// </summary>
            public int Count => Items.Count;

            /// <summary>
            /// Gets the node at specified index.
            /// </summary>
            /// <param name="i">Node index.</param>
            /// <returns></returns>
            public Node this[int i] => Items[i];

            /// <summary>
            /// Creates node collection for specified node.
            /// </summary>
            /// <param name="node"></param>
            public NodeCollection(Node node) => Parent = node;
            /// <summary>
            /// Adds specified node to the collection.
            /// </summary>
            /// <param name="node">Node to add.</param>
            public void Add(Node node) {
                if (Parent == null) throw new InvalidOperationException();
                node.Parent = Parent;
                Items.Add(node);
            }

            /// <summary>
            /// <see cref="IEnumerable"/> implementation.
            /// </summary>
            /// <returns></returns>
            public IEnumerator<Node> GetEnumerator() => Items.GetEnumerator();

            /// <summary>
            /// <see cref="IEnumerable"/> implementation.
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        }

    }

}