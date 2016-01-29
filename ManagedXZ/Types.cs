using System;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    public enum lzma_ret
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

    enum lzma_action
    {
        LZMA_RUN = 0,
        LZMA_SYNC_FLUSH = 1,
        LZMA_FULL_FLUSH = 2,
        LZMA_FULL_BARRIER = 4,
        LZMA_FINISH = 3
    }

    enum lzma_check
    {
        LZMA_CHECK_NONE = 0,
        LZMA_CHECK_CRC32 = 1,
        LZMA_CHECK_CRC64 = 4,
        LZMA_CHECK_SHA256 = 10
    }

    [StructLayout(LayoutKind.Sequential)]
    public class lzma_stream
    {
        public IntPtr next_in;
        public UIntPtr avail_in;
        public UInt64 total_in;

        public IntPtr next_out;
        public UIntPtr avail_out;
        public UInt64 total_out;

        IntPtr allocator;
        IntPtr _internal;
        IntPtr reserved_ptr1;
        IntPtr reserved_ptr2;
        IntPtr reserved_ptr3;
        IntPtr reserved_ptr4;
        UInt64 reserved_int1;
        UInt64 reserved_int2;
        UIntPtr reserved_int3;
        UIntPtr reserved_int4;
        uint reserved_enum1;
        uint reserved_enum2;
    }

    [StructLayout(LayoutKind.Sequential)]
    class lzma_mt
    {
        UInt32 flags;
        UInt32 threads;
        UInt64 block_size;
        UInt32 timeout;
        UInt32 preset;
        IntPtr filters;
        lzma_check check;
        Int32 reserved_enum1;
        Int32 reserved_enum2;
        Int32 reserved_enum3;
        UInt32 reserved_int1;
        UInt32 reserved_int2;
        UInt32 reserved_int3;
        UInt32 reserved_int4;
        UInt64 reserved_int5;
        UInt64 reserved_int6;
        UInt64 reserved_int7;
        UInt64 reserved_int8;
        IntPtr reserved_ptr1;
        IntPtr reserved_ptr2;
        IntPtr reserved_ptr3;
        IntPtr reserved_ptr4;
    }
}