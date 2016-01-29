using System;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    internal enum lzma_ret
    {
        LZMA_OK = 0,
        LZMA_STREAM_END = 1,
        LZMA_NO_CHECK = 2,
        LZMA_UNSUPPORTED_CHECK = 3,
        LZMA_GET_CHECK = 4,
        LZMA_MEM_ERROR = 5,
        LZMA_MEMLIMIT_ERROR = 6,
        LZMA_FORMAT_ERROR = 7,
        LZMA_OPTIONS_ERROR = 8,
        LZMA_DATA_ERROR = 9,
        LZMA_BUF_ERROR = 10,
        LZMA_PROG_ERROR = 11,
    }

    internal enum lzma_action
    {
        LZMA_RUN = 0,
        LZMA_SYNC_FLUSH = 1,
        LZMA_FULL_FLUSH = 2,
        LZMA_FULL_BARRIER = 4,
        LZMA_FINISH = 3
    }

    internal enum lzma_check
    {
        LZMA_CHECK_NONE = 0,
        LZMA_CHECK_CRC32 = 1,
        LZMA_CHECK_CRC64 = 4,
        LZMA_CHECK_SHA256 = 10
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class lzma_stream
    {
        public IntPtr next_in;
        public UIntPtr avail_in;
        public UInt64 total_in;

        public IntPtr next_out;
        public UIntPtr avail_out;
        public UInt64 total_out;

        private IntPtr allocator;
        private IntPtr _internal;
        private IntPtr reserved_ptr1;
        private IntPtr reserved_ptr2;
        private IntPtr reserved_ptr3;
        private IntPtr reserved_ptr4;
        private UInt64 reserved_int1;
        private UInt64 reserved_int2;
        private UIntPtr reserved_int3;
        private UIntPtr reserved_int4;
        private uint reserved_enum1;
        private uint reserved_enum2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class lzma_mt
    {
        private UInt32 flags;
        public UInt32 threads;
        private UInt64 block_size;
        private UInt32 timeout;
        public UInt32 preset;
        private IntPtr filters;
        public lzma_check check;
        private Int32 reserved_enum1;
        private Int32 reserved_enum2;
        private Int32 reserved_enum3;
        private UInt32 reserved_int1;
        private UInt32 reserved_int2;
        private UInt32 reserved_int3;
        private UInt32 reserved_int4;
        private UInt64 reserved_int5;
        private UInt64 reserved_int6;
        private UInt64 reserved_int7;
        private UInt64 reserved_int8;
        private IntPtr reserved_ptr1;
        private IntPtr reserved_ptr2;
        private IntPtr reserved_ptr3;
        private IntPtr reserved_ptr4;
    }
}