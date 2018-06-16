using System;
using System.Collections.Generic;
using System.IO;
using Woof.NetEx;
using Woof.TextEx;
using Xunit;

/// <summary>
/// Aims to prove the correctness of the implementation X.690 BER protocol for whole nodes.
/// </summary>
public class NetEx_X690_Nodes {

    [Fact]
    public void T01_EndOfContent_Stream_IO() {
        using (var stream = new MemoryStream()) {
            var x = new X690.EndOfContent();
            x.Write(stream);
            TT.Peek(x, stream);
            stream.Position = 0;
            var y = X690.Read(stream);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T02_EndOfContent_Buffered_IO() {
        var buffer = new byte[3];
        var x = new X690.EndOfContent();
        var length = x.Write(buffer, 0);
        TT.Peek(x, buffer, length);
        var y = X690.Read(buffer, 0, length);
        Assert.Equal(x, y);
        Assert.Equal(y.Header.MessageLength, length);
    }

    [Fact]
    public void T03_Null_Stream_IO() {
        using (var stream = new MemoryStream()) {
            var x = new X690.Null();
            x.Write(stream);
            TT.Peek(x, stream);
            stream.Position = 0;
            var y = X690.Read(stream);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T04_Null_Buffered_IO() {
        var buffer = new byte[3];
        var x = new X690.Null();
        var length = x.Write(buffer, 0);
        TT.Peek(x, buffer, length);
        var y = X690.Read(buffer, 0, length);
        Assert.Equal(x, y);
        Assert.Equal(y.Header.MessageLength, length);
    }

    [Fact]
    public void T05_Boolean_Stream_IO() {
        foreach (var i in new bool[] { true, false }) {
            using (var stream = new MemoryStream()) {
                var x = new X690.Boolean(i);
                x.Write(stream);
                TT.Peek(x, stream);
                stream.Position = 0;
                var y = (X690.Read(stream)) as X690.Boolean;
                Assert.Equal(x, y);
                Assert.Equal(i, x.Value);
                Assert.Equal(i, y.Value);
            }
        }
    }

    [Fact]
    public void T06_Boolean_Buffered_IO() {
        var buffer = new byte[3];
        foreach (var i in new bool[] { true, false }) {
            var x = new X690.Boolean(i);
            var length = x.Write(buffer, 0);
            TT.Peek(x, buffer, length);
            var y = (X690.Read(buffer, 0, length)) as X690.Boolean;
            Assert.Equal(x, y);
            Assert.Equal(y.Header.MessageLength, length);
            Assert.Equal(i, x.Value);
            Assert.Equal(i, y.Value);
        }
    }

    [Fact]
    public void T07_Integer_Stream_IO() {
        for (int i = 0; i < 256; i++) {
            var n = TT.PRNG.Next(Int32.MinValue, Int32.MaxValue);
            using (var stream = new MemoryStream()) {
                var x = new X690.Integer(n);
                x.Write(stream);
                TT.Peek(x, stream);
                stream.Position = 0;
                var y = (X690.Read(stream)) as X690.Integer;
                Assert.Equal(n, x.Value);
                Assert.Equal(x.ValueBits, y.ValueBits);
                Assert.Equal(n, y.Value);
            }
        }
    }

    [Fact]
    public void T08_Integer_Buffered_IO() {
        var buffer = new byte[10];
        for (int i = 0; i < 256; i++) {
            var n = TT.PRNG.Next(Int32.MinValue, Int32.MaxValue);
            var x = new X690.Integer(n);
            var length = x.Write(buffer, 0);
            TT.Peek(x, buffer, length);
            var y = (X690.Read(buffer, 0, length)) as X690.Integer;
            Assert.Equal(n, x.Value);
            Assert.Equal(x.ValueBits, y.ValueBits);
            Assert.Equal(n, y.Value);
            Assert.Equal(x.Header.MessageLength, length);
        }
    }

    [Fact]
    public void T09_Text_DER_Stream_IO() {
        foreach (var i in new string[] { "", "WOOF!" }) {
            using (var stream = new MemoryStream()) {
                var x = new X690.Text(i);
                x.Write(stream);
                TT.Peek(x, stream);
                stream.Position = 0;
                var y = (X690.Read(stream)) as X690.Text;
                Assert.Equal(i, x.Value);
                Assert.Equal(i, y.Value);
                Assert.Equal(x, y);
            }
        }
    }

    [Fact]
    public void T10_Text_DER_Buffered_IO() {
        var buffer = new byte[10];
        foreach (var i in new string[] { "", "WOOF!" }) {
            var x = new X690.Text(i);
            var length = x.Write(buffer, 0);
            TT.Peek(x, buffer, length);
            var y = (X690.Read(buffer, 0, length)) as X690.Text;
            Assert.Equal(i, x.Value);
            Assert.Equal(i, y.Value);
            Assert.Equal(x.Header.MessageLength, length);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T11_Text_BER_Stream_IO() {
        foreach (var i in new string[] { "WOOF!", "" }) {
            using (var stream = new MemoryStream()) {
                var x = new X690.Text(i) { IsDefiniteLength = false };
                Assert.Equal(-1, x.Header.MessageLength);
                x.Write(stream);
                TT.Peek(x, stream);
                stream.Position = 0;
                var y = (X690.Read(stream)) as X690.Text;
                Assert.Equal(i, x.Value);
                Assert.Equal(i, y.Value);
                Assert.False(y.Header.IsDefiniteLength);
                Assert.Equal(-1, y.Header.MessageLength);
                Assert.Equal(x, y);
            }
        }
    }

    [Fact]
    public void T12_Text_BER_Bufered_IO() {
        var buffer = new byte[10];
        foreach (var i in new string[] { "", "WOOF!" }) {
            var x = new X690.Text(i) { IsDefiniteLength = false };
            Assert.Equal(-1, x.Header.MessageLength);
            var length = x.Write(buffer, 0);
            TT.Peek(x, buffer, length);
            var y = (X690.Read(buffer, 0, 16)) as X690.Text;
            Assert.Equal(i, x.Value);
            Assert.Equal(i, y.Value);
            Assert.False(y.Header.IsDefiniteLength);
            Assert.Equal(-1, y.Header.MessageLength);
        }
    }

    [Fact]
    public void T13_Sequence_BER_Stream_IO() {
        using (var stream = new MemoryStream()) {
            var x = new X690.Sequence() { IsDefiniteLength = TT.PRNG.NextBool() };
            var leaves = new X690.Node[] {
                    new X690.Null(),
                    new X690.Boolean(false),
                    new X690.Boolean(true),
                    new X690.Integer(-1),
                    new X690.Integer(0),
                    new X690.Integer(1),
                    new X690.Text("WOOF!") { IsDefiniteLength = false },
                    new X690.Text("GRR.."),
                    new X690.Text("WOOF!") { IsDefiniteLength = false }
                };
            foreach (var leaf in leaves) x.Nodes.Add(leaf);
            x.Write(stream);
            TT.Peek(x, stream);
            stream.Position = 0;
            var y = X690.Read(stream);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T14_Sequence_BER_Buffered_IO() {
        var buffer = new byte[128];
        var x = new X690.Sequence() { IsDefiniteLength = TT.PRNG.NextBool() };
        var leaves = new X690.Node[] {
                new X690.Null(),
                new X690.Boolean(false),
                new X690.Boolean(true),
                new X690.Integer(-1),
                new X690.Integer(0),
                new X690.Integer(1),
                new X690.Text("WOOF!") { IsDefiniteLength = false },
                new X690.Text("GRR.."),
                new X690.Text("WOOF!") { IsDefiniteLength = false }
            };
        foreach (var leaf in leaves) x.Nodes.Add(leaf);
        var length = x.Write(buffer, 0);
        TT.Peek(x, buffer, length);
        var y = X690.Read(buffer, 0, length);
        Assert.Equal(x, y);
    }

    [Fact]
    public void T15_Sequence_Nested_BER_Stream_IO() {
        using (var stream = new MemoryStream()) {
            var x = new X690.Sequence();
            var s = new X690.Sequence();
            var leaves = new X690.Node[] {
                    new X690.Null(),
                    new X690.Boolean(false),
                    new X690.Boolean(true),
                    new X690.Integer(-1),
                    new X690.Integer(0),
                    new X690.Integer(1),
                    new X690.Text("WOOF!") { IsDefiniteLength = false },
                    new X690.Text("GRR.."),
                    new X690.Text("WOOF!") { IsDefiniteLength = false },
                    new X690.Sequence()
                };
            foreach (var leaf in leaves) s.Nodes.Add(leaf);
            x.Nodes.Add(s);
            x.Write(stream);
            stream.Position = 0;
            TT.Peek(x, stream);
            var y = X690.Read(stream);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T16_Sequence_Nested_BER_Buffered_IO() {
        var buffer = new byte[64];
        var x = new X690.Sequence();
        var s = new X690.Sequence();
        var leaves = new X690.Node[] {
                new X690.Null(),
                new X690.Boolean(false),
                new X690.Boolean(true),
                new X690.Integer(-1),
                new X690.Integer(0),
                new X690.Integer(1),
                new X690.Text("WOOF!") { IsDefiniteLength = false },
                new X690.Text("GRR.."),
                new X690.Text("WOOF!") { IsDefiniteLength = false },
                new X690.Sequence()
            };
        foreach (var leaf in leaves) s.Nodes.Add(leaf);
        x.Nodes.Add(s);
        var length = x.Write(buffer, 0);
        TT.Peek(x, buffer, length);
        var y = X690.Read(buffer, 0, length);
        Assert.Equal(x, y);
    }

    [Fact]
    public void T17_Random_BER_Stream_IO() {
        X690.Node x, y;
        for (var iterations = 0; iterations < 16; iterations++) {
            using (var stream = new MemoryStream()) {
                x = TT.RandomBranch(8, 8, 8, LengthEncoding.Random);
                x.Write(stream);
                stream.Position = 0;
                y = X690.Read(stream);
            }
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T18_Random_BER_Buffered_IO() {
        X690.Node x, y;
        var buffer = new byte[8192];
        for (var iterations = 0; iterations < 16; iterations++) {
            x = TT.RandomBranch(8, 8, 8, LengthEncoding.Random);
            var length = x.Write(buffer, 0);
            Console.WriteLine(length);
            y = X690.Read(buffer, 0, length);
            Assert.Equal(x, y);
        }
    }

}

