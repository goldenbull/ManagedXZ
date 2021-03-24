using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ManagedXZ
{
    internal static class Native
    {
        private static readonly IntPtr _handle;
        public static bool Is64Bit;

        static Native()
        {
            // check 32bit or 64bit
            PortableExecutableKinds peKinds;
            ImageFileMachine arch;
            typeof(object).Module.GetPEKind(out peKinds, out arch);
            string dllFilename;
            if (arch == ImageFileMachine.AMD64)
            {
                dllFilename = "liblzma_amd64.dll";
                Is64Bit = true;
            }
            else if (arch == ImageFileMachine.I386)
            {
                dllFilename = "liblzma_x86.dll";
                Is64Bit = false;
            }
            else
                throw new Exception(arch + " is not supported yet");

            // try load library. todo: embed native dlls as resource?
            string dllPath = GetDllPath(Assembly.GetExecutingAssembly().Location, dllFilename);
            if (dllPath == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                    dllPath = GetDllPath(assembly.Location, dllFilename);
            }

            if (dllPath == null)
                dllPath = GetDllPath(AppDomain.CurrentDomain.BaseDirectory, dllFilename);

            if (dllPath == null)
                throw new Exception("Can not find " + dllFilename);

            _handle = LoadLibrary(dllPath);
            if (_handle == IntPtr.Zero)
                throw new Exception("Can not load " + dllFilename);

            // get function pointers
            lzma_code = GetFunction<lzma_code_delegate>("lzma_code");
            lzma_end = GetFunction<lzma_end_delegate>("lzma_end");
            lzma_get_progress = GetFunction<lzma_get_progress_delegate>("lzma_get_progress");
            lzma_easy_encoder = GetFunction<lzma_easy_encoder_delegate>("lzma_easy_encoder");
            lzma_stream_encoder_mt = GetFunction<lzma_stream_encoder_mt_delegate>("lzma_stream_encoder_mt");
            lzma_auto_decoder = GetFunction<lzma_auto_decoder_delegate>("lzma_auto_decoder");
        }

        private static string GetDllPath(string path, string fileName)
        {
            if (File.Exists(path))
                path = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(path))
                path = fileName;
            else
                path = Path.Combine(path, fileName);

            if (File.Exists(path))
                return path;

            return null;
        }

        public static void CheckSize()
        {
            Console.WriteLine($"sizeof(lzma_stream)={Marshal.SizeOf(typeof(lzma_stream))}");
            Console.WriteLine($"sizeof(lzma_mt)={Marshal.SizeOf(typeof(lzma_mt))}");
        }

        #region dll helpers

        private static T GetFunction<T>(string fname)
        {
            var ptr = GetProcAddress(_handle, fname);
            if (ptr == IntPtr.Zero) throw new Exception("GetProcAddress returns nullptr for " + fname);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr moduleHandle, string procname);

        #endregion

        #region xz functions

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate lzma_ret lzma_code_delegate(lzma_stream strm, lzma_action action);

        internal static lzma_code_delegate lzma_code;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_end_delegate(lzma_stream strm);

        internal static lzma_end_delegate lzma_end;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void lzma_get_progress_delegate(lzma_stream strm, out UInt64 progress_in, out UInt64 progress_out);

        internal static lzma_get_progress_delegate lzma_get_progress;

        //extern LZMA_API(uint64_t) lzma_easy_decoder_memusage(uint32_t preset);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate lzma_ret lzma_easy_encoder_delegate(lzma_stream strm, UInt32 preset, lzma_check check);

        internal static lzma_easy_encoder_delegate lzma_easy_encoder;

        //extern LZMA_API(lzma_ret) lzma_easy_buffer_encode(uint32_t preset, lzma_check check,const lzma_allocator* allocator,const uint8_t*in, size_t in_size,uint8_t*out, size_t* out_pos, size_t out_size) lzma_nothrow;
        //extern LZMA_API(lzma_ret) lzma_stream_encoder(lzma_stream* strm,const lzma_filter* filters, lzma_check check)
        //extern LZMA_API(uint64_t) lzma_stream_encoder_mt_memusage(const lzma_mt* options) lzma_nothrow lzma_attr_pure;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate lzma_ret lzma_stream_encoder_mt_delegate(lzma_stream strm, lzma_mt options);

        internal static lzma_stream_encoder_mt_delegate lzma_stream_encoder_mt;

        //extern LZMA_API(lzma_ret) lzma_alone_encoder(lzma_stream* strm, const lzma_options_lzma* options)
        //extern LZMA_API(size_t) lzma_stream_buffer_bound(size_t uncompressed_size)
        //extern LZMA_API(lzma_ret) lzma_stream_buffer_encode(lzma_filter* filters, lzma_check check,const lzma_allocator* allocator,const uint8_t*in, size_t in_size,uint8_t*out, size_t* out_pos, size_t out_size)


        internal const UInt32 LZMA_TELL_NO_CHECK = 0x01;
        internal const UInt32 LZMA_TELL_UNSUPPORTED_CHECK = 0x02;
        internal const UInt32 LZMA_TELL_ANY_CHECK = 0x04;
        internal const UInt32 LZMA_IGNORE_CHECK = 0x10;
        internal const UInt32 LZMA_CONCATENATED = 0x08;
        //extern LZMA_API(lzma_ret) lzma_stream_decoder(lzma_stream* strm, uint64_t memlimit, uint32_t flags)

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate lzma_ret lzma_auto_decoder_delegate(lzma_stream strm, UInt64 memlimit, UInt32 flags);

        internal static lzma_auto_decoder_delegate lzma_auto_decoder;

        //extern LZMA_API(lzma_ret) lzma_alone_decoder(lzma_stream* strm, uint64_t memlimit)
        //extern LZMA_API(lzma_ret) lzma_stream_buffer_decode(uint64_t* memlimit, uint32_t flags,const lzma_allocator* allocator,const uint8_t*in, size_t *in_pos, size_t in_size,uint8_t *out, size_t *out_pos, size_t out_size)

        #endregion
    }
}