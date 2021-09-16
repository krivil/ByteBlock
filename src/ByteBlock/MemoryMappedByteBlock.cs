namespace ByteBlock;

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;

public sealed unsafe class MemoryMappedByteBlock : IByteBlock {
    private const int _fileStreamBufferSize = 4096; // Default is 4096
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _memoryMappedViewAccessor;
    private readonly SafeMemoryMappedViewHandle _memoryMappedViewHandle;

    public readonly FileStream FileStream;

    private byte* _ptrMemMap;

    public MemoryMappedByteBlock(FileStream fileStream) {
        FileStream = fileStream; // ?? throw new ArgumentNullException(nameof(fileStream));

        if (!FileStream.CanRead) throw new ArgumentException("FileStream is not readable.", nameof(fileStream));
        if (!FileStream.CanWrite) throw new ArgumentException("FileStream is not writable.", nameof(fileStream));

        MemoryMappedFileAccess access =
                FileStream.CanWrite
                    ? MemoryMappedFileAccess.ReadWrite
                    : MemoryMappedFileAccess.Read;

        _memoryMappedFile = MemoryMappedFile.CreateFromFile(
            fileStream,
            null,
            fileStream.Length,
            access,
            HandleInheritability.None,
            false);

        _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor();
        _memoryMappedViewHandle = _memoryMappedViewAccessor.SafeMemoryMappedViewHandle;

        Length = _memoryMappedViewHandle.ByteLength > int.MaxValue
            ? int.MaxValue
            : (int)_memoryMappedViewHandle.ByteLength;

        _memoryMappedViewHandle.AcquirePointer(ref _ptrMemMap);
    }

    public int Length { get; private set; }

    public Span<byte> GetSpan() => new(_ptrMemMap, Length);

    public void Dispose() {
        Flush();

        ReleaseUnmanagedResources();

        _memoryMappedViewAccessor.Dispose();

        _memoryMappedFile.Dispose();
        FileStream.Dispose();

        _ptrMemMap = (byte*)IntPtr.Zero;
        Length = 0;

        GC.SuppressFinalize(this);
    }

    public static MemoryMappedByteBlock Create(string fileName,
        int size = int.MaxValue, int fileStreamBufferSize = _fileStreamBufferSize) {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "size cannot be 0 or less!");

        var fileStream = new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                fileStreamBufferSize,
                FileOptions.SequentialScan | FileOptions.WriteThrough);

        fileStream.SetLength(size);

        return new MemoryMappedByteBlock(fileStream);
    }

    public static MemoryMappedByteBlock OpenOrCreate(string fileName,
        int sizeIfCreating = int.MaxValue, int fileStreamBufferSize = _fileStreamBufferSize) {
        if (sizeIfCreating <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeIfCreating), "size cannot be 0 or less!");

        var fileStream = new FileStream(
                fileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                fileStreamBufferSize,
                FileOptions.SequentialScan | FileOptions.WriteThrough);

        if (fileStream.Length == 0) fileStream.SetLength(sizeIfCreating);

        return new MemoryMappedByteBlock(fileStream);
    }

    public static MemoryMappedByteBlock Open(string fileName,
        int fileStreamBufferSize = _fileStreamBufferSize) {
        var fileStream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read,
                fileStreamBufferSize,
                FileOptions.SequentialScan | FileOptions.WriteThrough);

        return new MemoryMappedByteBlock(fileStream);
    }

    public void Flush() {
        _memoryMappedViewAccessor.Flush();
        FileStream.Flush(true);
    }

    private void ReleaseUnmanagedResources() => _memoryMappedViewHandle.ReleasePointer();

    ~MemoryMappedByteBlock() => ReleaseUnmanagedResources();
}
