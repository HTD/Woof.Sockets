using System;
using System.IO;
using System.Linq;
using Woof.Algorithms;
using Woof.NetEx;
using Xunit;

public class NetEx_X690_Headers {

    [Fact]
    public void T01_Identifier_Stream_IO() {
        using (var s = new MemoryStream()) {
            foreach (var i in TT.X_960_Identifiers) {
                s.Position = 0;
                var l = i.Write(s);
                s.Position = 0;
                var r = X690.Identifier.Read(s);
                Assert.Equal(i, r);
                Assert.Equal(i.Length, l);
            }
        }
    }

    [Fact]
    public void T02_Identifier_Buffered_IO() {
        byte[] buffer = new byte[1024];
        foreach (var i in TT.X_960_Identifiers) {
            var l = i.Write(buffer, 0);
            var r = X690.Identifier.Read(buffer, 0);
            Assert.Equal(i, r);
            Assert.Equal(i.Length, l);
        }
    }

    [Fact]
    public void T03_LengthOctets_Stream_IO() {
        using (var s = new MemoryStream()) {
            for (var i = 0; i < 256; i++) {
                var x = new X690.LengthOctets(TT.PRNG.Next(-1, Int32.MaxValue - 1));
                s.Position = 0;
                var l = X690.LengthOctets.Write(x.Value, s);
                s.Position = 0;
                var y = X690.LengthOctets.Read(s);
                Assert.Equal(x, y);
                Assert.Equal(x.ReadLength, l);
            }
        }
    }

    [Fact]
    public void T04_LengthOctets_Buffered_IO() {
        var buffer = new byte[9];
        for (var i = 0; i < 256; i++) {
            var x = new X690.LengthOctets(TT.PRNG.Next(-1, Int32.MaxValue - 1));
            var l = X690.LengthOctets.Write(x.Value, buffer, 0);
            var y = X690.LengthOctets.Read(buffer, 0);
            Assert.Equal(x, y);
            Assert.Equal(x.ReadLength, l);
        }
    }

    [Fact]
    public void T05_Header_Stream_IO() {
        var identifiers = TT.X_960_Identifiers.ToArray().Shuffled();
        using (var s = new MemoryStream()) {
            foreach (var i in identifiers) {
                var x = new X690.Header(i, TT.PRNG.Next(-1, Int32.MaxValue - 1));
                s.Position = 0;
                x.Write(s);
                s.Position = 0;
                var y = X690.Header.Read(s);
                Assert.Equal(x, y);
            }
        }
    }

    [Fact]
    public void T06_Header_Buffered_IO() {
        var identifiers = TT.X_960_Identifiers.ToArray().Shuffled();
        var buffer = new byte[1024];
        foreach (var i in identifiers) {
            var x = new X690.Header(i, TT.PRNG.Next(-1, Int32.MaxValue - 1));
            x.Write(buffer, 0);
            var y = X690.Header.Read(buffer, 0);
            Assert.Equal(x, y);
        }
    }

    [Fact]
    public void T07_Header_Missing_Stream_Read() {
        using (var stream = new MemoryStream()) {
            var read = X690.Header.Read(stream);
            Assert.Null(read);
        }
    }

    [Fact]
    public void T08_Header_Missing_Buffered_Read() {
        var buffer = new byte[0];
        var isExceptionThrown = false;
        try {
            var read = X690.Header.Read(buffer, 0);
        }
        catch (IndexOutOfRangeException) {
            isExceptionThrown = true;
        }
        Assert.True(isExceptionThrown);
    }

    [Fact]
    public void T09_Header_Incomplete_Stream_Read() {
        using (var stream = new MemoryStream()) {
            stream.WriteByte(8);
            stream.WriteByte(8);
            var exceptionCount = 0;
            for (var i = 0; i < 3; i++) {
                stream.Position = i;
                try {
                    var header = X690.Header.Read(stream);
                }
                catch (InvalidDataException x) {
                    Console.WriteLine(x.Message);
                    exceptionCount++;
                }
            }
            Assert.Equal(1, exceptionCount);
        }
    }

    [Fact]
    public void T10_Header_Incomplete_Buffered_Read() {
        var buffer = new byte[] { 0, 0 };
        var exceptionCount = 0;
        for (var i = 0; i < 3; i++) {
            try {
                var header = X690.Header.Read(buffer, i);
            }
            catch (IndexOutOfRangeException x) {
                Console.WriteLine(x.Message);
                exceptionCount++;
            }
        }
        Assert.Equal(2, exceptionCount);
    }

    [Fact]
    public void T11_Header_EndOfContent_Stream_Read() {
        using (var stream = new MemoryStream()) {
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.Position = 0;
            var header = X690.Header.Read(stream);
            var length = header.Length;
            Assert.Equal(2, length);
            Assert.Equal(X690.UniversalType.EndOfContent, header.NodeType.TagNumber);
        }
    }

    [Fact]
    public void T12_Header_EndOfContent_Buffered_Read() {
        var buffer = new byte[] { 0, 0 };
        var header = X690.Header.Read(buffer, 0);
        var length = header.Length;
        Assert.Equal(2, length);
        Assert.Equal(X690.UniversalType.EndOfContent, header.NodeType.TagNumber);
    }

}