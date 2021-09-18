namespace ByteBlock;

using System;
using System.IO;

public interface IByteBlock : IDisposable {
    int Length { get; }
    Span<byte> AsSpan();
    UnmanagedMemoryStream AsStream();
}
