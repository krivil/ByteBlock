# ByteBlock

Different buffers of bytes. Array, unmanaged heap and memory mapped file implementations.

## Implementations

All of them implement `IByteBlock` interface:

```csharp
public interface IByteBlock : IDisposable {
    int Length { get; }
    Span<byte> AsSpan();
    UnmanagedMemoryStream AsStream();
}
```

Each block provide read/write span or stream.

> Each call of `AsSpan()` or `AsStream()` produces new one.

> NOTE: Due to `Span<T>` `Length` property being `int`, maximum size of any `IByteBlock` is `int.MaxValue` : `2 147 483 647 (0x7fffffff)`.

### ArrayByteBlock

Backed by array of bytes.

> Maximum length of `byte[]` in C# is `2 147 483 591 (0x7FFFFFC7)`

### HeapByteBlock

Buffer allocated in heap.

### MemoryMappedByteBlock

Buffer persisted on file system.

Can be used for fast writing or reading of files smaller than `int.MaxValue` (2GiB - 1 byte).