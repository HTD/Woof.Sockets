using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Woof.Algorithms;
using Woof.NetEx;

/// <summary>
/// A toolset for testing X.690 protocol implementation correctness.
/// </summary>
static class TT {

    /// <summary>
    /// Gets a sequence of 210 partially random identifiers roughly representing the most of the property configurations.
    /// </summary>
    public static IEnumerable<X690.Identifier> X_960_Identifiers => X_960_Universal_Identifiers.Concat(X_960_Custom_Identifiers);

    /// <summary>
    /// Gets all (120) kinds of X.960 universal class identifiers with random IsConstructed property for classes with both types allowed.
    /// </summary>
    static IEnumerable<X690.Identifier> X_960_Universal_Identifiers {
        get {
            for (int cl = 0; cl < 4; cl++)
                for (int tn = 0; tn < 31; tn++)
                    yield return new X690.Identifier { Class = cl, TagNumber = tn, IsConstructed = RandomIsUniversalClassConstructed[tn] };
        }
    }

    /// <summary>
    /// Gets 90 random X.960 custom identifiers with tag numbers [31..65535].
    /// </summary>
    static IEnumerable<X690.Identifier> X_960_Custom_Identifiers {
        get {
            for (int cl = 1; cl < 4; cl++)
                for (int i = 0; i < 31; i++) {
                    int tn = PRNG.Next(31, 65536);
                    yield return new X690.Identifier { Class = cl, TagNumber = tn, IsConstructed = PRNG.NextBool() };
                }
        }
    }

    /// <summary>
    /// Generates a random string for testing text nodes.
    /// </summary>
    /// <returns></returns>
    static string RandomString() => Guid.NewGuid().ToString();

    /// <summary>
    /// Outputs the node as text tree and it's binary representation.
    /// </summary>
    /// <param name="node">The node under test.</param>
    /// <param name="stream">Stream containing encoded data.</param>
    public static void Peek(X690.Node node, MemoryStream stream) {
        var peek = stream.ToArray();
        Console.WriteLine(node.StringTree);
        Console.WriteLine(peek.Length.ToString("x2") + ": " + String.Join(" ", peek.Select(b => b.ToString("x2"))));
    }

    /// <summary>
    /// Outputs the node as text tree and it's binary representation.
    /// </summary>
    /// <param name="node">The node under test.</param>
    /// <param name="buffer">Buffer containing encoded data.</param>
    /// <param name="length">Binary data length.</param>
    public static void Peek(X690.Node node, byte[] buffer, int length) {
        Console.WriteLine(node.StringTree);
        Console.WriteLine(length.ToString("x2") + ": " + String.Join(" ", buffer.Take(length).Select(b => b.ToString("x2"))));
    }

    /// <summary>
    /// Generates random nested X.690 node.
    /// </summary>
    /// <param name="branches">Maximum number of branches to use.</param>
    /// <param name="leaves">Maximum number of leaves to use.</param>
    /// <param name="depth">Maximum nessting level.</param>
    /// <param name="lengthEncoding">Definite, indefinite or random.</param>
    /// <returns>Random X.690 node.</returns>
    static public X690.Node RandomBranch(int branches, int leaves, int depth, LengthEncoding lengthEncoding = LengthEncoding.Definite) {
        var stack = new Stack<X690.Node>();
        for (int i = 0; i < PRNG.Next(branches + 1); i++) stack.Push(RandomBranch(lengthEncoding));
        for (int i = 0; i < PRNG.Next(leaves + 1); i++) stack.Push(RandomLeaf(lengthEncoding));
        var root = RandomBranch(lengthEncoding);
        foreach (var node in stack.ToArray().Shuffled()) root.Nodes.Add(node);
        IEnumerable<X690.Node> target = root.Nodes.Where(i => i.IsConstructed);
        while (--depth > 0) {
            foreach (var node in target) node.Nodes.Add(RandomBranch(branches, leaves, 0, lengthEncoding));
            target = target.SelectMany(i => i.Nodes.Where(j => j.HasNodes));
        }
        return root;
    }

    /// <summary>
    /// Generates a single branch of X.690 node tree.
    /// </summary>
    /// <param name="lengthEncoding">Definite, indefinite or random.</param>
    /// <returns>A tree branch.</returns>
    static X690.Node RandomBranch(LengthEncoding lengthEncoding = LengthEncoding.Definite) => PRNG.NextBool()
            ? new X690.Sequence() { IsDefiniteLength = lengthEncoding.IsDefiniteLength() } as X690.Node
            : new X690.Set() { IsDefiniteLength = lengthEncoding.IsDefiniteLength() } as X690.Node;

    /// <summary>
    /// Generates a single random leaf of X.690 node tree.
    /// </summary>
    /// <param name="lengthEncoding">Definite, indefinite or random.</param>
    /// <returns>A tree leaf.</returns>
    static X690.Node RandomLeaf(LengthEncoding lengthEncoding = LengthEncoding.Definite) {
        switch (PRNG.Next(0, 4)) {
            case 0: return new X690.Null();
            case 1: return new X690.Boolean(PRNG.NextBool());
            case 2: return new X690.Integer(PRNG.Next(int.MinValue, int.MaxValue));
            case 3: return new X690.Text(RandomString()) { IsDefiniteLength = lengthEncoding.IsDefiniteLength() };
        }
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Pseudo-random numbers generator.
    /// </summary>
    public static IPseudoRandomNumberGenerator PRNG = ArrayFisherYates.PRNG = new XorShift32();

    /// <summary>
    /// Random but valid IsConstructed dictionary for X.960 universal tags.
    /// </summary>
    static readonly Dictionary<int, bool> RandomIsUniversalClassConstructed = new Dictionary<int, bool> {
        [0] = false,
        [1] = false,
        [2] = false,
        [3] = PRNG.NextBool(),
        [4] = PRNG.NextBool(),
        [5] = false,
        [6] = false,
        [7] = PRNG.NextBool(),
        [8] = true,
        [9] = false,
        [10] = false,
        [11] = true,
        [12] = PRNG.NextBool(),
        [13] = false,
        [14] = false,
        [15] = false,
        [16] = true,
        [17] = true,
        [18] = PRNG.NextBool(),
        [19] = PRNG.NextBool(),
        [20] = PRNG.NextBool(),
        [21] = PRNG.NextBool(),
        [22] = PRNG.NextBool(),
        [23] = PRNG.NextBool(),
        [24] = PRNG.NextBool(),
        [25] = PRNG.NextBool(),
        [26] = PRNG.NextBool(),
        [27] = PRNG.NextBool(),
        [28] = PRNG.NextBool(),
        [29] = PRNG.NextBool(),
        [30] = PRNG.NextBool()
    };

}

/// <summary>
/// Length encoding enumeration.
/// </summary>
public enum LengthEncoding { Definite, Indefinite, Random };

/// <summary>
/// Extends <see cref="LengthEncoding"/> enumeration.
/// </summary>
public static class LengthEncodingExtensions {

    /// <summary>
    /// Pseudo-random number generator.
    /// </summary>
    static readonly IPseudoRandomNumberGenerator PRNG = TT.PRNG;

    /// <summary>
    /// Returns true if requested length encoding represents definite length encoding.
    /// </summary>
    /// <param name="encoding">Requested encoding.</param>
    /// <returns>True for definite encoding.</returns>
    public static bool IsDefiniteLength(this LengthEncoding encoding) {
        switch (encoding) {
            case LengthEncoding.Definite: return true;
            case LengthEncoding.Indefinite: return false;
            default: return PRNG.NextBool();
        }
    }

}