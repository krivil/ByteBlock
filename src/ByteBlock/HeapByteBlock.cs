namespace ByteBlock {
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public sealed unsafe class HeapByteBlock : IByteBlock {
        private IntPtr _ptrMem;

        public HeapByteBlock(int size = int.MaxValue, bool clean = true) {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than 0");

            Length = size;
            _ptrMem = Marshal.AllocHGlobal(size);
            GC.AddMemoryPressure(size);

            if (clean) Unsafe.InitBlock(_ptrMem.ToPointer(), 0, (uint)size);
        }

        public int Length { get; private set; }

        public Span<byte> GetSpan() => new(_ptrMem.ToPointer(), Length);

        public void Dispose() {
            ReleaseUnmanagedResources();

            _ptrMem = IntPtr.Zero;
            Length = 0;

            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources() => Marshal.FreeHGlobal(_ptrMem);

        ~HeapByteBlock() => ReleaseUnmanagedResources();
    }
}