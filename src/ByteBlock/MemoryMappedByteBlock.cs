namespace ByteBlock;

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization;

public sealed unsafe class MemoryMappedByteBlock : IByteBlock {
    private const int _fileStreamBufferSize = 4096; // Default is 4096
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _memoryMappedViewAccessor;
    private readonly SafeMemoryMappedViewHandle _memoryMappedViewHandle;

    private byte* _ptrMemMap;

    public MemoryMappedByteBlock(MemoryMappedFile memoryMappedFile, bool openFirst2GbOnly = false) {
        _memoryMappedFile = memoryMappedFile ?? throw new ArgumentNullException(nameof(memoryMappedFile));

        _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor();
        _memoryMappedViewHandle = _memoryMappedViewAccessor.SafeMemoryMappedViewHandle;

        if (!openFirst2GbOnly && (_memoryMappedViewHandle.ByteLength > int.MaxValue)) {
            _memoryMappedViewHandle.Dispose();
            _memoryMappedViewAccessor.Dispose();
            throw new FileTooLongException();
        }

        Length = _memoryMappedViewHandle.ByteLength > int.MaxValue
            ? int.MaxValue
            : (int)_memoryMappedViewHandle.ByteLength;

        _memoryMappedViewHandle.AcquirePointer(ref _ptrMemMap);
    }

    internal MemoryMappedByteBlock(FileStream fileStream, bool openFirst2GbOnly = false, bool leaveStreamOpen = false) {
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
        if (!fileStream.CanRead) throw new ArgumentException("FileStream is not readable.", nameof(fileStream));
        if (!fileStream.CanWrite) throw new ArgumentException("FileStream is not writable.", nameof(fileStream));

        if (!openFirst2GbOnly && (fileStream.Length > int.MaxValue))
            throw new FileTooLongException();

        MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWrite;

        _memoryMappedFile = MemoryMappedFile.CreateFromFile(
            fileStream,
            null,
            0,
            access,
            HandleInheritability.None,
            leaveStreamOpen);

        _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor();
        _memoryMappedViewHandle = _memoryMappedViewAccessor.SafeMemoryMappedViewHandle;

        Length = _memoryMappedViewHandle.ByteLength > int.MaxValue
            ? int.MaxValue
            : (int)_memoryMappedViewHandle.ByteLength;

        _memoryMappedViewHandle.AcquirePointer(ref _ptrMemMap);
    }

    public int Length { get; private set; }

    public Span<byte> AsSpan() => new(_ptrMemMap, Length);

    public UnmanagedMemoryStream AsStream()
        => new(_ptrMemMap, Length, Length, FileAccess.ReadWrite);

    public void Dispose() {
        Flush();

        ReleaseUnmanagedResources();

        _memoryMappedViewAccessor.Dispose();

        _memoryMappedFile.Dispose();

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
        int fileStreamBufferSize = _fileStreamBufferSize,
        bool openFirst2GbOnly = false) {
        var fileStream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read,
                fileStreamBufferSize,
                FileOptions.SequentialScan | FileOptions.WriteThrough);

        return new MemoryMappedByteBlock(fileStream, openFirst2GbOnly);
    }

    public void Flush() => _memoryMappedViewAccessor.Flush();

    private void ReleaseUnmanagedResources() => _memoryMappedViewHandle.ReleasePointer();

    ~MemoryMappedByteBlock() => ReleaseUnmanagedResources();

    [Serializable]
    private class FileTooLongException : Exception {
        public FileTooLongException()
            : this("Cannot open files larger than int.Max") {
        }

        public FileTooLongException(string? message) : base(message) {
        }

        public FileTooLongException(string? message, Exception? innerException) : base(message, innerException) {
        }

        protected FileTooLongException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
