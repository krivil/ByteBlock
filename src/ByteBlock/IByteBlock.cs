namespace ByteBlock {
    using System;

    public interface IByteBlock : IDisposable {
        int Length { get; }
        Span<byte> GetSpan();
    }
}