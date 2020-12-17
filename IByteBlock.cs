using System;

namespace ByteBlock
{
    public interface IByteBlock : IDisposable
    {
        int Length { get; }
        Span<byte> GetSpan();
    }
}