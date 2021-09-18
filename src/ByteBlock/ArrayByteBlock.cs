namespace ByteBlock;

using System;
using System.IO;
using System.Runtime.CompilerServices;

public sealed class ArrayByteBlock : IByteBlock {
    public const int MaxByteArraySize = 0x7FFFFFC7;

    private readonly byte[] _array;

    public ArrayByteBlock(int size = MaxByteArraySize) {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than 0");
        if (size > MaxByteArraySize)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be less than 2147483591");
        _array = new byte[size];
    }

    public int Length => _array.Length;

    public void Dispose() { } // Fully managed

    public Span<byte> AsSpan() => _array.AsSpan();

    public unsafe UnmanagedMemoryStream AsStream() 
        => new((byte*)Unsafe.AsPointer(ref _array[0]), 
            _array.Length, _array.Length, FileAccess.ReadWrite);
}
